using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using LabApi.Features.Wrappers;

namespace GrannySCPSL.Core
{
    public static class ItemSpawner
    {
        public static List<ushort> PuzzleSerials = new List<ushort>();
        
        public static void SpawnAllItems()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SCP Secret Laboratory", "LabAPI", "Plugins", "global", "GrannySCPSL_Positions.txt");
            
            string[] lines;
            if (File.Exists(path))
            {
                lines = File.ReadAllLines(path);
            }
            else
            {
                lines = new string[] {
                    "29.96|101.84|69.10|T2",
                    "47.75|101.84|80.87|T2",
                    "43.26|102.15|98.08|T2",
                    "75.99|100.96|111.51|T2",
                    "68.71|100.96|98.51|T2",
                    "90.52|100.99|126.44|T2",
                    "95.66|100.97|88.41|T2",
                    "91.30|100.97|40.50|T2",
                    "59.09|101.65|49.65|T2",
                    "68.75|100.97|62.60|T2",
                    "36.96|100.96|91.26|T2",
                    "87.19|100.96|113.08|T2",
                    "47.49|100.96|34.30|T2",
                    "99.28|100.97|43.70|T2",
                    "68.06|100.97|73.72|T2",
                    "60.80|100.97|51.83|T2",
                    "29.91|101.92|86.58|T1 оружейка",
                    "25.07|100.96|92.50|T1 оружейка",
                    "116.63|112.44|101.23|T1 PT-00",
                    "132.58|112.44|109.36|T1 PT-00",
                    "40.45|101.02|25.39|T1 914",
                    "51.99|100.96|31.60|T1 914",
                    "26.66|100.96|56.16|T1 TC-01",
                    "33.65|100.95|64.43|T1 TC-01",
                    "56.58|101.14|119.79|TX",
                    "100.60|100.96|21.93|TX",
                    "105.048|101.460|69.579|T2",
                    "108.923|100.959|36.984|T2"
                };
            }

            var t1Points = new Dictionary<string, List<Vector3>>();
            var t2Points = new List<Vector3>();
            var txPoints = new List<Vector3>();

            foreach (var line in lines)
            {
                var parts = line.Split('|');
                if (parts.Length != 4) continue;
                if (!float.TryParse(parts[0], out float x) || !float.TryParse(parts[1], out float y) || !float.TryParse(parts[2], out float z)) continue;
                var pos = new Vector3(x, y, z);
                var desc = parts[3];

                if (desc.StartsWith("T1"))
                {
                    string roomKey = "unknown";
                    if (desc.Contains("оружейка")) roomKey = "armory";
                    else if (desc.Contains("PT-00")) roomKey = "pt00";
                    else if (desc.Contains("914")) roomKey = "914";
                    else if (desc.Contains("TC-01")) roomKey = "tc01";
                    
                    if (!t1Points.ContainsKey(roomKey)) t1Points[roomKey] = new List<Vector3>();
                    t1Points[roomKey].Add(pos);
                }
                else if (desc.StartsWith("T2"))
                {
                    t2Points.Add(pos);
                }
                else if (desc.StartsWith("TX"))
                {
                    txPoints.Add(pos);
                }
            }

            SpawnT1(t1Points);
            SpawnT2(t2Points);
            SpawnTX(txPoints);

            // Spawn COM-18 at Armory with 0 ammo
            var player = LabApi.Features.Wrappers.Player.GetAll().FirstOrDefault();
            if (player != null)
            {
                var comItem = player.AddItem(ItemType.GunCOM18);
                if (comItem is LabApi.Features.Wrappers.FirearmItem firearm)
                {
                    firearm.StoredAmmo = 0;
                    firearm.ChamberedAmmo = 0;
                }
                
                // Drop and move instantly
                var pickup = player.DropItem(comItem);
                if (pickup != null)
                {
                    pickup.Position = new Vector3(26.086f, 101.780f, 91.079f);
                }
            }
            else
            {
                // Fallback if no players (unlikely)
                Pickup.Create(ItemType.GunCOM18, new Vector3(26.086f, 101.780f, 91.079f)).Spawn();
            }
        }

        private static void SpawnT1(Dictionary<string, List<Vector3>> rooms)
        {
            var items = new List<ItemType> { CustomItems.Hammer, CustomItems.Battery, CustomItems.HackerDevice, CustomItems.TwoPanaceas };
            items = items.OrderBy(x => Guid.NewGuid()).ToList();

            int i = 0;
            foreach (var room in rooms)
            {
                if (i >= items.Count) break;
                var positions = room.Value;
                var randomPos = positions[UnityEngine.Random.Range(0, positions.Count)];
                if (items[i] == CustomItems.TwoPanaceas)
                {
                    SpawnModifiedItem(randomPos, items[i]);
                    SpawnModifiedItem(randomPos + Vector3.up * 0.1f, items[i]);
                }
                else
                {
                    SpawnModifiedItem(randomPos, items[i]);
                }
                i++;
            }
        }

        private static void SpawnT2(List<Vector3> points)
        {
            var itemsToSpawn = new System.Collections.Generic.List<System.Action<Vector3>>();
            
            itemsToSpawn.Add(pos => SpawnModifiedItem(pos, CustomItems.ExplosiveBag));
            itemsToSpawn.Add(pos => SpawnModifiedItem(pos, CustomItems.Flint));
            itemsToSpawn.Add(pos => SpawnModifiedItem(pos, CustomItems.HugeBattery));
            itemsToSpawn.Add(pos => SpawnModifiedItem(pos, CustomItems.Lantern));
            itemsToSpawn.Add(pos => SpawnModifiedItem(pos, CustomItems.SCP207));
            itemsToSpawn.Add(pos => SpawnModifiedItem(pos, CustomItems.VanillaMedkit));
            itemsToSpawn.Add(pos => SpawnModifiedItem(pos, CustomItems.VanillaPainkiller));
            itemsToSpawn.Add(pos => SpawnCustomKeycard(pos, "Лист с отпечатком", "Отпечаток", "Лист с отпечатком", new Interactables.Interobjects.DoorUtils.KeycardLevels(0,0,0,false), UnityEngine.Color.white));
            
            itemsToSpawn.Add(pos => {
                var p1 = SpawnCustomKeycard(pos, "Пазл 1", "Пазл", "Пазл 1", new Interactables.Interobjects.DoorUtils.KeycardLevels(0,0,0,false), UnityEngine.Color.red);
                PuzzleSerials.Add(p1);
            });
            itemsToSpawn.Add(pos => {
                var p2 = SpawnCustomKeycard(pos, "Пазл 2", "Пазл", "Пазл 2", new Interactables.Interobjects.DoorUtils.KeycardLevels(0,0,0,false), UnityEngine.Color.red);
                PuzzleSerials.Add(p2);
            });
            itemsToSpawn.Add(pos => {
                var p3 = SpawnCustomKeycard(pos, "Пазл 3", "Пазл", "Пазл 3", new Interactables.Interobjects.DoorUtils.KeycardLevels(0,0,0,false), UnityEngine.Color.red);
                PuzzleSerials.Add(p3);
            });
            
            itemsToSpawn.Add(pos => SpawnCustomKeycard(pos, "Пропуск в Оружейную", "Оружейная", "Пропуск в Оружейную", new Interactables.Interobjects.DoorUtils.KeycardLevels(0,1,0,false), UnityEngine.Color.gray));
            itemsToSpawn.Add(pos => SpawnCustomKeycard(pos, "Базовая Карта", "Карта", "Базовая Карта", new Interactables.Interobjects.DoorUtils.KeycardLevels(1,0,0,false), UnityEngine.Color.yellow));
            
            PuzzleSerials.Clear();

            var shuffledPoints = points.OrderBy(x => Guid.NewGuid()).ToList();

            for (int i = 0; i < itemsToSpawn.Count && i < shuffledPoints.Count; i++)
            {
                itemsToSpawn[i](shuffledPoints[i]);
            }
        }

        private static void SpawnTX(List<Vector3> points)
        {
            var itemsToSpawn = new System.Collections.Generic.List<System.Action<Vector3>>();
            
            itemsToSpawn.Add(pos => SpawnModifiedItem(pos, CustomItems.Ammo));
            itemsToSpawn.Add(pos => SpawnModifiedItem(pos, CustomItems.InvisibilityHat));
            
            var shuffledPoints = points.OrderBy(x => Guid.NewGuid()).ToList();

            for (int i = 0; i < itemsToSpawn.Count && i < shuffledPoints.Count; i++)
            {
                itemsToSpawn[i](shuffledPoints[i]);
            }
        }
        
        private static void SpawnModifiedItem(Vector3 pos, ItemType type)
        {
            if (type == CustomItems.Ammo)
            {
                var p = LabApi.Features.Wrappers.Pickup.Create(type, pos);
                p.Spawn();
                if (p.Base is InventorySystem.Items.Firearms.Ammo.AmmoPickup ammoPickup) {
                    ammoPickup.NetworkSavedAmmo = 3;
                }
                return;
            }

            var player = LabApi.Features.Wrappers.Player.GetAll().FirstOrDefault();
            if (player == null) return;

            var item = player.AddItem(type);
            
            if (item is LabApi.Features.Wrappers.FirearmItem firearm)
            {
                firearm.StoredAmmo = 0;
                firearm.ChamberedAmmo = 0;
            }
            if (item is LabApi.Features.Wrappers.MicroHIDItem hid)
            {
                hid.Energy = 0;
            }
            
            var pickup = player.DropItem(item);
            if (pickup != null)
            {
                pickup.Position = pos;
                if (type == CustomItems.Ammo && pickup.Base is InventorySystem.Items.Firearms.Ammo.AmmoPickup ammoPickup) {
                    ammoPickup.NetworkSavedAmmo = 3;
                }
            }
        }
        
        private static ushort SpawnCustomKeycard(Vector3 pos, string itemName, string holderName, string cardLabel, Interactables.Interobjects.DoorUtils.KeycardLevels permissions, UnityEngine.Color cardColor)
        {
            var player = LabApi.Features.Wrappers.Player.GetAll().FirstOrDefault();
            if (player == null) return 0;
            
            var keycardItem = LabApi.Features.Wrappers.KeycardItem.CreateCustomKeycardSite02(
                player, 
                itemName, 
                holderName, 
                cardLabel, 
                permissions, 
                cardColor, 
                UnityEngine.Color.white, 
                UnityEngine.Color.white, 
                0
            );
            
            if (keycardItem != null)
            {
                // Register name for broadcast text
                CustomItems.CustomKeycardNames[keycardItem.Serial] = itemName;
                
                var pickup = player.DropItem(keycardItem);
                if (pickup != null) {
                    pickup.Position = pos;
                    return pickup.Serial;
                }
            }
            return 0;
        }
    }

}
