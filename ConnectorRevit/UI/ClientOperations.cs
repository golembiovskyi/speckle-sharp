﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using RevitElement = Autodesk.Revit.DB.Element;
using Newtonsoft.Json;
using Speckle.ConnectorRevit.Storage;
using Speckle.Converter.Revit;
using Speckle.Core.Api;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using SpeckleElement = Speckle.Objects.Element;
using Speckle.DesktopUI.Utils;

namespace Speckle.ConnectorRevit.UI
{
  public partial class ConnectorBindingsRevit
  {
    /// <summary>
    /// Adds a new stream to the file. It takes a stream state, gets the Revit elements
    /// the user wants to add using the filter, and writes the StreamState to the file.
    /// </summary>
    /// <param name="state">StreamState passed by the UI</param>
    public override void AddNewStream(StreamState state)
    {
      // add stream and related data to the class
      LocalStateWrapper.StreamStates.Add(state);
      DEP_LocalState.Add(state.stream);

      GetSelectionFilterObjects(state.filter, state.accountId, state.stream.id);
      RaiseNotification("Stream created! Next, send to server ☁");
    }

    /// <summary>
    /// Update the stream state and adds adds the filtered objects
    /// </summary>
    /// <param name="state"></param>
    public override void UpdateStream(StreamState state)
    {
      var index = LocalStateWrapper.StreamStates.FindIndex(b => b.stream.id == state.stream.id);
      LocalStateWrapper.StreamStates[index] = state;

      GetSelectionFilterObjects(state.filter, state.accountId, state.stream.id);
    }

    /// <summary>
    /// Converts the Revit elements that have been added to the stream by the user, sends them to
    /// the Server and the local DB, and creates a commit with the objects.
    /// </summary>
    /// <param name="state">StreamState passed by the UI</param>
    public override async Task<StreamState> SendStream(StreamState state)
    {
      var kit = new ConverterRevit(CurrentDoc.Document);
      var objsToConvert = state.placeholders;
      var streamId = state.stream.id;
      var client = state.client;


      var convertedObjects = new List<Base>();
      var failedConversions = new List<RevitElement>();

      var units = CurrentDoc.Document.GetUnits().GetFormatOptions(UnitType.UT_Length).DisplayUnits.ToString()
        .ToLowerInvariant().Replace("dut_", "");
      // InjectScaleInKits(GetScale(units)); // this is used for feet to sane units conversion.

      int i = 0;
      long currentBucketSize = 0;
      var errorMsg = "";
      var errors = new List<SpeckleException>();

      foreach ( var obj in objsToConvert )
      {
        //NotifyUi
        var revitElement = CurrentDoc.Document.GetElement(obj.applicationId);
        if ( revitElement == null )
        {
          errors.Add(new SpeckleException(message: "Could not retrieve element"));
          continue;
        }

        var conversionResult = kit.ConvertToSpeckle(revitElement);
        if ( conversionResult == null )
        {
          // TODO what happens to failed conversions?
          failedConversions.Add(revitElement);
          continue;
        }
        convertedObjects.Add(conversionResult);
      }

      if ( errors.Any() || kit.ConversionErrors.Any() )
      {
        errorMsg = string.Format("There {0} {1} failed conversion{2} and {3} error{4}",
          kit.ConversionErrors.Count() == 1 ? "is" : "are",
          kit.ConversionErrors.Count(),
          kit.ConversionErrors.Count() == 1 ? "" : "s",
          errors.Count(),
          errors.Count() == 1 ? "" : "s");
      }

      var transports = new List<ITransport>() {new ServerTransport(client.Account, streamId)};
      var emptyBase = new Base();
      var @base = new Base {[ "@revitItems" ] = convertedObjects};
      var objectId = await Operations.Send(@base, transports);
      var res = await client.CommitCreate(new CommitCreateInput()
      {
        streamId = streamId,
        objectId = objectId,
        branchName = "master",
        message = "Commit from Revit Connector"
      });

      // update the state
      state.objects.AddRange(convertedObjects);
      state.placeholders.Clear();
      state.stream = await client.StreamGet(streamId);

      // Persist state to revit file
      Queue.Add(new Action(() =>
      {
        using ( Transaction t = new Transaction(CurrentDoc.Document, "Update local storage") )
        {
          t.Start();
          StreamStateManager.WriteState(CurrentDoc.Document, LocalStateWrapper);
          t.Commit();
        }
      }));

      RaiseNotification($"{convertedObjects.Count()} objects sent to Speckle 🚀");
      return state;
    }

    /// <summary>
    /// Pass selected element ids to UI
    /// </summary>
    /// <param name="args"></param>
    public override List<string> GetSelectedObjects()
    {
      var doc = CurrentDoc.Document;
      var selectedObjects = CurrentDoc != null ? CurrentDoc.Selection.GetElementIds().Select(id => doc.GetElement(id).UniqueId).ToList() : new List<string>();

      return  selectedObjects;
    }

    public override void RemoveSelectionFromClient(string args)
    {
      throw new NotImplementedException();
    }

    #region private methods

    private Type GetFilterType(string typeString)
    {
      Assembly ass = typeof(ISelectionFilter).Assembly;
      return ass.GetType(typeString);
    }

    /// <summary>
    /// Given the filter in use by a stream returns the document elements that match it
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="accountId"></param>
    /// <param name="streamId"></param>
    /// <returns></returns>
    private IEnumerable<Base> GetSelectionFilterObjects(ISelectionFilter filter, string accountId, string streamId)
    {
      var doc = CurrentDoc.Document;
      IEnumerable<Base> objects = new List<Base>();

      var selectionIds = new List<string>();

      if (filter.Name == "Selection")
      {
        var selFilter = filter as ElementsSelectionFilter;
        selectionIds = selFilter.Selection;
      }
      else if (filter.Name == "Category")
      {
        var catFilter = filter as ListSelectionFilter;
        var bics = new List<BuiltInCategory>();
        var categories = Globals.GetCategories(doc);
        IList<ElementFilter> elementFilters = new List<ElementFilter>();

        foreach (var cat in catFilter.Selection)
        {
          elementFilters.Add(new ElementCategoryFilter(categories[cat].Id));
        }
        LogicalOrFilter categoryFilter = new LogicalOrFilter(elementFilters);

        selectionIds = new FilteredElementCollector(doc)
          .WhereElementIsNotElementType()
          .WhereElementIsViewIndependent()
          .WherePasses(categoryFilter)
          .Select(x => x.UniqueId).ToList();

      }
      else if (filter.Name == "View")
      {
        var viewFilter = filter as ListSelectionFilter;

        var views = new FilteredElementCollector(doc)
          .WhereElementIsNotElementType()
          .OfClass(typeof(View))
          .Where(x => viewFilter.Selection.Contains(x.Name));

        foreach (var view in views)
        {
          var ids = new FilteredElementCollector(doc, view.Id)
            .WhereElementIsNotElementType()
            .WhereElementIsViewIndependent()
            .Where(x => x.IsPhysicalElement())
            .Select(x => x.UniqueId).ToList();

          selectionIds = selectionIds.Union(ids).ToList();
        }
      }
      else if (filter.Name == "Parameter")
      {
        try
        {
          var propFilter = filter as PropertySelectionFilter;
          var query = new FilteredElementCollector(doc)
            .WhereElementIsNotElementType()
            .WhereElementIsNotElementType()
            .WhereElementIsViewIndependent()
            .Where(x => x.IsPhysicalElement())
            .Where(fi => fi.LookupParameter(propFilter.PropertyName) != null);

          propFilter.PropertyValue = propFilter.PropertyValue.ToLowerInvariant();

          switch (propFilter.PropertyOperator)
          {
            case "equals":
              query = query.Where(fi => GetStringValue(fi.LookupParameter(propFilter.PropertyName)) == propFilter.PropertyValue);
              break;
            case "contains":
              query = query.Where(fi => GetStringValue(fi.LookupParameter(propFilter.PropertyName)).Contains(propFilter.PropertyValue));
              break;
            case "is greater than":
              query = query.Where(fi => UnitUtils.ConvertFromInternalUnits(
                fi.LookupParameter(propFilter.PropertyName).AsDouble(),
                fi.LookupParameter(propFilter.PropertyName).DisplayUnitType) > double.Parse(propFilter.PropertyValue));
              break;
            case "is less than":
              query = query.Where(fi => UnitUtils.ConvertFromInternalUnits(
                fi.LookupParameter(propFilter.PropertyName).AsDouble(),
                fi.LookupParameter(propFilter.PropertyName).DisplayUnitType) < double.Parse(propFilter.PropertyValue));
              break;
            default:
              break;
          }

          selectionIds = query.Select(x => x.UniqueId).ToList();

        }
        catch (Exception e)
        {
          Console.WriteLine(e);
        }
      }

      // LOCAL STATE management
      objects = selectionIds.Select(id =>
      {
        var temp = new Base();
        temp.applicationId = id;
        temp["__type"] = "Placeholder";
        return temp;
      });

      var streamState = LocalStateWrapper.StreamStates.FirstOrDefault(
        cl => (string)cl.stream.id == (string)streamId
        );
      streamState.placeholders.AddRange(objects);

      // Persist state and clients to revit file
      Queue.Add(new Action(() =>
      {
        using (Transaction t = new Transaction(CurrentDoc.Document, "Update local storage"))
        {
          t.Start();
          StreamStateManager.WriteState(CurrentDoc.Document, LocalStateWrapper);
          t.Commit();
        }
      }));
      Executor.Raise();
      var plural = objects.Count() == 1 ? "" : "s";

      if (objects.Any())
        NotifyUi( new RetrievedFilteredObjectsEvent()
        {
          Notification = $"You have added {objects.Count()} object{plural} to this stream.",
          AccountId = accountId,
          Objects = objects
        });
      RaiseNotification($"You have added {objects.Count()} object{plural} to this stream.");

      return objects;
    }

    private string GetStringValue(Parameter p)
    {
      string value = "";
      if (!p.HasValue)
        return value;
      if (string.IsNullOrEmpty(p.AsValueString()) && string.IsNullOrEmpty(p.AsString()))
        return value;
      if (!string.IsNullOrEmpty(p.AsValueString()))
        return p.AsValueString().ToLowerInvariant();
      else
        return p.AsString().ToLowerInvariant();
    }

    // TODO move to converter?
    private static byte[ ] GetBytes(object obj)
    {
      using ( MemoryStream memoryStream = new MemoryStream() )
      {
        new BinaryFormatter().Serialize(memoryStream, obj);
        return memoryStream.ToArray();
      }
    }
    #endregion
  }
}
