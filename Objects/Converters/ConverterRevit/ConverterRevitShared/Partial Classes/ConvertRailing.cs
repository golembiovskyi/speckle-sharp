﻿using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject RailingToNative(BuiltElements.Revit.RevitRailing speckleRailing)
    {
      var revitRailing = GetExistingElementByApplicationId(speckleRailing.applicationId) as Railing;
      var appObj = new ApplicationObject(speckleRailing.id, speckleRailing.speckle_type) { applicationId = speckleRailing.applicationId };
      if (revitRailing != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore)
      {
        appObj.Update(status: ApplicationObject.State.Skipped, createdId: revitRailing.UniqueId, convertedItem: revitRailing);
        return appObj;
      }
     
      if (speckleRailing.path == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Path was null");
        return appObj;
      }

      if (!GetElementType<RailingType>(speckleRailing, appObj, out RailingType railingType))
      {
        appObj.Update(status: ApplicationObject.State.Failed);
        return appObj;
      }

      Level level = ConvertLevelToRevit(speckleRailing.level, out ApplicationObject.State levelState);
      if (level == null) //we currently don't support railings hosted on stairs, and these have null level
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Level was null");
        return appObj;
      }

      var baseCurve = CurveArrayToCurveLoop(CurveToNative(speckleRailing.path));

      //if it's a new element, we don't need to update certain properties
      bool isUpdate = true;
      if (revitRailing == null)
      {
        isUpdate = false;
        revitRailing = Railing.Create(Doc, baseCurve, railingType.Id, level.Id);
      }
      if (revitRailing == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Creation returned null");
        return appObj;
      }

      if (revitRailing.GetTypeId() != railingType.Id)
        revitRailing.ChangeTypeId(railingType.Id);

      if (isUpdate)
      {
        revitRailing.SetPath(baseCurve);
        TrySetParam(revitRailing, BuiltInParameter.WALL_BASE_CONSTRAINT, level);
      }

      if (speckleRailing.flipped != revitRailing.Flipped)
        revitRailing.Flip();

      SetInstanceParameters(revitRailing, speckleRailing);

      var status = isUpdate ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
      appObj.Update(status: status, createdId: revitRailing.UniqueId, convertedItem: revitRailing);
      Doc.Regenerate();
      return appObj;
    }

    //TODO: host railings, where possible
    private RevitRailing RailingToSpeckle(Railing revitRailing)
    {
      var railingType = revitRailing.Document.GetElement(revitRailing.GetTypeId()) as RailingType;
      var speckleRailing = new RevitRailing();
      //speckleRailing.family = railingType.FamilyName;
      speckleRailing.type = railingType.Name;
      speckleRailing.level = ConvertAndCacheLevel(revitRailing, BuiltInParameter.STAIRS_RAILING_BASE_LEVEL_PARAM);
      speckleRailing.path = CurveListToSpeckle(revitRailing.GetPath());

      GetAllRevitParamsAndIds(speckleRailing, revitRailing, new List<string> { "STAIRS_RAILING_BASE_LEVEL_PARAM" });

      speckleRailing.displayValue = GetElementDisplayMesh(revitRailing, new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });

      return speckleRailing;
    }

  }
}
