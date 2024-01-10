using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LethalLib;
using LethalLib.Modules;
using UnityEngine;
using static LethalLib.Modules.ContentLoader;


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

        public static void Load()
        {
            TryLoadAssets();

            customEnemies = new List<CustomEnemy>()
            {
                CustomEnemy.Add("Demogorgon", "Assets/Demogorgon/Demogorgon.asset", Config.demogorgonSpawnWeight.Value, Levels.LevelTypes.All, Enemies.SpawnType.Default, "Assets/Demogorgon/Bestiary/DemogorgonTK.asset", "Assets/Demogorgon/Bestiary/DemogorgonTN.asset", enabled: true),
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

            // loop through prefabs
            foreach (var prefabSet in Prefabs)
            {
                var prefab = prefabSet.Value;

                // get prefab name
                var prefabName = prefabSet.Key;


                // get all AudioSources
                var audioSources = prefab.GetComponentsInChildren<AudioSource>();

                // if has any AudioSources

                // if( audioSources.Length > 0)
                // {
                //     var configValue = Config.VolumeConfig.Bind<float>("Volume", $"{prefabName}", 100f, $"Audio volume for {prefabName} (0 - 100)");

                //     // loop through AudioSources, adjust volume by multiplier
                //     foreach (var audioSource in audioSources)
                //     {
                //         audioSource.volume *= (configValue.Value / 100);
                //     }
                // }
            }

            Plugin.logger.LogInfo("Loaded content");

        }
    }
}