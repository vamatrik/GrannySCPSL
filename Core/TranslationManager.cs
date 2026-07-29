using System.Collections.Generic;
using LabApi.Features.Wrappers;

namespace GrannySCPSL.Core
{
    public static class TranslationManager
    {
        public static Dictionary<string, string> PlayerLanguages = new Dictionary<string, string>();

        public static void SetLanguage(Player player, string lang)
        {
            PlayerLanguages[player.PlayerId.ToString()] = lang;
        }

        public static string GetLanguage(Player player)
        {
            if (player == null) return "ru";
            string id = player.PlayerId.ToString();
            return PlayerLanguages.TryGetValue(id, out string lang) ? lang : "ru";
        }

        public static string GetString(string key, Player player = null)
        {
            string lang = GetLanguage(player);
            if (lang == "en" && EnTranslations.TryGetValue(key, out string enText))
            {
                return enText;
            }
            if (RuTranslations.TryGetValue(key, out string ruText))
            {
                return ruText;
            }
            return key; // fallback
        }

        public static void BroadcastAll(string key, ushort duration, bool shouldClearPrevious = true, params object[] args)
        {
            foreach (var p in Player.GetAll())
            {
                string text = GetString(key, p);
                if (args != null && args.Length > 0)
                {
                    text = string.Format(text, args);
                }
                p.SendBroadcast(text, duration, shouldClearPrevious: shouldClearPrevious);
            }
        }

        private static readonly Dictionary<string, string> RuTranslations = new Dictionary<string, string>
        {
            // GameManager
            {"waiting_players", "<size=25><color=#a3a3a3>To change language to English, type <b>.en</b> in console (~)</color></size>\n<color=red>Р</color>е<color=red>ж</color>и<color=red>м</color> g<color=red>r</color>a<color=red>n</color>n<color=red>y</color> з а<color=red>п</color>у<color=red>с</color>к<color=red>а</color>е т с<color=red>я</color>..."},
            {"day_1", "День 1"},
            {"day_n", "День {0}"},
            {"day_5_start", "<color=red>НАЧАЛСЯ 5 ДЕНЬ!</color>"},
            {"victory", "Победа!"},
            {"victory_escaped", "ПОБЕДА! ВЫ СБЕЖАЛИ ОТ ГРЕННИ!"},
            {"fun_time_left", "До конца веселья {0} секунд{1}"},
            {"game_over", "ИГРА ОКОНЧЕНА."},

            // GateAMechanics
            {"gate_hack_start", "Отлично! Тяжелая дверь взломана, но она тяжелая!"},
            {"gate_hack_opening", "Дверь потихоньку открывается, подождите 25 секунд..."},
            {"gate_opening_cancel", "Открытие прервано"},
            {"gate_need_card", "Аппарат не отвечает. Требуется ключ-карта."},
            {"gate_need_item", "У вас нет устройства для взлома. Найдите его или скрафтите."},
            {"hack_no_player", "Только для игроков."},
            {"hack_not_hacking", "Вы сейчас не взламываете дверь."},
            {"hack_usage", "Использование: .hack <ответ>"},
            {"hack_success_full", "ВЗЛОМ УСПЕШЕН! Внешняя дверь разблокирована."},
            {"hack_success_stage", "ПРАВИЛЬНО! Переход на следующий этап..."},
            {"hack_failed_console", "ОШИБКА ДОСТУПА! Тревога поднята."},
            {"hack_failed_hint", "ОШИБКА! SCP-939 ВЫДВИНУЛАСЬ К ВАМ!"},
            {"hack_need_device_in_hand", "Вам нужно держать устройство взлома в руках!"},
            {"gate_need_salt", "Дверь заблокирована. Нужна соль."},
            {"gate_wrong_door", "Не та дверь."},
            {"gate_no_hack_granny", "Невозможно взломать. Гренни мертва."},
            {"gate_granny_alive", "Доступ запрещен. Гренни жива. Сначала избавьтесь от нее."},
            {"gate_opening", "Дверь открывается!"},
            {"gate_opening_all", "Ворота А открываются!"},
            {"gate_boarded", "Дверь заколочена досками!"},
            {"gate_radio_charged", "Устройство заряжено!"},
            {"gate_need_battery", "Для аппарата требуется Батарея."},
            {"gate_hack_started", "Взлом начат! Откройте консоль (~)."},
            {"gate_need_hack", "Дверь заблокирована. Необходимо устройство для взлома."},
            {"gate_939_bio", "Биометрия SCP-939 подтверждена."},
            {"gate_need_939", "Требуется биометрия SCP-939 (запах)."},
            {"gate_math_1", "{0} + {1} = ?"},
            {"gate_math_2", "{0} + {1} = ?"},
            {"gate_math_3", "{0} * {1} = ?"},
            {"gate_math_4", "2 в степени {0} = ?"},
            {"gate_hack_stage", "Стадия взлома: {0}/4\nРешите пример (введите .hack ОТВЕТ): {1}"},
            {"gate_elevators_need_battery", "Лифты обесточены. Нужна Огромная Батарея."},
            {"gate_elevators_charged", "Питание лифтов Ворот А восстановлено!"},
            {"gate_boards_broken", "Доски сломаны!"},

            // ItemManager
            {"im_914_charged", "SCP-914 успешно заряжена от огромной батареи!"},
            {"im_914_already_charged", "SCP-914 уже заряжена!"},
            {"im_914_needs_charge", "Аппарат разряжен"},
            {"im_914_success_charge", "Вы успешно зарядили аппарат!"},
            {"im_device_discharged", "Устройство разряжено! Найдите источник питания."},
            {"im_upgrade_success", "Улучшение успешно!"},
            {"im_upgrade_destroyed", "Предмет уничтожен при попытке улучшения!"},
            {"im_upgrade_fail", "Улучшение не удалось. Предмет уничтожен!"},
            {"im_cannot_upgrade", "Этот предмет нельзя улучшить."},
            {"im_hold_item", "Держите предмет в руках для улучшения."},
            {"im_inventory_full", "У ТЕБЯ УЖЕ ЕСТЬ ПРЕДМЕТ! ВЫКИНЬ ЕГО, ЧТОБЫ ВЗЯТЬ ДРУГОЙ."},
            {"im_picked_up", "ВЫ ПОДОБРАЛИ:\n<color=yellow>{0}</color>"},
            {"im_in_hands", "В руках:\n<color=yellow>{0}</color>"},
            {"im_lantern_hint", "Не занимает места"},
            {"im_use_huge_battery", "Тебе нужно использовать этот предмет для побега!"},
            {"im_use_flint", "Это кремень. Его нельзя есть!"},
            {"im_use_explosive", "Нужно приложить эту взрывчатку к двери (PT-00)!"},
            {"im_puzzle_need", "Собери пазл: {0}/3 частей."},
            {"im_914_unlocked", "Гермоворота 914 успешно разблокированы! Теперь примените карту."},
            {"im_914_locked", "Гермоворота заблокированы! Приложите палец к сканеру (Лист с отпечатком)."},
            {"im_pt00_need_explosive", "Принесите взрывчатку и киньте ее в дверь."},
            {"im_pt00_activated", "Взрывчатка активирована! У вас 5 секунд!"},
            {"im_pt00_need_flint", "Активируйте взрывчатку Кремнем (клик по двери с ним в руках)."},
            {"im_door_locked_forever", "Двери заблокированы навсегда."},
            {"im_door_need_hammer", "Дверь заколочена. Тебе нужен молоток!"},
            {"im_boards_broken", "Ты сломал доски!"},

            // Events
            {"event_reveal", "ИВЕНТ: Вскрытие позиции! Гренни знает где вы..."},
            {"event_good_hearing", "ИВЕНТ: Отличный слух! Гренни слышит всё на 200 метров на 45 сек!"},
            {"event_good_hearing_end", "Слух Гренни вернулся в норму."},
            {"event_teleport", "ИВЕНТ: Случайный телепорт! Гренни переместилась..."},
            {"event_noisy", "ИВЕНТ: Шумная Гренни! Имитация звуков раз в 2 секунды (25 сек)!"},
            {"event_noisy_end", "Имитация звуков прекратилась."},
            {"event_bad_hearing", "ИВЕНТ: Плохой слух! Гренни ничего не слышит 45 сек!"},
            {"event_slow_granny", "ИВЕНТ: Медленная Гренни! Бабка замедлена на 20 сек!"},
            {"event_slow_granny_end", "Гренни снова движется с обычной скоростью."},
            {"event_fast_granny", "ИВЕНТ: Быстрая Гренни! Бабка ускорилась на 20 сек!"},
            {"event_big_pockets", "ИВЕНТ: Большие карманы! Можно нести 2 предмета (30 сек)!"},
            {"event_big_pockets_end", "Большие карманы закончились!"},
            {"event_slow_players", "ИВЕНТ: Замедление игроков! Все замедлены на 20 сек!"},
            {"event_slow_players_end", "Вы снова двигаетесь с обычной скоростью."},
            {"event_fast_players", "ИВЕНТ: Ускорение игроков! Все ускорены на 20 сек!"},
            {"event_fast_players_end", "Эффект ускорения спал."},
            {"event_medkit", "ИВЕНТ: Медицинская помощь! Аптечки и таблетки доставлены!"},
            {"event_invis", "ИВЕНТ: Взрывное зелье невидимости! Вы невидимы 10 сек, а Гренни - 20 сек!"},
            {"event_invis_player_end", "Ваша невидимость спала!"},
            {"event_invis_granny_end", "Гренни снова видима!"},

            // GrannyAI
            {"granny_woke_up", "Гренни проснулась!"},
            {"granny_stunned", "Гренни оглушена!"},
            {"granny_survive", "Выживите 40 секунд! Бегите!"},
            {"death_granny", "Гренни убила вас!"},

            // Item Names
            {"item_hammer", "Молоток"},
            {"item_battery", "Батарея"},
            {"item_hacker_device", "Устройство Взлома"},
            {"item_two_panaceas", "Панацея"},
            {"item_explosive_bag", "Сумка Взрывчатки"},
            {"item_flint", "Кремень"},
            {"item_huge_battery", "Огромная Батарея"},
            {"item_lantern", "Фонарь"},
            {"item_scp207", "SCP-207"},
            {"item_medkit", "Аптечка"},
            {"item_painkiller", "Обезболивающее"},
            {"item_ammo", "Сумка с патронами 9x19"},
            {"item_invisibility", "Шапка невидимка"},
            {"item_granny_smell", "Колба с запахом Гренни"},
            {"item_fingerprint", "Лист с отпечатком"},
            {"item_guncom18", "Пистолет COM-18"},
            {"item_keycardjanitor", "Карта Уборщика"},
            {"item_keycardscientist", "Карта Ученого"},
            {"item_keycardresearchcoordinator", "Карта Менеджера Исследований"},
            {"item_keycardzonemanager", "Карта Менеджера Зоны"},
            {"item_keycardguard", "Карта Охранника"},
            {"item_keycardntfofficer", "Карта Офицера МОГ"},
            {"item_keycardcontainmentengineer", "Карта Инженера Содержания"},
            {"item_keycardntflieutenant", "Карта Сержанта МОГ"},
            {"item_keycardntfcommander", "Карта Капитана МОГ"},
            {"item_keycardfacilitymanager", "Карта Менеджера Комплекса"},
            {"item_keycardchaosinsurgency", "Устройство Взлома Повстанцев Хаоса"},
            {"item_keycardo5", "Карта O5"},
            {"item_unknown", "Неизвестный предмет"},
            {"item_adrenaline", "Адреналин"}
        };

        private static readonly Dictionary<string, string> EnTranslations = new Dictionary<string, string>
        {
            // GameManager
            {"waiting_players", "<size=25><color=#a3a3a3>Для смены языка на русский введите <b>.ru</b> в консоли (~)</color></size>\n<color=red>G</color>r<color=red>a</color>n<color=red>n</color>y... W<color=red>e</color> c<color=red>a</color>m<color=red>e</color> t<color=red>o</color> G<color=red>r</color>a<color=red>n</color>n<color=red>y</color>."},
            {"day_1", "Day 1"},
            {"day_n", "Day {0}"},
            {"day_5_start", "<color=red>DAY 5 HAS STARTED!</color>"},
            {"victory", "Victory!"},
            {"victory_escaped", "VICTORY! YOU ESCAPED FROM GRANNY!"},
            {"fun_time_left", "Time until fun ends: {0} second{1}"},
            {"game_over", "GAME OVER."},

            // GateAMechanics
            {"gate_hack_start", "Great! The heavy door is hacked, but it's heavy!"},
            {"gate_hack_opening", "The door is opening slowly, wait 25 seconds..."},
            {"gate_opening_cancel", "Opening cancelled"},
            {"gate_need_card", "Device unresponsive. Need a keycard."},
            {"gate_need_item", "You don't have a hacking device. Find one or craft it."},
            {"hack_no_player", "Players only."},
            {"hack_not_hacking", "You are not hacking a door right now."},
            {"hack_usage", "Usage: .hack <answer>"},
            {"hack_success_full", "HACK SUCCESSFUL! Outer door unlocked."},
            {"hack_success_stage", "CORRECT! Proceeding to next stage..."},
            {"hack_failed_console", "ACCESS DENIED! Alarm raised."},
            {"hack_failed_hint", "ERROR! SCP-939 IS HEADING TO YOUR LOCATION!"},
            {"hack_need_device_in_hand", "You need to hold the hacking device in your hands!"},
            {"gate_need_salt", "Door is blocked. Need salt."},
            {"gate_wrong_door", "Wrong door."},
            {"gate_no_hack_granny", "Cannot hack. Granny is dead."},
            {"gate_granny_alive", "Access denied. Granny is alive. Get rid of Granny first."},
            {"gate_opening", "Door is opening!"},
            {"gate_opening_all", "Gate A is opening!"},
            {"gate_boarded", "The door is boarded up!"},
            {"gate_radio_charged", "Device charged!"},
            {"gate_need_battery", "The device requires a Battery."},
            {"gate_hack_started", "Hack started! Open the console (~)."},
            {"gate_need_hack", "Door is locked. A hacking device is required."},
            {"gate_939_bio", "SCP-939 biometrics confirmed."},
            {"gate_need_939", "SCP-939 biometrics required (smell)."},
            {"gate_math_1", "{0} + {1} = ?"},
            {"gate_math_2", "{0} + {1} = ?"},
            {"gate_math_3", "{0} * {1} = ?"},
            {"gate_math_4", "2 to the power of {0} = ?"},
            {"gate_hack_stage", "Hack stage: {0}/4\nSolve (type .hack ANSWER): {1}"},
            {"gate_elevators_need_battery", "Elevators have no power. Need a Huge Battery."},
            {"gate_elevators_charged", "Gate A elevators power restored!"},
            {"gate_boards_broken", "Boards are broken!"},

            // ItemManager
            {"im_914_charged", "SCP-914 is successfully charged from the huge battery!"},
            {"im_914_already_charged", "SCP-914 is already charged!"},
            {"im_914_needs_charge", "Device is discharged"},
            {"im_914_success_charge", "You have successfully charged the device!"},
            {"im_device_discharged", "Device is discharged! Find a power source."},
            {"im_upgrade_success", "Upgrade successful!"},
            {"im_upgrade_destroyed", "Item was destroyed during the upgrade!"},
            {"im_upgrade_fail", "Upgrade failed. Item destroyed!"},
            {"im_cannot_upgrade", "This item cannot be upgraded."},
            {"im_hold_item", "Hold an item in your hands to upgrade."},
            {"im_inventory_full", "YOU ALREADY HAVE AN ITEM! DROP IT TO TAKE ANOTHER."},
            {"im_picked_up", "YOU PICKED UP:\n<color=yellow>{0}</color>"},
            {"im_in_hands", "In hands:\n<color=yellow>{0}</color>"},
            {"im_lantern_hint", "Doesn't take up space"},
            {"im_use_huge_battery", "You must use this item for escape!"},
            {"im_use_flint", "This is flint. Do not eat it!"},
            {"im_use_explosive", "You must place this explosive on the door (PT-00)!"},
            {"im_puzzle_need", "Collect the puzzle: {0}/3 parts."},
            {"im_914_unlocked", "SCP-914 doors successfully unlocked! Now use a keycard."},
            {"im_914_locked", "Blast doors locked! Place fingerprint on scanner (Fingerprint Paper)."},
            {"im_pt00_need_explosive", "Bring the explosive and throw it at the door."},
            {"im_pt00_activated", "Explosive activated! You have 5 seconds!"},
            {"im_pt00_need_flint", "Activate the explosive with Flint (click on door while holding)."},
            {"im_door_locked_forever", "Doors are permanently locked."},
            {"im_door_need_hammer", "Door is boarded up. You need a hammer!"},
            {"im_boards_broken", "You broke the boards!"},

            // Events
            {"event_reveal", "EVENT: Position Reveal! Granny knows where you are..."},
            {"event_good_hearing", "EVENT: Great Hearing! Granny hears everything within 200 meters for 45 sec!"},
            {"event_good_hearing_end", "Granny's hearing returned to normal."},
            {"event_teleport", "EVENT: Random Teleport! Granny has moved..."},
            {"event_noisy", "EVENT: Noisy Granny! Sound imitation every 2 seconds (25 sec)!"},
            {"event_noisy_end", "Sound imitation has stopped."},
            {"event_bad_hearing", "EVENT: Bad Hearing! Granny cannot hear anything for 45 sec!"},
            {"event_slow_granny", "EVENT: Slow Granny! Granny is slowed down for 20 sec!"},
            {"event_slow_granny_end", "Granny is moving at normal speed again."},
            {"event_fast_granny", "EVENT: Fast Granny! Granny has sped up for 20 sec!"},
            {"event_big_pockets", "EVENT: Big Pockets! You can carry 2 items (30 sec)!"},
            {"event_big_pockets_end", "Big pockets have ended!"},
            {"event_slow_players", "EVENT: Player Slowdown! Everyone is slowed for 20 sec!"},
            {"event_slow_players_end", "You are moving at normal speed again."},
            {"event_fast_players", "EVENT: Player Speedup! Everyone is sped up for 20 sec!"},
            {"event_fast_players_end", "Speedup effect has worn off."},
            {"event_medkit", "EVENT: Medical Aid! Medkits and painkillers delivered!"},
            {"event_invis", "EVENT: Explosive Invisibility Potion! You are invisible for 10 sec, and Granny for 20 sec!"},
            {"event_invis_player_end", "Your invisibility has worn off!"},
            {"event_invis_granny_end", "Granny is visible again!"},

            // GrannyAI
            {"granny_woke_up", "Granny woke up!"},
            {"granny_stunned", "Granny is stunned!"},
            {"granny_survive", "Survive for 40 seconds! Run!"},
            {"death_granny", "Granny killed you!"},

            // Item Names
            {"item_hammer", "Hammer"},
            {"item_battery", "Battery"},
            {"item_hacker_device", "Hacking Device"},
            {"item_two_panaceas", "Panacea"},
            {"item_explosive_bag", "Explosive Bag"},
            {"item_flint", "Flint"},
            {"item_huge_battery", "Huge Battery"},
            {"item_lantern", "Lantern"},
            {"item_scp207", "SCP-207"},
            {"item_medkit", "Medkit"},
            {"item_painkiller", "Painkiller"},
            {"item_ammo", "Ammo Bag (9x19)"},
            {"item_invisibility", "Invisibility Hat"},
            {"item_granny_smell", "Flask with Granny Smell"},
            {"item_fingerprint", "Fingerprint Paper"},
            {"item_guncom18", "COM-18 Pistol"},
            {"item_keycardjanitor", "Janitor Keycard"},
            {"item_keycardscientist", "Scientist Keycard"},
            {"item_keycardresearchcoordinator", "Research Supervisor Keycard"},
            {"item_keycardzonemanager", "Zone Manager Keycard"},
            {"item_keycardguard", "Guard Keycard"},
            {"item_keycardntfofficer", "MTF Private Keycard"},
            {"item_keycardcontainmentengineer", "Containment Engineer Keycard"},
            {"item_keycardntflieutenant", "MTF Sergeant Keycard"},
            {"item_keycardntfcommander", "MTF Captain Keycard"},
            {"item_keycardfacilitymanager", "Facility Manager Keycard"},
            {"item_keycardchaosinsurgency", "Chaos Insurgency Access Device"},
            {"item_keycardo5", "O5 Keycard"},
            {"Базовая Карта", "Base Keycard"},
            {"Карта от Чекпоинта", "Checkpoint Keycard"},
            {"Пропуск в Оружейную", "Armory Pass"},
            {"Пазл 1", "Puzzle Piece 1"},
            {"Пазл 2", "Puzzle Piece 2"},
            {"Пазл 3", "Puzzle Piece 3"},
            {"Лист с отпечатком", "Fingerprint Paper"},
            {"item_unknown", "Unknown Item"},
            {"item_adrenaline", "Adrenaline"}
        };
    }
}


