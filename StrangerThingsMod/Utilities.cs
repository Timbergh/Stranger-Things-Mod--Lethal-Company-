using UnityEngine;
using Unity.Netcode;
using LethalLib.Modules;
using GameNetcodeStuff;

namespace StrangerThingsMod
{
    public class Utilities
    {
        public static void SpawnDemogorgon(Vector3 spawningPosition)
        {
            string prefabName = "Demogorgon";

            if (Content.Prefabs.ContainsKey(prefabName))
            {
                Vector3 spawnPosition = spawningPosition;
                Quaternion spawnRotation = Quaternion.identity;

                Plugin.logger.LogInfo($"Spawning {prefabName} at light");
                GameObject demogorgon = UnityEngine.Object.Instantiate(Content.Prefabs[prefabName], spawnPosition, spawnRotation);
                demogorgon.GetComponent<NetworkObject>().Spawn();
            }
            else
            {
                Plugin.logger.LogWarning($"Prefab {prefabName} not found!");
            }
        }
    }
}
