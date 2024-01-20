using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace StrangerThingsMod
{
    public class Config
    {
        public static ConfigEntry<int> demogorgonSpawnWeight;

        public static ConfigFile VolumeConfig;

        public static ConfigEntry<string> version;

        public static void Load()
        {
            demogorgonSpawnWeight = Plugin.config.Bind<int>("Enemies", "Demogorgon", 10000, "The weight of the Demogorgon in the enemy spawn table. Higher values mean it will spawn more often.");

            version = Plugin.config.Bind<string>("Info", "Version", "0.0.1", "The version of the mod.");
        }
    }
}