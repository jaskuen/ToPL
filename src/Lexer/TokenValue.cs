using System.Globalization;

namespace Lexer;

public class TokenValue
{
    public const double DoubleTolerance = 0.001;
    private readonly object value;

    public TokenValue(string value)
    {
        this.value = value;
    }

    public TokenValue(double value)
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
            double d => d.ToString(CultureInfo.InvariantCulture),
            _ => throw new NotImplementedException(),
        };
    }

    /// <summary>
    ///  Возвращает значение токена в виде числа.
    /// </summary>
    public double ToDouble()
    {
        return value switch
        {
            string s => double.Parse(s, CultureInfo.InvariantCulture),
            double d => d,
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
                double d => Math.Abs((double)other.value - d) < DoubleTolerance,
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