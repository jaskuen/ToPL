using System.Text;

namespace ExampleLib;

public static class TextUtil
{
    // Символы Unicode, которые мы принимаем как дефис.
    private static readonly Rune[] Hyphens = [new Rune('‐'), new Rune('-')];

    // Символы Unicode, которые мы принимаем как апостроф.
    private static readonly Rune[] Apostrophes = [new Rune('\''), new Rune('`')];

    // Состояния распознавателя слов.
    private static readonly Dictionary<char, int> RomanToArabic = new()
    {
        { 'I', (int)Roman.I },
        { 'V', (int)Roman.V },
        { 'X', (int)Roman.X },
        { 'L', (int)Roman.L },
        { 'C', (int)Roman.C },
        { 'D', (int)Roman.D },
        { 'M', (int)Roman.M },
    };

    private enum WordState
    {
        NoWord,
        Letter,
        Hyphen,
        Apostrophe,
    }

    private enum Roman
    {
        I = 1,
        V = 5,
        X = 10,
        L = 50,
        C = 100,
        D = 500,
        M = 1000,
    }

    /// <summary>
    ///  Распознаёт слова в тексте. Поддерживает Unicode, в том числе английский и русский языки.
    ///  Слово состоит из букв, может содержать дефис в середине и апостроф в середине либо в конце.
    /// </summary>
    /// <remarks>
    ///  Функция использует автомат-распознаватель с четырьмя состояниями:
    ///   1. NoWord — автомат находится вне слова;
    ///   2. Letter — автомат находится в буквенной части слова;
    ///   3. Hyphen — автомат обработал дефис;
    ///   4. Apostrophe — автомат обработал апостроф.
    ///
    ///  Переходы между состояниями:
    ///   - NoWord → Letter — при получении буквы;
    ///   - Letter → Hyphen — при получении дефиса;
    ///   - Letter → Apostrophe — при получении апострофа;
    ///   - Letter → NoWord — при получении любого символа, кроме буквы, дефиса или апострофа;
    ///   - Hyphen → Letter — при получении буквы;
    ///   - Hyphen → NoWord — при получении любого символа, кроме буквы;
    ///   - Apostrophe → Letter — при получении буквы;
    ///   - Apostrophe → NoWord — при получении любого символа, кроме буквы.
    ///
    ///  Разница между состояниями Hyphen и Apostrophe в том, что дефис не может стоять в конце слова.
    /// </remarks>
    public static List<string> ExtractWords(string text)
    {
        WordState state = WordState.NoWord;

        List<string> results = [];
        StringBuilder currentWord = new();
        foreach (Rune ch in text.EnumerateRunes())
        {
            switch (state)
            {
                case WordState.NoWord:
                    if (Rune.IsLetter(ch))
                    {
                        PushCurrentWord();
                        currentWord.Append(ch);
                        state = WordState.Letter;
                    }

                    break;

                case WordState.Letter:
                    if (Rune.IsLetter(ch))
                    {
                        currentWord.Append(ch);
                    }
                    else if (Hyphens.Contains(ch))
                    {
                        currentWord.Append(ch);
                        state = WordState.Hyphen;
                    }
                    else if (Apostrophes.Contains(ch))
                    {
                        currentWord.Append(ch);
                        state = WordState.Apostrophe;
                    }
                    else
                    {
                        state = WordState.NoWord;
                    }

                    break;

                case WordState.Hyphen:
                    if (Rune.IsLetter(ch))
                    {
                        currentWord.Append(ch);
                        state = WordState.Letter;
                    }
                    else
                    {
                        // Убираем дефис, которого не должно быть в конце слова.
                        currentWord.Remove(currentWord.Length - 1, 1);
                        state = WordState.NoWord;
                    }

                    break;

                case WordState.Apostrophe:
                    if (Rune.IsLetter(ch))
                    {
                        currentWord.Append(ch);
                        state = WordState.Letter;
                    }
                    else
                    {
                        state = WordState.NoWord;
                    }

                    break;
            }
        }

        PushCurrentWord();

        return results;

        void PushCurrentWord()
        {
            if (currentWord.Length > 0)
            {
                results.Add(currentWord.ToString());
                currentWord.Clear();
            }
        }
    }

    public static int ParseRoman(string text)
    {
        ValidateRomanText(text);

        int sum = 0;
        char previousChar = '\0';
        for (int i = 0, textLength = text.Length; i < textLength; i++)
        {
            char currentChar = text[i];

            if (!RomanToArabic.ContainsKey(currentChar))
            {
                throw new FormatException("Invalid or unsupported Roman character");
            }

            if (i == 0)
            {
                if (textLength == 1)
                {
                    sum = RomanToArabic[currentChar];
                    return sum;
                }

                previousChar = currentChar;
                continue;
            }

            int previousCharNumber = RomanToArabic[previousChar];
            int currentCharNumber = RomanToArabic[currentChar];

            if (previousCharNumber < currentCharNumber)
            {
                // Нельзя вычитать числами, начинающимися с 5, а также не-соседними числами
                if (previousChar is 'D' or 'L' or 'V' ||
                    previousCharNumber * 10 < currentCharNumber ||
                    sum % currentCharNumber != 0)
                {
                    throw new FormatException("Invalid Roman format");
                }

                sum -= previousCharNumber;
            }
            else
            {
                sum += previousCharNumber;
            }

            if (i == textLength - 1)
            {
                sum += currentCharNumber;
            }

            previousChar = currentChar;
        }

        return sum;
    }

    private static void ValidateRomanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new FormatException("String is null or whitespace");
        }

        List<char> upTo3 = ['M', 'C', 'X', 'I'];
        List<char> onlySingle = ['D', 'L', 'V'];

        foreach (char c in upTo3)
        {
            if (text.Count(x => x == c) > 3)
            {
                throw new FormatException("Wrong Roman format");
            }
        }

        foreach (char c in onlySingle)
        {
            if (text.Count(x => x == c) > 1)
            {
                throw new FormatException("Wrong Roman format");
            }
        }
    }
}