using System.Text.RegularExpressions;

namespace BotService
{
    internal sealed record BotWritingStyle(
        string Name,
        bool UseCapitalLetters,
        bool UsePunctuation,
        bool UseComplexSentences)
    {
        // Все сочетания регистра, пунктуации и сложности. Стиль выбирается один раз на бота.
        public static IReadOnlyList<BotWritingStyle> Presets { get; } = Array.AsReadOnly(new[]
        {
            new BotWritingStyle("Обычный краткий", true, true, false),
            new BotWritingStyle("Обычный развёрнутый", true, true, true),
            new BotWritingStyle("Краткий без пунктуации", true, false, false),
            new BotWritingStyle("Развёрнутый без пунктуации", true, false, true),
            new BotWritingStyle("Строчные кратко", false, true, false),
            new BotWritingStyle("Строчные развёрнуто", false, true, true),
            new BotWritingStyle("Строчные кратко без пунктуации", false, false, false),
            new BotWritingStyle("Строчные развёрнуто без пунктуации", false, false, true)
        });

        public string BuildPrompt()
        {
            var letterCase = UseCapitalLetters
                ? "Начинай предложения с заглавной буквы, используй обычный регистр."
                : "Пиши всё строчными буквами, включая начало предложения.";
            var punctuation = UsePunctuation
                ? "Ставь обычные знаки препинания по смыслу, включая знак в конце предложения."
                : "Не используй знаки препинания. Разделяй мысли словами и пробелами.";
            var sentences = UseComplexSentences
                ? "Можно одно сложное предложение с причиной, условием или противопоставлением, примерно 10–22 слова."
                : "Пиши одним простым предложением, примерно 5–12 слов, без вложенных оборотов.";

            return $"ТВОЙ ПОСТОЯННЫЙ СТИЛЬ: {Name}. {letterCase} {punctuation} {sentences} " +
                   "Пиши естественным современным русским языком. Без грубости, навязанного сленга, " +
                   "канцелярита, намеренных опечаток и эмодзи. Это короткий игровой чат: даже сложная фраза должна звучать разговорно, без книжных оборотов. Не копируй манеру других игроков. " +
                   "Более сложная фраза не должна содержать больше подсказок о слове.";
        }

        public string FormatMessage(string message)
        {
            if (!UsePunctuation)
            {
                // Заменяем разделители пробелами, чтобы случайно не склеить слова.
                message = new string(message.Select(c => char.IsPunctuation(c) ? ' ' : c).ToArray());
            }

            message = Regex.Replace(message, @"\s+", " ").Trim();

            if (!UseCapitalLetters)
                return message.ToLowerInvariant();

            if (message.Length > 0)
                message = char.ToUpperInvariant(message[0]) + message[1..];

            return message;
        }
    }
}
