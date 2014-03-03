using JetBrains.Annotations;

namespace System.Zones
{
  /// <summary>
  ///   <para>Declares a new zone.</para>
  ///   <para>If the marked class/interface derives some other zone, then it's a value in the existing axis. Otherwise it's a root of the new axis.</para>
  /// </summary>
  [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct)]
  [BaseTypeRequired(typeof(IZone))]
  [MeansImplicitUse]
  public class ZoneDefinitionAttribute : Attribute
  {
  }
}