using HarmonyLib;
using UnityEngine;

namespace StrangerThingsMod
{
    [HarmonyPatch]
    public class Patches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.StartGame))]
        public static void ResetAI()
        {
            // Find an existing DemogorgonAI instance in the scene
            DemogorgonAI demogorgonAI = GameObject.FindObjectOfType<DemogorgonAI>();

            // If an instance exists, reset its state for the new game round
            if (demogorgonAI != null)
            {
                demogorgonAI.InitializeDemogorgon();
            }
        }
    }
}
