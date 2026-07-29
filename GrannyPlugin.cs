using LabApi.Loader.Features.Plugins;
using LabApi.Events.Handlers;
using System;

namespace GrannySCPSL
{
    public class Config
    {
        public bool IsEnabled { get; set; } = true;
        public int FixedMapSeed { get; set; } = 12345;
    }

    public class GrannyPlugin : Plugin<Config>
    {
        public override string Name => "GrannyMinigame";
        public override string Author => "Antigravity";
        public override Version Version => new Version(1, 0, 0);
        public override Version RequiredApiVersion => new Version(1, 0, 0);
        public override string Description => "Granny Minigame";

        public static GrannyPlugin Instance { get; private set; }

        public override void Enable()
        {
            Instance = this;
            
            Core.ItemManager.RegisterEvents();
            Core.GateAMechanics.RegisterEvents();

            var btn = new UserSettings.ServerSpecific.SSButton(
                99,
                "Granny SCPSL",
                "Начать игру",
                null,
                "Нажмите, чтобы запустить режим Бабки в лобби."
            );
            var list = UserSettings.ServerSpecific.ServerSpecificSettingsSync.DefinedSettings != null 
                ? new System.Collections.Generic.List<UserSettings.ServerSpecific.ServerSpecificSettingBase>(UserSettings.ServerSpecific.ServerSpecificSettingsSync.DefinedSettings)
                : new System.Collections.Generic.List<UserSettings.ServerSpecific.ServerSpecificSettingBase>();
            
            bool exists = false;
            foreach (var s in list) if (s.SettingId == 99) exists = true;
            if (!exists)
            {
                list.Add(btn);
                UserSettings.ServerSpecific.ServerSpecificSettingsSync.DefinedSettings = list.ToArray();
            }
            UserSettings.ServerSpecific.ServerSpecificSettingsSync.ServerOnSettingValueReceived += OnSSSReceived;

            // We are activating the AI and game logic!
            LabApi.Events.Handlers.ServerEvents.RoundStarted += Core.GameManager.Instance.OnRoundStarted;
            LabApi.Events.Handlers.ServerEvents.WaitingForPlayers += Core.GameManager.Instance.OnWaitingForPlayers;
            LabApi.Events.Handlers.PlayerEvents.Joined += Core.GameManager.Instance.OnPlayerJoined;
            LabApi.Events.Handlers.PlayerEvents.DroppedItem += Core.GameManager.Instance.OnItemDropped;
            LabApi.Events.Handlers.PlayerEvents.Jumped += Core.GameManager.Instance.OnPlayerJumped;
            LabApi.Events.Handlers.PlayerEvents.Death += Core.GameManager.Instance.OnPlayerDied;
            LabApi.Events.Handlers.PlayerEvents.Left += Core.GameManager.Instance.OnPlayerLeft;
            LabApi.Events.Handlers.PlayerEvents.InteractingDoor += Core.GameManager.Instance.OnInteractingDoor;
            
            if (!LabApi.Features.Wrappers.Round.IsRoundStarted)
            {
                Core.GameManager.Instance.OnWaitingForPlayers();
            }

            Graph.GraphManager.Instance.LoadGraph();
        }

        public override void Disable()
        {
            Core.ItemManager.UnregisterEvents();
            Core.GateAMechanics.UnregisterEvents();
            LabApi.Events.Handlers.ServerEvents.RoundStarted -= Core.GameManager.Instance.OnRoundStarted;
            LabApi.Events.Handlers.ServerEvents.WaitingForPlayers -= Core.GameManager.Instance.OnWaitingForPlayers;
            LabApi.Events.Handlers.PlayerEvents.DroppedItem -= Core.GameManager.Instance.OnItemDropped;
            LabApi.Events.Handlers.PlayerEvents.Jumped -= Core.GameManager.Instance.OnPlayerJumped;
            LabApi.Events.Handlers.PlayerEvents.Death -= Core.GameManager.Instance.OnPlayerDied;
            LabApi.Events.Handlers.PlayerEvents.Left -= Core.GameManager.Instance.OnPlayerLeft;
            UserSettings.ServerSpecific.ServerSpecificSettingsSync.ServerOnSettingValueReceived -= OnSSSReceived;
            Instance = null;
        }

        private void OnSSSReceived(ReferenceHub hub, UserSettings.ServerSpecific.ServerSpecificSettingBase setting)
        {
            if (setting.SettingId == 99)
            {
                if (!Core.GameManager.Instance.GameStarted)
                {
                    var p = LabApi.Features.Wrappers.Player.Get(hub);
                    if (p != null)
                    {
                        if (hub.serverRoles.RemoteAdmin)
                        {
                            if (Core.GameManager.Instance.GameEnded)
                            {
                                p.SendConsoleMessage("Игра уже была сыграна в этом раунде. Перезапустите раунд (команда roundrestart), чтобы вся карта и процессы корректно обновились.", "red");
                                p.SendHint("Игра уже окончена. Пропишите roundrestart в консоль сервера.", 10f);
                            }
                            else
                            {
                                p.SendConsoleMessage("Запуск игры...", "green");
                                p.SendHint("Постарайся сбежать как можно скорее...", 5f);
                                Core.GameManager.Instance.StartGame();
                            }
                        }
                        else
                        {
                            p.SendConsoleMessage("У вас нет прав для запуска режима.", "red");
                        }
                    }
                }
            }
        }
    }
}

