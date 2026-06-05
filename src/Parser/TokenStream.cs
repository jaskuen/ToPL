using Lexer;

namespace Parser;

/// <summary>
/// Представляет поток токенов с двумя операциями:
///  - Peek() возвращает текущий токен / последний прочитанный токен
///  - Advance() переходит к следующему токену
/// </summary>
public class TokenStream
{
    private readonly Lexer.Lexer lexer;
    private readonly List<Token> tokens;

    public TokenStream(string sql)
    {
        lexer = new Lexer.Lexer(sql);
        tokens = [lexer.ParseToken()];
    }

    public Token Peek(int n = 0)
    {
        while (n >= tokens.Count)
        {
            tokens.Add(lexer.ParseToken());
        }

        return tokens[n];
    }

    public void Advance()
    {
        tokens.RemoveAt(0);

        if (tokens.Count == 0)
        {
            tokens.Add(lexer.ParseToken());
        }
    }
}
