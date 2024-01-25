using UnityEngine;
using UnityEngine.AI;
using BepInEx.Logging;
using GameNetcodeStuff;
using Unity.Netcode;
using HarmonyLib;
using System.Collections;
using LethalLib.Modules;

namespace StrangerThingsMod
{
    public class DemogorgonAI : EnemyAI
    {
        private Animator anim;
        private AudioSource audioSource;
        public AudioClip[] sounds;
        public AudioClip hitDemogorgonSFX;
        private AudioClip demogorgonSpawnSound;
        public AudioClip[] screamSounds;
        private float soundTimer;

        public AISearchRoutine roamMap;
        private PlayerControllerB closestSeenPlayer;
        private PlayerControllerB lastSeenPlayer;
        private bool playerIsInLOS;
        private bool isSwitchingToWandering = false;
        private bool isCarryingPlayer = false;
        private PlayerControllerB carriedPlayer = null;
        public Transform carryPoint;
        private Coroutine damageCoroutine;
        private float pickupCooldownTimer = 0f;
        private bool isFleeing = false;
        private float fleeCooldownTimer = 0f;
        private ManualLogSource myLogSource;

        private bool hasStartedChasing = false;
        public AudioClip[] stepSounds;
        private Vector3 lastStepPosition;
        private float stepDistance = 2f;
        System.Random enemyRandom;

        public enum DemogorgonState
        {
            Wandering,
            Chasing,
            CarryingPlayer,
            Fleeing
        }

        public override void Start()
        {
            base.Start();

            myLogSource = BepInEx.Logging.Logger.CreateLogSource("Demogorgon AI");
            myLogSource.LogInfo("Demogorgon Spawned");

            anim = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();

            lastStepPosition = transform.position;

            stepSounds = new AudioClip[5];
            for (int i = 0; i < 5; i++)
            {
                stepSounds[i] = Content.LoadAudioClip("Footstep" + (i + 1));
            }

            demogorgonSpawnSound = Content.LoadAudioClip("DemogorgonSpawn");
            audioSource.PlayOneShot(demogorgonSpawnSound);


            if (Config.EnableFlickeringLights.Value)
            {
                LightTriggerScript[] lightTriggerScripts = FindObjectsOfType<LightTriggerScript>();
                foreach (LightTriggerScript lightTriggerScript in lightTriggerScripts)
                {
                    lightTriggerScript.AddDemogorgon(gameObject);
                }
            }

            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);

            currentBehaviourStateIndex = (int)DemogorgonState.Wandering;

            InitializeDemogorgon();
        }

        public void InitializeDemogorgon()
        {
            enemyHP = 6;
            soundTimer = 0f;
            isCarryingPlayer = false;
            carriedPlayer = null;
            fleeCooldownTimer = 0f;
            pickupCooldownTimer = 0f;
            agent.speed = 4.5f;
            hasStartedChasing = false;
            isFleeing = false;
            isSwitchingToWandering = false;
            closestSeenPlayer = null;
            lastSeenPlayer = null;
            playerIsInLOS = false;
            CarriedPlayerManager.ClearCarriedPlayer();

            currentBehaviourStateIndex = (int)DemogorgonState.Wandering;

            anim.SetBool("IsWalking", false);
            anim.SetBool("IsRunning", false);
            anim.SetBool("IsCloseRunning", false);
            anim.SetBool("IsCarrying", false);
        }

        public override void Update()
        {
            base.Update();

            if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
            {
                return;
            }

            soundTimer += Time.deltaTime;

            if (soundTimer >= 5f) // Sound interval
            {
                PlayRandomSound();
                soundTimer = 0;
            }

            if (pickupCooldownTimer > 0f)
            {
                pickupCooldownTimer -= Time.deltaTime;
            }

            if (fleeCooldownTimer > 0f)
            {
                fleeCooldownTimer -= Time.deltaTime;
            }

            if (isCarryingPlayer && carriedPlayer != null)
            {
                RunAway();
                UpdateCarriedPlayerPositionServerRpc(carryPoint.position, carryPoint.rotation);
                carriedPlayer.ResetFallGravity();
            }

            if (Vector3.Distance(transform.position, lastStepPosition) > stepDistance)
            {
                OnStep();
                lastStepPosition = transform.position;
            }

            if (stunNormalizedTimer > 0f)
            {
                agent.speed = 0f;
                ReleasePlayerServerRpc();
            }
        }

        private void OnStep()
        {
            AudioClip stepSound = stepSounds[enemyRandom.Next(0, stepSounds.Length)];
            audioSource.PlayOneShot(stepSound, 0.4f);
        }

        public override void DoAIInterval()
        {
            base.DoAIInterval();

            if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
            {
                return;
            }

            PlayerControllerB playerControllerB = null;
            switch (currentBehaviourStateIndex)
            {
                case (int)DemogorgonState.Wandering:
                    agent.speed = 4.5f;
                    DoAnimationsServerRPC(true, false, false, false);
                    if (!roamMap.inProgress && !isCarryingPlayer && !isEnemyDead && !isFleeing)
                    {
                        StartSearch(transform.position, roamMap);
                        myLogSource.LogInfo("Starting search...");
                    }
                    playerControllerB = CheckLineOfSightForPlayer(140f, 25, 6);
                    playerIsInLOS = playerControllerB;
                    if (playerIsInLOS && !playerControllerB.isPlayerDead && !isCarryingPlayer && !isEnemyDead && CarriedPlayerManager.CarriedPlayer != playerControllerB)
                    {
                        ChangeOwnershipOfEnemy(playerControllerB.actualClientId);
                        SwitchToBehaviourClientRpc((int)DemogorgonState.Chasing);
                    }
                    break;
                case (int)DemogorgonState.Chasing:
                    if (roamMap.inProgress)
                    {
                        StopSearch(roamMap);
                    }
                    closestSeenPlayer = CheckLineOfSightForClosestPlayer(140f, 25, 6);
                    if (closestSeenPlayer != null)
                    {
                        lastSeenPlayer = closestSeenPlayer;
                    }
                    playerIsInLOS = closestSeenPlayer;
                    if (playerIsInLOS && !closestSeenPlayer.isPlayerDead && !isCarryingPlayer && !isEnemyDead && CarriedPlayerManager.CarriedPlayer != closestSeenPlayer)
                    {
                        agent.speed = 7f;
                        if (!hasStartedChasing)
                        {
                            int randomSoundIndex = enemyRandom.Next(0, screamSounds.Length);
                            audioSource.PlayOneShot(screamSounds[randomSoundIndex]);
                            WalkieTalkie.TransmitOneShotAudio(audioSource, screamSounds[randomSoundIndex]);
                            hasStartedChasing = true;
                        }

                        if (closestSeenPlayer != null)
                        {
                            SetDestinationToPosition(closestSeenPlayer.transform.position);
                        }
                        myLogSource.LogInfo("Chasing player! " + closestSeenPlayer.name);
                        DoAnimationsServerRPC(false, false, true, false);

                        isSwitchingToWandering = false;
                    }
                    else
                    {
                        if (lastSeenPlayer != null)
                        {
                            SetDestinationToPosition(lastSeenPlayer.transform.position);
                        }
                        if (!isSwitchingToWandering)
                        {
                            myLogSource.LogInfo("Lost sight of player, loosing interest...");
                            StartCoroutine(SwitchToWanderingIfLostPlayer(5f));
                            isSwitchingToWandering = true;
                        }
                    }
                    break;
                case (int)DemogorgonState.CarryingPlayer:
                    agent.speed = 5f;

                    if (carriedPlayer == null || carriedPlayer.isPlayerDead || isEnemyDead)
                    {
                        myLogSource.LogInfo("Player is dead or enemy is dead or carriedPlayer is null, releasing player...");
                        ReleasePlayerServerRpc();
                    }
                    break;
                case (int)DemogorgonState.Fleeing:
                    if (fleeCooldownTimer <= 0f)
                    {
                        isFleeing = false;
                    }
                    if (isFleeing)
                    {
                        RunAway();
                    }
                    else
                    {
                        SwitchToBehaviourClientRpc((int)DemogorgonState.Wandering);
                    }
                    break;
                default:
                    myLogSource.LogWarning("Demogorgon state not found!");
                    break;
            }
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            base.OnCollideWithPlayer(other);
            if (isEnemyDead || isCarryingPlayer)
            {
                return;
            }

            PlayerControllerB collidedPlayer = other.gameObject.GetComponent<PlayerControllerB>();

            if (pickupCooldownTimer <= 0f && !isCarryingPlayer && !collidedPlayer.isPlayerDead && CarriedPlayerManager.CarriedPlayer != collidedPlayer)
            {
                myLogSource.LogInfo("Picking up player! " + collidedPlayer.name);
                GrabPlayerServerRpc(collidedPlayer.playerClientId);
                SwitchToBehaviourClientRpc((int)DemogorgonState.CarryingPlayer);
            }

        }

        IEnumerator SwitchToWanderingIfLostPlayer(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (isSwitchingToWandering)
            {
                SwitchToBehaviourClientRpc((int)DemogorgonState.Wandering);
                myLogSource.LogInfo("Switching to wandering...");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void DoAnimationsServerRPC(bool isWalking, bool isRunning, bool isCloseRunning, bool isCarrying)
        {
            SetAnimationState(isWalking, isRunning, isCloseRunning, isCarrying);

            DoAnimationsClientRPC(isWalking, isRunning, isCloseRunning, isCarrying);
        }

        [ClientRpc]
        public void DoAnimationsClientRPC(bool isWalking, bool isRunning, bool isCloseRunning, bool isCarrying)
        {
            SetAnimationState(isWalking, isRunning, isCloseRunning, isCarrying);
        }

        private void SetAnimationState(bool isWalking, bool isRunning, bool isCloseRunning, bool isCarrying)
        {
            anim.SetBool("IsWalking", isWalking);
            anim.SetBool("IsRunning", isRunning);
            anim.SetBool("IsCloseRunning", isCloseRunning);
            anim.SetBool("IsCarrying", isCarrying);
        }

        [ServerRpc(RequireOwnership = false)]
        public void GrabPlayerServerRpc(ulong playerId)
        {
            // Find the player using the player's ID
            var player = StartOfRound.Instance.allPlayerScripts[playerId];

            this.carriedPlayer = player;

            // Pass the player's ID to the ClientRpc method
            GrabPlayerClientRpc(playerId);
        }

        [ClientRpc]
        public void GrabPlayerClientRpc(ulong playerId)
        {
            // Find the player using the player's ID
            var carriedPlayer = StartOfRound.Instance.allPlayerScripts[playerId];

            if (carriedPlayer != null)
            {
                isCarryingPlayer = true;
                this.carriedPlayer = carriedPlayer;
                CarriedPlayerManager.SetCarriedPlayer(carriedPlayer);
                carriedPlayer.playerActions.Movement.Move.Disable();
                carriedPlayer.playerActions.Movement.Jump.Disable();
                carriedPlayer.playerActions.Movement.Sprint.Disable();
                carriedPlayer.playerActions.Movement.Crouch.Disable();
                DoAnimationsServerRPC(false, false, false, true);
                damageCoroutine = StartCoroutine(DamagePlayerOverTime());

                carriedPlayer.transform.position = carryPoint.position;
                carriedPlayer.transform.rotation = carryPoint.rotation;
                carriedPlayer.ResetFallGravity();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ReleasePlayerServerRpc()
        {
            ReleasePlayer();

            ReleasePlayerClientRpc();
        }

        [ClientRpc]
        public void ReleasePlayerClientRpc()
        {
            ReleasePlayer();
        }

        private void ReleasePlayer()
        {
            if (isCarryingPlayer && carriedPlayer != null)
            {
                carriedPlayer.transform.parent = null;
                carriedPlayer.playerActions.Movement.Move.Enable();
                carriedPlayer.playerActions.Movement.Jump.Enable();
                carriedPlayer.playerActions.Movement.Sprint.Enable();
                carriedPlayer.playerActions.Movement.Crouch.Enable();
                carriedPlayer.takingFallDamage = true;
                DoAnimationsServerRPC(true, false, false, false);
                StopCoroutine(damageCoroutine);
                isCarryingPlayer = false;
                CarriedPlayerManager.ClearCarriedPlayer();
                pickupCooldownTimer = 5f;
                isFleeing = true;
                fleeCooldownTimer = 5f;
                carriedPlayer = null;
                SwitchToBehaviourClientRpc((int)DemogorgonState.Fleeing);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateCarriedPlayerPositionServerRpc(Vector3 position, Quaternion rotation)
        {
            carriedPlayer.transform.position = position;
            carriedPlayer.transform.rotation = rotation;

            UpdateCarriedPlayerPositionClientRpc(position, rotation);
        }

        [ClientRpc]
        public void UpdateCarriedPlayerPositionClientRpc(Vector3 position, Quaternion rotation)
        {
            carriedPlayer.transform.position = position;
            carriedPlayer.transform.rotation = rotation;
        }

        private void RunAway()
        {
            if (isEnemyDead || agent.hasPath || agent.pathPending || fleeCooldownTimer > 0f || !agent.isOnNavMesh)
            {
                return;
            }

            Transform runAwayTransform = ChooseFarthestNodeFromPosition(transform.position, avoidLineOfSight: true);
            if (runAwayTransform != null)
            {
                targetNode = runAwayTransform;
                SetDestinationToPosition(targetNode.position);
                return;
            }
        }

        private IEnumerator DamagePlayerOverTime()
        {
            while (isCarryingPlayer)
            {
                yield return new WaitForSeconds(1f); // Damage interval
                if (carriedPlayer != null)
                {
                    carriedPlayer.DamagePlayer(15); // Damage amount
                }
            }
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
                if (isCarryingPlayer)
                {
                    ReleasePlayerServerRpc();
                }
                if (IsOwner)
                {
                    if (enemyHP <= 0 && !isEnemyDead)
                    {
                        KillEnemyOnOwnerClient();
                        return;
                    }
                }
            }
        }
    }
}
