using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;
using System.Collections;
using GameNetcodeStuff;
using UnityEngine.InputSystem;

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
            eligibleLights.Clear();
            Light[] lights = Object.FindObjectsOfType<Light>();

            foreach (Light light in lights)
            {
                Transform parent = light.transform.parent;
                if (parent != null &&
                    (parent.name.StartsWith("HangingLight") || parent.name.StartsWith("MansionWallLamp") || parent.name.StartsWith("Chandelier")))
                {
                    eligibleLights.Add(light.gameObject);

                    LightTriggerScript lightTriggerScript = light.gameObject.AddComponent<LightTriggerScript>();
                    lightTriggerScript.Init(light);
                }
            }
        }
    }

    public class LightTriggerScript : MonoBehaviour
    {
        private Light lightComponent;
        private AudioSource audioSource;
        private AudioClip[] lightFlickerSounds;
        private List<GameObject> demogorgons = new List<GameObject>();
        private bool isFlickering = false;
        private bool hasExited = true;

        public Vector3 spawnPosition = PlayerControllerB.FindAnyObjectByType<PlayerControllerB>().transform.position;

        public void Init(Light light)
        {
            lightComponent = light;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f;

            lightFlickerSounds = new AudioClip[]
            {
                Content.LoadAudioClip("BreakerLever1"),
                Content.LoadAudioClip("BreakerLever2"),
                Content.LoadAudioClip("BreakerLever3"),
                Content.LoadAudioClip("FlashlightFlicker"),
            };
        }

        public void AddDemogorgon(GameObject demogorgon)
        {
            demogorgons.Add(demogorgon);
            Debug.Log("Demogorgon added: " + demogorgon);
        }

        private void Update()
        {
            foreach (var demogorgon in demogorgons)
            {
                if (demogorgon == null)
                {
                    continue;
                }
                float distance = Vector3.Distance(transform.position, demogorgon.transform.position);

                if (distance < 5f)
                {
                    if (!isFlickering && hasExited)
                    {
                        Debug.Log("Starting FlickerLight coroutine");
                        StartCoroutine(FlickerLight());
                        break;
                    }
                }
                else
                {
                    hasExited = true;
                }
            }
        }

        private IEnumerator FlickerLight()
        {
            Debug.Log("In FlickerLight coroutine");
            isFlickering = true;
            hasExited = false;
            int flickerCount = Random.Range(1, 5);

            for (int i = 0; i < flickerCount; i++)
            {
                // Play a random sound each flicker
                audioSource.PlayOneShot(lightFlickerSounds[Random.Range(0, lightFlickerSounds.Length)]);
                lightComponent.enabled = !lightComponent.enabled;
                yield return new WaitForSeconds(Random.Range(0.08f, 0.3f));
            }

            lightComponent.enabled = true;
            isFlickering = false;
        }
    }
}