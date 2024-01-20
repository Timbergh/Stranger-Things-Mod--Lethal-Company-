using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;
using BepInEx.Logging;
using StrangerThingsMod;
using System.Collections;
using DunGen;

namespace StrangerThingsMod
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class LightColliderSpawnerPatch
    {
        public static List<GameObject> eligibleLights = new List<GameObject>();

        [HarmonyPatch("FinishGeneratingLevel")]
        [HarmonyPostfix]
        public static void FinishGeneratingLevelPostfix(RoundManager __instance)
        {
            Plugin.logger.LogInfo("Adding colliders to lights");
            eligibleLights.Clear(); // Clear the list to prevent duplicates
            Light[] lights = Object.FindObjectsOfType<Light>();
            GameObject[] insideNodes = __instance.insideAINodes;
            GameObject[] outsideNodes = __instance.outsideAINodes;

            foreach (Light light in lights)
            {
                Transform parent = light.transform.parent;
                if (parent != null && IsIndoorLight(light, insideNodes, outsideNodes) &&
                    (parent.name.StartsWith("HangingLight") || parent.name.StartsWith("MansionWallLamp") || parent.name.StartsWith("Chandelier")))
                {
                    eligibleLights.Add(light.gameObject);
                    AddColliderToLight(light.gameObject);
                }
            }
        }

        private static bool IsIndoorLight(Light light, GameObject[] insideNodes, GameObject[] outsideNodes)
        {
            // Calculate distance to nearest indoor and outdoor nodes and decide if the light is indoors
            float minInsideDistance = FindMinDistanceToNodes(light.transform.position, insideNodes);
            float minOutsideDistance = FindMinDistanceToNodes(light.transform.position, outsideNodes);
            return minInsideDistance < minOutsideDistance;
        }

        private static float FindMinDistanceToNodes(Vector3 position, GameObject[] nodes)
        {
            float minDistance = float.MaxValue;
            foreach (GameObject node in nodes)
            {
                float distance = Vector3.Distance(position, node.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }
            return minDistance;
        }

        private static void AddColliderToLight(GameObject lightGameObject)
        {
            BoxCollider collider = lightGameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(5f, 10f, 5f);
            collider.center = new Vector3(0f, -5f, 0f);

            LightTriggerScript triggerScript = lightGameObject.AddComponent<LightTriggerScript>();
            triggerScript.Init(lightGameObject.GetComponent<Light>());
        }
    }

    public class LightTriggerScript : MonoBehaviour
    {
        private Light lightComponent;
        private bool isOnCooldown = false;
        private AudioSource audioSource;
        private AudioClip lightFlickerSound;


        public void Init(Light light)
        {
            lightComponent = light;
            audioSource = gameObject.AddComponent<AudioSource>();

            // Load audio clips
            lightFlickerSound = Content.LoadAudioClip("FlickeringLights");

            audioSource.clip = lightFlickerSound;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && !isOnCooldown)
            {
                isOnCooldown = true;
                StartCoroutine(FlickerLightAndSpawnDemogorgon());

                Plugin.logger.LogInfo("Light collider triggered");
            }
        }

        private IEnumerator FlickerLightAndSpawnDemogorgon()
        {
            // Flicker light effect
            StartCoroutine(FlickerLight());
            audioSource.Play(); // Play light flicker sound

            // Wait for flickering duration
            yield return new WaitForSeconds(5f);

            // Spawn Demogorgon
            Utilities.SpawnDemogorgonNearWall(transform.position);

            // Reset cooldown after a delay
            yield return new WaitForSeconds(115f); // 120 seconds cooldown in total
            isOnCooldown = false;
        }

        private IEnumerator FlickerLight()
        {
            float duration = 5f;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                lightComponent.enabled = !lightComponent.enabled;
                yield return new WaitForSeconds(0.2f); // Flicker interval
                elapsedTime += 0.2f;
            }

            lightComponent.enabled = true; // Ensure light is on at the end
        }
    }
}