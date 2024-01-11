using UnityEngine;
using UnityEngine.AI;
using BepInEx.Logging;
using GameNetcodeStuff;

namespace StrangerThingsMod
{
    public class DemogorgonAI : EnemyAI
    {
        // Animation hashes
        private static readonly int IsWalking = Animator.StringToHash("IsWalking");
        private static readonly int IsRunning = Animator.StringToHash("IsRunning");
        private static readonly int IsCloseRunning = Animator.StringToHash("IsCloseRunning");

        private Animator anim;
        private AudioSource audioSource;
        public AudioClip[] sounds;
        public AudioClip hitDemogorgonSFX;
        private float soundTimer;

        private float timer; // Timer for wandering
        private bool investigating;
        private Vector3 investigatePosition;
        private float investigateTimer;

        private ManualLogSource myLogSource;

        public override void Start()
        {
            base.Start();

            myLogSource = BepInEx.Logging.Logger.CreateLogSource("Demogorgon AI");
            myLogSource.LogInfo("Demogorgon Spawned");

            anim = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();

            timer = 5f; // Wander timer
            soundTimer = 0f;
            investigating = false;
            investigateTimer = 0f;
        }

        public override void Update()
        {
            base.Update();

            timer += Time.deltaTime;
            soundTimer += Time.deltaTime;

            if (investigating)
            {
                InvestigateNoise();
            }
            else
            {
                CheckForPlayerToChase();
            }

            if (soundTimer >= 10f) // Sound interval
            {
                PlayRandomSound();
                soundTimer = 0;
            }
        }

        private void InvestigateNoise()
        {
            if (investigateTimer < 10f) // Investigate duration
            {
                if (Vector3.Distance(transform.position, investigatePosition) > 2f)
                {
                    agent.SetDestination(investigatePosition);
                    agent.speed = 4.5f; // Investigation speed
                    anim.SetBool(IsWalking, true);
                }
                investigateTimer += Time.deltaTime;
            }
            else
            {
                EndInvestigation();
            }
        }

        private void EndInvestigation()
        {
            investigating = false;
            investigateTimer = 0f;
            Wander();
        }

        public override void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesPlayedInOneSpot = 0, int noiseID = 0)
        {
            base.DetectNoise(noisePosition, noiseLoudness, timesPlayedInOneSpot, noiseID);

            if (targetPlayer == null && !investigating && noiseLoudness > 2f) // Noise threshold
            {
                investigatePosition = noisePosition;
                investigating = true;
                investigateTimer = 0f;
                myLogSource.LogInfo("Demogorgon heard a noise and is investigating!");
            }
        }

        private void CheckForPlayerToChase()
        {
            if (targetPlayer == null)
            {
                PlayerControllerB potentialTarget = CheckLineOfSightForClosestPlayer(120f, 500);
                if (potentialTarget != null)
                {
                    float distanceToPotentialTarget = Vector3.Distance(transform.position, potentialTarget.transform.position);
                    if (distanceToPotentialTarget <= 30f) // Change this to your desired chase distance
                    {
                        targetPlayer = potentialTarget;
                        BeginChase();
                    }
                }
            }
            else
            {
                ContinueOrEndChase();
            }
        }

        private void BeginChase()
        {
            myLogSource.LogInfo("Demogorgon has spotted the player! Starting to chase!");
            agent.SetDestination(targetPlayer.transform.position);
            agent.speed = 7f; // Chase speed
            anim.SetBool(IsWalking, false);
            anim.SetBool(IsRunning, true);
        }

        private void ContinueOrEndChase()
        {
            float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);
            if (distanceToPlayer > 30f) // Chase distance
            {
                EndChase();
            }
            else if (distanceToPlayer <= 15f) // Close chase distance
            {
                CloseRangeChase();
            }
            else
            {
                agent.SetDestination(targetPlayer.transform.position);
                agent.speed = 7f; // Chase speed
                anim.SetBool(IsWalking, false);
                anim.SetBool(IsRunning, true);
                anim.SetBool(IsCloseRunning, false);
            }
        }

        private void EndChase()
        {
            targetPlayer = null;
            timer = 5f; // Reset wander timer
            agent.speed = 4.5f; // Normal speed
            anim.SetBool(IsRunning, false);
            anim.SetBool(IsCloseRunning, false);
            anim.SetBool(IsWalking, true);
        }

        private void CloseRangeChase()
        {
            agent.SetDestination(targetPlayer.transform.position);
            agent.speed = 7f; // Close chase speed
            anim.SetBool(IsRunning, false);
            anim.SetBool(IsCloseRunning, true);
        }

        private void Wander()
        {
            if (timer >= 5f) // Wander timer
            {
                Vector3 newPos = RandomNavmeshLocation(10f); // Wander radius
                if (newPos != Vector3.zero)
                {
                    agent.SetDestination(newPos);
                    timer = 0;
                    anim.SetBool(IsWalking, true);
                    anim.SetBool(IsRunning, false);
                    anim.SetBool(IsCloseRunning, false);
                }
            }
        }

        private Vector3 RandomNavmeshLocation(float radius)
        {
            Vector3 randomDirection = Random.insideUnitSphere * radius;
            randomDirection += transform.position;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
            {
                return hit.position;
            }
            return Vector3.zero;
        }

        private void PlayRandomSound()
        {
            if (sounds.Length > 0)
            {
                AudioClip soundToPlay = sounds[Random.Range(0, sounds.Length)];
                audioSource.PlayOneShot(soundToPlay);
            }
        }

        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false)
        {
            base.HitEnemy(force, playerWhoHit, playHitSFX);

            if (!isEnemyDead)
            {
                creatureSFX.PlayOneShot(hitDemogorgonSFX, 1f);
                WalkieTalkie.TransmitOneShotAudio(creatureSFX, hitDemogorgonSFX);
                enemyHP -= force;
                if (IsOwner)
                {
                    if (enemyHP <= 0)
                    {
                        KillEnemyOnOwnerClient();
                        anim.SetBool(IsWalking, false);
                        anim.SetBool(IsRunning, false);
                        anim.SetBool(IsCloseRunning, false);
                        anim.SetTrigger("IsDead");
                        myLogSource.LogInfo("Demogorgon has been killed!");
                        return;
                    }
                }
            }
        }
    }
}
