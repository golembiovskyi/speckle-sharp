﻿using DesktopUI2;
using DesktopUI2.Models.Filters;
using Speckle.ConnectorCSI.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorCSI.UI
{
  public partial class ConnectorBindingsCSI : ConnectorBindings

  {
    public override List<string> GetSelectedObjects()
    {
      var names = new List<string>();
      var util = new ConnectorCSIUtils();
      var typeNameTupleList = ConnectorCSIUtils.SelectedObjects(Model);
      if (typeNameTupleList == null) return new List<string>() { };
      foreach (var item in typeNameTupleList)
      {
        (string typeName, string name) = item;
        if (ConnectorCSIUtils.IsTypeCSIAPIUsable(typeName))
        {
          names.Add(string.Concat(typeName, ": ", name));
        }
      }
      if (names.Count == 0)
      {
        return new List<string>() { };
      }
      return names;
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      var objectTypes = new List<string>();
      //var objectIds = new List<string>();
      string[] groupNames = null;
      var groups = new List<string>();
      int numNames = 0;
      if (Model != null)
      {
        ConnectorCSIUtils.GetObjectIDsTypesAndNames(Model);
        objectTypes = ConnectorCSIUtils.ObjectIDsTypesAndNames
            .Select(pair => pair.Value.Item1).Distinct().ToList();
        //objectIds = ConnectorCSIUtils.ObjectIDsTypesAndNames.Select(pair => pair.Key).ToList();
        Model.GroupDef.GetNameList(ref numNames, ref groupNames);
        groups = groupNames.ToList();
      }

      return new List<ISelectionFilter>()
            {
            new AllSelectionFilter {Slug="all",  Name = "Everything",
                Icon = "CubeScan", Description = "Selects all document objects." },
            new ListSelectionFilter {Slug="type", Name = "Categories",
                Icon = "Category", Values = objectTypes,
                Description="Adds all objects belonging to the selected types"},
        //new PropertySelectionFilter{
        //  Slug="param",
        //  Name = "Param",
        //  Description="Adds  all objects satisfying the selected parameter",
        //  Icon = "FilterList",
        //  HasCustomProperty = false,
        //  Values = objectNames,
        //  Operators = new List<string> {"equals", "contains", "is greater than", "is less than"}
        //},
            
            new ManualSelectionFilter(),
            new ListSelectionFilter { Slug = "group", Name = "Group",
            Icon = "SelectGroup", Values = groups, Description = "Add all objects belonging to CSI Group" }
            };
            
    }

    public override void SelectClientObjects(string args)
    {
      throw new NotImplementedException();
    }

    private List<string> GetSelectionFilterObjects(ISelectionFilter filter)
    {
      var doc = Model;

      var selection = new List<string>();

      switch (filter.Slug)
      {
        case "manual":
          return GetSelectedObjects();
        case "all":
          if (ConnectorCSIUtils.ObjectIDsTypesAndNames == null)
          {
            ConnectorCSIUtils.GetObjectIDsTypesAndNames(Model);
          }
          selection.AddRange(ConnectorCSIUtils.ObjectIDsTypesAndNames
                      .Select(pair => pair.Key).ToList());
          return selection;


        case "type":
          var typeFilter = filter as ListSelectionFilter;
          if (ConnectorCSIUtils.ObjectIDsTypesAndNames == null)
          {
            ConnectorCSIUtils.GetObjectIDsTypesAndNames(Model);
          }
          foreach (var type in typeFilter.Selection)
          {
            selection.AddRange(ConnectorCSIUtils.ObjectIDsTypesAndNames
                .Where(pair => pair.Value.Item1 == type)
                .Select(pair => pair.Key)
                .ToList());
          }
          return selection;
        case "group":
          //Clear objects first
          Model.SelectObj.ClearSelection();
          var groupFilter = filter as ListSelectionFilter;
          foreach (var group in groupFilter.Selection)
          {
            Model.SelectObj.Group(group);
          }
          return GetSelectedObjects();



          /// CSI doesn't list fields of different objects. 
          /// For "param" search, maybe search over the name of
          /// methods of each type?

          //case "param":
          //    try
          //    {
          //        if (ConnectorCSIUtils.ObjectTypes.Count == 0)
          //        {
          //            var _ = ConnectorCSIUtils.GetObjectTypesAndNames(Model);
          //        }

          //        var propFilter = filter as PropertySelectionFilter;
          //        var query = new FilteredElementCollector(doc)
          //          .WhereElementIsNotElementType()
          //          .WhereElementIsNotElementType()
          //          .WhereElementIsViewIndependent()
          //          .Where(x => x.IsPhysicalElement())
          //          .Where(fi => fi.LookupParameter(propFilter.PropertyName) != null);

          //        propFilter.PropertyValue = propFilter.PropertyValue.ToLowerInvariant();

          //        switch (propFilter.PropertyOperator)
          //        {
          //            case "equals":
          //                query = query.Where(fi =>
          //                  GetStringValue(fi.LookupParameter(propFilter.PropertyName)) == propFilter.PropertyValue);
          //                break;
          //            case "contains":
          //                query = query.Where(fi =>
          //                  GetStringValue(fi.LookupParameter(propFilter.PropertyName)).Contains(propFilter.PropertyValue));
          //                break;
          //            case "is greater than":
          //                query = query.Where(fi => RevitVersionHelper.ConvertFromInternalUnits(
          //                                            fi.LookupParameter(propFilter.PropertyName).AsDouble(),
          //                                            fi.LookupParameter(propFilter.PropertyName)) >
          //                                          double.Parse(propFilter.PropertyValue));
          //                break;
          //            case "is less than":
          //                query = query.Where(fi => RevitVersionHelper.ConvertFromInternalUnits(
          //                                            fi.LookupParameter(propFilter.PropertyName).AsDouble(),
          //                                            fi.LookupParameter(propFilter.PropertyName)) <
          //                                          double.Parse(propFilter.PropertyValue));
          //                break;
          //        }

          //        selection = query.ToList();
          //    }
          //    catch (Exception e)
          //    {
          //        Log.CaptureException(e);
          //    }
          //    return selection;
      }

      return selection;

    }
  }
}