namespace BotService;

// Only this bot's card and public conversation are available, never other roles.
public sealed class RuleBasedDecisionMaker : IDecisionMaker
{
    public string MakeMessage(GameContext context)
    {
        var turn = context.Messages.Count(m => m.Sender.Equals(context.MyId));
        if (context.IsPlayerAmogus)
        {
            string[] vague = ["У меня это связано с яркими воспоминаниями.",
                "Это можно встретить в самых разных обстоятельствах.", "Думаю, каждый из нас с этим знаком."];
            return vague[turn % vague.Length];
        }
        var clue = context.Card.ToLowerInvariant() switch
        {
            "lion" => "Сила и величие.", "tiger" or "zebra" => "Узнать можно по полосатому рисунку.",
            "elephant" or "hippopotamus" or "rhinoceros" => "Размер трудно не заметить.",
            "giraffe" => "Смотрит на многих свысока.", "panda" => "Чёрное и белое — хорошее сочетание.",
            "kangaroo" => "Путешествует необычным способом.", "penguin" => "Холод ему не помеха.",
            "shark" or "crocodile" => "В воде с ним лучше не встречаться.", "dolphin" => "Любит компанию и воду.",
            "turtle" => "Спешка ему не свойственна.", "snake" => "Для движения ноги не обязательны.",
            "eagle" => "Всё видно с высоты.", "parrot" => "Может удивить разговором.",
            "owl" => "Ночная жизнь ему по душе.", "bear" => "Зимой его трудно застать активным.",
            "wolf" => "В компании сородичей чувствует себя увереннее.", "fox" => "В сказках ему приписывают хитрость.",
            "squirrel" => "Делает запасы на будущее.", "monkey" => "Ловкости можно позавидовать.",
            "camel" => "Суровые условия не мешают долгой дороге.",
            "rose" or "cactus" or "nettle" => "Лучше не трогать голыми руками.",
            "tulip" or "lilac" => "Ассоциируется с весной.", "sunflower" => "Солнечное настроение.",
            "daisy" => "Вспоминается летнее поле.", "birch" => "Светлый силуэт узнаётся издалека.",
            "oak" or "sequoia" => "Может пережить много поколений.", "palm tree" => "Напоминает об отдыхе в тепле.",
            "fir tree" => "Праздничные ассоциации.", "bamboo" => "Растёт быстрее, чем ожидаешь.",
            "dandelion" => "Ветер помогает отправиться в путь.", "lotus" or "reed" => "Искать стоит рядом с водой.",
            "fern" or "moss" => "Влажная тень — подходящее место.", "maple" => "Осенью выглядит особенно ярко.",
            "lily" or "orchid" => "Часто выбирают за красоту.", "aloe" => "Может пригодиться в домашнем уходе.",
            "vine" => "Хорошо цепляется за опору.", "jasmine" or "lavender" => "Запах запоминается надолго.",
            "gymnasium" or "university" => "Здесь проводят годы ради знаний.", "student" => "Учиться — его ежедневная работа.",
            "teacher" => "Помогает разобраться в сложном.", "notebook" or "journal" => "Бумага хранит много записей.",
            "diploma" => "Получают после долгого пути.", "credit" or "exam" => "Перед этим многие волнуются.",
            "lecture" => "Слушать и записывать полезно.", "marker" => "Позволяет выделить главное.",
            "briefcase" => "Помогает носить нужные вещи.", "discipline" => "Без этого трудно добиться результата.",
            "whistle" => "Звук может остановить действие.", "medal" => "Напоминание о достижении.",
            "timeout" => "Иногда нужна короткая передышка.", "fan" => "Эмоций больше, чем физической нагрузки.",
            "season ticket" => "Один раз приобрёл — много раз пришёл.", "equipment" => "Без подготовки не обойтись.",
            "disqualification" => "Нарушение правил дорого обходится.", "overtime" => "Основного времени не хватило.",
            "grandstand" => "Отсюда удобно наблюдать.", "mascot" => "Создаёт настроение и узнаваемость.",
            "standard" or "rank" => "Позволяет оценить уровень подготовки.", "championship" => "Сюда стремятся лучшие.",
            "ticket" => "Подтверждает право отправиться в путь.", "suitcase" => "Вместить всё нужное бывает трудно.",
            "passport" => "За границей без него непросто.", "layover" => "Пауза между частями пути.",
            "engine" => "Движение требует энергии.", "route" => "Лучше продумать заранее.",
            "flight attendant" => "Поможет, пока земля далеко внизу.", "fellow traveler" => "В дороге можно завести знакомство.",
            "security check" => "Придётся ненадолго остановиться ради безопасности.",
            "hitchhiking" => "Дорога зависит от доброты незнакомцев.", "customs" => "На границе могут задать вопросы.",
            _ => null
        };
        if (clue != null && turn % 3 != 1) return clue;
        return (turn % 3) switch
        {
            0 => context.Card.Count(char.IsLetter) > 7 ? "Название довольно длинное." : "Название легко запомнить.",
            1 => context.Card.Contains(' ') ? "Я описал бы это несколькими словами." : "Для этого достаточно одного слова.",
            _ => "У меня есть вполне конкретная ассоциация; не хочу выдавать слишком много."
        };
    }
    public authorization.UserId MakeVote(GameContext context)
    {
        var candidates = context.Players.Where(p => !p.Id.Equals(context.MyId)).ToArray();
        var silent = candidates.Where(p => context.Messages.Any(m => m.Sender.Equals(p.Id)
            && m.MessageBody == "Ход пропущен")).ToArray();
        var pool = silent.Length > 0 ? silent : candidates;
        return pool[Random.Shared.Next(pool.Length)].Id;
    }
}
