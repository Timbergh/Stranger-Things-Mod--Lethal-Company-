using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LethalLib;
using LethalLib.Modules;
using UnityEngine;
using UnityEngine.VFX;
using static LethalLib.Modules.ContentLoader;

// Credit to Evaisa's LethalThings mod for this code: https://github.com/EvaisaDev/LethalThings
namespace StrangerThingsMod
{
    public class Content
    {
        public static List<CustomEnemy> customEnemies;
        public static AssetBundle MainAssets;
        public static Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();

        public class CustomEnemy
        {
            public string name;
            public string enemyPath;
            public int rarity;
            public Levels.LevelTypes levelFlags;
            public Enemies.SpawnType spawnType;
            public string infoKeyword;
            public string infoNode;
            public bool enabled = true;

            public CustomEnemy(string name, string enemyPath, int rarity, Levels.LevelTypes levelFlags, Enemies.SpawnType spawnType, string infoKeyword, string infoNode)
            {
                this.name = name;
                this.enemyPath = enemyPath;
                this.rarity = rarity;
                this.levelFlags = levelFlags;
                this.spawnType = spawnType;
                this.infoKeyword = infoKeyword;
                this.infoNode = infoNode;
            }

            public static CustomEnemy Add(string name, string enemyPath, int rarity, Levels.LevelTypes levelFlags, Enemies.SpawnType spawnType, string infoKeyword, string infoNode, bool enabled = true)
            {
                CustomEnemy enemy = new CustomEnemy(name, enemyPath, rarity, levelFlags, spawnType, infoKeyword, infoNode);
                enemy.enabled = enabled;
                return enemy;
            }
        }

        public static void TryLoadAssets()
        {
            if (MainAssets == null)
            {
                MainAssets = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "demogorgonenemy"));
                Plugin.logger.LogInfo("Loaded asset bundle");
            }
        }

        public static AudioClip LoadAudioClip(string clipName)
        {
            if (MainAssets == null)
            {
                Plugin.logger.LogWarning("MainAssets is null, cannot load audio clip.");
                return null;
            }

            AudioClip clip = MainAssets.LoadAsset<AudioClip>(clipName);
            if (clip == null)
            {
                Plugin.logger.LogWarning($"Failed to load audio clip: {clipName}");
            }
            return clip;
        }

        public static void Load()
        {
            TryLoadAssets();

            customEnemies = new List<CustomEnemy>()
            {
                CustomEnemy.Add("Demogorgon", "Assets/Demogorgon/Demogorgon.asset", 10, Levels.LevelTypes.All, Enemies.SpawnType.Default, null, "DemogorgonTN", enabled: true),
            };

            foreach (var enemy in customEnemies)
            {
                if (!enemy.enabled)
                {
                    continue;
                }

                var enemyAsset = MainAssets.LoadAsset<EnemyType>(enemy.enemyPath);
                var enemyInfo = MainAssets.LoadAsset<TerminalNode>(enemy.infoNode);
                TerminalKeyword enemyTerminal = null;
                if (enemy.infoKeyword != null)
                {
                    enemyTerminal = MainAssets.LoadAsset<TerminalKeyword>(enemy.infoKeyword);
                }

                NetworkPrefabs.RegisterNetworkPrefab(enemyAsset.enemyPrefab);

                Prefabs.Add(enemy.name, enemyAsset.enemyPrefab);

                Enemies.RegisterEnemy(enemyAsset, enemy.rarity, enemy.levelFlags, enemy.spawnType, enemyInfo, enemyTerminal);
            }

            Plugin.logger.LogInfo("Loaded content");

        }
    }
}