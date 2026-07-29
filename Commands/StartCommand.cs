using CommandSystem;
using System;
using GrannySCPSL.Core;

namespace GrannySCPSL.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(ClientCommandHandler))]
    public class StartCommand : ICommand
    {
        public string Command => "granny";
        public string[] Aliases => new string[] { };
        public string Description => "Start the Granny minigame.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count == 0 || arguments.At(0).ToLower() != "start")
            {
                response = "Usage: granny start";
                return false;
            }

            if (!LabApi.Features.Wrappers.Round.IsRoundStarted)
            {
                response = "Пожалуйста, дождитесь начала раунда, прежде чем запускать мини-игру!";
                return false;
            }

            if (GameManager.Instance.GameStarted)
            {
                response = "Game is already running!";
                return false;
            }

            GameManager.Instance.StartGame();
            response = "Granny minigame started!";
            return true;
        }
    }
}
