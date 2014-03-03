namespace System.Zones
{
  /// <summary>
  ///   <para>An interface to mark some class as requiring a zone.</para>
  ///   <para>Applies either to a component class, or to a special marker class which marks the whole namespace it's placed in.</para>
  /// </summary>
  /// <typeparam name="TZoneInterface">The zone.</typeparam>
  public interface IRequire<TZoneInterface> where TZoneInterface : IZone
  {
  }
}