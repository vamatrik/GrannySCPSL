using CommandSystem;
using LabApi.Features.Wrappers;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GrannySCPSL.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class PosCommand : ICommand
    {
        public string Command => "pos";
        public string[] Aliases => new string[] { };
        public string Description => "Manage positions for Granny minigame items/objects.";

        public static List<SavedPosition> Positions = new List<SavedPosition>();
        private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SCP Secret Laboratory", "LabAPI", "Plugins", "global", "GrannySCPSL_Positions.txt");

        public static void LoadPositions()
        {
            string[] lines;
            if (File.Exists(FilePath))
            {
                lines = File.ReadAllLines(FilePath);
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

            Positions.Clear();
            foreach (var line in lines)
                {
                    var p = line.Split('|');
                    if (p.Length >= 4)
                    {
                        if(float.TryParse(p[0], out float x) && float.TryParse(p[1], out float y) && float.TryParse(p[2], out float z))
                        {
                            Positions.Add(new SavedPosition { X=x, Y=y, Z=z, Description=p[3] });
                        }
                    }
                }
        }

        public static void SavePositions()
        {
            var lines = new List<string>();
            foreach (var pos in Positions)
            {
                lines.Add($"{pos.X}|{pos.Y}|{pos.Z}|{pos.Description}");
            }
            File.WriteAllLines(FilePath, lines);
        }

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);
            if (player == null)
            {
                response = "You must be a player to use this command.";
                return false;
            }

            if (arguments.Count == 0)
            {
                response = "Usage: pos <add [description] / deletenear / save>";
                return false;
            }

            string subcmd = arguments.At(0).ToLower();

            if (subcmd == "add")
            {
                if (arguments.Count < 2)
                {
                    response = "You must provide a description.";
                    return false;
                }
                string description = arguments.At(1);
                
                var newPos = new SavedPosition
                {
                    X = player.Position.x,
                    Y = player.Position.y,
                    Z = player.Position.z,
                    Description = description
                };
                
                if (Positions.Count == 0) LoadPositions();
                Positions.Add(newPos);
                
                response = $"Added position: {description} at {player.Position}";
                return true;
            }
            else if (subcmd == "deletenear")
            {
                if (Positions.Count == 0) LoadPositions();
                
                SavedPosition closest = null;
                float closestDist = float.MaxValue;

                foreach (var pos in Positions)
                {
                    float dist = Vector3.Distance(player.Position, new Vector3(pos.X, pos.Y, pos.Z));
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = pos;
                    }
                }

                if (closest != null && closestDist < 5f)
                {
                    Positions.Remove(closest);
                    response = $"Deleted nearby position: {closest.Description} (Distance: {closestDist:F2}m)";
                    return true;
                }
                else
                {
                    response = "No positions found near you within 5 meters.";
                    return false;
                }
            }
            else if (subcmd == "save")
            {
                SavePositions();
                response = $"Saved {Positions.Count} positions to disk.";
                return true;
            }

            response = "Unknown subcommand. Usage: pos <add [desc] / deletenear / save>";
            return false;
        }
    }

    public class SavedPosition
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public string Description { get; set; }
    }
}


