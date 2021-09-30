﻿using ConnectorGSA.Models;
using ConnectorGSA.Utilities;
using Microsoft.Win32;
using Newtonsoft.Json;
using Speckle.ConnectorGSA.Proxy;
using Speckle.ConnectorGSA.Proxy.Cache;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConnectorGSA
{
  public static class Commands
  {
    public static object Assert { get; private set; }

    public static async Task<bool> InitialLoad(TabCoordinator coordinator, IProgress<MessageEventArgs> loggingProgress)
    {
      coordinator.Init();
      try
      {
        //This will throw an exception if there is no default account
        var account = AccountManager.GetDefaultAccount();
        if (account == null)
        {
          return false;
        }
        ((GsaModel)Instance.GsaModel).Account = account;
        return await CompleteLogin(coordinator, new SpeckleAccountForUI(), loggingProgress);
      }
      catch
      {
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "No default account found - press the Login button to login/select an account"));
        return false;
      }
    }

    public static async Task<bool> CompleteLogin(TabCoordinator coordinator, SpeckleAccountForUI accountCandidate, IProgress<MessageEventArgs> loggingProgress)
    {
      var messenger = new ProgressMessenger(loggingProgress);

      if (accountCandidate != null && accountCandidate.IsValid)
      {
        var streamsForAccount = new List<Stream>();
        var client = new Client(((GsaModel)Instance.GsaModel).Account);
        try
        {
          streamsForAccount = await client.StreamsGet(50);  //Undocumented limitation servers cannot seem to return more than 50 items
        }
        catch (Exception ex)
        {
          loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Error, "Unable to get stream list"));
        }
        

        coordinator.Account = accountCandidate;
        coordinator.ServerStreamList.StreamListItems.Clear();

        foreach (var sd in streamsForAccount)
        {
          coordinator.ServerStreamList.StreamListItems.Add(new StreamListItem(sd.id, sd.name));
        }

        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Logged into account at: " + coordinator.Account.ServerUrl));
        return true;
      }
      else
      {
        return false;
      }
    }

    public static bool OpenFile(TabCoordinator coordinator, IProgress<MessageEventArgs> loggingProgress)
    {
      var openFileDialog = new OpenFileDialog();
      if (openFileDialog.ShowDialog() == true)
      {
        try
        {
          Commands.OpenFile(openFileDialog.FileName, true);
        }
        catch (Exception ex)
        {
          loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Error, "Unable to load " + openFileDialog.FileName + " - refer to logs for more information"));
          loggingProgress.Report(new MessageEventArgs(MessageIntent.TechnicalLog, MessageLevel.Error, ex, "Unable to load file"));
          return false;
        }
        if (!string.IsNullOrEmpty(openFileDialog.FileName))
        {
          coordinator.FilePath = openFileDialog.FileName;
        }

        coordinator.FileStatus = GsaLoadedFileType.ExistingFile;
        return true;
      }
      else
      {
        return false;
      }
    }

    public static bool OpenFile(string filePath, bool visible)
    {
      Instance.GsaModel.Proxy = new Speckle.ConnectorGSA.Proxy.GsaProxy(); //Use a real proxy
      var opened = Instance.GsaModel.Proxy.OpenFile(filePath, visible);
      if (!opened)
      {

        return false;
      }
      return true;
    }

    public static bool ExtractSavedReceptionStreamInfo(bool? receive, bool? send, out List<StreamState> streamStates)
    {
      var sid = Instance.GsaModel.Proxy.GetTopLevelSid();
      List<StreamState> allSaved;
      try
      {
        allSaved = JsonConvert.DeserializeObject<List<StreamState>>(sid);
      }
      catch
      {
        allSaved = new List<StreamState>();
      }
      var userId = ((GsaModel)Instance.GsaModel).Account.userInfo.id;
      var restApi = ((GsaModel)Instance.GsaModel).Account.serverInfo.url;

      //So currently it assumes that a new user for this file will have a new stream created for them, even if other users saved this file with their stream info
      streamStates = allSaved.Where(ss => ((ss.UserId == userId) && ss.ServerUrl.Equals(restApi, StringComparison.InvariantCultureIgnoreCase))).ToList();
      if (receive.HasValue)
      {
        streamStates = streamStates.Where(ss => ss.IsReceiving == receive.Value).ToList();
      }
      if (send.HasValue)
      {
        streamStates = streamStates.Where(ss => ss.IsSending == send.Value).ToList();
      }
      return (streamStates != null && streamStates.Count > 0);
    }

    public static bool UpsertSavedReceptionStreamInfo(bool? receive, bool? send, params StreamState[] streamStates)
    {
      var sid = Instance.GsaModel.Proxy.GetTopLevelSid();
      List<StreamState> allSs = null;
      try
      {
        allSs = JsonConvert.DeserializeObject<List<StreamState>>(sid);
      }
      catch (JsonException ex)
      {
        //Could not deserialise, probably because it has a v1-format of stream information.  In this case, ignore the info

        //TO DO: write technical long line here
      }

      if (allSs == null || allSs.Count() == 0)
      {
        allSs = streamStates.ToList();
      }
      else
      {
        var merged = new List<StreamState>();
        foreach (var ss in streamStates)
        {
          var matching = allSs.FirstOrDefault(s => s.Equals(ss));
          if (matching != null)
          {
            if (matching.IsReceiving != ss.IsReceiving)
            {
              matching.IsReceiving = true;  //This is merging of two booleans, where a true value is to be set if any are true
            }
            if (matching.IsSending != ss.IsSending)
            {
              matching.IsSending = true;  //This is merging of two booleans, where a true value is to be set if any are true
            }
            merged.Add(ss);
          }
        }

        allSs = allSs.Union(streamStates.Except(merged)).ToList();
      }

      var newSid = JsonConvert.SerializeObject(allSs);
      return Instance.GsaModel.Proxy.SetTopLevelSid(newSid);
    }

    public static bool CloseFile(string filePath, bool visible)
    {
      Instance.GsaModel.Proxy.Close();
      return Instance.GsaModel.Proxy.Clear();
    }

    public static bool LoadDataFromFile(IEnumerable<ResultGroup> resultGroups = null, IEnumerable<ResultType> resultTypes = null)
    {
      var loadedCache = UpdateCache();
      int cumulativeErrorRows = 0;

      if (resultGroups != null && resultGroups.Any() && resultTypes != null && resultTypes.Any())
      {
        if (!Instance.GsaModel.Proxy.PrepareResults(resultTypes, Instance.GsaModel.Result1DNumPosition + 2))
        {
          return false;
        }
        foreach (var g in resultGroups)
        {
          if (!Instance.GsaModel.Proxy.LoadResults(g, out int numErrorRows) || numErrorRows > 0)
          {
            return false;
          }
          cumulativeErrorRows += numErrorRows;
        }
      }

      return (loadedCache && (cumulativeErrorRows == 0));
    }

    public static bool ConvertToNative(ISpeckleConverter converter) //Includes writing to Cache
    {
      //With the attached objects in speckle objects, there is no type dependency needed on the receive side, so just convert each object

      if (Instance.GsaModel.Cache.GetSpeckleObjects(out var speckleObjects))
      {
        foreach (var so in speckleObjects.Cast<Base>())
        {
          try
          {
            if (converter.CanConvertToNative(so))
            {
              var nativeObjects = converter.ConvertToNative(new List<Base> { so }).Cast<GsaRecord>().ToList();
              var appId = string.IsNullOrEmpty(so.applicationId) ? so.id : so.applicationId;
              Instance.GsaModel.Cache.SetNatives(so.GetType(), appId, nativeObjects);
            }
          }
          catch (Exception ex)
          {

          }
        } 
      }

      return true;
    }

    public static List<Base> ConvertToSpeckle(ISpeckleConverter converter)
    {
      if (!Instance.GsaModel.Cache.GetNatives(out List<GsaRecord> gsaRecords))
      {
        return null;
      }

      return converter.ConvertToSpeckle(gsaRecords.Cast<object>().ToList());
    }

    public static async Task<bool> Send(Base commitObj, StreamState state, params ITransport[] transports)
    {
      var commitObjId = await Operations.Send(
        @object: commitObj,
        transports: transports.ToList(),
        onErrorAction: (s, e) =>
        {
          state.Errors.Add(e);
        },
        disposeTransports: true
        );

      if (transports.Any(t => t is ServerTransport))
      {
        var actualCommit = new CommitCreateInput
        {
          streamId = state.Stream.id,
          objectId = commitObjId,
          branchName = "main",
          message = "Pushed it real good",
          sourceApplication = Applications.GSA
        };

        //if (state.PreviousCommitId != null) { actualCommit.parents = new List<string>() { state.PreviousCommitId }; }

        try
        {
          var commitId = await state.Client.CommitCreate(actualCommit);
        }
        catch (Exception e)
        {
          state.Errors.Add(e);
        }
      }

      return (state.Errors.Count == 0);
    }

    internal static async Task<bool> Receive(TabCoordinator coordinator, IProgress<StreamState> streamCreationProgress, IProgress<MessageEventArgs> loggingProgress, IProgress<string> statusProgress, IProgress<double> percentageProgress)
    {
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(Applications.GSA);
      var percentage = 0;

      Instance.GsaModel.StreamLayer = coordinator.ReceiverTab.TargetLayer;
      Instance.GsaModel.Units = UnitEnumToString(coordinator.ReceiverTab.CoincidentNodeUnits);
      Instance.GsaModel.CoincidentNodeAllowance = coordinator.ReceiverTab.CoincidentNodeAllowance;
      Instance.GsaModel.LoggingMinimumLevel = (int)coordinator.LoggingMinimumLevel;
      var perecentageProgressLock = new object();

      var account = ((GsaModel)Instance.GsaModel).Account;
      var client = new Client(account);

      var startTime = DateTime.Now;

      statusProgress.Report("Reading GSA data into cache");
      //Load data to cause merging
      Commands.LoadDataFromFile(); //Ensure all nodes

      percentage = 10;
      percentageProgress.Report(percentage);

      TimeSpan duration = DateTime.Now - startTime;
      if (duration.Seconds > 0)
      {
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Loaded data into cache"));
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Duration of reading GSA model into cache: " + duration.ToString(@"hh\:mm\:ss")));
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Telemetry, MessageLevel.Information, "receive", "update-cache", "duration", duration.ToString(@"hh\:mm\:ss")));
      }
      startTime = DateTime.Now;


      statusProgress.Report("Accessing streams");
      var streamIds = coordinator.ReceiverTab.StreamList.StreamListItems.Select(i => i.StreamId).ToList();
      var receiveTasks = new List<Task>();
      foreach (var streamId in streamIds)
      {
        var streamState = new StreamState(account.userInfo.id, account.serverInfo.url) { Stream = new Stream() { id = streamId } };
        var transport = new ServerTransport(streamState.Client.Account, streamState.Stream.id);
        receiveTasks.Add(
          streamState.RefreshStream().ContinueWith(async (refreshed) =>
            {
              if (refreshed.Result)
              {
                streamState.Stream.branch = client.StreamGetBranches(streamId, 1).Result.First();
                var commitId = streamState.Stream.branch.commits.items.FirstOrDefault().referencedObject;

                var received = await Commands.Receive(commitId, streamState, transport, converter.CanConvertToNative);
                if (received)
                {
                  loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Received data from " + streamId + " stream"));
                }

                if (streamState.Errors != null && streamState.Errors.Count > 0)
                {
                  foreach (var se in streamState.Errors)
                  {
                    loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Error, se.Message));
                    loggingProgress.Report(new MessageEventArgs(MessageIntent.TechnicalLog, MessageLevel.Error, se, se.Message));
                  }
                }

                lock (perecentageProgressLock)
                {
                  percentage += (50 / streamIds.Count);
                  percentageProgress.Report(percentage);
                }
              }
            }));
      }
      await Task.WhenAll(receiveTasks.ToArray());

      duration = DateTime.Now - startTime;
      loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Duration of reception from Speckle and scaling: " + duration.ToString(@"hh\:mm\:ss")));
      loggingProgress.Report(new MessageEventArgs(MessageIntent.Telemetry, MessageLevel.Information, "receive", "reception and scaling", "duration", duration.ToString(@"hh\:mm\:ss")));

      startTime = DateTime.Now;

      statusProgress.Report("Converting");
      var numToConvert = ((GsaCache)Instance.GsaModel.Cache).NumSpeckleObjects;
      int numConverted = 0;
      int totalConversionPercentage = 90 - percentage;
      Instance.GsaModel.ConversionProgress = new Progress<bool>((bool success) =>
      {
        lock (perecentageProgressLock)
        {
          numConverted++;
        }
        percentageProgress.Report(percentage + Math.Round(((double)numConverted / (double)numToConvert) * totalConversionPercentage, 0));
      });

      Commands.ConvertToNative(converter);

      if (converter.ConversionErrors != null && converter.ConversionErrors.Count > 0)
      {
        foreach (var ce in converter.ConversionErrors)
        {
          loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Error, ce.Message));
          loggingProgress.Report(new MessageEventArgs(MessageIntent.TechnicalLog, MessageLevel.Error, ce, ce.Message));
        }
      }

      loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Converted Speckle to GSA objects"));

      duration = DateTime.Now - startTime;
      if (duration.Seconds > 0)
      {
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Duration of conversion from Speckle: " + duration.ToString(@"hh\:mm\:ss")));
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Telemetry, MessageLevel.Information, "receive", "conversion", "duration", duration.ToString(@"hh\:mm\:ss")));
      }
      startTime = DateTime.Now;

      //The cache is filled with natives
      if (Instance.GsaModel.Cache.GetNatives(out var gsaRecords))
      {
        Instance.GsaModel.Proxy.WriteModel(gsaRecords, Instance.GsaModel.StreamLayer);
      }

      percentageProgress.Report(100);

      ((GsaProxy)Instance.GsaModel.Proxy).UpdateViews();

      duration = DateTime.Now - startTime;
      if (duration.Seconds > 0)
      {
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Duration of writing converted objects to GSA: " + duration.ToString(@"hh\:mm\:ss")));
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Telemetry, MessageLevel.Information, "receive", "write-model", "duration", duration.ToString(@"hh\:mm\:ss")));
      }

      statusProgress.Report("Ready");
      Console.WriteLine("Receiving complete");

      return true;
    }

    public static async Task<bool> Receive(string commitId, StreamState state, ITransport transport, Func<Base, bool> IsSingleObjectFn)
    {
      var commitObject = await Operations.Receive(
          commitId,
          transport,
          onErrorAction: (s, e) =>
          {
            state.Errors.Add(e);
          },
          disposeTransports: true
          );

      if (commitObject != null)
      {
        var receivedObjects = FlattenCommitObject(commitObject, IsSingleObjectFn);

        return (Instance.GsaModel.Cache.Upsert(receivedObjects.ToDictionary(
            ro => string.IsNullOrEmpty(ro.applicationId) ? ro.id : ro.applicationId, 
            ro => (object)ro))
          && receivedObjects != null && receivedObjects.Any() && state.Errors.Count == 0);
      }
      return false;
    }

    private static bool UpdateCache(bool onlyNodesWithApplicationIds = true)
    {
      var errored = new Dictionary<int, GsaRecord>();

      try
      {
        if (Instance.GsaModel.Proxy.GetGwaData(out var records))
        {
          for (int i = 0; i < records.Count(); i++)
          {
            if (!Instance.GsaModel.Cache.Upsert(records[i]))
            {
              errored.Add(i, records[i]);
            }
          }
        }
        return true;
      }
      catch
      {
        return false;
      }
    }

    private static List<Base> FlattenCommitObject(object obj, Func<Base, bool> IsSingleObjectFn)
    {
      //This is needed because with GSA models, there could be a design and analysis layer with objects appearing in both, so only include the first
      //occurrence of each object (distinguished by the ID returned by the Base.GetId() method) in the list returned
      var uniques = new Dictionary<Type, HashSet<string>>();
      return FlattenCommitObject(obj, IsSingleObjectFn, uniques);
    }


    private static List<Base> FlattenCommitObject(object obj, Func<Base, bool> IsSingleObjectFn, Dictionary<Type, HashSet<string>> uniques)
    {
      List<Base> objects = new List<Base>();

      if (obj is Base @base)
      {
        if (IsSingleObjectFn(@base))
        {
          var t = obj.GetType();
          var id = @base.GetId();
          if (!uniques.ContainsKey(t))
          {
            uniques.Add(t, new HashSet<string>() { @base.GetId() });
          }
          if (!uniques[t].Contains(id))
          {
            objects.Add(@base);
            uniques[t].Add(id);
          }

          return objects;
        }
        else
        {
          foreach (var prop in @base.GetDynamicMembers())
          {
            objects.AddRange(FlattenCommitObject(@base[prop], IsSingleObjectFn, uniques));
          }
          foreach (var kvp in @base.GetMembers())
          {
            var prop = kvp.Key;
            objects.AddRange(FlattenCommitObject(@base[prop], IsSingleObjectFn, uniques));
          }
          return objects;
        }
      }

      if (obj is List<object> list)
      {
        foreach (var listObj in list)
        {
          objects.AddRange(FlattenCommitObject(listObj, IsSingleObjectFn, uniques));
        }
        return objects;
      }
      else if (obj is List<Base> baseObjList)
      {
        foreach (var baseObj in baseObjList)
        {
          objects.AddRange(FlattenCommitObject(baseObj, IsSingleObjectFn, uniques));
        }
        return objects;
      }
      else if (obj is IDictionary dict)
      {
        foreach (DictionaryEntry kvp in dict)
        {
          objects.AddRange(FlattenCommitObject(kvp.Value, IsSingleObjectFn, uniques));
        }
        return objects;
      }

      return objects;
    }

    internal static async Task<List<StreamState>> GetStreamList(TabCoordinator coordinator, SpeckleAccountForUI account, Progress<MessageEventArgs> loggingProgress)
    {
      return new List<StreamState>();
    }

    internal static bool NewFile(TabCoordinator coordinator, IProgress<MessageEventArgs> loggingProgress)
    {
      Instance.GsaModel.Proxy.NewFile(true);

      coordinator.ReceiverTab.ReceiverSidRecords.Clear();
      coordinator.SenderTab.SenderSidRecords.Clear();

      loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Created new file."));

      return true;
    }

    public static async Task<bool> ReadSavedStreamInfo(TabCoordinator coordinator, IProgress<MessageEventArgs> loggingProgress)
    {
      if (coordinator.FileStatus == GsaLoadedFileType.ExistingFile && coordinator.Account != null && coordinator.Account.IsValid)
      {
        var retrieved = ExtractSavedReceptionStreamInfo(true, true, out List<StreamState> steamStates);
        coordinator.ReceiverTab.ReceiverSidRecords.Clear();
        coordinator.SenderTab.SenderSidRecords.Clear();
        
        if (retrieved)
        {
          if (coordinator.ReceiverTab.ReceiverSidRecords.Count() > 0)
          {
            var messenger = new ProgressMessenger(loggingProgress);

            var invalidSidRecords = new List<StreamState>();
            //Since the buckets are stored in the SID tags, but not the stream names, get the stream names
            foreach (var r in coordinator.ReceiverTab.ReceiverSidRecords)
            {
              if (!(await r.RefreshStream()))
              {
                invalidSidRecords.Add(r);
              }  
              /*
              var basicStreamData = await SpeckleInterface.SpeckleStreamManager.GetStream(coordinator.Account.ServerUrl, coordinator.Account.Token,
                r.StreamId, messenger);

              if (basicStreamData == null)
              {
                invalidSidRecords.Add(r);
              }
              else if (!string.IsNullOrEmpty(basicStreamData.Name))
              {
                r.SetName(basicStreamData.Name);
              }
              */
            }
            invalidSidRecords.ForEach(r => coordinator.ReceiverTab.RemoveSidSpeckleRecord(r));
            coordinator.ReceiverTab.SidRecordsToStreamList();

            loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Found streams from the same server stored in file for receiving: "
               + string.Join(", ", coordinator.ReceiverTab.ReceiverSidRecords.Select(r => r.StreamId))));
          }
          if (coordinator.SenderTab.SenderSidRecords.Count() > 0)
          {
            var messenger = new ProgressMessenger(loggingProgress);

            var invalidSidRecords = new List<StreamState>();
            //Since the buckets are stored in the SID tags, but not the stream names, get the stream names
            foreach (var r in coordinator.SenderTab.SenderSidRecords)
            {
              if (!(await r.RefreshStream()))
              {
                invalidSidRecords.Add(r);
              }
              /*
              var basicStreamData = await SpeckleInterface.SpeckleStreamManager.GetStream(coordinator.Account.ServerUrl, coordinator.Account.Token,
                r.StreamId, messenger);
              if (basicStreamData == null)
              {
                invalidSidRecords.Add(r);
              }
              else if (!string.IsNullOrEmpty(basicStreamData.Name))
              {
                r.SetName(basicStreamData.Name);
              }
              */
            }
            invalidSidRecords.ForEach(r => coordinator.SenderTab.RemoveSidSpeckleRecord(r));
            coordinator.SenderTab.SidRecordsToStreamList();

            loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Found streams from the same server stored in file for sending: "
               + string.Join(", ", coordinator.SenderTab.SenderSidRecords.Select(r => r.StreamId))));
          }
        }
        return retrieved;
      }
      return true;
    }

    internal static async Task<bool> SaveFile(TabCoordinator coordinator)
    {
      return true;
    }

    internal static async Task<bool> RenameStream(TabCoordinator coordinator, string streamId, string newStreamName, Progress<MessageEventArgs> loggingProgress)
    {
      return true;
    }

    internal static async Task<bool> CloneStream(TabCoordinator coordinator, string streamId, Progress<MessageEventArgs> loggingProgress)
    {
      return true;
    }

    internal static async Task<bool> SendTriggered(object gsaSenderCoordinator)
    {
      return true;
    }

    internal static async Task<bool> SendInitial(TabCoordinator coordinator, IProgress<StreamState> streamCreationProgress, IProgress<StreamState> streamDeletionProgress, 
      IProgress<MessageEventArgs> loggingProgress, IProgress<string> statusProgress, IProgress<double> percentageProgress)
    {
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(Applications.GSA);
      var percentage = 0;

      Instance.GsaModel.StreamLayer = coordinator.SenderTab.TargetLayer;
      Instance.GsaModel.StreamSendConfig = coordinator.SenderTab.StreamContentConfig;
      Instance.GsaModel.Result1DNumPosition = coordinator.SenderTab.AdditionalPositionsFor1dElements; //end points (2) plus additional
      Instance.GsaModel.LoggingMinimumLevel = (int)coordinator.LoggingMinimumLevel;
      var perecentageProgressLock = new object();

      var account = ((GsaModel)Instance.GsaModel).Account;
      var client = new Client(account);

      var startTime = DateTime.Now;

      statusProgress.Report("Preparing cache");
      Commands.LoadDataFromFile(); //Ensure all nodes
      loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Loaded data from file into cache"));

      percentage += 20;
      percentageProgress.Report(percentage);

      TimeSpan duration = DateTime.Now - startTime;
      if (duration.Seconds > 0)
      {
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Duration of reading GSA model into cache: " + duration.ToString(@"hh\:mm\:ss")));
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Telemetry, MessageLevel.Information, "send", "update-cache", "duration", duration.ToString(@"hh\:mm\:ss")));
      }
      startTime = DateTime.Now;

      var resultsToSend = coordinator.SenderTab.ResultSettings.ResultSettingItems.Where(rsi => rsi.Selected).ToList();
      if (resultsToSend != null && resultsToSend.Count() > 0 && !string.IsNullOrEmpty(coordinator.SenderTab.LoadCaseList))
      {
        statusProgress.Report("Preparing results");
        var analIndices = new List<int>();
        var comboIndices = new List<int>();
        if (((GsaCache)Instance.GsaModel.Cache).GetNatives<GsaAnal>(out var analRecords) && analRecords != null && analRecords.Count() > 0)
        {
          analIndices.AddRange(analRecords.Select(r => r.Index.Value));
        }
        if (((GsaCache)Instance.GsaModel.Cache).GetNatives<GsaAnal>(out var comboRecords) && comboRecords != null && comboRecords.Count() > 0)
        {
          comboIndices.AddRange(comboRecords.Select(r => r.Index.Value));
        }
        var expanded = ((GsaProxy)Instance.GsaModel.Proxy).ExpandLoadCasesAndCombinations(coordinator.SenderTab.LoadCaseList, analIndices, comboIndices);
        if (expanded != null && expanded.Count() > 0)
        {
          percentage += 2;
          percentageProgress.Report(percentage);

          loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Resolved load cases"));

          Instance.GsaModel.ResultCases = expanded;
          Instance.GsaModel.ResultTypes = resultsToSend.Select(rts => rts.ResultType).ToList();

        }
      }

      if (Instance.GsaModel.SendResults)
      {
        Instance.GsaModel.Proxy.PrepareResults(Instance.GsaModel.ResultTypes);
        foreach (var rg in Instance.GsaModel.ResultGroups)
        {
          Instance.GsaModel.Proxy.LoadResults(rg, out int numErrorRows);
        }

        percentage += 20;
        percentageProgress.Report(percentage);

        duration = DateTime.Now - startTime;
        if (duration.Seconds > 0)
        {
          loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Duration of preparing results: " + duration.ToString(@"hh\:mm\:ss")));
          loggingProgress.Report(new MessageEventArgs(MessageIntent.Telemetry, MessageLevel.Information, "send", "prepare-results", "duration", duration.ToString(@"hh\:mm\:ss")));
        }
        startTime = DateTime.Now;
      }

      var numToConvert = ((GsaCache)Instance.GsaModel.Cache).NumNatives;
      int numConverted = 0;
      int totalConversionPercentage = 80 - percentage;
      Instance.GsaModel.ConversionProgress = new Progress<bool>((bool success) =>
      {
        lock (perecentageProgressLock)
        {
          numConverted++;
        }
        percentageProgress.Report(percentage + Math.Round(((double)numConverted / (double)numToConvert) * totalConversionPercentage, 0));
      });

      var objs = Commands.ConvertToSpeckle(converter);

      if (converter.ConversionErrors != null && converter.ConversionErrors.Count > 0)
      {
        foreach (var ce in converter.ConversionErrors)
        {
          loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Error, ce.Message));
          loggingProgress.Report(new MessageEventArgs(MessageIntent.TechnicalLog, MessageLevel.Error, ce, ce.Message));
        }
      }

      loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Converted cache data to Speckle"));

      duration = DateTime.Now - startTime;
      if (duration.Seconds > 0)
      {
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Duration of conversion to Speckle: " + duration.ToString(@"hh\:mm\:ss")));
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Telemetry, MessageLevel.Information, "send", "conversion", "duration", duration.ToString(@"hh\:mm\:ss")));
      }
      startTime = DateTime.Now;

      //The converter itself can't give anything back other than Base objects, so this is the first time it can be adorned with any
      //info useful to the sending in streams

      var commitObj = new Base();
      foreach (var obj in objs)
      {
        var typeName = obj.GetType().Name;
        string name = "";
        if (typeName.ToLower().Contains("model"))
        {
          try
          {
            name = string.Join(" ", (string)obj["layerDescription"], "Model");
          }
          catch
          {
            name = typeName;
          }
        }
        else if (typeName.ToLower().Contains("result"))
        {
          name = "Results";
        }

        commitObj[name] = obj;
      }

      statusProgress.Report("Sending to Server");

      var stream = NewStream(client, "GSA data", "GSA data").Result;
      var streamState = new StreamState(account.userInfo.id, account.serverInfo.url) { Stream = stream };
      streamCreationProgress.Report(streamState);

      var serverTransport = new ServerTransport(account, streamState.Stream.id);
      var sent = Commands.Send(commitObj, streamState, serverTransport).Result;

      if (sent)
      {
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Successfully sent data to stream"));
        Commands.UpsertSavedReceptionStreamInfo(true, null, streamState);
      }
      else
      {
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Error, "Unable to send data to stream"));
      }

      if (streamState.Errors != null && streamState.Errors.Count > 0)
      {
        foreach (var se in streamState.Errors)
        {
          loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Error, se.Message));
          loggingProgress.Report(new MessageEventArgs(MessageIntent.TechnicalLog, MessageLevel.Error, se, se.Message));
        }
      }

      percentageProgress.Report(100);

      duration = DateTime.Now - startTime;
      if (duration.Seconds > 0)
      {
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Duration of sending to Speckle: " + duration.ToString(@"hh\:mm\:ss")));
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Telemetry, MessageLevel.Information, "send", "sending", "duration", duration.ToString(@"hh\:mm\:ss")));
      }
      startTime = DateTime.Now;

      Console.WriteLine("Sending complete");

      coordinator.SenderTab.SetDocumentName(((GsaProxy)Instance.GsaModel.Proxy).GetTitle());


      coordinator.WriteStreamInfo();

      return true;
    }

    private static async Task<Stream> NewStream(Client client, string streamName, string streamDesc)
    {
      string streamId = "";

      try
      {
        streamId = await client.StreamCreate(new StreamCreateInput()
        {
          name = streamName,
          description = streamDesc,
          isPublic = false
        });

        return await client.StreamGet(streamId);

      }
      catch (Exception e)
      {
        try
        {
          if (!string.IsNullOrEmpty(streamId))
          {
            await client.StreamDelete(streamId);
          }
        }
        catch
        {
          // POKEMON! (server is prob down)
        }
      }

      return null;
    }

    private static string UnitEnumToString(GsaUnit unit)
    {
      switch (unit)
      {
        case GsaUnit.Inches: return "in";
        case GsaUnit.Metres: return "m";
        default: return "mm";
      }
    }
  }
}
