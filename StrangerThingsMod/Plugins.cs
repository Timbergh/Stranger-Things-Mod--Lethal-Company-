using BepInEx;
using System.Security.Permissions;
using BepInEx.Logging;
using BepInEx.Configuration;
using UnityEngine;
using System.Reflection;
using System;

namespace StrangerThingsMod
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGUID = "tim.strangerthingsmod";
        public const string ModName = "StrangerThingsMod";
        public const string ModVersion = "0.0.1";

        public static ManualLogSource logger;
        public static ConfigFile config;

        private void Awake()
        {
            logger = Logger;
            config = Config;

            StrangerThingsMod.Config.Load();
            Content.Load();
            
            Logger.LogInfo("Loaded StrangerThingsMod");
        }
    }
}
