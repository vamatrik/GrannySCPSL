using CommandSystem;
using System;

namespace GrannySCPSL.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class DumpMimicryCommand : ICommand
    {
        public string Command => "dumpmimicry";
        public string[] Aliases => new string[] { };
        public string Description => "Dump EnvMimicry";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            try {
                var mimicryType = typeof(PlayerRoles.PlayableScps.Scp939.Mimicry.EnvironmentalMimicry);
                var field = mimicryType.GetField("_syncOption", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (field != null) {
                    var t = field.FieldType;
                    response = "Field type: " + t.FullName + "\n";
                    if (t.IsEnum) {
                        foreach (var val in Enum.GetValues(t)) {
                            response += val.ToString() + " = " + (int)val + "\n";
                        }
                    }
                    return true;
                }
                response = "Field not found";
                return false;
            } catch (Exception ex) {
                response = ex.ToString();
                return false;
            }
        }
    }
}