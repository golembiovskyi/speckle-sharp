﻿using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System.Collections.Generic;
using DB = Autodesk.Revit.DB;
using Line = Objects.Geometry.Line;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject ConduitToNative(BuiltElements.Conduit speckleConduit)
    {
      var speckleRevitConduit = speckleConduit as RevitConduit;

      var docObj = GetExistingElementByApplicationId((speckleConduit).applicationId);
      var appObj = new ApplicationObject(speckleConduit.id, speckleConduit.speckle_type) { applicationId = speckleConduit.applicationId };
      if (docObj != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore)
      {
        appObj.Update(status: ApplicationObject.State.Skipped, createdId: docObj.UniqueId, convertedItem: docObj);
        return appObj;
      }

      //var conduitType = GetElementType<ConduitType>(speckleConduit);

      if (!GetElementType(speckleConduit, appObj, out ConduitType conduitType))
      {
        appObj.Update(status: ApplicationObject.State.Failed);
        return appObj;
      }

      Element conduit = null;
      if (speckleConduit.baseCurve is Line)
      {
        DB.Line baseLine = LineToNative(speckleConduit.baseCurve as Line);
        XYZ startPoint = baseLine.GetEndPoint(0);
        XYZ endPoint = baseLine.GetEndPoint(1);
        DB.Level lineLevel = ConvertLevelToRevit(speckleRevitConduit != null ? speckleRevitConduit.level : LevelFromCurve(baseLine), out ApplicationObject.State levelState);
        Conduit lineConduit = Conduit.Create(Doc, conduitType.Id, startPoint, endPoint, lineLevel.Id);
        conduit = lineConduit;
      }
      else
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"BaseCurve of type ${speckleConduit.baseCurve.GetType()} cannot be used to create a Revit CableTray");
        return appObj;
      }

      // deleting instead of updating for now!
      if (docObj != null)
        Doc.Delete(docObj.Id);

      if (speckleRevitConduit != null)
      {
        TrySetParam(conduit, BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM, speckleRevitConduit.diameter, speckleRevitConduit.units);
        TrySetParam(conduit, BuiltInParameter.CURVE_ELEM_LENGTH, speckleRevitConduit.length, speckleRevitConduit.units);
        SetInstanceParameters(conduit, speckleRevitConduit);
      }

      appObj.Update(status: ApplicationObject.State.Created, createdId: conduit.UniqueId, convertedItem: conduit);
      return appObj;
    }

    public BuiltElements.Conduit ConduitToSpeckle(Conduit revitConduit)
    {
      var baseGeometry = LocationToSpeckle(revitConduit);
      if (!(baseGeometry is Line baseLine))
        throw new Speckle.Core.Logging.SpeckleException("Only line based Conduits are currently supported.");

      var conduitType = revitConduit.Document.GetElement(revitConduit.GetTypeId()) as ConduitType;

      // SPECKLE CONDUIT
      var speckleConduit = new RevitConduit
      {
        family = conduitType.FamilyName,
        type = conduitType.Name,
        baseCurve = baseLine,
        diameter = GetParamValue<double>(revitConduit, BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM),
        length = GetParamValue<double>(revitConduit, BuiltInParameter.CURVE_ELEM_LENGTH),
        level = ConvertAndCacheLevel(revitConduit, BuiltInParameter.RBS_START_LEVEL_PARAM),
        displayValue = GetElementMesh(revitConduit)
      };

      GetAllRevitParamsAndIds(speckleConduit, revitConduit,
        new List<string>
        {
          "RBS_CONDUIT_DIAMETER_PARAM", "CURVE_ELEM_LENGTH", "RBS_START_LEVEL_PARAM"
        });

      return speckleConduit;
    }
  }
}