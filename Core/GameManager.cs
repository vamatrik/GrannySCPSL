using PlayerRoles.FirstPersonControl;
using MapGeneration;
using LabApi.Features.Wrappers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MEC;
using LabApi.Features;
using PlayerRoles;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using AdminToys;

namespace GrannySCPSL.Core
{
    public class GameManager
    {
        public static GameManager Instance { get; } = new GameManager();
        public bool GameStarted { get; private set; }
        public bool GameEnded { get; private set; }
        public static int CurrentDay = 1;
        
        private MEC.CoroutineHandle _blackoutHandle;
        private MEC.CoroutineHandle _winConditionHandle;
        private MEC.CoroutineHandle _mechanicsHandle;
        private MEC.CoroutineHandle _waitingHandle;
        private MEC.CoroutineHandle _cutsceneHandle;
        public static HashSet<string> ActivePlayers = new HashSet<string>();

        public static bool PT00ExplosivePlaced = false;
        public static bool PT00Activated = false;
        public static int TC01PuzzlesCollected = 0;
        public static bool TC01Unlocked = false;
        
        public static Dictionary<int, float> playerStillTime = new Dictionary<int, float>();
        public static Dictionary<int, Vector3> playerLastPos = new Dictionary<int, Vector3>();

        public void OnWaitingForPlayers()
        {
            GameStarted = false;
            EventManager.ResetEvents();
            GameEnded = false;
            ActivePlayers.Clear();
            CurrentDay = 1;
            MEC.Timing.KillCoroutines(_waitingHandle);
            MEC.Timing.KillCoroutines(_blackoutHandle);
            MEC.Timing.KillCoroutines(_winConditionHandle);
            MEC.Timing.KillCoroutines(_mechanicsHandle);
            MEC.Timing.KillCoroutines(_cutsceneHandle);
            _waitingHandle = Timing.RunCoroutine(WaitingCoroutine());
            
            try
            {
                LightContainmentZoneDecontamination.DecontaminationController.Singleton.DecontaminationOverride = LightContainmentZoneDecontamination.DecontaminationController.DecontaminationStatus.Disabled;
            }
            catch (System.Exception ex)
            {
                LabApi.Features.Console.Logger.Error("Could not disable decontamination: " + ex.Message);
            }
        }
        
        public void OnPlayerJoined(LabApi.Events.Arguments.PlayerEvents.PlayerJoinedEventArgs ev)
        {
            if (!GameStarted && !LabApi.Features.Wrappers.Round.IsRoundStarted)
            {
                ev.Player.SetRole(RoleTypeId.Tutorial, RoleChangeReason.None);
            }
            if (ev.Player != null)
            {
                TranslationManager.SetLanguage(ev.Player, "ru");
                ev.Player.SendConsoleMessage("Для перевода на английский напишите .en (в консоль на ~) | To change language to English, type .en", "green");
            }
        }
        
        private IEnumerator<float> WaitingCoroutine()
        {
            while (!GameStarted)
            {
                var players = Player.GetAll();
                if (players.Any() && !LabApi.Features.Wrappers.Round.IsRoundStarted)
                {
                    try { CharacterClassManager.ForceRoundStart(); } catch { }
                }
                foreach(var player in players)
                {
                    player.SendBroadcast(TranslationManager.GetString("waiting_players", player), 1, shouldClearPrevious: true);
                    if (player.Role != RoleTypeId.Tutorial)
                    {
                        player.SetRole(RoleTypeId.Tutorial, RoleChangeReason.None);
                    }
                }
                yield return Timing.WaitForSeconds(1f);
            }
        }
        
        public void OnRoundStarted() 
        {
            try
            {
                RoundSummary.RoundLock = true;
                LabApi.Features.Console.Logger.Info("Round locked by GrannyMinigame.");
            }
            catch (System.Exception ex) { }
            
            var players = Player.GetAll();
                if (players.Any())
                {
                    try { CharacterClassManager.ForceRoundStart(); } catch { }
                }
                foreach(var player in players)
            {
                player.SetRole(RoleTypeId.Tutorial, RoleChangeReason.None);
            }
            
            try 
            {
                LightContainmentZoneDecontamination.DecontaminationController.Singleton.DecontaminationOverride = LightContainmentZoneDecontamination.DecontaminationController.DecontaminationStatus.None;
                LightContainmentZoneDecontamination.DecontaminationController.Singleton.RoundStartTime = 9999999999d;
            } catch { }
        }

        public void StartGame()
        {
            if (GameStarted) return;
            GameStarted = true;
            EventManager.StartEvents();
            Respawning.WaveManager.Waves.Clear();
            ActivePlayers.Clear();
            CurrentDay = 1;
            
            PT00ExplosivePlaced = false;
            PT00Activated = false;
            TC01PuzzlesCollected = 0;
            TC01Unlocked = false;
            
            ItemManager.InitRound();
            GateAMechanics.InitRound();
            
            if (AI.GrannyAI.Instance != null)
                AI.GrannyAI.Instance.IsCutscene = false;

            // Clear items and ragdolls for a fresh round
            foreach (var door in UnityEngine.Object.FindObjectsOfType<Interactables.Interobjects.DoorUtils.DoorVariant>())
            {
                if (door is Interactables.Interobjects.BreakableDoor breakable)
                {
                    breakable.IsDestroyed = false;
                }
                
                try {
                    if (Scp914.Scp914Controller.Singleton != null && Vector3.Distance(door.transform.position, Scp914.Scp914Controller.Singleton.transform.position) < 12f)
                    {
                        if (door.GetType() == typeof(Interactables.Interobjects.BasicDoor)) continue;
                    }
                } catch { }

                if (door.gameObject.name.Contains("914 Door"))
                {
                    door.NetworkTargetState = true;
                    door.ServerChangeLock(Interactables.Interobjects.DoorUtils.DoorLockReason.SpecialDoorFeature, false);
                    continue;
                }
                door.NetworkTargetState = false;
                door.ServerChangeLock(Interactables.Interobjects.DoorUtils.DoorLockReason.SpecialDoorFeature, false);
                
                if (Vector3.Distance(door.transform.position, new Vector3(112.05f, 100.967f, 105.007f)) < 5f ||
                    Vector3.Distance(door.transform.position, new Vector3(37.964f, 100.967f, 59.974f)) < 5f ||
                    Vector3.Distance(door.transform.position, new Vector3(105.9639f, 99.969f, 72.38874f)) < 5f ||
                    Vector3.Distance(door.transform.position, new Vector3(105.964f, 99.969f, 75.35528f)) < 5f)
                {
                    door.ServerChangeLock(Interactables.Interobjects.DoorUtils.DoorLockReason.AdminCommand, true);
                }
            }
            
            // Re-apply special locks for regular breakable doors if needed
            foreach (var door in UnityEngine.Object.FindObjectsOfType<Interactables.Interobjects.BreakableDoor>())
            {
                if (door.IsDestroyed) continue;
                door.NetworkTargetState = false;
            }

            foreach (var pickup in UnityEngine.Object.FindObjectsOfType<InventorySystem.Items.Pickups.ItemPickupBase>())
                pickup.DestroySelf();
            foreach (var ragdoll in UnityEngine.Object.FindObjectsOfType<PlayerRoles.Ragdolls.BasicRagdoll>())
                Mirror.NetworkServer.Destroy(ragdoll.gameObject);
                
            ItemSpawner.SpawnAllItems();

            foreach (var player in Player.GetAll())
            {
                if (player.Role != RoleTypeId.Scp939 && !player.UserId.Contains("@server"))
                {
                    ActivePlayers.Add(player.PlayerId.ToString());
                    player.SetRole(RoleTypeId.ClassD, RoleChangeReason.RoundStart);
                    player.Health = 100;
                    player.ClearInventory();
                    TranslationManager.BroadcastAll("day_1", 5, shouldClearPrevious: true);
                }
            }

            var grannySpawnRoom = MapGeneration.RoomIdentifier.AllRoomIdentifiers.FirstOrDefault(r => r.Name == MapGeneration.RoomName.LczComputerRoom);
            Vector3 grannySpawn = grannySpawnRoom != null ? grannySpawnRoom.transform.position + Vector3.up : new Vector3(5, 1, 5);

            Timing.CallDelayed(2f, () => 
            {
                AI.GrannyAI.Instance.SpawnGranny(grannySpawn);
            });
            
            MEC.Timing.KillCoroutines(_winConditionHandle);
            MEC.Timing.KillCoroutines(_blackoutHandle);
            MEC.Timing.KillCoroutines(_mechanicsHandle);
            _winConditionHandle = Timing.RunCoroutine(WinConditionCoroutine());
            _blackoutHandle = Timing.RunCoroutine(BlackoutCoroutine());
            _mechanicsHandle = Timing.RunCoroutine(MechanicsTick());
        }

        private IEnumerator<float> WinConditionCoroutine()
        {
            while (GameStarted)
            {
                yield return Timing.WaitForSeconds(1f);
                foreach (var player in Player.GetAll().Where(p => p.IsAlive && p.Role != RoleTypeId.Scp939))
                {
                    if (player.Zone == FacilityZone.HeavyContainment)
                    {
                        _cutsceneHandle = Timing.RunCoroutine(VictoryCutscene(player));
                        yield break;
                    }
                }
            }
        }
        private IEnumerator<float> BlackoutCoroutine()
        {
            while (GameStarted)
            {
                yield return Timing.WaitForSeconds(60f);
                if (!GameStarted) break;
                if (UnityEngine.Random.Range(0, 100) < 60)
                {
                    try
                    {
                        LabApi.Features.Wrappers.Announcer.Message(@"$PITCH_0.3 .g4 $PITCH_0.2 . .g4 $PITCH_0.15 . .g4", "ВНИМАНИЕ. ЭЛЕКТРОСНАБЖЕНИЕ КОМПЛЕКСА ПЕРЕГРУЖЕНО. ПЕРЕЗАГРУЗКА", false, 0f, 1f);
                        LabApi.Features.Wrappers.Map.TurnOffLights(10f, MapGeneration.FacilityZone.LightContainment);
                    }
                    catch { }
                }
            }
        }


        private IEnumerator<float> MechanicsTick()
        {
            while (GameStarted)
            {
                yield return Timing.WaitForSeconds(1f);
                
                GateAMechanics.MechanicsTick();
                
                foreach (var p in Player.GetAll()) {
                    if (p.Role != RoleTypeId.Spectator && p.Role != RoleTypeId.Tutorial && p.Role != RoleTypeId.Scp939 && !ActivePlayers.Contains(p.PlayerId.ToString())) {
                        p.SetRole(RoleTypeId.Spectator, RoleChangeReason.None);
                    }
                    else if (p.IsAlive && ActivePlayers.Contains(p.PlayerId.ToString()))
                    {
                        int pid = p.PlayerId;
                        if (!playerLastPos.ContainsKey(pid)) playerLastPos[pid] = p.Position;
                        if (!playerStillTime.ContainsKey(pid)) playerStillTime[pid] = 0f;
                        
                        if (Vector3.Distance(playerLastPos[pid], p.Position) < 0.1f)
                        {
                            playerStillTime[pid] += 1f;
                        }
                        else
                        {
                            playerStillTime[pid] = 0f;
                        }
                        playerLastPos[pid] = p.Position;
                        
                        if (playerStillTime[pid] >= 5.0f)
                        {
                            try { p.ReferenceHub.playerEffectsController.EnableEffect<CustomPlayerEffects.Sinkhole>(1f, true); } catch { }
                        }
                        else
                        {
                            try { p.ReferenceHub.playerEffectsController.DisableEffect<CustomPlayerEffects.Sinkhole>(); } catch { }
                        }
                    }
                }
                
                foreach (var pickup in LabApi.Features.Wrappers.Map.Pickups.ToList())
                {
                    if (Vector3.Distance(pickup.Position, new Vector3(112.05f, 100.967f, 105.007f)) < 3f)
                    {
                        if (!PT00ExplosivePlaced && pickup.Type == ItemType.GunShotgun)
                        {
                            PT00ExplosivePlaced = true;
                            pickup.Destroy();
                            foreach(var p in Player.GetAll().Where(p => Vector3.Distance(p.Position, new Vector3(112.05f, 100.967f, 105.007f)) < 15f))
                            {
                                p.SendHint("Взрывчатка установлена!\nНажмите на дверь с огнивом в руках, чтобы подорвать.", 5f);
                            }
                        }
                    }
                    
                    if (!TC01Unlocked && Vector3.Distance(pickup.Position, new Vector3(37.964f, 100.967f, 59.974f)) < 3f)
                    {
                        if (ItemSpawner.PuzzleSerials.Contains(pickup.Serial))
                        {
                            ItemSpawner.PuzzleSerials.Remove(pickup.Serial);
                            pickup.Destroy();
                            TC01PuzzlesCollected++;
                            
                            foreach(var p in Player.GetAll().Where(p => Vector3.Distance(p.Position, new Vector3(37.964f, 100.967f, 59.974f)) < 15f))
                            {
                                if (TC01PuzzlesCollected >= 3)
                                {
                                    p.SendHint("Все детали пазла собраны! Дверь TC-01 разблокирована.", 5f);
                                }
                                else
                                {
                                    p.SendHint($"Деталь вставлена! Собрано: {TC01PuzzlesCollected}/3", 5f);
                                }
                            }
                            
                            if (TC01PuzzlesCollected >= 3)
                            {
                                TC01Unlocked = true;
                                foreach(var door in UnityEngine.Object.FindObjectsOfType<Interactables.Interobjects.DoorUtils.DoorVariant>())
                                {
                                    if (Vector3.Distance(door.transform.position, new Vector3(37.964f, 100.967f, 59.974f)) < 5f ||
                                        Vector3.Distance(door.transform.position, new Vector3(105.9639f, 99.969f, 72.38874f)) < 5f ||
                                        Vector3.Distance(door.transform.position, new Vector3(105.964f, 99.969f, 75.35528f)) < 5f)
                                    {
                                        door.ServerChangeLock(Interactables.Interobjects.DoorUtils.DoorLockReason.AdminCommand, false);
                                        door.NetworkTargetState = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


        public IEnumerator<float> VictoryCutscene(Player winner)
        {
            GameStarted = false;
            EventManager.ResetEvents();
            
            var allActive = Player.GetAll().Where(p => ActivePlayers.Contains(p.PlayerId.ToString())).ToList();
            
            foreach(var p in allActive)
            {
                p.SetRole(RoleTypeId.ClassD, RoleChangeReason.None);
                p.ClearInventory();
                p.AddItem(ItemType.Jailbird);
                p.Position = new Vector3(39.396f, 314.112f, -35.510f);
            }
            
            AI.GrannyAI.Instance.DespawnGranny();
            
            var dummyUtilsType = System.Linq.Enumerable.FirstOrDefault(typeof(ServerConsole).Assembly.GetTypes(), t => t.Name == "DummyUtils");
            if (dummyUtilsType != null)
            {
                var method = dummyUtilsType.GetMethod("SpawnDummy", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (method != null)
                {
                    ReferenceHub cutsceneDummy = (ReferenceHub)method.Invoke(null, new object[] { "Гренни" });
                    if (cutsceneDummy != null)
                    {
                        cutsceneDummy.roleManager.ServerSetRole(RoleTypeId.Scp939, RoleChangeReason.None);
                        Player.Get(cutsceneDummy).Position = new Vector3(39.187f, 314.112f, -31.543f);
                        Player.Get(cutsceneDummy).Health = 1250f;
                        
                        Vector3 dir = (winner.Position - cutsceneDummy.transform.position).normalized;
                        dir.y = 0;
                        var q = Quaternion.LookRotation(dir);
                        cutsceneDummy.TryOverrideRotation(new Vector2(0f - q.eulerAngles.x, q.eulerAngles.y));
                        
                        if (cutsceneDummy.roleManager.CurrentRole is PlayerRoles.FirstPersonControl.FpcStandardRoleBase fpc)
                        {
                            fpc.FpcModule.MouseLook.CurrentHorizontal = q.eulerAngles.y;
                        }
                          
                        if (cutsceneDummy.roleManager.CurrentRole is PlayerRoles.PlayableScps.Scp939.Scp939Role scp939Role)
                        {
                            if (scp939Role.SubroutineModule.TryGetSubroutine<PlayerRoles.PlayableScps.Scp939.Scp939FocusKeySync>(out var focusSync))
                            {
                                var prop = typeof(PlayerRoles.PlayableScps.Scp939.Scp939FocusKeySync).GetField("<FocusKeyHeld>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                if (prop != null) prop.SetValue(focusSync, true);
                            }
                        }
        
                        var anim = cutsceneDummy.GetComponentInChildren<UnityEngine.Animator>();
                        if (anim != null) anim.Play("Sneak->Hold");
                    }
                }
            }
            
            foreach(var p in allActive)
            {
                TranslationManager.BroadcastAll("victory", 5, shouldClearPrevious: true);
            }
            yield return Timing.WaitForSeconds(5f);
            
            for (int i = 25; i > 0; i--)
            {
                foreach(var p in allActive)
                {
                    p.SendBroadcast(string.Format(TranslationManager.GetString("fun_time_left", p), i, i == 1 ? "а" : (i >= 2 && i <= 4 ? "ы" : "")), 1, shouldClearPrevious: true);
                }
                yield return Timing.WaitForSeconds(1f);
            }
            
            EndGame(winner);
        }

        public void EndGame(Player? winner)
        {
            GameStarted = false;
            EventManager.ResetEvents();
            GameEnded = true;
            RoundSummary.RoundLock = false;
            var players = Player.GetAll();
            
            var dummyUtilsType = System.Linq.Enumerable.FirstOrDefault(typeof(ServerConsole).Assembly.GetTypes(), t => t.Name == "DummyUtils");
            if (dummyUtilsType != null)
            {
                var method = dummyUtilsType.GetMethod("DestroyAllDummies", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (method != null)
                {
                    method.Invoke(null, null);
                }
            }
            
            if (players.Any())
            {
                try { CharacterClassManager.ForceRoundStart(); } catch { }
            }
                foreach(var player in players)
            {
                if (winner != null && player == winner)
                {
                    TranslationManager.BroadcastAll("victory_escaped", 10, shouldClearPrevious: true);
                }
                else
                {
                    TranslationManager.BroadcastAll("game_over", 10, shouldClearPrevious: true);
                }
                
                player.SetRole(RoleTypeId.Tutorial, RoleChangeReason.None);
            }
            
            AI.GrannyAI.Instance.DespawnGranny();
            
            // Cleanup map
            foreach (var door in UnityEngine.Object.FindObjectsOfType<Interactables.Interobjects.BreakableDoor>())
            {
                door.IsDestroyed = false;
                door.NetworkTargetState = false;
            }

            foreach (var pickup in UnityEngine.Object.FindObjectsOfType<InventorySystem.Items.Pickups.ItemPickupBase>())
                pickup.DestroySelf();
            foreach (var ragdoll in UnityEngine.Object.FindObjectsOfType<PlayerRoles.Ragdolls.BasicRagdoll>())
                Mirror.NetworkServer.Destroy(ragdoll.gameObject);
                
            try 
            {
                var forceEnd = typeof(RoundSummary).GetMethod("ForceEnd", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, new System.Type[] { typeof(RoundSummary.LeadingTeam) }, null);
                if (forceEnd != null && RoundSummary.singleton != null)
                {
                    forceEnd.Invoke(RoundSummary.singleton, new object[] { winner != null ? RoundSummary.LeadingTeam.FacilityForces : RoundSummary.LeadingTeam.Anomalies });
                }
                else
                {
                    var forceEndNoArgs = typeof(RoundSummary).GetMethod("ForceEnd", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, new System.Type[0], null);
                    if (forceEndNoArgs != null && RoundSummary.singleton != null)
                    {
                        forceEndNoArgs.Invoke(RoundSummary.singleton, null);
                    }
                }
            } catch { }
        }

        public void OnPlayerLeft(LabApi.Events.Arguments.PlayerEvents.PlayerLeftEventArgs ev)
        {
            if (!GameStarted || ev.Player == null) return;
            
            if (ActivePlayers.Contains(ev.Player.PlayerId.ToString()))
            {
                CheckEndGame(ev.Player);
            }
        }

        public void OnPlayerDied(LabApi.Events.Arguments.PlayerEvents.PlayerDeathEventArgs ev)
        {
            if (!GameStarted || ev.Player == null) return;
            
            if (ActivePlayers.Contains(ev.Player.PlayerId.ToString()))
            {
                CheckEndGame(ev.Player);
            }
        }

        public void CheckEndGame(LabApi.Features.Wrappers.Player excludingPlayer)
        {
            bool anyAlive = Player.GetAll().Any(p => ActivePlayers.Contains(p.PlayerId.ToString()) && p.IsAlive && p.PlayerId != excludingPlayer.PlayerId);
            if (!anyAlive)
            {
                CurrentDay++;
                int day = CurrentDay;
                
                if (day == 6)
                {
                    _cutsceneHandle = Timing.RunCoroutine(EndingCutscene(excludingPlayer));
                    return;
                }
                else if (day >= 7)
                {
                    EndGame(null);
                    return;
                }
                
                Timing.CallDelayed(2f, () =>
                {
                    if (!GameStarted) return;
                    
                    foreach(var p in Player.GetAll().Where(x => ActivePlayers.Contains(x.PlayerId.ToString())))
                    {
                        p.SetRole(RoleTypeId.ClassD, RoleChangeReason.RoundStart);
                        p.Health = 100;
                        
                        if (day == 5)
                        {
                            Timing.CallDelayed(0.5f, () => {
                                try {
                                    var slowness = p.ReferenceHub.playerEffectsController.GetEffect<CustomPlayerEffects.Slowness>();
                                    slowness.Intensity = 15;
                                    p.ReferenceHub.playerEffectsController.EnableEffect<CustomPlayerEffects.Slowness>(0, false);
                                } catch { }
                            });
                        }
                        
                        if (day == 5)
                            TranslationManager.BroadcastAll("day_5_start", 7, shouldClearPrevious: true);
                        else
                            TranslationManager.BroadcastAll("day_n", 5, true, day);
                    }
                    
                    var grannySpawnRoom = MapGeneration.RoomIdentifier.AllRoomIdentifiers.FirstOrDefault(r => r.Name == MapGeneration.RoomName.LczComputerRoom);
                    Vector3 grannySpawn = grannySpawnRoom != null ? grannySpawnRoom.transform.position + Vector3.up : excludingPlayer.Position + new Vector3(5, 1, 5);
                    AI.GrannyAI.Instance.DespawnGranny();
                    AI.GrannyAI.Instance.SpawnGranny(grannySpawn);
                });
            }
        }
        
        private IEnumerator<float> EndingCutscene(Player player)
        {
            yield return Timing.WaitForSeconds(1f);
            
            if (!GameStarted) yield break;
            
            player.SetRole(RoleTypeId.ClassD, RoleChangeReason.None);
            yield return Timing.WaitForSeconds(0.5f); // Wait for role to set
            
            player.Position = new Vector3(39.396f, 314.112f, -35.510f);
            player.Health = 100;
            player.ClearInventory();
            player.AddItem(ItemType.Painkillers);
            
            if (AI.GrannyAI.Instance.Dummy != null)
            {
                AI.GrannyAI.Instance.IsCutscene = true;
                AI.GrannyAI.Instance.Dummy.transform.position = new Vector3(39.123f, 314.111f, -34.038f);
                
                Vector3 dir = (player.Position - AI.GrannyAI.Instance.Dummy.transform.position).normalized;
                dir.y = 0;
                var q = Quaternion.LookRotation(dir);
                AI.GrannyAI.Instance.dummyHub.TryOverrideRotation(new Vector2(0f - q.eulerAngles.x, q.eulerAngles.y));
                if (AI.GrannyAI.Instance.dummyHub.roleManager.CurrentRole is PlayerRoles.FirstPersonControl.IFpcRole fpc)
                    fpc.FpcModule.MouseLook.CurrentHorizontal = q.eulerAngles.y;
            }
            
            try {
                player.ReferenceHub.playerEffectsController.EnableEffect<CustomPlayerEffects.Ensnared>(0, true);
                player.ReferenceHub.playerEffectsController.EnableEffect<CustomPlayerEffects.Blurred>(0, true);
            } catch { }
            
            float damageTimer = 1f;
            float angle = 0f;
            while (player.IsAlive && player.Role == RoleTypeId.ClassD && GameStarted)
            {
                if (AI.GrannyAI.Instance.Dummy != null && AI.GrannyAI.Instance.dummyHub != null)
                {
                    angle += 1440f * Timing.DeltaTime;
                    if (AI.GrannyAI.Instance.dummyHub.roleManager.CurrentRole is PlayerRoles.FirstPersonControl.IFpcRole fpc)
                    {
                        fpc.FpcModule.MouseLook.CurrentHorizontal = angle;
                        AI.GrannyAI.Instance.dummyHub.TryOverrideRotation(new Vector2(0f, angle));
                    }
                }
                
                damageTimer -= Timing.DeltaTime;
                if (damageTimer <= 0f)
                {
                    player.Damage(5f, "Granny");
                    damageTimer = 1f;
                }
                
                yield return Timing.WaitForOneFrame;
            }
        }

        public void OnItemDropped(LabApi.Events.Arguments.PlayerEvents.PlayerDroppedItemEventArgs ev)
        {
            if (GameStarted && ev.Player != null && AI.GrannyAI.Instance.Dummy != null && !AI.GrannyAI.Instance.IsCutscene)
            {
                if (EventManager.BadHearing) return;
                if (Vector3.Distance(ev.Player.Position, AI.GrannyAI.Instance.Dummy.transform.position) <= (EventManager.GoodHearing ? 200f : 60f))
                {
                    AI.GrannyAI.Instance.HearNoise(ev.Player.Position, ev.Player);
                }
            }
        }


        public void OnInteractingDoor(LabApi.Events.Arguments.PlayerEvents.PlayerInteractingDoorEventArgs ev)
        {
            if (ev.Player != null && ev.Player.CurrentItem != null && ev.Player.CurrentItem.Type == CustomItems.HackerDevice)
            {
                ev.IsAllowed = false;
            }
        }
        public void OnPlayerJumped(LabApi.Events.Arguments.PlayerEvents.PlayerJumpedEventArgs ev)
        {
            if (GameStarted && ev.Player != null && AI.GrannyAI.Instance.Dummy != null && !AI.GrannyAI.Instance.IsCutscene)
            {
                if (EventManager.BadHearing) return;
                if (Vector3.Distance(ev.Player.Position, AI.GrannyAI.Instance.Dummy.transform.position) <= (EventManager.GoodHearing ? 200f : 50f))
                {
                    AI.GrannyAI.Instance.HearNoise(ev.Player.Position, ev.Player);
                }
            }
        }
    }
}








