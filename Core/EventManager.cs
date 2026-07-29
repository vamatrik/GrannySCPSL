using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;
using CustomPlayerEffects;
using PlayerRoles;

namespace GrannySCPSL.Core
{
    public static class EventManager
    {
        public static int MaxItems = 1;
        public static float GrannySpeedMultiplier = 1f;
        public static bool GoodHearing = false;
        public static bool BadHearing = false;
        public static bool FastGrenades = false;
        public static bool SlowPlayers = false;
        
        private static CoroutineHandle eventCoroutine;

        public static void ResetEvents()
        {
            Timing.KillCoroutines(eventCoroutine);
            MaxItems = 1;
            GrannySpeedMultiplier = 1f;
            GoodHearing = false;
            BadHearing = false;
            FastGrenades = false;
            SlowPlayers = false;
        }

        public static void StartEvents()
        {
            ResetEvents();
            eventCoroutine = Timing.RunCoroutine(EventLoop());
        }

        private static IEnumerator<float> EventLoop()
        {
            yield return Timing.WaitForSeconds(90f);
            
            while (GameManager.Instance.GameStarted)
            {
                TriggerRandomEvent();
                yield return Timing.WaitForSeconds(120f);
            }
        }

        public static void TriggerRandomEvent(int evId = -1)
        {
            if (!GameManager.Instance.GameStarted) return;

            int ev = evId == -1 ? UnityEngine.Random.Range(1, 13) : evId;
            switch (ev)
            {
                case 1:
                    TranslationManager.BroadcastAll("event_reveal", 7, shouldClearPrevious: true);
                    RevealPosition();
                    break;
                case 2:
                    TranslationManager.BroadcastAll("event_good_hearing", 7, shouldClearPrevious: true);
                    GoodHearing = true;
                    Timing.CallDelayed(45f, () => {
                        GoodHearing = false;
                        TranslationManager.BroadcastAll("event_good_hearing_end", 5, shouldClearPrevious: true);
                    });
                    break;
                case 3:
                    TranslationManager.BroadcastAll("event_teleport", 7, shouldClearPrevious: true);
                    RandomTeleport();
                    break;
                case 4:
                    TranslationManager.BroadcastAll("event_noisy", 7, shouldClearPrevious: true);
                    FastGrenades = true;
                    Timing.CallDelayed(25f, () => {
                        FastGrenades = false;
                        TranslationManager.BroadcastAll("event_noisy_end", 5, shouldClearPrevious: true);
                    });
                    break;
                case 5:
                    TranslationManager.BroadcastAll("event_bad_hearing", 7, shouldClearPrevious: true);
                    BadHearing = true;
                    Timing.CallDelayed(45f, () => {
                        BadHearing = false;
                        TranslationManager.BroadcastAll("event_good_hearing_end", 5, shouldClearPrevious: true);
                    });
                    break;
                case 6:
                    TranslationManager.BroadcastAll("event_slow_granny", 7, shouldClearPrevious: true);
                    GrannySpeedMultiplier = 0.7f;
                    Timing.CallDelayed(20f, () => {
                        GrannySpeedMultiplier = 1f;
                        TranslationManager.BroadcastAll("event_slow_granny_end", 5, shouldClearPrevious: true);
                    });
                    break;
                case 7:
                    TranslationManager.BroadcastAll("event_fast_granny", 7, shouldClearPrevious: true);
                    GrannySpeedMultiplier = 1.3f;
                    Timing.CallDelayed(20f, () => {
                        GrannySpeedMultiplier = 1f;
                        TranslationManager.BroadcastAll("event_slow_granny_end", 5, shouldClearPrevious: true);
                    });
                    break;
                case 8:
                    TranslationManager.BroadcastAll("event_big_pockets", 7, shouldClearPrevious: true);
                    MaxItems = 2;
                    Timing.CallDelayed(30f, () => {
                        MaxItems = 1;
                        TranslationManager.BroadcastAll("event_big_pockets_end", 5, shouldClearPrevious: true);
                        foreach (var p in Player.GetAll().Where(x => GameManager.ActivePlayers.Contains(x.PlayerId.ToString())))
                        {
                            if (p.ReferenceHub.inventory.UserInventory.Items.Count > 1)
                            {
                                var lastItem = p.ReferenceHub.inventory.UserInventory.Items.Values.LastOrDefault();
                                if (lastItem != null)
                                    p.DropItem(lastItem.ItemSerial);
                            }
                        }
                    });
                    break;
                case 9:
                    TranslationManager.BroadcastAll("event_slow_players", 7, shouldClearPrevious: true);
                    SlowPlayers = true;
                    foreach (var p in Player.GetAll().Where(x => GameManager.ActivePlayers.Contains(x.PlayerId.ToString())))
                    {
                        try {
                            p.EnableEffect<Sinkhole>(1, 20f, false);
                        } catch { }
                    }
                    Timing.CallDelayed(20f, () => {
                        SlowPlayers = false;
                        TranslationManager.BroadcastAll("event_slow_players_end", 5, shouldClearPrevious: true);
                    });
                    break;
                case 10:
                    TranslationManager.BroadcastAll("event_fast_players", 7, shouldClearPrevious: true);
                    foreach (var p in Player.GetAll().Where(x => GameManager.ActivePlayers.Contains(x.PlayerId.ToString())))
                    {
                        try {
                            p.EnableEffect<MovementBoost>(30, 20f, false);
                        } catch { }
                    }
                    Timing.CallDelayed(20f, () => {
                        TranslationManager.BroadcastAll("event_fast_players_end", 5, shouldClearPrevious: true);
                    });
                    break;
                case 11:
                    TranslationManager.BroadcastAll("event_medkit", 7, shouldClearPrevious: true);
                    foreach (var p in Player.GetAll().Where(x => GameManager.ActivePlayers.Contains(x.PlayerId.ToString())))
                    {
                        if (p.Role == RoleTypeId.ClassD && p.IsAlive)
                        {
                            var type = UnityEngine.Random.Range(0, 2) == 0 ? ItemType.Medkit : ItemType.Painkillers;
                            var pickup = p.DropItem(p.AddItem(type));
                            if (pickup != null) pickup.Position = p.Position;
                        }
                    }
                    break;
                case 12:
                    TranslationManager.BroadcastAll("event_invis", 7, shouldClearPrevious: true);
                    foreach (var p in Player.GetAll().Where(x => GameManager.ActivePlayers.Contains(x.PlayerId.ToString())))
                    {
                        try { p.ReferenceHub.playerEffectsController.EnableEffect<CustomPlayerEffects.Invisible>(10f, false); } catch { }
                    }
                    if (AI.GrannyAI.Instance != null && AI.GrannyAI.Instance.grannyPlayer != null)
                    {
                        try { AI.GrannyAI.Instance.grannyPlayer.ReferenceHub.playerEffectsController.EnableEffect<CustomPlayerEffects.Invisible>(20f, false); } catch { }
                    }
                    Timing.CallDelayed(10f, () => {
                        TranslationManager.BroadcastAll("event_invis_player_end", 5, shouldClearPrevious: true);
                    });
                    Timing.CallDelayed(20f, () => {
                        TranslationManager.BroadcastAll("event_invis_granny_end", 5, shouldClearPrevious: true);
                    });
                    break;
            }
        }

        private static void RevealPosition()
        {
            var p = Player.GetAll().FirstOrDefault(x => GameManager.ActivePlayers.Contains(x.PlayerId.ToString()) && x.IsAlive && x.Role == RoleTypeId.ClassD);
            if (p != null)
            {
                AI.GrannyAI.Instance.HearNoise(p.Position);
            }
        }

        private static void RandomTeleport()
        {
            var nodes = Graph.GraphManager.Instance?.Nodes;
            if (nodes != null && nodes.Count > 0)
            {
                var targetNode = nodes[UnityEngine.Random.Range(0, nodes.Count)];
                if (AI.GrannyAI.Instance.grannyPlayer != null)
                {
                    AI.GrannyAI.Instance.grannyPlayer.Position = new Vector3(targetNode.X, targetNode.Y, targetNode.Z);
                }
            }
        }
    }
}