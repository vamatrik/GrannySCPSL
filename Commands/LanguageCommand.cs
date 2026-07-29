using CommandSystem;
using System;
using LabApi.Features.Wrappers;
using GrannySCPSL.Core;

namespace GrannySCPSL.Commands
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class LanguageCommandRu : ICommand
    {
        public string Command => "ru";
        public string[] Aliases => new string[] { };
        public string Description => "Переключить язык на Русский";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (Player.TryGet(sender, out Player player))
            {
                TranslationManager.SetLanguage(player, "ru");
                response = "Язык успешно изменен на Русский.";
                return true;
            }
            response = "Только игроки могут использовать эту команду.";
            return false;
        }
    }

    [CommandHandler(typeof(ClientCommandHandler))]
    public class LanguageCommandEn : ICommand
    {
        public string Command => "en";
        public string[] Aliases => new string[] { };
        public string Description => "Change language to English";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (Player.TryGet(sender, out Player player))
            {
                TranslationManager.SetLanguage(player, "en");
                response = "Language successfully changed to English.";
                return true;
            }
            response = "Only players can use this command.";
            return false;
        }
    }

    [CommandHandler(typeof(ClientCommandHandler))]
    public class GrannyHelpCommand : ICommand
    {
        public string Command => "granny";
        public string[] Aliases => new string[] { "help" };
        public string Description => "Показать информацию о режиме";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = "\nИгру сделали Vamatrik и Antigravity.\n" +
                       "Выживайте в комплексе, ищите предметы, открывайте двери и попытайтесь сбежать от ужасной бабки!\n" +
                       "Доступные команды клиента:\n" +
                       ".en - Переключить язык на Английский\n" +
                       ".ru - Переключить язык на Русский\n" +
                       ".help - Помощь по командам\n" +
                       ".granny - Описание режима";
            return true;
        }
    }
}
