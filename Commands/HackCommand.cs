using System;
using CommandSystem;
using GrannySCPSL.Core;
using LabApi.Features.Wrappers;

namespace GrannySCPSL.Commands
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class HackCommand : ICommand
    {
        public string Command => "hack";
        public string[] Aliases => new string[] { };
        public string Description => "Взлом двери Gate A";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);
            if (player == null)
            {
                response = TranslationManager.GetString("hack_no_player", null);
                return false;
            }

            if (player.CurrentItem == null || player.CurrentItem.Type != Core.CustomItems.HackerDevice)
            {
                response = TranslationManager.GetString("hack_need_device_in_hand", player);
                return false;
            }

            if (!GateAMechanics.HackStates.TryGetValue(player.UserId, out var state))
            {
                response = TranslationManager.GetString("hack_not_hacking", player);
                return false;
            }

            if (arguments.Count == 0)
            {
                response = TranslationManager.GetString("hack_usage", player);
                return false;
            }

            string answer = arguments.At(0);

            if (answer.Trim() == state.ExpectedAnswer.Trim())
            {
                state.Stage++;
                if (state.Stage > 4)
                {
                    GateAMechanics.OuterDoorHacked = true;
                    if (GateAMechanics.OuterDoor != null)
                    {
                        GateAMechanics.OuterDoor.NetworkTargetState = true;
                        GateAMechanics.OuterDoor.ServerChangeLock(Interactables.Interobjects.DoorUtils.DoorLockReason.SpecialDoorFeature, false);
                    }
                    GateAMechanics.HackStates.Remove(player.UserId);
                    player.SendHint(TranslationManager.GetString("hack_success_full", player), 5);
                    response = TranslationManager.GetString("hack_success_full", player);
                    
                    foreach(var p in Player.GetAll())
                    {
                        var serialsToRemove = new System.Collections.Generic.List<ushort>();
                        foreach(var item in p.ReferenceHub.inventory.UserInventory.Items.Values)
                        {
                            if (item.ItemTypeId == Core.CustomItems.HackerDevice)
                            {
                                serialsToRemove.Add(item.ItemSerial);
                            }
                        }
                        foreach(var serial in serialsToRemove)
                        {
                            p.ReferenceHub.inventory.UserInventory.Items.Remove(serial);
                            p.ReferenceHub.inventory.SendItemsNextFrame = true;
                        }
                    }
                    
                    return true;
                }
                
                GateAMechanics.GenerateHackProblem(player, true);
                response = TranslationManager.GetString("hack_success_stage", player);
                return true;
            }
            else
            {
                response = TranslationManager.GetString("hack_failed_console", player);
                player.SendHint(TranslationManager.GetString("hack_failed_hint", player), 5);
                
                if (GrannySCPSL.AI.GrannyAI.Instance != null)
                {
                    GrannySCPSL.AI.GrannyAI.Instance.HearNoise(player.Position);
                }
                
                return false;
            }
        }
    }
}
