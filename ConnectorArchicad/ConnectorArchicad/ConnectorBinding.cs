﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Archicad.Communication;
using Archicad.Model;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using static DesktopUI2.ViewModels.MappingViewModel;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Archicad.Launcher
{
  public class ArchicadBinding : ConnectorBindings
  {
    public override string GetActiveViewName()
    {
      throw new NotImplementedException();
    }

    public override List<MenuItem> GetCustomStreamMenuItems()
    {
      return new List<MenuItem>();
    }

    public override List<ISetting> GetSettings()
    {
      return new List<ISetting>();
    }

    public ProjectInfoData? GetProjectInfo()
    {
      return AsyncCommandProcessor.Execute(new Communication.Commands.GetProjectInfo())?.Result;
    }

    public override string GetDocumentId()
    {
      var projectInfo = AsyncCommandProcessor.Execute(new Communication.Commands.GetProjectInfo())?.Result;
      return projectInfo is null ? string.Empty : projectInfo.name;
    }

    public override string GetDocumentLocation()
    {
      var projectInfo = AsyncCommandProcessor.Execute(new Communication.Commands.GetProjectInfo())?.Result;
      return projectInfo is null ? string.Empty : projectInfo.location;
    }

    public override string GetFileName()
    {
      return Path.GetFileName(GetDocumentLocation());
    }

    public override string GetHostAppNameVersion()
    {
      return "Archicad 25";
    }

    public override string GetHostAppName()
    {
      return "Archicad";
    }

    public override List<string> GetObjectsInView()
    {
      throw new NotImplementedException();
    }

    public override List<string> GetSelectedObjects()
    {
      var elementIds = AsyncCommandProcessor.Execute(new Communication.Commands.GetSelectedElements())?.Result;
      return elementIds is null ? new List<string>() : elementIds.ToList();
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      return new List<ISelectionFilter> { new ManualSelectionFilter() };
    }

    public override List<StreamState> GetStreamsInFile()
    {
      return new List<StreamState>();
    }

    public override void ResetDocument()
    {
      // TODO!
    }

    public override List<ReceiveMode> GetReceiveModes()
    {
      return new List<ReceiveMode> { ReceiveMode.Create };
    }

    public override Task<StreamState> PreviewReceive(StreamState state, ProgressViewModel progress)
    {
      return null;
      // TODO!
    }

    public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
    {
      Base commitObject = await Helpers.Receive(IdentifyStream(state));
      if (commitObject is null)
        return null;

      state.SelectedObjectIds = await ElementConverterManager.Instance.ConvertToNative(commitObject, progress.CancellationTokenSource.Token);

      return state;
    }

    public override void SelectClientObjects(List<string> args, bool deselect = false) 
    {
      // TODO!
    }

    public override void PreviewSend(StreamState state, ProgressViewModel progress)
    {
      // TODO!
    }
    public override async Task<string> SendStream(StreamState state, ProgressViewModel progress)
    {
      if (state.Filter is null)
        return null;

      state.SelectedObjectIds = state.Filter.Selection;

      var commitObject = await ElementConverterManager.Instance.ConvertToSpeckle(state.SelectedObjectIds, progress.CancellationTokenSource.Token);
      if (commitObject is not null)
      {
        return await Helpers.Send(IdentifyStream(state), commitObject, state.CommitMessage, Speckle.Core.Kits.Applications.Archicad);
      }

      return null;
    }

    public override void WriteStreamsToFile(List<StreamState> streams) { }

    private static string IdentifyStream(StreamState state)
    {
      var stream = new StreamWrapper { StreamId = state.StreamId, ServerUrl = state.ServerUrl, BranchName = state.BranchName, CommitId = state.CommitId != "latest" ? state.CommitId : null };
      return stream.ToString();
    }

    public override async Task<Dictionary<string, List<MappingValue>>> ImportFamily(Dictionary<string, List<MappingValue>> Mapping)
    {
      await Task.Delay(TimeSpan.FromMilliseconds(500));
      return new Dictionary<string, List<MappingValue>>();
    }
  }
}
