using CommandSystem;
using LabApi.Features.Wrappers;
using System;
using System.Linq;
using GrannySCPSL.Core;

namespace GrannySCPSL.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class VideoModeCommand : ICommand
    {
        public string Command => "videomode";
        public string[] Aliases => new string[] { "vm" };
        public string Description => "Toggles video mode for a player (ignores 939). Usage: videomode <player_id>";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 1)
            {
                response = "Usage: videomode <player_id>";
                return false;
            }

            if (!int.TryParse(arguments.At(0), out int pid))
            {
                response = "Invalid Player ID.";
                return false;
            }

            var player = Player.GetAll().FirstOrDefault(p => p.PlayerId == pid);
            if (player == null)
            {
                response = "Player not found.";
                return false;
            }

            if (GameManager.VideoModePlayers.Contains(pid))
            {
                GameManager.VideoModePlayers.Remove(pid);
                response = $"Video mode disabled for {player.Nickname}.";
            }
            else
            {
                GameManager.VideoModePlayers.Add(pid);
                response = $"Video mode enabled for {player.Nickname}. 939 will now ignore them.";
            }

            return true;
        }
    }
}
