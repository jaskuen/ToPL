using System.Text;

namespace Lexer;

public class Token(TokenType type, TokenValue? value = null)
{
    public TokenType Type { get; } = type;

    public TokenValue? Value { get; } = value;

    /// <summary>
    ///  Сравнивает токены по типу и значению.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is Token other)
        {
            return Type == other.Type && Equals(Value, other.Value);
        }

        return false;
    }

    /// <summary>
    ///  Возвращает хеш от свойств токена.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine((int)Type, Value);
    }

    /// <summary>
    /// Форматирует токен в стиле "Type (Value)".
    /// </summary>
    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append(Type.ToString());
        if (Value != null)
        {
            sb.Append($" ({Value})");
        }

        return sb.ToString();
    }
}