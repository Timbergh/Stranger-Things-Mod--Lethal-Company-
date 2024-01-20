using BepInEx;
using HarmonyLib;
using BepInEx.Logging;
using BepInEx.Configuration;

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

            var harmony = new Harmony(ModGUID);
            harmony.PatchAll();

            Logger.LogInfo("Loaded StrangerThingsMod");
        }
    }
}
