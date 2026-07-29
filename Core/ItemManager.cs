using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MapGeneration;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Pickups;

namespace GrannySCPSL.Core
{
    public class ItemManager
    {
        public static List<PlankObject> Planks = new List<PlankObject>();
        public static bool Is914Charged = false;
        public static List<ushort> GrannySmellSerials = new List<ushort>();
        public static bool Is914Unlocked = false;

        public static void InitRound()
        {
            Is914Unlocked = false;
            foreach (var door in LabApi.Features.Wrappers.Door.List)
            {
                if (door.DoorName == LabApi.Features.Enums.DoorName.Lcz914Gate)
                {
                    door.Base.ServerChangeLock(Interactables.Interobjects.DoorUtils.DoorLockReason.AdminCommand, true);
                }
            }
        }

        public static void RegisterEvents()
        {
            PlayerEvents.PickingUpItem += OnPickingUpItem;
            PlayerEvents.PickedUpItem += OnPickedUpItem;
            PlayerEvents.InteractingDoor += OnInteractingDoor;
            PlayerEvents.ChangedItem += OnChangedItem;
            PlayerEvents.UsingItem += OnUsingItem;
            PlayerEvents.ThrowingItem += OnThrowingItem;
            PlayerEvents.UsedItem += OnUsedItem;
            PlayerEvents.Hurting += OnHurting;
            Scp914Events.Activating += On914Activating;
        }

        public static void UnregisterEvents()
        {
            PlayerEvents.PickingUpItem -= OnPickingUpItem;
            PlayerEvents.PickedUpItem -= OnPickedUpItem;
            PlayerEvents.InteractingDoor -= OnInteractingDoor;
            PlayerEvents.ChangedItem -= OnChangedItem;
            PlayerEvents.UsingItem -= OnUsingItem;
            PlayerEvents.ThrowingItem -= OnThrowingItem;
            PlayerEvents.UsedItem -= OnUsedItem;
            PlayerEvents.Hurting -= OnHurting;
            Scp914Events.Activating -= On914Activating;
        }

        private static void OnHurting(PlayerHurtingEventArgs ev)
        {
            if (GrannySCPSL.AI.GrannyAI.Instance != null && ev.Player == GrannySCPSL.AI.GrannyAI.Instance.grannyPlayer)
            {
                if (ev.DamageHandler is PlayerStatsSystem.FirearmDamageHandler firearmDamageHandler)
                {
                    if (firearmDamageHandler.WeaponType == ItemType.GunCOM18)
                    {
                        ev.IsAllowed = false;
                        GrannySCPSL.AI.GrannyAI.Instance.Stun();
                    }
                }
            }
        }

        private static void On914Activating(LabApi.Events.Arguments.Scp914Events.Scp914ActivatingEventArgs ev)
        {
            if (ev.Player == null) return;
            var heldItem = ev.Player.CurrentItem;
            
            if (heldItem != null && heldItem.Type == CustomItems.HugeBattery)
            {
                ev.IsAllowed = false;
                if (Is914Charged)
                {
                    ev.Player.SendHint(TranslationManager.GetString("im_914_charged", ev.Player), 3f);
                }
                else
                {
                    Is914Charged = true;
                    ev.Player.SendHint(TranslationManager.GetString("im_914_success_charge", ev.Player), 5f);
                }
                return;
            }

            if (!Is914Charged)
            {
                ev.Player.SendHint(TranslationManager.GetString("im_device_discharged", ev.Player), 3f);
                ev.IsAllowed = false;
                return;
            }

            if (heldItem != null)
            {
                string customName = "";
                CustomItems.CustomKeycardNames.TryGetValue(heldItem.Serial, out customName);
                
                if (customName == "Базовая Карта")
                {
                    ev.Player.RemoveItem(heldItem);
                    GiveCustomKeycard(ev.Player, "Карта от Чекпоинта", "Чекпоинт", "Карта от Чекпоинта", new KeycardLevels(1,0,1,false), UnityEngine.Color.cyan);
                    ev.Player.SendHint(TranslationManager.GetString("im_upgrade_success", ev.Player), 3f);
                    ev.IsAllowed = false;
                }
                else if (heldItem.Type == CustomItems.VanillaPainkiller)
                {
                    ev.Player.RemoveItem(heldItem);
                    if (UnityEngine.Random.Range(0, 100) < 25) {
                        ev.Player.SendHint(TranslationManager.GetString("im_upgrade_destroyed", ev.Player), 3f);
                    } else {
                        ev.Player.AddItem(CustomItems.VanillaMedkit);
                        ev.Player.SendHint(TranslationManager.GetString("im_upgrade_success", ev.Player), 3f);
                    }
                    ev.IsAllowed = false;
                }
                else if (heldItem.Type == CustomItems.VanillaMedkit)
                {
                    ev.Player.RemoveItem(heldItem);
                    if (UnityEngine.Random.Range(0, 100) < 25) {
                        ev.Player.SendHint(TranslationManager.GetString("im_upgrade_destroyed", ev.Player), 3f);
                    } else {
                        ev.Player.AddItem(ItemType.Adrenaline);
                        ev.Player.SendHint(TranslationManager.GetString("im_upgrade_success", ev.Player), 3f);
                    }
                    ev.IsAllowed = false;
                }
                else if (heldItem.Type == ItemType.Adrenaline)
                {
                    ev.Player.RemoveItem(heldItem);
                    if (UnityEngine.Random.Range(0, 100) < 25) {
                        ev.Player.SendHint(TranslationManager.GetString("im_upgrade_destroyed", ev.Player), 3f);
                    } else {
                        ev.Player.AddItem(ItemType.SCP500);
                        ev.Player.SendHint(TranslationManager.GetString("im_upgrade_success", ev.Player), 3f);
                    }
                    ev.IsAllowed = false;
                }
                else if (heldItem.Type == ItemType.SCP500)
                {
                    ev.Player.RemoveItem(heldItem);
                    ev.Player.SendHint(TranslationManager.GetString("im_upgrade_fail", ev.Player), 3f);
                    ev.IsAllowed = false;
                }
                else
                {
                    ev.Player.SendHint(TranslationManager.GetString("im_cannot_upgrade", ev.Player), 3f);
                    ev.IsAllowed = false;
                }
            }
            else
            {
                ev.Player.SendHint(TranslationManager.GetString("im_hold_item", ev.Player), 3f);
                ev.IsAllowed = false;
            }
        }

        private static void GiveCustomKeycard(Player player, string itemName, string holderName, string cardLabel, KeycardLevels permissions, UnityEngine.Color cardColor)
        {
            var keycardItem = KeycardItem.CreateCustomKeycardSite02(
                player, itemName, holderName, cardLabel, permissions, cardColor, UnityEngine.Color.white, UnityEngine.Color.white, 0
            );
            if (keycardItem != null)
            {
                CustomItems.CustomKeycardNames[keycardItem.Serial] = itemName;
            }
        }

        private static void OnPickedUpItem(PlayerPickedUpItemEventArgs ev)
        {
            if (ev.Player == null || ev.Item == null) return;

            if (ev.Player.Role != PlayerRoles.RoleTypeId.Scp939 && ev.Player.Role != PlayerRoles.RoleTypeId.Spectator)
            {
                int count = 0;
                foreach(var item in ev.Player.ReferenceHub.inventory.UserInventory.Items.Values)
                {
                    if (item.ItemTypeId != ItemType.Lantern) count++;
                }
                
                if (count > EventManager.MaxItems && ev.Item.Type != ItemType.Lantern)
                {
                    MEC.Timing.CallDelayed(0.1f, () => {
                        ev.Player.DropItem(ev.Item);
                        ev.Player.SendHint(TranslationManager.GetString("im_inventory_full", ev.Player), 3f);
                    });
                    return;
                }
            }

            string customName = "Неизвестный предмет";
            if (CustomItems.CustomKeycardNames.TryGetValue(ev.Item.Serial, out string kName)) {
                customName = kName;
            } else {
                customName = CustomItems.GetName(ev.Item.Type, ev.Player);
            }

            if (customName != "Неизвестный предмет")
            {
                string translatedName = TranslationManager.GetString(customName, ev.Player);
                ev.Player.SendBroadcast(string.Format(TranslationManager.GetString("im_picked_up", ev.Player), translatedName), 3, shouldClearPrevious: true);
            }
        }

        private static void OnChangedItem(PlayerChangedItemEventArgs ev)
        {
            if (ev.Player == null || ev.NewItem == null) return;
            string customName = "Неизвестный предмет";
            if (CustomItems.CustomKeycardNames.TryGetValue(ev.NewItem.Serial, out string kName)) {
                customName = kName;
            } else {
                customName = CustomItems.GetName(ev.NewItem.Type, ev.Player);
            }
            if (customName != "Неизвестный предмет")
            {
                string translatedName = TranslationManager.GetString(customName, ev.Player);
                ev.Player.SendBroadcast(string.Format(TranslationManager.GetString("im_in_hands", ev.Player), translatedName), 3, shouldClearPrevious: true);
            }

            if (ev.NewItem.Type == ItemType.Lantern)
            {
                ev.Player.SendHint(TranslationManager.GetString("im_lantern_hint", ev.Player), 3f);
            }
        }
        private static void OnUsingItem(PlayerUsingItemEventArgs ev)
        {
            if (ev.Item.Type == CustomItems.HugeBattery)
            {
                ev.IsAllowed = false;
                ev.Player.SendHint(TranslationManager.GetString("im_use_huge_battery", ev.Player), 3f);
            }
            if (ev.Item.Type == CustomItems.Flint)
            {
                ev.IsAllowed = false;
                ev.Player.SendHint(TranslationManager.GetString("im_use_flint", ev.Player), 3f);
            }
            if (ev.Item.Type == CustomItems.ExplosiveBag)
            {
                ev.IsAllowed = false;
                ev.Player.SendHint(TranslationManager.GetString("im_use_explosive", ev.Player), 3f);
            }
        }

        private static void OnThrowingItem(PlayerThrowingItemEventArgs ev)
        {
            if (ev.Player != null && ev.Player.CurrentItem != null && ev.Player.CurrentItem.Type == CustomItems.ExplosiveBag)
            {
                ev.IsAllowed = false;
                ev.Player.SendHint(TranslationManager.GetString("im_use_explosive", ev.Player), 3f);
                ev.Player.CurrentItem = null;
            }
        }

        private static void OnUsedItem(PlayerUsedItemEventArgs ev)
        {
            if (ev.Item.Type == CustomItems.SCP207)
            {
                ev.Player.EnableEffect<CustomPlayerEffects.MovementBoost>(30, 0, false);
            }
            if (ev.Item.Type == ItemType.Adrenaline)
            {
                ev.Player.EnableEffect<CustomPlayerEffects.MovementBoost>(50, 10, true);
            }
        }

        private static void OnPickingUpItem(PlayerPickingUpItemEventArgs ev)
        {
            // The limit is now enforced in OnPickedUpItem to ensure it works reliably.
        }

        private static void OnInteractingDoor(PlayerInteractingDoorEventArgs ev)
        {
            if (Vector3.Distance(ev.Door.GameObject.transform.position, new Vector3(37.964f, 100.967f, 59.974f)) < 4f)
            {
                if (!GameManager.TC01Unlocked)
                {
                    ev.IsAllowed = false;
                    ev.Player.SendHint(string.Format(TranslationManager.GetString("im_puzzle_need", ev.Player), GameManager.TC01PuzzlesCollected), 3f);
                    return;
                }
            }
            
            if (ev.Door.DoorName == LabApi.Features.Enums.DoorName.Lcz914Gate)
            {
                if (!Is914Unlocked)
                {
                    ev.IsAllowed = false;
                    
                    var curItem = ev.Player.CurrentItem;
                    string customName = "";
                    if (curItem != null) CustomItems.CustomKeycardNames.TryGetValue(curItem.Serial, out customName);
                    
                    if (customName == "Лист с отпечатком")
                    {
                        Is914Unlocked = true;
                        ev.Door.Base.ServerChangeLock(Interactables.Interobjects.DoorUtils.DoorLockReason.AdminCommand, false);
                        ev.Player.SendBroadcast(TranslationManager.GetString("im_914_unlocked", ev.Player), 5, shouldClearPrevious: true);
                    }
                    else
                    {
                        ev.Player.SendBroadcast(TranslationManager.GetString("im_914_locked", ev.Player), 5, shouldClearPrevious: true);
                    }
                    return;
                }
            }

            if (Vector3.Distance(ev.Door.GameObject.transform.position, new Vector3(112.05f, 100.967f, 105.007f)) < 4f)
            {
                ev.IsAllowed = false;
                if (GameManager.PT00Activated) return;
                
                if (!GameManager.PT00ExplosivePlaced)
                {
                    ev.Player.SendHint(TranslationManager.GetString("im_pt00_need_explosive", ev.Player), 3f);
                    return;
                }
                
                if (ev.Player.CurrentItem != null && ev.Player.CurrentItem.Type == CustomItems.Flint)
                {
                    GameManager.PT00Activated = true;
                    ev.Player.SendHint(TranslationManager.GetString("im_pt00_activated", ev.Player), 5f);
                    
                    var pos = new Vector3(112.05f, 100.967f, 105.007f);
                    var playerHub = ev.Player.ReferenceHub;
                    var doorObj = ev.Door.Base;
                    
                    MEC.Timing.CallDelayed(5f, () => {
                        if (GameManager.Instance != null && !GameManager.Instance.GameStarted) return;
                        
                        try {
                            Utils.ExplosionUtils.ServerExplode(pos, new Footprinting.Footprint(playerHub), ExplosionType.Grenade);
                            AI.GrannyAI.Instance.HearNoise(pos);
                        } catch { }
                        
                        if (doorObj is Interactables.Interobjects.BreakableDoor breakable)
                        {
                            breakable.IsDestroyed = true;
                        }
                    });
                }
                else
                {
                    ev.Player.SendHint(TranslationManager.GetString("im_pt00_need_flint", ev.Player), 3f);
                }
                return;
            }

            // Block Checkpoint B completely
            if (ev.Door.DoorName == LabApi.Features.Enums.DoorName.LczCheckpointB)
            {
                ev.IsAllowed = false;
                ev.Player.SendHint(TranslationManager.GetString("im_door_locked_forever", ev.Player), 3f);
                return;
            }

            var heldItem = ev.Player.ReferenceHub.inventory.CurItem;
            bool hasHammer = heldItem != null && heldItem.TypeId == CustomItems.Hammer;

            var planksToBreak = new List<PlankObject>();
            foreach (var plank in Planks)
            {
                if (Vector3.Distance(ev.Player.Position, plank.Position) < 3f)
                {
                    if (hasHammer)
                    {
                        planksToBreak.Add(plank);
                    }
                    else
                    {
                        ev.IsAllowed = false;
                        ev.Player.SendHint(TranslationManager.GetString("im_door_need_hammer", ev.Player), 3f);
                        return; // Prevent opening the door if boarded up and no hammer
                    }
                }
            }

            foreach (var plank in planksToBreak)
            {
                plank.Break();
                ev.Player.SendHint(TranslationManager.GetString("im_boards_broken", ev.Player), 2f);
            }
        }
    }

    public class PlankObject
    {
        public Vector3 Position { get; private set; }

        public PlankObject(Vector3 position)
        {
            Position = position;
        }

        public void Break()
        {
            ItemManager.Planks.Remove(this);
        }
    }

    public static class CustomItems
    {
        public static Dictionary<ushort, string> CustomKeycardNames = new Dictionary<ushort, string>();

        // T1
        public const ItemType Hammer = ItemType.GunAK;
        public const ItemType Battery = ItemType.Radio;
        public const ItemType HackerDevice = ItemType.KeycardChaosInsurgency;
        public const ItemType TwoPanaceas = ItemType.SCP500;

        // T2
        public const ItemType ExplosiveBag = ItemType.GunShotgun;
        public const ItemType Flint = ItemType.GunRevolver;
        public const ItemType HugeBattery = ItemType.MicroHID;
        public const ItemType Lantern = ItemType.Lantern;
        public const ItemType SCP207 = ItemType.SCP207;
        public const ItemType VanillaMedkit = ItemType.Medkit;
        public const ItemType VanillaPainkiller = ItemType.Painkillers;
        public const ItemType Fingerprint = ItemType.KeycardJanitor;

        // TX
        public const ItemType Ammo = ItemType.Ammo9x19;
        public const ItemType InvisibilityHat = ItemType.SCP268;
        public const ItemType GrannySmell = ItemType.SCP1853;

        public static string GetName(ItemType type, Player player = null)
        {
            if (type == ItemType.Adrenaline) return TranslationManager.GetString("item_adrenaline", player);
            switch(type)
            {
                case Hammer: return TranslationManager.GetString("item_hammer", player);
                case Battery: return TranslationManager.GetString("item_battery", player);
                case HackerDevice: return TranslationManager.GetString("item_hacker_device", player);
                case TwoPanaceas: return TranslationManager.GetString("item_two_panaceas", player);
                case ExplosiveBag: return TranslationManager.GetString("item_explosive_bag", player);
                case Flint: return TranslationManager.GetString("item_flint", player);
                case HugeBattery: return TranslationManager.GetString("item_huge_battery", player);
                case Lantern: return TranslationManager.GetString("item_lantern", player);
                case SCP207: return TranslationManager.GetString("item_scp207", player);
                case VanillaMedkit: return TranslationManager.GetString("item_medkit", player);
                case VanillaPainkiller: return TranslationManager.GetString("item_painkiller", player);
                case Ammo: return TranslationManager.GetString("item_ammo", player);
                case InvisibilityHat: return TranslationManager.GetString("item_invisibility", player);
                case GrannySmell: return TranslationManager.GetString("item_granny_smell", player);
                case Fingerprint: return TranslationManager.GetString("item_fingerprint", player);
                default:
                    string typeName = "item_" + type.ToString().ToLower();
                    string localized = TranslationManager.GetString(typeName, player);
                    return localized != typeName ? localized : TranslationManager.GetString("item_unknown", player);
            }
        }
    }
}
