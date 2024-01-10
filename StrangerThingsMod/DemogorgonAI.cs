using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using BepInEx.Logging;

namespace StrangerThingsMod
{
    public class DemogorgonAI : EnemyAI // Assuming EnemyAI is the base class
    {
        [Header("Movement")]
        public float wanderRadius = 10f;
        public float wanderTimer = 5f;
        private float timer;

        [Header("Audio")]
        public AudioClip[] sounds;
        private AudioSource audioSource;
        public float soundInterval = 10f;
        private float soundTimer;

        private ManualLogSource myLogSource;

        // Start is overridden from EnemyAI
        public override void Start()
        {
            base.Start(); // Call the base start to initialize the agent and other components

            myLogSource = BepInEx.Logging.Logger.CreateLogSource("Demogorgon AI");
            myLogSource.LogInfo("Demogorgon Spawned");

            audioSource = GetComponent<AudioSource>();
            timer = wanderTimer;
            soundTimer = soundInterval;
        }

        // Update is overridden from EnemyAI
        public override void Update()
        {
            base.Update(); // Call the base update to handle common update logic

            timer += Time.deltaTime;
            soundTimer += Time.deltaTime;

            if (timer >= wanderTimer)
            {
                Vector3 newPos = RandomNavmeshLocation(wanderRadius);
                agent.SetDestination(newPos); // Use the agent from the base class
                timer = 0;
            }

            if (soundTimer >= soundInterval)
            {
                PlayRandomSound();
                soundTimer = 0;
            }
        }

        // RandomNavmeshLocation is a new method for DemogorgonAI
        private Vector3 RandomNavmeshLocation(float radius)
        {
            Vector3 randomDirection = Random.insideUnitSphere * radius;
            randomDirection += transform.position;
            NavMeshHit hit;
            Vector3 finalPosition = Vector3.zero;
            if (NavMesh.SamplePosition(randomDirection, out hit, radius, -1))
            {
                finalPosition = hit.position;
            }
            return finalPosition;
        }

        // PlayRandomSound is a new method for DemogorgonAI
        private void PlayRandomSound()
        {
            if (sounds.Length > 0)
            {
                AudioClip soundToPlay = sounds[Random.Range(0, sounds.Length)];
                audioSource.PlayOneShot(soundToPlay);
            }
        }
    }
}
