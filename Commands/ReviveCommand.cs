using CommandSystem;
using System;
using LabApi.Features.Wrappers;
using PlayerRoles;

namespace GrannySCPSL.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class ReviveCommand : ICommand
    {
        public string Command => "revive";
        public string[] Aliases => new string[] { };
        public string Description => "Revives a dead player in the current day";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count == 0)
            {
                response = "Usage: revive <player_id>";
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

            if (p.Role == RoleTypeId.ClassD)
            {
                response = "Player is already alive.";
                return false;
            }

            if (!Core.GameManager.ActivePlayers.Contains(p.PlayerId.ToString()))
            {
                Core.GameManager.ActivePlayers.Add(p.PlayerId.ToString());
            }
            
            p.SetRole(RoleTypeId.ClassD, RoleChangeReason.RoundStart);
            

            response = $"Player {p.Nickname} revived.";
            return true;
        }
    }
}




