using JetBrains.Annotations;

namespace System.Zones
{
  /// <summary>
  ///   <para>Marks a class that it requires the specified zones. This means that all the classes and resources within that namespace also require these zones.</para>
  ///   <para>There are two ways to specify the zones: (1) inherit the marked class from <see cref="IRequire{TZoneInterface}" /> of the zones, and (2) pass zone types as attribute constructor parameters.</para>
  /// </summary>
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
  [MeansImplicitUse]
  public class ZoneAttribute : Attribute
  {
  }
}