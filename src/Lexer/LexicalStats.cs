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
            case TokenType.Int:
            case TokenType.String:
            case TokenType.Bool:
            case TokenType.While:
            case TokenType.Return:
            case TokenType.Break:
            case TokenType.Continue:
            case TokenType.Write:
            case TokenType.Read:
            //case TokenType.Case:
            //case TokenType.Switch:
            //case TokenType.Default:
                return LexicalType.Keyword;
            case TokenType.Identifier:
                return LexicalType.Identifier;
            case TokenType.IntLiteral:
            case TokenType.FloatLiteral:
                return LexicalType.IntLiteral;
            case TokenType.StringLiteral:
                return LexicalType.StringLiteral;
            case TokenType.Plus:
            case TokenType.Minus:
            case TokenType.Divide:
            case TokenType.Multiply:
            case TokenType.Increment:
            case TokenType.Decrement:
            case TokenType.Modulo:
            case TokenType.Assignment:
            case TokenType.GreaterThan:
            case TokenType.GreaterThanOrEqual:
            case TokenType.LessThan:
            case TokenType.LessThanOrEqual:
            case TokenType.LogicalOr:
            case TokenType.Equal:
            case TokenType.NotEqual:
            case TokenType.True:
            case TokenType.False:
            case TokenType.LogicalNot:
                return LexicalType.Operator;
            default:
                return LexicalType.Punctuation; // Тут было OtherLexems, думаю что это {}(),;
        }
    }

    private static string StatsToString(IDictionary<LexicalType, int> dict)
    {
        return $"""
                keywords: {dict[LexicalType.Keyword]}
                identifier: {dict[LexicalType.Identifier]}
                number literals: {dict[LexicalType.IntLiteral]}
                string literals: {dict[LexicalType.StringLiteral]}
                operators: {dict[LexicalType.Operator]}
                other lexemes: {dict[LexicalType.Punctuation]}
                """;
    }
}