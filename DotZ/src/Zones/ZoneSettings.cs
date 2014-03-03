using JetBrains.Application.Settings;

namespace DotZ.Zones
{
  [SettingsKey(typeof(EnvironmentSettings), "Zone settings")]
  public class ZoneSettings
  {
    [SettingsEntry(true, "Zones Support Is Enables")] 
    public bool CheckZones;
  }
}