using Xunit;

namespace ExampleLib.UnitTests;

public class TextUtilTest
{
    public static TheoryData<string, int> CorrectRomanData => new()
    {
        { "I", 1 },
        { "MMM", 3000 },

// Basic numerals
        { "II", 2 },
        { "III", 3 },
        { "V", 5 },
        { "X", 10 },
        { "L", 50 },
        { "C", 100 },
        { "D", 500 },
        { "M", 1000 },

// Subtractive cases
        { "IV", 4 },
        { "IX", 9 },
        { "XL", 40 },
        { "XC", 90 },
        { "CD", 400 },
        { "CM", 900 },

// Combination numbers
        { "XIII", 13 },
        { "XIX", 19 },
        { "XLIV", 44 },
        { "XCIX", 99 },
        { "CDXLIV", 444 },
        { "CMXCIX", 999 },

// Larger values
        { "MCMXC", 1990 },
        { "MMXXV", 2025 },
    };

    public static TheoryData<string> IncorrectRomanData => new()
    {
        "IIII",
        "VV",
        "LL",
        "DD",
        "XXXX",
        "CCCC",
        "MMMM",
        "IL",
        "IC",
        "ID",
        "IM",
        "XD",
        "XM",
        "VX",
        "LC",
        "DM",
        "",
        "A",
        "MXM",
        "IIV",
        "VXII",
        "IXC",
    };

    [Fact]
    public void Can_extract_russian_words()
    {
        const string text = """
                            Играют волны — ветер свищет,
                            И мачта гнётся и скрыпит…
                            Увы! он счастия не ищет
                            И не от счастия бежит!
                            """;
        List<string> expected =
        [
            "Играют",
            "волны",
            "ветер",
            "свищет",
            "И",
            "мачта",
            "гнётся",
            "и",
            "скрыпит",
            "Увы",
            "он",
            "счастия",
            "не",
            "ищет",
            "И",
            "не",
            "от",
            "счастия",
            "бежит",
        ];

        List<string> actual = TextUtil.ExtractWords(text);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Can_extract_words_with_hyphens()
    {
        const string text = "Что-нибудь да как-нибудь, и +/- что- то ещё";
        List<string> expected =
        [
            "Что-нибудь",
            "да",
            "как-нибудь",
            "и",
            "что",
            "то",
            "ещё",
        ];

        List<string> actual = TextUtil.ExtractWords(text);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Can_extract_words_with_apostrophes()
    {
        const string text = "Children's toys and three cats' toys";
        List<string> expected =
        [
            "Children's",
            "toys",
            "and",
            "three",
            "cats'",
            "toys",
        ];

        List<string> actual = TextUtil.ExtractWords(text);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Can_extract_words_with_grave_accent()
    {
        const string text = "Children`s toys and three cats` toys, all of''them are green";
        List<string> expected =
        [
            "Children`s",
            "toys",
            "and",
            "three",
            "cats`",
            "toys",
            "all",
            "of'",
            "them",
            "are",
            "green",
        ];

        List<string> actual = TextUtil.ExtractWords(text);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(CorrectRomanData))]
    public void Can_parse_roman_style_correctly(string roman, int expected)
    {
        int parsed = TextUtil.ParseRoman(roman);
        Assert.Equal(expected, parsed);
    }

    [Theory]
    [MemberData(nameof(IncorrectRomanData))]
    public void Can_throw_exception_on_wrong_roman_format(string roman)
    {
        Assert.Throws<FormatException>(() => TextUtil.ParseRoman(roman));
    }
}