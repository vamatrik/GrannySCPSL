using CommandSystem;
using System;
using LabApi.Features.Wrappers;
using PlayerRoles;

namespace GrannySCPSL.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class LatejoinCommand : ICommand
    {
        public string Command => "latejoin";
        public string[] Aliases => new string[] { };
        public string Description => "Latejoin a player as ClassD";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count == 0)
            {
                response = "Usage: latejoin <player_id>";
                return false;
            }

            if (!int.TryParse(arguments.At(0), out int id))
            {
                response = "Invalid player ID.";
                return false;
            }

            var p = Player.Get(id);
            if (p == null)
            {
                response = "Player not found.";
                return false;
            }

            if (!Core.GameManager.Instance.GameStarted)
            {
                response = "Game has not started yet.";
                return false;
            }

            if (!Core.GameManager.ActivePlayers.Contains(p.PlayerId.ToString()))
            {
                Core.GameManager.ActivePlayers.Add(p.PlayerId.ToString());
            }
            
            p.SetRole(RoleTypeId.ClassD, RoleChangeReason.RoundStart);
            
            
            response = $"Player {p.Nickname} latejoined as ClassD.";
            return true;
        }
    }
}




