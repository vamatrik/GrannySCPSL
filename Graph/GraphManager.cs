using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using LabApi.Features.Console;

namespace GrannySCPSL.Graph
{
    [Serializable]
    public class GraphData
    {
        public List<Node> nodes = new List<Node>();
    }

    public class GraphManager
    {
        public static GraphManager Instance { get; } = new GraphManager();

        public List<Node> Nodes { get; set; } = new List<Node>();
        
        public string GraphFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SCP Secret Laboratory", "granny_graph.json");

        public void LoadGraph()
        {
            string txtPath = GraphFilePath.Replace(".json", ".txt");
            if (File.Exists(txtPath))
            {
                try
                {
                    Nodes = new List<Node>();
                    var lines = File.ReadAllLines(txtPath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('|');
                        if (parts.Length >= 4)
                        {
                            var n = new Node
                            {
                                Id = int.Parse(parts[0]),
                                X = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
                                Y = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture),
                                Z = float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture)
                            };
                            if (parts.Length == 5 && !string.IsNullOrEmpty(parts[4]))
                            {
                                foreach (var c in parts[4].Split(';'))
                                {
                                    if (int.TryParse(c, out int cid)) n.ConnectedNodeIds.Add(cid);
                                }
                            }
                            Nodes.Add(n);
                        }
                    }
                    LabApi.Features.Console.Logger.Info($"Loaded {Nodes.Count} nodes from txt file.");
                }
                catch (Exception e)
                {
                    LabApi.Features.Console.Logger.Error($"Error loading graph: {e.Message}");
                }
            }
            else
            {
                Nodes = new List<Node>();
                LabApi.Features.Console.Logger.Info("No graph file found. Starting with empty graph.");
            }
        }

        public void SaveGraph()
        {
            try
            {
                string txtPath = GraphFilePath.Replace(".json", ".txt");
                string dir = Path.GetDirectoryName(txtPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var lines = new List<string>();
                foreach(var n in Nodes)
                {
                    string conns = string.Join(";", n.ConnectedNodeIds);
                    lines.Add($"{n.Id}|{n.X.ToString(System.Globalization.CultureInfo.InvariantCulture)}|{n.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}|{n.Z.ToString(System.Globalization.CultureInfo.InvariantCulture)}|{conns}");
                }
                File.WriteAllLines(txtPath, lines);
                LabApi.Features.Console.Logger.Info($"Saved {Nodes.Count} nodes to txt file.");
            }
            catch (Exception e)
            {
                LabApi.Features.Console.Logger.Error($"Error saving graph: {e.Message}");
            }
        }

        public Node AddNode(Vector3 position)
        {
            int nextId = Nodes.Any() ? Nodes.Max(n => n.Id) + 1 : 1;
            var node = new Node(nextId, position.x, position.y, position.z);
            Nodes.Add(node);
            
            foreach (var other in Nodes)
            {
                if (other.Id == nextId) continue;
                Vector3 otherPos = new Vector3(other.X, other.Y, other.Z);
                if (Vector3.Distance(position, otherPos) <= 5.0f)
                {
                    node.ConnectedNodeIds.Add(other.Id);
                    other.ConnectedNodeIds.Add(node.Id);
                }
            }

            return node;
        }

        public bool ForceConnect(int id1, int id2)
        {
            var n1 = Nodes.FirstOrDefault(n => n.Id == id1);
            var n2 = Nodes.FirstOrDefault(n => n.Id == id2);
            if (n1 != null && n2 != null)
            {
                if (!n1.ConnectedNodeIds.Contains(id2)) n1.ConnectedNodeIds.Add(id2);
                if (!n2.ConnectedNodeIds.Contains(id1)) n2.ConnectedNodeIds.Add(id1);
                return true;
            }
            return false;
        }

        public bool ForceDisconnect(int id1, int id2)
        {
            var n1 = Nodes.FirstOrDefault(n => n.Id == id1);
            var n2 = Nodes.FirstOrDefault(n => n.Id == id2);
            if (n1 != null && n2 != null)
            {
                n1.ConnectedNodeIds.Remove(id2);
                n2.ConnectedNodeIds.Remove(id1);
                return true;
            }
            return false;
        }

        public List<Node> GetPath(Node start, Node end)
        {
            if (start.Id == end.Id) return new List<Node> { start };

            Dictionary<int, float> dist = new Dictionary<int, float>();
            Dictionary<int, int> prev = new Dictionary<int, int>();
            List<int> unvisited = new List<int>();

            foreach (var node in Nodes)
            {
                dist[node.Id] = float.MaxValue;
                unvisited.Add(node.Id);
            }
            dist[start.Id] = 0;

            while (unvisited.Count > 0)
            {
                unvisited.Sort((a, b) => dist[a].CompareTo(dist[b]));
                int currentId = unvisited[0];
                unvisited.RemoveAt(0);

                if (currentId == end.Id || dist[currentId] == float.MaxValue)
                    break;

                var currentNode = Nodes.First(n => n.Id == currentId);
                Vector3 currentPos = new Vector3(currentNode.X, currentNode.Y, currentNode.Z);

                foreach (var neighborId in currentNode.ConnectedNodeIds)
                {
                    if (!unvisited.Contains(neighborId)) continue;
                    
                    var neighborNode = Nodes.First(n => n.Id == neighborId);
                    Vector3 neighborPos = new Vector3(neighborNode.X, neighborNode.Y, neighborNode.Z);
                    
                    float weight = Vector3.Distance(currentPos, neighborPos);
                    float alt = dist[currentId] + weight;
                    
                    if (alt < dist[neighborId])
                    {
                        dist[neighborId] = alt;
                        prev[neighborId] = currentId;
                    }
                }
            }

            List<Node> path = new List<Node>();
            if (!prev.ContainsKey(end.Id)) return path;

            int curr = end.Id;
            while (curr != start.Id)
            {
                path.Add(Nodes.First(n => n.Id == curr));
                curr = prev[curr];
            }
            path.Reverse();
            return path;
        }
    }
}
