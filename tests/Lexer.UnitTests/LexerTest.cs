namespace Lexer.UnitTests;

public class LexerTest
{
    [Theory]
    [MemberData(nameof(GetTokenizeChurchLangData))]
    public void Can_tokenize_text(string text, List<Token> expected)
    {
        List<Token> actual = Tokenize(text);
        Assert.Equal(expected, actual);
    }

    public static TheoryData<string, List<Token>> GetTokenizeChurchLangData()
    {
        return new TheoryData<string, List<Token>>
        {
            // === Идентификаторы ===
            {
                "variable", [
                    new Token(TokenType.Identifier, new TokenValue("variable")),
                ]
            },
            {
                "_variable", [
                    new Token(TokenType.Identifier, new TokenValue("_variable")),
                ]
            },
            {
                "Var123", [
                    new Token(TokenType.Identifier, new TokenValue("Var123")),
                ]
            },
            {
                "Var123raV", [
                    new Token(TokenType.Identifier, new TokenValue("Var123raV")),
                ]
            },

            // === Числовые литералы ===
            {
                "123", [
                    new Token(TokenType.IntLiteral, new TokenValue(123)),
                ]
            },
            {
                "1.23", [
                    new Token(TokenType.DoubleLiteral, new TokenValue(1.23)),
                ]
            },
            {
                "0.123", [
                    new Token(TokenType.DoubleLiteral, new TokenValue(0.123)),
                ]
            },
            {
                "0123", [
                    new Token(TokenType.IntLiteral, new TokenValue(123)),
                ]
            },

            // === Строковые литералы ===
            {
                "\"Приветствую!\"", [
                    new Token(TokenType.StringLiteral, new TokenValue("Приветствую!")),
                ]
            },
            {
                "\"Здравствуйте!\\n\\tПока!\\rДо свидания!\"", [
                    new Token(TokenType.StringLiteral, new TokenValue("Здравствуйте!\n\tПока!\rДо свидания!")),
                ]
            },
            {
                "\"A\"", [
                    new Token(TokenType.StringLiteral, new TokenValue("A")),
                ]
            },
            {
                """
                "Начало
                Строки"
                """,
                [
                    new Token(TokenType.Error, new TokenValue("Начало")),
                    new Token(TokenType.Identifier, new TokenValue("Строки")),
                    new Token(TokenType.Error, new TokenValue("")),
                ]
            },
            {
                "\"\\\\n - перенос строки\"", [
                    new Token(TokenType.StringLiteral, new TokenValue("\\n - перенос строки"))
                ]
            },

            // === Арифметические выражения ===
            {
                "1 + 3 - 2", [
                    new Token(TokenType.IntLiteral, new TokenValue(1)),
                    new Token(TokenType.PlusSign),
                    new Token(TokenType.IntLiteral, new TokenValue(3)),
                    new Token(TokenType.MinusSign),
                    new Token(TokenType.IntLiteral, new TokenValue(2)),
                ]
            },
            {
                "5 * 10 / 25", [
                    new Token(TokenType.IntLiteral, new TokenValue(5)),
                    new Token(TokenType.MultiplySign),
                    new Token(TokenType.IntLiteral, new TokenValue(10)),
                    new Token(TokenType.DivideSign),
                    new Token(TokenType.IntLiteral, new TokenValue(25)),
                ]
            },
            {
                "10 % 3", [
                    new Token(TokenType.IntLiteral, new TokenValue(10)),
                    new Token(TokenType.ModuloSign),
                    new Token(TokenType.IntLiteral, new TokenValue(3)),
                ]
            },
            {
                "приумножу var", [
                    new Token(TokenType.Increment),
                    new Token(TokenType.Identifier, new TokenValue("var")),
                ]
            },
            {
                "умалю var", [
                    new Token(TokenType.Decrement),
                    new Token(TokenType.Identifier, new TokenValue("var")),
                ]
            },
            {
                "num даруй 6", [
                    new Token(TokenType.Identifier, new TokenValue("num")),
                    new Token(TokenType.Assignment),
                    new Token(TokenType.IntLiteral, new TokenValue(6)),
                ]
            },

            // === Операторы сравнения ===
            {
                "num1 велий num2", [
                    new Token(TokenType.Identifier, new TokenValue("num1")),
                    new Token(TokenType.GreaterThan),
                    new Token(TokenType.Identifier, new TokenValue("num2")),
                ]
            },
            {
                "num1 малый num2", [
                    new Token(TokenType.Identifier, new TokenValue("num1")),
                    new Token(TokenType.LessThan),
                    new Token(TokenType.Identifier, new TokenValue("num2")),
                ]
            },
            {
                "num1 паче num2", [
                    new Token(TokenType.Identifier, new TokenValue("num1")),
                    new Token(TokenType.GreaterThanOrEqual),
                    new Token(TokenType.Identifier, new TokenValue("num2")),
                ]
            },
            {
                "num1 меньше num2", [
                    new Token(TokenType.Identifier, new TokenValue("num1")),
                    new Token(TokenType.LessThanOrEqual),
                    new Token(TokenType.Identifier, new TokenValue("num2")),
                ]
            },

            // === Логические операторы ===
            {
                "bool1 и bool2", [
                    new Token(TokenType.Identifier, new TokenValue("bool1")),
                    new Token(TokenType.And),
                    new Token(TokenType.Identifier, new TokenValue("bool2")),
                ]
            },
            {
                "bool1 или bool2", [
                    new Token(TokenType.Identifier, new TokenValue("bool1")),
                    new Token(TokenType.Or),
                    new Token(TokenType.Identifier, new TokenValue("bool2")),
                ]
            },
            {
                "bool1 яко bool2", [
                    new Token(TokenType.Identifier, new TokenValue("bool1")),
                    new Token(TokenType.Equal),
                    new Token(TokenType.Identifier, new TokenValue("bool2")),
                ]
            },
            {
                "bool1 негоже bool2", [
                    new Token(TokenType.Identifier, new TokenValue("bool1")),
                    new Token(TokenType.NotEqual),
                    new Token(TokenType.Identifier, new TokenValue("bool2")),
                ]
            },
            {
                "не bool1", [
                    new Token(TokenType.Not),
                    new Token(TokenType.Identifier, new TokenValue("bool1")),
                ]
            },
            {
                "истинно", [
                    new Token(TokenType.True),
                ]
            },
            {
                "лукаво", [
                    new Token(TokenType.False),
                ]
            },

            // === Комментарии ===
            { "#Комментарий", [] },
            {
                "словеса операция даруй \"\" поклон #Создаем пустую строку", [
                    new Token(TokenType.StringType),
                    new Token(TokenType.Identifier, new TokenValue("операция")),
                    new Token(TokenType.Assignment),
                    new Token(TokenType.StringLiteral, new TokenValue("")),
                    new Token(TokenType.Semicolon),
                ]
            },

            // === Прочие лексемы ===
            {
                "воистину аминь", [
                    new Token(TokenType.CodeBlockBegin),
                    new Token(TokenType.CodeBlockEnd),
                ]
            },
            {
                "поклон", [
                    new Token(TokenType.Semicolon),
                ]
            },
            {
                "(bool1 и bool2) или (bool3 и bool4)", [
                    new Token(TokenType.OpenParenthesis),
                    new Token(TokenType.Identifier, new TokenValue("bool1")),
                    new Token(TokenType.And),
                    new Token(TokenType.Identifier, new TokenValue("bool2")),
                    new Token(TokenType.CloseParenthesis),
                    new Token(TokenType.Or),
                    new Token(TokenType.OpenParenthesis),
                    new Token(TokenType.Identifier, new TokenValue("bool3")),
                    new Token(TokenType.And),
                    new Token(TokenType.Identifier, new TokenValue("bool4")),
                    new Token(TokenType.CloseParenthesis),
                ]
            },
            {
                "возгласи(\"На ноль делить не подобает, чадо!\") поклон", [
                    new Token(TokenType.ConsoleWrite),
                    new Token(TokenType.OpenParenthesis),
                    new Token(TokenType.StringLiteral, new TokenValue("На ноль делить не подобает, чадо!")),
                    new Token(TokenType.CloseParenthesis),
                    new Token(TokenType.Semicolon),
                ]
            },
            {
                "аще (bool1) воистину возгласи(\"Истинно\") поклон аминь илиже воистину возгласи(\"Ложно\") поклон аминь", [
                    new Token(TokenType.If),
                    new Token(TokenType.OpenParenthesis),
                    new Token(TokenType.Identifier, new TokenValue("bool1")),
                    new Token(TokenType.CloseParenthesis),
                    new Token(TokenType.CodeBlockBegin),
                    new Token(TokenType.ConsoleWrite),
                    new Token(TokenType.OpenParenthesis),
                    new Token(TokenType.StringLiteral, new TokenValue("Истинно")),
                    new Token(TokenType.CloseParenthesis),
                    new Token(TokenType.Semicolon),
                    new Token(TokenType.CodeBlockEnd),
                    new Token(TokenType.Else),
                    new Token(TokenType.CodeBlockBegin),
                    new Token(TokenType.ConsoleWrite),
                    new Token(TokenType.OpenParenthesis),
                    new Token(TokenType.StringLiteral, new TokenValue("Ложно")),
                    new Token(TokenType.CloseParenthesis),
                    new Token(TokenType.Semicolon),
                    new Token(TokenType.CodeBlockEnd),
                ]
            },
            {
                "доколе (num1 малый 10) воистину возгласи(\"Монетка \", приумножу num1) поклон аминь", [
                    new Token(TokenType.While),
                    new Token(TokenType.OpenParenthesis),
                    new Token(TokenType.Identifier, new TokenValue("num1")),
                    new Token(TokenType.LessThan),
                    new Token(TokenType.IntLiteral, new TokenValue(10)),
                    new Token(TokenType.CloseParenthesis),
                    new Token(TokenType.CodeBlockBegin),
                    new Token(TokenType.ConsoleWrite),
                    new Token(TokenType.OpenParenthesis),
                    new Token(TokenType.StringLiteral, new TokenValue("Монетка ")),
                    new Token(TokenType.Comma),
                    new Token(TokenType.Increment),
                    new Token(TokenType.Identifier, new TokenValue("num1")),
                    new Token(TokenType.CloseParenthesis),
                    new Token(TokenType.Semicolon),
                    new Token(TokenType.CodeBlockEnd),
                ]
            },
            {
                """
                изберется операция
                воистину
                    егда "+"
                        результат даруй а + б поклон
                        отрешити поклон
                    егда "-"
                        результат даруй а - б поклон
                        отрешити поклон
                    поеликуже
                        возгласи("Сего деяния я не ведаю, чадо") поклон
                        возврати -1 поклон
                        отрешити поклон
                аминь
                """,
                [
                    new Token(TokenType.Switch),
                    new Token(TokenType.Identifier, new TokenValue("операция")),
                    new Token(TokenType.CodeBlockBegin),
                    new Token(TokenType.Case),
                    new Token(TokenType.StringLiteral, new TokenValue("+")),
                    new Token(TokenType.Identifier, new TokenValue("результат")),
                    new Token(TokenType.Assignment),
                    new Token(TokenType.Identifier, new TokenValue("а")),
                    new Token(TokenType.PlusSign),
                    new Token(TokenType.Identifier, new TokenValue("б")),
                    new Token(TokenType.Semicolon),
                    new Token(TokenType.Break),
                    new Token(TokenType.Semicolon),
                    new Token(TokenType.Case),
                    new Token(TokenType.StringLiteral, new TokenValue("-")),
                    new Token(TokenType.Identifier, new TokenValue("результат")),
                    new Token(TokenType.Assignment),
                    new Token(TokenType.Identifier, new TokenValue("а")),
                    new Token(TokenType.MinusSign),
                    new Token(TokenType.Identifier, new TokenValue("б")),
                    new Token(TokenType.Semicolon),
                    new Token(TokenType.Break),
                    new Token(TokenType.Semicolon),
                    new Token(TokenType.Default),
                    new Token(TokenType.ConsoleWrite),
                    new Token(TokenType.OpenParenthesis),
                    new Token(TokenType.StringLiteral, new TokenValue("Сего деяния я не ведаю, чадо")),
                    new Token(TokenType.CloseParenthesis),
                    new Token(TokenType.Semicolon),
                    new Token(TokenType.Return),
                    new Token(TokenType.MinusSign),
                    new Token(TokenType.IntLiteral, new TokenValue(1)),
                    new Token(TokenType.Semicolon),
                    new Token(TokenType.Break),
                    new Token(TokenType.Semicolon),
                    new Token(TokenType.CodeBlockEnd),
                ]
            },
            {
                "[1, 2, 3, 4, 5]", [
                    new Token(TokenType.OpenArrayParenthesis),
                    new Token(TokenType.IntLiteral, new TokenValue(1)),
                    new Token(TokenType.Comma),
                    new Token(TokenType.IntLiteral, new TokenValue(2)),
                    new Token(TokenType.Comma),
                    new Token(TokenType.IntLiteral, new TokenValue(3)),
                    new Token(TokenType.Comma),
                    new Token(TokenType.IntLiteral, new TokenValue(4)),
                    new Token(TokenType.Comma),
                    new Token(TokenType.IntLiteral, new TokenValue(5)),
                    new Token(TokenType.CloseArrayParenthesis),
                ]
            },
        };
    }

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