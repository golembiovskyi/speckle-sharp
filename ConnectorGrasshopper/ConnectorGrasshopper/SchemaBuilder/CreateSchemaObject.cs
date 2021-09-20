﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using ConnectorGrasshopper.Extras;
using ConnectorGrasshopper.Objects;
using ConnectorGrasshopperUtils;
using GH_IO.Serialization;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper
{
  public class CreateSchemaObject : SelectKitComponentBase, IGH_VariableParameterComponent
  {
    public override GH_Exposure Exposure => GH_Exposure.quarternary;

    private ConstructorInfo SelectedConstructor;
    private bool readFailed = false;
    private GH_Document _document;

    public override Guid ComponentGuid => new Guid("4dc285e3-810d-47db-bfb5-cd96fe459fdd");
    protected override Bitmap Icon => Properties.Resources.SchemaBuilder;

    public string Seed;

    public CreateSchemaObject() : base("Create Schema Object", "CsO",
      "Allows you to create a Speckle object from a schema class.",
      ComponentCategories.PRIMARY_RIBBON, ComponentCategories.OBJECTS)
    {
      Seed = GenerateSeed();
    }

    public string GenerateSeed()
    {
      return new string(Speckle.Core.Models.Utilities.hashString(Guid.NewGuid().ToString()).Take(20).ToArray());
    }

    public override void AddedToDocument(GH_Document document)
    {
      if (readFailed)
        return;

      if (SelectedConstructor != null)
      {
        base.AddedToDocument(document);
        if (Grasshopper.Instances.ActiveCanvas.Document != null)
        {
          var otherSchemaBuilders = Grasshopper.Instances.ActiveCanvas.Document.FindObjects(new List<string>() { Name }, 10000);
          foreach (var comp in otherSchemaBuilders)
          {
            if (comp is CreateSchemaObject scb)
            {
              if (scb.Seed == Seed)
              {
                Seed = GenerateSeed();
                break;
              }
            }
          }
        }
        return;
      }

      _document = document;

      var dialog = new CreateSchemaObjectDialog();
      dialog.Owner = Grasshopper.Instances.EtoDocumentEditor;
      var mouse = GH_Canvas.MousePosition;
      dialog.Location = new Eto.Drawing.Point((int)((mouse.X - 150) / dialog.Screen.LogicalPixelSize), (int)((mouse.Y - 150) / dialog.Screen.LogicalPixelSize)); //approx the dialog half-size

      dialog.ShowModal();

      if (dialog.HasResult)
      {
        base.AddedToDocument(document);
        SwitchConstructor(dialog.model.SelectedItem.Tag as ConstructorInfo);
      }
      else
      {
        document.RemoveObject(this.Attributes, true);
      }
    }

    public void SwitchConstructor(ConstructorInfo constructor)
    {
      int k = 0;
      var props = constructor.GetParameters();

      foreach (var p in props)
      {
        RegisterPropertyAsInputParameter(p, k++);
      }

      this.Name = constructor.GetCustomAttribute<SchemaInfo>().Name;
      this.Description = constructor.GetCustomAttribute<SchemaInfo>().Description;

      Message = constructor.DeclaringType.FullName.Split('.')[0];
      SelectedConstructor = constructor;
      Params.Output[0].NickName = constructor.DeclaringType.Name;
      Params.OnParametersChanged();
      ExpireSolution(true);
    }

    /// <summary>
    /// Adds a property to the component's inputs.
    /// </summary>
    /// <param name="param"></param>
    private void RegisterPropertyAsInputParameter(ParameterInfo param, int index)
    {
      // get property name and value
      Type propType = param.ParameterType;
      if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
        propType = Nullable.GetUnderlyingType(propType);

      string propName = param.Name;
      object propValue = param;

      var inputDesc = param.GetCustomAttribute<SchemaParamInfo>();
      var d = inputDesc != null ? inputDesc.Description : "";
      if (param.IsOptional)
      {
        if (!string.IsNullOrEmpty(d))
          d += ", ";
        var def = param.DefaultValue == null ? "null" : param.DefaultValue.ToString();
        d += "default = " + def;
      }

      // Create new param based on property name
      Param_GenericObject newInputParam = new Param_GenericObject();
      newInputParam.Name = propName;
      newInputParam.NickName = propName;
      newInputParam.MutableNickName = false;

      newInputParam.Description = $"({propType.Name}) {d}";
      newInputParam.Optional = param.IsOptional;
      if (param.IsOptional)
        newInputParam.SetPersistentData(param.DefaultValue);

      // check if input needs to be a list or item access
      bool isCollection = typeof(System.Collections.IEnumerable).IsAssignableFrom(propType) && propType != typeof(string) && !propType.Name.ToLower().Contains("dictionary");
      if (isCollection == true)
      {
        newInputParam.Access = GH_ParamAccess.list;
      }
      else
      {
        newInputParam.Access = GH_ParamAccess.item;
      }
      Params.RegisterInputParam(newInputParam, index);

      //add dropdown
      if (propType.IsEnum)
      {
        //expire solution so that node gets proper size
        ExpireSolution(true);

        var instance = Activator.CreateInstance(propType);

        var vals = Enum.GetValues(propType).Cast<Enum>().Select(x => x.ToString()).ToList();
        var options = CreateDropDown(propName, vals, Attributes.Bounds.X, Params.Input[index].Attributes.Bounds.Y);
        _document.AddObject(options, false);
        Params.Input[index].AddSource(options);
      }
    }

    public static GH_ValueList CreateDropDown(string name, List<string> values, float x, float y)
    {
      var valueList = new GH_ValueList();
      valueList.CreateAttributes();
      valueList.Name = name;
      valueList.NickName = name + ":";
      valueList.Description = "Select an option...";
      valueList.ListMode = GH_ValueListMode.DropDown;
      valueList.ListItems.Clear();

      for (int i = 0; i < values.Count; i++)
      {
        valueList.ListItems.Add(new GH_ValueListItem(values[i], i.ToString()));
      }

      valueList.Attributes.Pivot = new PointF(x - 200, y - 10);

      return valueList;
    }

    public override bool Read(GH_IReader reader)
    {
      try
      {
        var constructorName = reader.GetString("SelectedConstructorName");
        var typeName = reader.GetString("SelectedTypeName");

        SelectedConstructor = CSOUtils.FindConstructor(constructorName, typeName);
        if (SelectedConstructor == null)
          readFailed = true;

      }
      catch
      {
        readFailed = true;
      }

      try
      {
        Seed = reader.GetString("seed");
      }
      catch { }
      return base.Read(reader);
    }

    public override bool Write(GH_IWriter writer)
    {
      if (SelectedConstructor != null)
      {
        var methodFullName = CSOUtils.MethodFullName(SelectedConstructor);
        var declaringTypeFullName = SelectedConstructor.DeclaringType.FullName;
        writer.SetString("SelectedConstructorName", methodFullName);
        writer.SetString("SelectedTypeName", declaringTypeFullName);
      }

      writer.SetString("seed", Seed);
      return base.Write(writer);
    }


    protected override void RegisterInputParams(GH_InputParamManager pManager)
    { }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      //pManager.AddGenericParameter("Debug", "d", "debug output, please ignore", GH_ParamAccess.list);
      pManager.AddParameter(new SpeckleBaseParam("Speckle Object", "O", "Created speckle object", GH_ParamAccess.item));
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (readFailed)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "This component has changed or cannot be found, please create a new one");
        return;
      }

      if (SelectedConstructor is null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No schema has been selected.");
        return;
      }

      var units = Units.GetUnitsFromString(Rhino.RhinoDoc.ActiveDoc.GetUnitSystemName(true, false, false, false));

      List<object> cParamsValues = new List<object>();
      var cParams = SelectedConstructor.GetParameters();
      object mainSchemaObj = null;
      for (var i = 0; i < cParams.Length; i++)
      {
        var cParam = cParams[i];
        var param = Params.Input[i];
        object objectProp = null;
        if (param.Access == GH_ParamAccess.list)
        {
          var inputValues = new List<object>();
          DA.GetDataList(i, inputValues);
          if (!inputValues.Any() && !param.Optional)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input list `" + param.Name + "` is empty.");
            return;
          }

          try
          {
            inputValues = inputValues.Select(x => ExtractRealInputValue(x)).ToList();
            objectProp = GetObjectListProp(param, inputValues, cParam.ParameterType);
          }
          catch (Exception e)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.InnerException?.Message ?? e.Message);
            return;
          }
        }
        else if (param.Access == GH_ParamAccess.item)
        {
          object inputValue = null;
          DA.GetData(i, ref inputValue);
          var extractRealInputValue = ExtractRealInputValue(inputValue);
          objectProp = GetObjectProp(param, extractRealInputValue, cParam.ParameterType);
        }
        cParamsValues.Add(objectProp);
        if (CustomAttributeData.GetCustomAttributes(cParam)?.Where(o => o.AttributeType.IsEquivalentTo(typeof(SchemaMainParam)))?.Count() > 0)
          mainSchemaObj = objectProp;
      }
      

      object schemaObject = null;
      try
      {
        schemaObject = SelectedConstructor.Invoke(cParamsValues.ToArray());
        ((Base)schemaObject).applicationId = $"{Seed}-{SelectedConstructor.DeclaringType.FullName}-{DA.Iteration}";
        ((Base)schemaObject)["units"] = units;
      }
      catch (Exception e)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.InnerException?.Message ?? e.Message);
        return;
      }

      // create commit obj from main geometry param and try to attach schema obj. use schema obj if no main geom param was found.
      Base commitObj = ((Base) schemaObject).ShallowCopy();
      try
      {
        if (mainSchemaObj != null)
        {
          commitObj = ((Base) mainSchemaObj).ShallowCopy();
          commitObj["@SpeckleSchema"] = schemaObject;
          commitObj["units"] = units;
        }
      }
      catch { }
        
      // Finally, add any custom props created by the user.
      for( var j = cParams.Length; j < Params.Input.Count; j++)
      {
        // Additional props added to the object
        var ghParam = Params.Input[j];
        if (ghParam.Access == GH_ParamAccess.item)
        {
          object input = null;
          DA.GetData(j, ref input);
          
          commitObj[ghParam.Name] = Utilities.TryConvertItemToSpeckle(input, Converter);
        }
        else if (ghParam.Access == GH_ParamAccess.list)
        {
          List<object> input = new List<object>();
          DA.GetDataList(j, input);
          commitObj[ghParam.Name] = input.Select(i => Utilities.TryConvertItemToSpeckle(i, Converter)).ToList();
        }
      }
      DA.SetData(0, new GH_SpeckleBase() { Value = commitObj });
    }

    private object ExtractRealInputValue(object inputValue)
    {
      if (inputValue == null)
        return null;

      if (inputValue is Grasshopper.Kernel.Types.IGH_Goo)
      {
        var type = inputValue.GetType();
        var propertyInfo = type.GetProperty("Value");
        var value = propertyInfo.GetValue(inputValue);
        return value;
      }

      return inputValue;
    }

    //list input
    private object GetObjectListProp(IGH_Param param, List<object> values, Type t)
    {
      if (!values.Any()) return null;

      var list = (IList)Activator.CreateInstance(t);
      var listElementType = list.GetType().GetGenericArguments().Single();
      foreach (var value in values)
      {
        list.Add(ConvertType(listElementType, value, param.Name));
      }

      return list;
    }

    private object GetObjectProp(IGH_Param param, object value, Type t)
    {
      var convertedValue = ConvertType(t, value, param.Name);
      return convertedValue;
    }

    private object ConvertType(Type type, object value, string name)
    {
      if (value == null)
      {
        return null;
      }

      var typeOfValue = value.GetType();
      if (value == null || typeOfValue == type || type.IsAssignableFrom(typeOfValue))
        return value;

      //needs to be before IsSimpleType
      if (type.IsEnum)
      {
        try
        {
          return Enum.Parse(type, value.ToString());
        }
        catch { }
      }

      // int, doubles, etc
      if (Speckle.Core.Models.Utilities.IsSimpleType(value.GetType()))
      {
        try
        {
          return Convert.ChangeType(value, type);
        }
        catch (Exception e)
        {
          throw new Exception($"Cannot convert {value.GetType()} to {type}");
        }      
      }

      if (Converter.CanConvertToSpeckle(value))
      {
        var converted = Converter.ConvertToSpeckle(value);
        //in some situations the converted type is not exactly the type needed by the constructor
        //even if an implicit casting is defined, invoking the constructor will fail because the type is boxed
        //to convert the boxed type, it seems the only valid solution is to use Convert.ChangeType
        //currently, this is to support conversion of Polyline to Polycurve in Objects
        if (converted.GetType() != type && !type.IsAssignableFrom(converted.GetType()))
        {
          try
          {
            return Convert.ChangeType(converted, type);
          }
          catch (Exception e)
          {
            throw new Exception($"Cannot convert {converted.GetType()} to {type}");
          }
        }
        return converted;
      }
      else
      {
        // Log conversion error?
      }

      //tries an explicit casting, given that the required type is a variable, we need to use reflection
      //not really sure this method is needed
      try
      {
        MethodInfo castIntoMethod = this.GetType().GetMethod("CastObject").MakeGenericMethod(type);
        return castIntoMethod.Invoke(null, new[] { value });
      }
      catch { }

      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unable to set " + name + ".");
      throw new SpeckleException($"Could not covert object to {type}");
    }

    //keep public so it can be picked by reflection
    public static T CastObject<T>(object input)
    {
      return (T)input;
    }

    public bool CanInsertParameter(GH_ParameterSide side, int index)
    {
      var intPtr = SelectedConstructor.GetParameters().Length;
      var canInsertParameter = side == GH_ParameterSide.Input && index >= intPtr;
      return canInsertParameter;
    }

    public bool CanRemoveParameter(GH_ParameterSide side, int index)
    {
      var intPtr = SelectedConstructor.GetParameters().Length;
      return side == GH_ParameterSide.Input && index >= intPtr;
    }

    public IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
      var myParam = new GenericAccessParam
      {
        Name = GH_ComponentParamServer.InventUniqueNickname("ABCD", Params.Input),
        MutableNickName = true,
        Optional = false
      };
      
      myParam.NickName = myParam.Name;
      //myParam.ObjectChanged += (sender, e) => Debouncer.Start();

      return myParam;
    }

    public bool DestroyParameter(GH_ParameterSide side, int index)
    {
      return true;
    }

    public void VariableParameterMaintenance()
    { }

    protected override void BeforeSolveInstance()
    {
      Converter?.SetContextDocument(Rhino.RhinoDoc.ActiveDoc);
      Tracker.TrackPageview("objects", "create", "variableinput");
      base.BeforeSolveInstance();
    }
  }

}
