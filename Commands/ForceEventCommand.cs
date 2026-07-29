using CommandSystem;
using System;
using LabApi.Features.Wrappers;

namespace GrannySCPSL.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class ForceEventCommand : ICommand
    {
        public string Command => "forceevent";
        public string[] Aliases => new string[] { };
        public string Description => "Force trigger a specific event (1-12)";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!Core.GameManager.Instance.GameStarted)
            {
                response = "Game has not started yet.";
                return false;
            }

            if (arguments.Count == 0 || !int.TryParse(arguments.At(0), out int eventId) || eventId < 1 || eventId > 12)
            {
                response = "Usage: forceevent <1-12>";
                return false;
            }

            Core.EventManager.TriggerRandomEvent(eventId);
            
            response = $"Forced event #{eventId}.";
            return true;
        }
    }
}