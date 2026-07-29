using System;
using System.Collections.Generic;
using System.Linq;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;

namespace GrannySCPSL.Core
{
    public class GateAMechanics
    {
        public static bool OuterDoorCharged = false;
        public static bool OuterDoorHacked = false;
        public static bool MiddleDoorsBoardsDestroyed = false;
        public static bool ElevatorsCharged = false;
        
        public static List<PrimitiveObjectToy> Boards = new List<PrimitiveObjectToy>();
        
        public static Dictionary<string, HackState> HackStates = new Dictionary<string, HackState>();

        public static List<DoorVariant> MiddleDoors = new List<DoorVariant>();
        public static List<DoorVariant> InnerDoors = new List<DoorVariant>();
        public static List<DoorVariant> ElevatorDoors = new List<DoorVariant>();
        public static DoorVariant OuterDoor = null;

        public class HackState
        {
            public int Stage = 1;
            public string ExpectedAnswer = "";
            public string CurrentQuestion = "";
        }

        public static void InitRound()
        {
            OuterDoorCharged = false;
            OuterDoorHacked = false;
            MiddleDoorsBoardsDestroyed = false;
            ElevatorsCharged = false;
            HackStates.Clear();
            Boards.Clear();
            MiddleDoors.Clear();
            InnerDoors.Clear();
            ElevatorDoors.Clear();
            OuterDoor = null;
            
            Vector3 board1Pos = new Vector3(59.621f, 100.960f, 120.483f);
            Vector3 board2Pos = new Vector3(62.662f, 100.960f, 120.512f);
            
            var b1 = PrimitiveObjectToy.Create(board1Pos, Quaternion.identity, new Vector3(1f, 3f, 0.2f), null, true);
            if (b1 != null) { b1.Type = PrimitiveType.Cube; b1.Color = new Color(0.4f, 0.2f, 0.1f); Boards.Add(b1); }
            
            var b2 = PrimitiveObjectToy.Create(board2Pos, Quaternion.identity, new Vector3(1f, 3f, 0.2f), null, true);
            if (b2 != null) { b2.Type = PrimitiveType.Cube; b2.Color = new Color(0.4f, 0.2f, 0.1f); Boards.Add(b2); }

            foreach (var door in UnityEngine.Object.FindObjectsOfType<DoorVariant>())
            {
                // Outer Door
                if (Vector3.Distance(door.transform.position, new Vector3(60f, 100f, 112.5f)) < 3f)
                {
                    door.ServerChangeLock(DoorLockReason.AdminCommand, true);
                    OuterDoor = door;
                }
                
                // Middle Doors
                if (Vector3.Distance(door.transform.position, new Vector3(59.6f, 100f, 121f)) < 3f || 
                    Vector3.Distance(door.transform.position, new Vector3(62.6f, 100f, 121f)) < 3f)
                {
                    door.ServerChangeLock(DoorLockReason.AdminCommand, true);
                    MiddleDoors.Add(door);
                }
                
                // Inner Doors
                if (Vector3.Distance(door.transform.position, new Vector3(60f, 100f, 127.5f)) < 3f)
                {
                    door.ServerChangeLock(DoorLockReason.AdminCommand, true);
                    InnerDoors.Add(door);
                }
                
                // Elevator Doors
                if (Vector3.Distance(door.transform.position, new Vector3(63f, 100f, 135f)) < 3f || 
                    Vector3.Distance(door.transform.position, new Vector3(57f, 100f, 135f)) < 3f)
                {
                    door.ServerChangeLock(DoorLockReason.AdminCommand, true);
                    ElevatorDoors.Add(door);
                }
            }

            // Lock the Gate A elevators so their panels glow red
            var gateAElevators = LabApi.Features.Wrappers.Map.Elevators.Where(e => e.Group.ToString() == "GateA");
            foreach(var elev in gateAElevators)
            {
                // Let's check if LabApi Elevator wrapper exposes DynamicAdminLock, wait it has 'DynamicAdminLock' setter?
                // In earlier dump: Elevator: Boolean DynamicAdminLock
                try { elev.DynamicAdminLock = true; } catch { }
            }
        }
        
        public static void RegisterEvents()
        {
            PlayerEvents.InteractingDoor += OnInteractingDoor;
            PlayerEvents.InteractingElevator += OnInteractingElevator;
        }

        public static void UnregisterEvents()
        {
            PlayerEvents.InteractingDoor -= OnInteractingDoor;
            PlayerEvents.InteractingElevator -= OnInteractingElevator;
        }

        private static void OnInteractingDoor(PlayerInteractingDoorEventArgs ev)
        {
            if (!GameManager.Instance.GameStarted) return;
            
            if (ev.Door.Base == OuterDoor)
            {
                ev.IsAllowed = false;
                
                if (OuterDoorHacked)
                {
                    ev.Door.Base.ServerChangeLock(DoorLockReason.AdminCommand, false);
                    ev.IsAllowed = true;
                    return;
                }
                
                if (!OuterDoorCharged)
                {
                    if (ev.Player.CurrentItem != null && ev.Player.CurrentItem.Type == global::ItemType.MicroHID)
                    {
                        ev.Player.Damage(35f, "Electrocuted by Gate A Door");
                        ev.Player.SendHint(TranslationManager.GetString("gate_hack_start", ev.Player), 5);
                        return;
                    }
                    else if (ev.Player.CurrentItem != null && ev.Player.CurrentItem.Type == global::ItemType.Radio)
                    {
                        OuterDoorCharged = true;
                        ev.Player.RemoveItem(ev.Player.CurrentItem);
                        ev.Player.SendHint(TranslationManager.GetString("gate_radio_charged", ev.Player), 5);
                    }
                    else
                    {
                        ev.Player.SendHint(TranslationManager.GetString("gate_need_battery", ev.Player), 5);
                    }
                }
                else
                {
                    if (ev.Player.CurrentItem != null && ev.Player.CurrentItem.Type == CustomItems.HackerDevice)
                    {
                        ev.Player.SendHint(TranslationManager.GetString("gate_hack_started", ev.Player), 7);
                        if (!HackStates.ContainsKey(ev.Player.UserId))
                        {
                            HackStates[ev.Player.UserId] = new HackState();
                            GenerateHackProblem(ev.Player, true);
                        }
                        else
                        {
                            GenerateHackProblem(ev.Player, false);
                        }
                    }
                    else
                    {
                        ev.Player.SendHint(TranslationManager.GetString("gate_need_hack", ev.Player), 5);
                    }
                }
            }
            else if (MiddleDoors.Contains(ev.Door.Base))
            {
                ev.IsAllowed = false;
                
                if (MiddleDoorsBoardsDestroyed)
                {
                    if (ev.Player.CurrentItem != null && ev.Player.CurrentItem.Type.ToString().Contains("Keycard"))
                    {
                        ev.Door.Base.ServerChangeLock(DoorLockReason.AdminCommand, false);
                        ev.IsAllowed = true;
                    }
                    else
                    {
                        ev.Player.SendHint(TranslationManager.GetString("gate_need_card", ev.Player), 3);
                    }
                }
                else
                {
                    ev.Player.SendHint(TranslationManager.GetString("gate_boarded", ev.Player), 5);
                }
            }
            else if (InnerDoors.Contains(ev.Door.Base))
            {
                ev.IsAllowed = false;
                
                bool hasSmell = false;
                var effect = ev.Player.ReferenceHub.playerEffectsController.GetEffect<CustomPlayerEffects.Scp1853>();
                if (effect != null && effect.Intensity > 0) hasSmell = true;
                
                if (hasSmell)
                {
                    ev.Door.Base.ServerChangeLock(DoorLockReason.AdminCommand, false);
                    ev.IsAllowed = true;
                    ev.Player.SendHint(TranslationManager.GetString("gate_939_bio", ev.Player), 3);
                }
                else
                {
                    ev.Player.SendHint(TranslationManager.GetString("gate_need_939", ev.Player), 5);
                }
            }
        }
        
        private static void OnInteractingElevator(PlayerInteractingElevatorEventArgs ev)
        {
            if (!GameManager.Instance.GameStarted) return;
            
            if (ev.Elevator.Group.ToString() == "GateA")
            {
                if (!ElevatorsCharged)
                {
                    ev.IsAllowed = false;
                }
            }
        }

        public static void GenerateHackProblem(Player player, bool generateNew = true)
        {
            if (!HackStates.TryGetValue(player.UserId, out var state)) return;

            if (generateNew)
            {
                string question = "";
                switch (state.Stage)
                {
                    case 1:
                        int a = UnityEngine.Random.Range(1, 10);
                        int b = UnityEngine.Random.Range(1, 10);
                        question = string.Format(TranslationManager.GetString("gate_math_1", player), a, b);
                        state.ExpectedAnswer = (a + b).ToString();
                        break;
                    case 2:
                        int c = UnityEngine.Random.Range(100, 1000);
                        int d = UnityEngine.Random.Range(100, 1000);
                        question = string.Format(TranslationManager.GetString("gate_math_2", player), c, d);
                        state.ExpectedAnswer = (c + d).ToString();
                        break;
                    case 3:
                        int e = UnityEngine.Random.Range(10, 100);
                        int f = UnityEngine.Random.Range(10, 100);
                        question = string.Format(TranslationManager.GetString("gate_math_3", player), e, f);
                        state.ExpectedAnswer = (e * f).ToString();
                        break;
                    case 4:
                        int power = UnityEngine.Random.Range(1, 21);
                        question = string.Format(TranslationManager.GetString("gate_math_4", player), power);
                        state.ExpectedAnswer = Math.Pow(2, power).ToString();
                        break;
                }
                state.CurrentQuestion = question;
            }
            
            player.SendConsoleMessage(string.Format(TranslationManager.GetString("gate_hack_stage", player), state.Stage, state.CurrentQuestion), "yellow");
        }
        
        public static void MechanicsTick()
        {
            if (!GameManager.Instance.GameStarted) return;
            
            if (!ElevatorsCharged)
            {
                foreach(var player in Player.GetAll().Where(p => p.IsAlive))
                {
                    bool nearElevator = false;
                    foreach(var ed in ElevatorDoors)
                    {
                        if (ed != null && Vector3.Distance(player.Position, ed.transform.position) < 3f)
                        {
                            nearElevator = true;
                            break;
                        }
                    }
                    if (nearElevator && (player.CurrentItem == null || player.CurrentItem.Type != global::ItemType.MicroHID))
                    {
                        player.SendHint(TranslationManager.GetString("gate_elevators_need_battery", player), 3f);
                    }
                }

                foreach (var pickup in UnityEngine.Object.FindObjectsOfType<InventorySystem.Items.Pickups.ItemPickupBase>())
                {
                    if (pickup.Info.ItemId == ItemType.MicroHID)
                    {
                        bool nearElevator = false;
                        foreach(var ed in ElevatorDoors)
                        {
                            if (ed != null && Vector3.Distance(pickup.Position, ed.transform.position) < 4f)
                            {
                                nearElevator = true;
                                break;
                            }
                        }
                        
                        if (nearElevator)
                        {
                            ElevatorsCharged = true;
                            foreach (var d in ElevatorDoors) d.ServerChangeLock(DoorLockReason.AdminCommand, false);
                            try { LabApi.Features.Wrappers.Map.Elevators.Where(e => e.Group.ToString() == "GateA").ToList().ForEach(e => e.DynamicAdminLock = false); } catch { }
                            pickup.DestroySelf();
                            foreach (var p in Player.GetAll()) p.SendBroadcast(TranslationManager.GetString("gate_elevators_charged", p), 10, shouldClearPrevious: true);
                            break;
                        }
                    }
                }
            }

            if (!MiddleDoorsBoardsDestroyed && Boards.Count > 0)
            {
                foreach(var player in Player.GetAll().Where(p => p.IsAlive))
                {
                    if (player.CurrentItem != null && player.CurrentItem.Type == global::ItemType.GunAK)
                    {
                        foreach(var board in Boards)
                        {
                            if (board != null && board.GameObject != null)
                            {
                                if (Vector3.Distance(player.Position, board.GameObject.transform.position) < 3f)
                                {
                                    MiddleDoorsBoardsDestroyed = true;
                                    foreach(var b in Boards) if (b != null) b.Destroy();
                                    Boards.Clear();
                                    player.SendHint(TranslationManager.GetString("gate_boards_broken", player), 3);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

