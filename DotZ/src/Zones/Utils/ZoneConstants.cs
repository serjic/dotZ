using System.Zones;
using JetBrains.ReSharper.Psi;

namespace DotZ.Zones.Utils
{
  public static class ZoneConstants
  {
    public static readonly IClrTypeName ZoneAttributeClrName = new ClrTypeName(typeof(ZoneAttribute).FullName);
    public static readonly string ZoneAttributeNameFull = typeof(ZoneAttribute).FullName;
    public static readonly string ZoneAttributeName = typeof(ZoneAttribute).Name;
    public static readonly string ZoneAttributeNameShort = typeof(ZoneAttribute).Name.Substring(0, typeof(ZoneAttribute).Name.Length - "Attribute".Length);

    public static readonly string ZoneDefinitionAttributeNameFull = typeof(ZoneDefinitionAttribute).FullName;
    public static readonly string ZoneDefinitionAttributeName = typeof(ZoneDefinitionAttribute).Name;
    public static readonly string ZoneDefinitionAttributeNameShort = typeof(ZoneDefinitionAttribute).Name.Substring(0, typeof(ZoneDefinitionAttribute).Name.Length - "Attribute".Length);

    public static readonly string RequireName = typeof(IRequire<>).Name.Substring(0, typeof(IRequire<>).Name.Length - 2); // IRequire
    public static readonly string RequireNameFull = typeof(IRequire<>).FullName.Substring(0, typeof(IRequire<>).FullName.Length); // IRequire'1

    public static bool IsIRequire(IInterface iRequire)
    {
      if (iRequire == null)
        return false;


      if (iRequire.ShortName != RequireName)
        return false;

      if (iRequire.GetClrName().FullName != RequireNameFull)
        return false;

      return true;
    }

    public static bool IsZoneDefinitionClass(ITypeElement typeElement)
    {
      return typeElement.HasAttributeInstance(new ClrTypeName(ZoneDefinitionAttributeNameFull), false);
    }

    public static bool IsZoneClass(ITypeElement typeElement)
    {
      return typeElement.HasAttributeInstance(new ClrTypeName(ZoneAttributeNameFull), false);
    }
  }
}