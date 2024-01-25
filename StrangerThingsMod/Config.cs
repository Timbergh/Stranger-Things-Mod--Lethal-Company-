using BepInEx.Configuration;

namespace StrangerThingsMod
{
    public class Config
    {
        public static ConfigEntry<int> DemogorgonSpawnWeight;
        public static ConfigEntry<bool> EnableFlickeringLights;

        public static ConfigEntry<string> version;

        public static void Load()
        {
            DemogorgonSpawnWeight = Plugin.config.Bind("Enemies", "DemogorgonSpawnWeight", 10, "The weight of the Demogorgon spawning in the level. Higher values mean it will spawn more often.");

            EnableFlickeringLights = Plugin.config.Bind("General", "FlickeringLights", true, "Enable or disable flickering lights and sound when the Demogorgon is nearby.");

            version = Plugin.config.Bind<string>("Misc", "Version", "1.0.1", "Version of the mod config.");
        }
    }
}