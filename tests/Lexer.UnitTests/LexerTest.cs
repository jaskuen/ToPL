using Lexer.UnitTests.Helpers;

namespace Lexer.UnitTests;

public class LexerTest
{
    [Theory]
    [MemberData(nameof(GetTokenizeWLangData))]
    public void Can_tokenize_text(string text, List<Token> expected)
    {
        Assert.Equal(expected, Tokenize(text));
    }

    public static TheoryData<string, List<Token>> GetTokenizeWLangData() => new()
    {
        // === Идентификаторы ===
        { "variable", [Lex.Id("variable")] },
        { "_variable", [Lex.Id("_variable")] },
        { "_variable123", [Lex.Id("_variable123")] },
        { "_variable_123", [Lex.Id("_variable_123")] },

        // === Числовые литералы ===
        { "123", [Lex.Int(123)] },
        { "0123", [Lex.Int(123)] },
        { "1.23", [Lex.Flt(1.23f)] },
        { "1.23", [Lex.Flt(1.23f)] },
        { "0.123", [Lex.Flt(0.123f)] },

        // === Строковые литералы ===
        { "\"Приветствую!\"", [Lex.Str("Приветствую!")] },
        { "\"Здравствуйте!\\n\\tПока!\\rДо свидания!\"", [Lex.Str("Здравствуйте!\n\tПока!\rДо свидания!")] },
        { "\"A\"", [Lex.Str("A")] },
        { "\"\\\\n - перенос строки\"", [Lex.Str("\\n - перенос строки")] },
        {
            """
            "Nachalo
            Stroki"
            """,
            [
                Lex.Err("Nachalo"),
                Lex.Id("Stroki"),
                Lex.Err(""),
            ]
        },

        // === Булевы литералы ===
        { "true", [Lex.True] },
        { "false", [Lex.False] },

        // === Арифметические выражения ===
        { "1 + 3 - 2", [Lex.Int(1), Lex.Plus, Lex.Int(3), Lex.Minus, Lex.Int(2)] },
        { "5 * 10 / 25", [Lex.Int(5), Lex.Mul, Lex.Int(10), Lex.Div, Lex.Int(25)] },
        { "10 % 3", [Lex.Int(10), Lex.Mod, Lex.Int(3)] },
        { "a = b + 3", [Lex.Id("a"), Lex.Assign, Lex.Id("b"), Lex.Plus, Lex.Int(3)] },

        // === Инкремент, декремент  ===
        { "++i", [Lex.Inc, Lex.Id("i")] },
        { "i++", [Lex.Id("i"), Lex.Inc] },
        { "--i", [Lex.Dec, Lex.Id("i")] },
        { "i--", [Lex.Id("i"), Lex.Dec] },

        // === Типы данных  ===
        { "int i", [ Lex.KwInt, Lex.Id("i")] },
        { "float i", [Lex.KwFloat, Lex.Id("i")] },
        { "string i", [Lex.KwStr, Lex.Id("i")] },
        { "bool i", [Lex.KwBool, Lex.Id("i")] },
        { "const float PI = 3.141592;", [Lex.KwConst, Lex.KwFloat, Lex.Id("PI"), Lex.Assign, Lex.Flt(3.141592f), Lex.Semi] },

        // === Операторы сравнения ===
        { "a < b", [Lex.Id("a"), Lex.Lt, Lex.Id("b")] },
        { "a <= b", [Lex.Id("a"), Lex.LtEq, Lex.Id("b")] },
        { "a > b", [Lex.Id("a"), Lex.Gt, Lex.Id("b")] },
        { "a >= b", [Lex.Id("a"), Lex.GtEq, Lex.Id("b")] },
        { "a == b", [Lex.Id("a"), Lex.Eq, Lex.Id("b")] },
        { "a equals b", [Lex.Id("a"), Lex.Eq, Lex.Id("b")] },
        { "a != b", [Lex.Id("a"), Lex.NotEq, Lex.Id("b")] },

        // === Логические операторы ===
        { "bool1 && bool2", [Lex.Id("bool1"), Lex.And, Lex.Id("bool2")] },
        { "bool1 and bool2", [Lex.Id("bool1"), Lex.And, Lex.Id("bool2")] },
        { "bool1 || bool2", [Lex.Id("bool1"), Lex.Or, Lex.Id("bool2")] },
        { "bool1 or bool2", [Lex.Id("bool1"), Lex.Or, Lex.Id("bool2")] },
        { "!bool1", [Lex.Not, Lex.Id("bool1")] },
        { "not bool1", [Lex.Not, Lex.Id("bool1")] },

        // === Комментарии ===
        { "// Первый комментарий", [] },
        { "a = b; // Присвоение", [Lex.Id("a"), Lex.Assign, Lex.Id("b"), Lex.Semi] },

        // === Встроенные функции ===
        { "abs(-123)", [Lex.FnAbs, Lex.LPar, Lex.Minus, Lex.Int(123), Lex.RPar] },
        { "round(3.14)", [Lex.FnRound, Lex.LPar, Lex.Flt(3.14f), Lex.RPar] },
        { "ceil(3.14)", [Lex.FnCeil, Lex.LPar, Lex.Flt(3.14f), Lex.RPar] },
        { "floor(3.14)", [Lex.FnFloor, Lex.LPar, Lex.Flt(3.14f), Lex.RPar] },
        { "min(a, b)", [Lex.FnMin, Lex.LPar, Lex.Id("a"), Lex.Comma, Lex.Id("b"), Lex.RPar] },
        { "max(a, b)", [Lex.FnMax, Lex.LPar, Lex.Id("a"), Lex.Comma, Lex.Id("b"), Lex.RPar] },
        { "length(s)", [Lex.FnLength, Lex.LPar, Lex.Id("s"), Lex.RPar] },
        { "substring(s, 0, 3)", [Lex.FnSubstring, Lex.LPar, Lex.Id("s"), Lex.Comma, Lex.Int(0), Lex.Comma, Lex.Int(3), Lex.RPar] },
        { "read(s)", [Lex.KwRead, Lex.LPar, Lex.Id("s"), Lex.RPar] },
        { "write(s)", [Lex.KwWrite, Lex.LPar, Lex.Id("s"), Lex.RPar] },

        // === Функция main ===
        {
            """
            void main()
            {

            }
            """,
            [Lex.KwVoid, Lex.KwMain, Lex.LPar, Lex.RPar, Lex.LBrace, Lex.RBrace]
        },

        // === Циклы и ветвления (2 эпик) ===
    };

    private List<Token> Tokenize(string sql)
    {
        List<Token> results = [];
        Lexer lexer = new(sql);

        for (Token t = lexer.ParseToken(); t.Type != TokenType.End; t = lexer.ParseToken())
        {
            results.Add(t);
        }

        return results;
    }
}