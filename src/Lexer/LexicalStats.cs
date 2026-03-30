using System.Text;

namespace Lexer;

public static class LexicalStats
{
    public static string CollectFromFile(string path)
    {
        string fileText = File.ReadAllText(path, Encoding.UTF8);

        Lexer lexer = new Lexer(fileText);
        IDictionary<LexicalType, int> result = new Dictionary<LexicalType, int>();
        for (Token t = lexer.ParseToken(); t.Type != TokenType.End; t = lexer.ParseToken())
        {
            LexicalType type = TokenToLexicalType(t.Type);

            if (!result.ContainsKey(type))
            {
                result.Add(new KeyValuePair<LexicalType, int>(type, 0));
            }

            result[type]++;
        }

        return StatsToString(result);
    }

    private static LexicalType TokenToLexicalType(TokenType type)
    {
        switch (type)
        {
            case TokenType.If:
            case TokenType.Else:
            case TokenType.Void:
            case TokenType.NumericType:
            case TokenType.StringType:
            case TokenType.BooleanType:
            case TokenType.While:
            case TokenType.Return:
            case TokenType.Break:
            case TokenType.Continue:
            case TokenType.ConsoleWrite:
            case TokenType.ConsoleRead:
            case TokenType.Case:
            case TokenType.Switch:
            case TokenType.Default:
                return LexicalType.Keyword;
            case TokenType.Identifier:
                return LexicalType.Identifier;
            case TokenType.IntLiteral:
            case TokenType.DoubleLiteral:
                return LexicalType.Number;
            case TokenType.StringLiteral:
                return LexicalType.String;
            case TokenType.PlusSign:
            case TokenType.MinusSign:
            case TokenType.DivideSign:
            case TokenType.MultiplySign:
            case TokenType.Increment:
            case TokenType.Decrement:
            case TokenType.ModuloSign:
            case TokenType.Assignment:
            case TokenType.GreaterThan:
            case TokenType.GreaterThanOrEqual:
            case TokenType.LessThan:
            case TokenType.LessThanOrEqual:
            case TokenType.Or:
            case TokenType.Equal:
            case TokenType.NotEqual:
            case TokenType.True:
            case TokenType.False:
            case TokenType.Not:
                return LexicalType.Operator;
            default:
                return LexicalType.OtherLexemes;
        }
    }

    private static string StatsToString(IDictionary<LexicalType, int> dict)
    {
        return $"""
                keywords: {dict[LexicalType.Keyword]}
                identifier: {dict[LexicalType.Identifier]}
                number literals: {dict[LexicalType.Number]}
                string literals: {dict[LexicalType.String]}
                operators: {dict[LexicalType.Operator]}
                other lexemes: {dict[LexicalType.OtherLexemes]}
                """;
    }
}