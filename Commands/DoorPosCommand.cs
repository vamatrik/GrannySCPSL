using System;
using System.Linq;
using CommandSystem;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace GrannySCPSL.Commands
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class DoorPosCommand : ICommand
    {
        public string Command => "doorpos";
        public string[] Aliases => new string[] { };
        public string Description => "Узнать позицию ближайшей двери";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);
            if (player == null)
            {
                response = "Только для игроков.";
                return false;
            }

            var closestDoor = UnityEngine.Object.FindObjectsOfType<DoorVariant>()
                .OrderBy(d => Vector3.Distance(player.Position, d.transform.position))
                .FirstOrDefault();

            if (closestDoor != null && Vector3.Distance(player.Position, closestDoor.transform.position) < 5f)
            {
                response = $"Ближайшая дверь: {closestDoor.name}\nПозиция: {closestDoor.transform.position.x}, {closestDoor.transform.position.y}, {closestDoor.transform.position.z}";
                return true;
            }

            response = "Рядом нет дверей (ближе 5 метров).";
            return false;
        }
    }
}
