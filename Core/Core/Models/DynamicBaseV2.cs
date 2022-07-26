using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Speckle.Core.Kits;

namespace Speckle.Core.Models
{
  public class BaseV2 : Dictionary<string, object>
  {
    [IgnoreMember]
    new public object this[string key]
    {
      get
      {
        if (ContainsKey(key)) return this[key];
        return GetType().GetProperty(key)?.GetValue(this);
      }
      set
      {
        var prop = GetTypedProperty(GetType(), key);
        if (prop != null)
        {
          prop.SetValue(this, value);
        }
        else
        {
          this[key] = value;
        }
      }
    }

    static Dictionary<Type, List<PropertyInfo>> propInfoCache = new Dictionary<Type, List<PropertyInfo>>();

    private static void PopulatePropInfoCache(Type type)
    {
      if (!propInfoCache.ContainsKey(type))
      {
        propInfoCache[type] = type.GetProperties().Where(p => p.CanRead && p.CanWrite && p.DeclaringType != typeof(Dictionary<string, object>) && p.GetCustomAttribute(typeof(IgnoreMember)) == null).ToList();
      }
    }

    private static PropertyInfo GetTypedProperty(Type type, string name)
    {
      PopulatePropInfoCache(type);
      return propInfoCache[type].FirstOrDefault(prop => prop.Name == name);
    }

    /// <summary>
    /// Gets all the accessible typed properties of this object.
    /// </summary>
    /// <returns></returns>
    private IEnumerable<PropertyInfo> GetInstanceProperties(bool ignoreAttributes = true)
    {
      var type = GetType();
      PopulatePropInfoCache(type);
      if (ignoreAttributes)
      {
        return propInfoCache[type];
      }

      return propInfoCache[type].Where(p => p.GetCustomAttribute(typeof(SchemaIgnore)) == null && p.GetCustomAttribute(typeof(ObsoleteAttribute)) == null);
    }

    /// <summary>
    /// Gets all the dynamic property names (dictionary keys) of this object.
    /// </summary>
    /// <param name="ignoreAttributes">Whether to ignore properties starting with "__" (e.g., "__hiddenProp").</param>
    /// <returns></returns>
    private IEnumerable<string> GetDynamicPropertyNames(bool ignoreAttributes = true)
    {
      if (ignoreAttributes)
      {
        return Keys;
      }
      return Keys.Where(key => !key.StartsWith("__"));
    }

    public IEnumerable<PropertyDetails> GetProperties(PropertyType propertyType = PropertyType.All, bool ignoreAttributes = true)
    {
      if(propertyType == PropertyType.All || propertyType == PropertyType.Typed)
      {
        foreach (var prop in GetInstanceProperties(ignoreAttributes))
          yield return new PropertyDetails { key = prop.Name, propertyInfo = prop };
      }

      if (propertyType == PropertyType.All || propertyType == PropertyType.Dynamic)
      {
        foreach(var key in GetDynamicPropertyNames())
          yield return new PropertyDetails { key = key };
      }
    }

    public Dictionary<string, object> CopyToDictionary()
    {
      var copy = new Dictionary<string, object>();
      foreach (var (key, _) in GetProperties())
      {
        copy[key] = this[key];
      }
      return copy;
    }

    public new IEnumerator GetEnumerator()
    {
      foreach (var (key, _) in GetProperties())
        yield return this[key];
    }
  }

  internal class IgnoreMember : Attribute { }

  public struct PropertyDetails
  {
    public string key;
    public PropertyInfo propertyInfo;

    public void Deconstruct( out string key, out PropertyInfo propertyInfo)
    {
      key = this.key;
      propertyInfo = this.propertyInfo;
    }
  }

  public enum PropertyType { Dynamic, Typed, All }

}

