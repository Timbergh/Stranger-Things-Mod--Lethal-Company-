using BepInEx;
using HarmonyLib;
using BepInEx.Logging;
using BepInEx.Configuration;
using System.Reflection;
using UnityEngine;

namespace StrangerThingsMod
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGUID = "scoliosis.strangerthingsmod";
        public const string ModName = "StrangerThingsMod";
        public const string ModVersion = "0.0.3";

        public static ManualLogSource logger;
        public static ConfigFile config;

        private void Awake()
        {
            logger = Logger;
            config = Config;

            StrangerThingsMod.Config.Load();
            Content.Load();

            var harmony = new Harmony(ModGUID);
            harmony.PatchAll();

            Logger.LogInfo("Loaded Stranger Things Mod");

            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }
    }
}
