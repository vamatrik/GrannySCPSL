using CommandSystem;
using System;
using LabApi.Features.Wrappers;
using GrannySCPSL.Graph;

namespace GrannySCPSL.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    // [CommandHandler(typeof(ClientCommandHandler))]
    public class NodeCommand : ICommand
    {
        public string Command => "node";
        public string[] Aliases => new string[] { };
        public string Description => "Manage graph nodes for Granny.";

        private static System.Collections.Generic.Dictionary<string, int> _connectNearState = new System.Collections.Generic.Dictionary<string, int>();
        private static System.Collections.Generic.Dictionary<string, int> _disconnectNearState = new System.Collections.Generic.Dictionary<string, int>();

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count == 0)
            {
                response = "Usage: node <add/save/clear>";
                return false;
            }

            var player = Player.Get(sender);
            if (player == null)
            {
                response = "You must be a player to use this command.";
                return false;
            }

            switch (arguments.At(0).ToLower())
            {
                case "add":
                    var node = GraphManager.Instance.AddNode(player.Position);
                    Visualizer.SpawnNodeVisual(node);
                    foreach (var connectedId in node.ConnectedNodeIds)
                    {
                        var other = GraphManager.Instance.Nodes.Find(n => n.Id == connectedId);
                        if (other != null) Visualizer.SpawnConnectionVisual(node, other);
                    }
                    response = $"Added node {node.Id} at {node.X:F1}, {node.Y:F1}, {node.Z:F1}. Auto-connected to {node.ConnectedNodeIds.Count} nodes.";
                    return true;
                
                case "save":
                    GraphManager.Instance.SaveGraph();
                    response = "Graph saved successfully.";
                    return true;
                
                case "clear":
                    GraphManager.Instance.Nodes.Clear();
                    Visualizer.ClearAllVisuals();
                    GraphManager.Instance.SaveGraph();
                    response = "Graph cleared and visuals removed.";
                    return true;
                
                case "delete":
                    if (arguments.Count < 2) { response = "Usage: node delete <id>"; return false; }
                    if (int.TryParse(arguments.At(1), out int delId))
                    {
                        var delNode = GraphManager.Instance.Nodes.Find(n => n.Id == delId);
                        if (delNode != null)
                        {
                            GraphManager.Instance.Nodes.Remove(delNode);
                            foreach(var n in GraphManager.Instance.Nodes) n.ConnectedNodeIds.Remove(delId);
                            Visualizer.ClearAllVisuals();
                            // Need to redraw manually with .node redraw
                            response = $"Node {delId} deleted. (Run .node redraw)";
                            return true;
                        }
                    }
                    response = "Node not found.";
                    return false;

                case "forceconnect":
                    if (arguments.Count < 3) { response = "Usage: node forceconnect <id1> <id2>"; return false; }
                    if (int.TryParse(arguments.At(1), out int fc1) && int.TryParse(arguments.At(2), out int fc2))
                    {
                        var n1 = GraphManager.Instance.Nodes.Find(n => n.Id == fc1);
                        var n2 = GraphManager.Instance.Nodes.Find(n => n.Id == fc2);
                        if (n1 != null && n2 != null)
                        {
                            if (!n1.ConnectedNodeIds.Contains(n2.Id)) n1.ConnectedNodeIds.Add(n2.Id);
                            if (!n2.ConnectedNodeIds.Contains(n1.Id)) n2.ConnectedNodeIds.Add(n1.Id);
                            Visualizer.SpawnConnectionVisual(n1, n2);
                            response = $"Connected {fc1} and {fc2}.";
                            return true;
                        }
                    }
                    response = "Nodes not found.";
                    return false;

                case "forcedisconnect":
                    if (arguments.Count < 3) { response = "Usage: node forcedisconnect <id1> <id2>"; return false; }
                    if (int.TryParse(arguments.At(1), out int fd1) && int.TryParse(arguments.At(2), out int fd2))
                    {
                        var nd1 = GraphManager.Instance.Nodes.Find(n => n.Id == fd1);
                        var nd2 = GraphManager.Instance.Nodes.Find(n => n.Id == fd2);
                        if (nd1 != null && nd2 != null)
                        {
                            nd1.ConnectedNodeIds.Remove(nd2.Id);
                            nd2.ConnectedNodeIds.Remove(nd1.Id);
                            Visualizer.ClearAllVisuals();
                            // Visualizer will be updated when they redraw or on next frame. Actually we should just redraw.
                            response = $"Disconnected {fd1} and {fd2}. (Run .node redraw to update visuals)";
                            return true;
                        }
                    }
                    response = "Nodes not found.";
                    return false;

                case "redraw":
                    Visualizer.ClearAllVisuals();
                    foreach (var n in GraphManager.Instance.Nodes) Visualizer.SpawnNodeVisual(n);
                    foreach (var n in GraphManager.Instance.Nodes)
                    {
                        foreach (var cId in n.ConnectedNodeIds)
                        {
                            if (cId > n.Id) // avoid double draw
                            {
                                var other = GraphManager.Instance.Nodes.Find(o => o.Id == cId);
                                if (other != null) Visualizer.SpawnConnectionVisual(n, other);
                            }
                        }
                    }
                    response = "Redrew graph visuals.";
                    return true;

                case "near":
                    Node closest = null;
                    float minD = float.MaxValue;
                    foreach(var n in GraphManager.Instance.Nodes)
                    {
                        float d = UnityEngine.Vector3.Distance(player.Position, new UnityEngine.Vector3(n.X, n.Y, n.Z));
                        if(d < minD) { minD = d; closest = n; }
                    }
                    if (closest != null)
                        response = $"Nearest node is ID: {closest.Id} (Distance: {minD:F1}m)";
                    else
                        response = "No nodes found.";
                    return true;

                case "connectnear":
                {
                    Node cClosest = null;
                    float cMin = float.MaxValue;
                    foreach(var n in GraphManager.Instance.Nodes)
                    {
                        float d = UnityEngine.Vector3.Distance(player.Position, new UnityEngine.Vector3(n.X, n.Y, n.Z));
                        if(d < cMin) { cMin = d; cClosest = n; }
                    }
                    if (cClosest == null) { response = "No nodes found."; return false; }
                    
                    if (_connectNearState.TryGetValue(player.Nickname, out int firstId))
                    {
                        if (firstId == cClosest.Id) { response = "You selected the same node twice."; return false; }
                        GraphManager.Instance.ForceConnect(firstId, cClosest.Id);
                        var firstNode = GraphManager.Instance.Nodes.Find(n => n.Id == firstId);
                        if(firstNode != null) Visualizer.SpawnConnectionVisual(firstNode, cClosest);
                        _connectNearState.Remove(player.Nickname);
                        response = $"Connected Node {firstId} and Node {cClosest.Id}!";
                        return true;
                    }
                    else
                    {
                        _connectNearState[player.Nickname] = cClosest.Id;
                        response = $"Selected Node {cClosest.Id}. Run .node connectnear again near another node to connect them.";
                        return true;
                    }
                }
                
                case "disconnectnear":
                {
                    Node cClosest = null;
                    float cMin = float.MaxValue;
                    foreach(var n in GraphManager.Instance.Nodes)
                    {
                        float d = UnityEngine.Vector3.Distance(player.Position, new UnityEngine.Vector3(n.X, n.Y, n.Z));
                        if(d < cMin) { cMin = d; cClosest = n; }
                    }
                    if (cClosest == null) { response = "No nodes found."; return false; }
                    
                    if (_disconnectNearState.TryGetValue(player.Nickname, out int firstId))
                    {
                        if (firstId == cClosest.Id) { response = "You selected the same node twice."; return false; }
                        GraphManager.Instance.ForceDisconnect(firstId, cClosest.Id);
                        Visualizer.ClearAllVisuals();
                        _disconnectNearState.Remove(player.Nickname);
                        response = $"Disconnected Node {firstId} and Node {cClosest.Id}! (Run .node redraw to update visuals)";
                        return true;
                    }
                    else
                    {
                        _disconnectNearState[player.Nickname] = cClosest.Id;
                        response = $"Selected Node {cClosest.Id}. Run .node disconnectnear again near another node to disconnect them.";
                        return true;
                    }
                }

                default:
                    response = "Invalid subcommand. Use add, save, clear, delete, forceconnect, forcedisconnect, redraw, near, connectnear, disconnectnear.";
                    return false;
            }
        }
    }
}
