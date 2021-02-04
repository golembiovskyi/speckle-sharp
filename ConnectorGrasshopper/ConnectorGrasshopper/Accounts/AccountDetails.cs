﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;

namespace ConnectorGrasshopper.Streams
{
  public class AccountDetails : GH_Component
  {
    public AccountDetails() : base("Account Details", "AccDet", "Gets the details from a specific account", "Speckle 2",
      "Accounts")
    {
    }

    public override Guid ComponentGuid => new Guid("04822A33-777A-457B-BEF3-E54044322DB0");

    protected override Bitmap Icon => null;

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      var acc = pManager.AddTextParameter("Account", "A", "Account to get stream with.", GH_ParamAccess.item);

      Params.Input[acc].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("ID", "ID", "Unique ID of the account.", GH_ParamAccess.item);
      pManager.AddBooleanParameter("isDefault", "D", "Determines if the account is the default of this machine.",
        GH_ParamAccess.item);
      pManager.AddTextParameter("Server name", "SN", "Name of the server.", GH_ParamAccess.item);
      pManager.AddTextParameter("Server Company", "SC", "Name of the company running this server.",
        GH_ParamAccess.item);
      pManager.AddTextParameter("Server URL", "SU", "URL of the server.", GH_ParamAccess.item);
      pManager.AddTextParameter("User ID", "UID", "Unique ID of this account's user.", GH_ParamAccess.item);
      pManager.AddTextParameter("User Name", "UN", "Name of this account's user", GH_ParamAccess.item);
      pManager.AddTextParameter("User Company", "UC", "Name of the company this user belongs to", GH_ParamAccess.item);
      pManager.AddTextParameter("User Email", "UE", "Email of this account's user", GH_ParamAccess.item);
    }
  

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      string accountId = null;
      DA.GetData(0, ref accountId);

      if (string.IsNullOrEmpty(accountId))
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "No account provided. Trying with default account.");
      }

      var account = string.IsNullOrEmpty(accountId)  ? AccountManager.GetDefaultAccount()
        : AccountManager.GetAccounts().FirstOrDefault(a => a.id == accountId);

      if(account == null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not find default account in this machine. Use the Speckle Manager to add an account.");
        return;
      }
      Params.Input[0].AddVolatileData(new GH_Path(0), 0, account.id);

      DA.SetData(0, account.id);
      DA.SetData(1, account.isDefault);
      DA.SetData(2, account.serverInfo.name);
      DA.SetData(3, account.serverInfo.company);
      DA.SetData(4, account.serverInfo.url);
      DA.SetData(5, account.userInfo.id);
      DA.SetData(6, account.userInfo.name);
      DA.SetData(7, account.userInfo.company);
      DA.SetData(8, account.userInfo.email);
    }
    
    protected override void BeforeSolveInstance()
    {
      Tracker.TrackPageview("account", "details");
      base.BeforeSolveInstance();
    }
  }
}
