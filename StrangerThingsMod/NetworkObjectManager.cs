using System.IO;
using System.Reflection;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using LethalLib.Modules;
using static LethalLib.Modules.Levels;
using static LethalLib.Modules.Enemies;

namespace StrangerThingsMod
{
    // [HarmonyPatch]
    // public class NetworkObjectManager
    // {
    //     static GameObject networkPrefab;
    //     public static AssetBundle MainAssetBundle;
    //     [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.StartGame))]
    //     public static void SpawnNetworkHandler()
    //     {
    //         MainAssetBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "demogorgonenemy"));
    //         networkPrefab = (GameObject)MainAssetBundle.LoadAsset("Demogorgon");

    //         if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
    //         {
    //             var networkHandlerHost = Object.Instantiate(networkPrefab, Vector3.zero, Quaternion.identity);
    //             networkHandlerHost.GetComponent<NetworkObject>().Spawn();
    //         }
    //     }
    // }
}