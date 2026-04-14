using System.Globalization;

namespace Lexer;

public class TokenValue
{
    public const float FloatTolerance = 0.001f;
    private readonly object value;

    public TokenValue(string value)
    {
        this.value = value;
    }

    public TokenValue(float value)
    {
        this.value = value;
    }

    public TokenValue(int value)
    {
        this.value = value;
    }

    /// <summary>
    ///  Возвращает значение токена в виде строки.
    /// </summary>
    /// <remarks>
    ///  Имя метода пересекается со стандартным методом ToString(), поэтому добавлен `override`.
    /// </remarks>
    public override string ToString()
    {
        return value switch
        {
            string s => s,
            float d => d.ToString(CultureInfo.InvariantCulture),
            _ => throw new NotImplementedException(),
        };
    }

    /// <summary>
    ///  Возвращает значение токена в виде числа.
    /// </summary>
    public float ToFloat()
    {
        return value switch
        {
            string s => float.Parse(s, CultureInfo.InvariantCulture),
            float d => d,
            _ => throw new NotImplementedException(),
        };
    }

    /// <summary>
    ///  Возвращает значение токена в виде числа.
    /// </summary>
    public int ToInt()
    {
        return value switch
        {
            string s => int.Parse(s, CultureInfo.InvariantCulture),
            int i => i,
            _ => throw new NotImplementedException(),
        };
    }

    /// <summary>
    ///  Проверяет равенство значений токенов. Значения разных типов всегда считаются разными.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is TokenValue other)
        {
            return value switch
            {
                string s => (string)other.value == s,
                float d => Math.Abs((float)other.value - d) < FloatTolerance,
                int i => (int)value == i,
                _ => throw new NotImplementedException(),
            };
        }

        return false;
    }

    public override int GetHashCode()
    {
        return value.GetHashCode();
    }
}