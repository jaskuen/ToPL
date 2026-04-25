using System.Globalization;

namespace Runtime;

public class RuntimeValue
{
    private const float DoubleTolerance = 0.001f;
    private readonly object value;
    private RuntimeValueType type;

    public RuntimeValue(int value, bool isConstant = false)
    {
        this.value = value;
        IsConstant = isConstant;
        type = RuntimeValueType.Int;
    }

    public RuntimeValue(float value, bool isConstant = false)
    {
        this.value = value;
        IsConstant = isConstant;
        type = RuntimeValueType.Double;
    }

    public RuntimeValue(bool value, bool isConstant = false)
    {
        this.value = value;
        IsConstant = isConstant;
        type = RuntimeValueType.Boolean;
    }

    public RuntimeValue(string value, bool isConstant = false)
    {
        this.value = value;
        IsConstant = isConstant;
        type = RuntimeValueType.String;
    }

    public RuntimeValue(RuntimeValueType type, bool isConstant = false)
    {
        this.type = type;
        IsConstant = isConstant;
        value = type switch
        {
            RuntimeValueType.Int => 0,
            RuntimeValueType.Double => 0.0,
            RuntimeValueType.Boolean => false,
            RuntimeValueType.String => string.Empty,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public bool IsConstant { get; }

    public static RuntimeValue operator +(RuntimeValue left, RuntimeValue right)
    {
        return left.value switch
        {
            bool _ => throw new Exception("Cannot sum a bool value."),
            float d => right.value switch
            {
                float d2 => new RuntimeValue(d + d2),
                int i => new RuntimeValue(d + i),
                _ => throw new Exception("Incorrect sum parameters.")
            },
            int i => right.value switch
            {
                int j => new RuntimeValue(i + j),
                float d => new RuntimeValue(i + d),
                _ => throw new Exception("Incorrect sum parameters.")
            },
            string s => right.value switch
            {
                string s2 => new RuntimeValue(s + s2),
                _ => throw new Exception("Incorrect sum parameters.")
            },
            _ => throw new NotImplementedException()
        };
    }

    public static RuntimeValue operator -(RuntimeValue left, RuntimeValue right)
    {
        return left.value switch
        {
            bool _ => throw new Exception("Cannot subtract a bool value."),
            string _ => throw new Exception("Cannot subtract a string value."),
            float d => right.value switch
            {
                float d2 => new RuntimeValue(d - d2),
                int i => new RuntimeValue(d - i),
                _ => throw new Exception("Incorrect subtract parameters.")
            },
            int i => right.value switch
            {
                int j => new RuntimeValue(i - j),
                float d => new RuntimeValue(i - d),
                _ => throw new Exception("Incorrect subtract parameters.")
            },
            _ => throw new NotImplementedException()
        };
    }

    public static RuntimeValue operator -(RuntimeValue value)
    {
        return value.value switch
        {
            bool _ => throw new Exception("Cannot subtract a bool value."),
            string _ => throw new Exception("Cannot subtract a string value."),
            float d => new RuntimeValue(-d),
            int i => new RuntimeValue(-i),
            _ => throw new NotImplementedException()
        };
    }

    public static RuntimeValue operator *(RuntimeValue left, RuntimeValue right)
    {
        return left.value switch
        {
            bool _ => throw new Exception("Cannot multiply a bool value."),
            string _ => throw new Exception("Cannot multiply a string value."),
            float d => right.value switch
            {
                float d2 => new RuntimeValue(d * d2),
                int i => new RuntimeValue(d * i),
                _ => throw new Exception("Incorrect multiply parameters.")
            },
            int i => right.value switch
            {
                int j => new RuntimeValue(i * j),
                float d => new RuntimeValue(i * d),
                _ => throw new Exception("Incorrect multiply parameters.")
            },
            _ => throw new NotImplementedException()
        };
    }

    public static RuntimeValue operator /(RuntimeValue left, RuntimeValue right)
    {
        return left.value switch
        {
            bool _ => throw new Exception("Cannot divide a bool value."),
            string _ => throw new Exception("Cannot divide a string value."),
            float d => right.value switch
            {
                float d2 => new RuntimeValue(d / d2),
                int i => new RuntimeValue(d / i),
                _ => throw new Exception("Incorrect divide parameters.")
            },
            int i => right.value switch
            {
                int j => new RuntimeValue(i / j),
                float d => new RuntimeValue(i / d),
                _ => throw new Exception("Incorrect divide parameters.")
            },
            _ => throw new NotImplementedException()
        };
    }

    public static RuntimeValue operator %(RuntimeValue left, RuntimeValue right)
    {
        return left.value switch
        {
            bool _ => throw new Exception("Cannot find module of a bool value."),
            string _ => throw new Exception("Cannot find module of a string value."),
            float d => right.value switch
            {
                float d2 => new RuntimeValue(d % d2),
                int i => new RuntimeValue(d % i),
                _ => throw new Exception("Incorrect find module parameters.")
            },
            int i => right.value switch
            {
                int j => new RuntimeValue(i % j),
                float d => new RuntimeValue(i % d),
                _ => throw new Exception("Incorrect find module parameters.")
            },
            _ => throw new NotImplementedException()
        };
    }

    public static bool operator true(RuntimeValue value)
    {
        return value.value switch
        {
            bool b => b,
            int i => i != 0,
            float d => Math.Abs(d) < DoubleTolerance,
            _ => throw new NotImplementedException()
        };
    }

    public static bool operator false(RuntimeValue value)
    {
        return value.value switch
        {
            bool b => !b,
            int i => i == 0,
            float d => Math.Abs(d) > DoubleTolerance,
            _ => throw new NotImplementedException()
        };
    }

    public static bool operator !(RuntimeValue value)
    {
        return value.value switch
        {
            bool b => !b,
            _ => throw new NotImplementedException()
        };
    }

    public static bool operator >(RuntimeValue left, RuntimeValue right)
    {
        return left.value switch
        {
            float d => right.value switch
            {
                float d2 => d - d2 > DoubleTolerance,
                int i => d - i > DoubleTolerance,
                _ => throw new Exception("Incorrect comparison parameters.")
            },
            int i => right.value switch
            {
                int j => i > j,
                float d => i - d > DoubleTolerance,
                _ => throw new Exception("Incorrect comparison parameters.")
            },
            _ => throw new NotImplementedException()
        };
    }

    public static bool operator <(RuntimeValue left, RuntimeValue right)
    {
        return !(left > right) && !left.Equals(right);
    }

    public static bool operator >=(RuntimeValue left, RuntimeValue right)
    {
        return !(left < right);
    }

    public static bool operator <=(RuntimeValue left, RuntimeValue right)
    {
        return !(left > right);
    }

    /// <summary>
    ///  Возвращает значение в виде boolean.
    /// </summary>
    public bool ToBoolean()
    {
        return value switch
        {
            bool s => s,
            float d => Math.Abs(d) < DoubleTolerance,
            int i => i != 0,
            _ => throw new NotImplementedException()
        };
    }

    /// <summary>
    ///  Возвращает значение в виде числа c плавающей точкой.
    /// </summary>
    public float ToFloat()
    {
        return value switch
        {
            bool s => s ? 1 : 0,
            float d => d,
            int i => i,
            string s => float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out float v)
                ? v
                : throw new Exception("Failed to parse string to float value"),
            _ => throw new NotImplementedException()
        };
    }

    public int ToInt()
    {
        return value switch
        {
            bool s => s ? 1 : 0,
            float d => (int)d,
            int i => i,
            string s => int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out int v)
                ? v
                : throw new Exception("Failed to parse string to int value"),
            _ => throw new NotImplementedException()
        };
    }

    public override string ToString()
    {
        return value switch
        {
            bool b => b ? "True" : "False",
            float d => d.ToString("0.####", CultureInfo.InvariantCulture),
            int i => i.ToString(CultureInfo.InvariantCulture),
            string s => s,
            _ => throw new NotImplementedException()
        };
    }

    public override int GetHashCode()
    {
        return value.GetHashCode();
    }

    /// <summary>
    ///  Проверяет равенство значений. Сравнение может проводиться между числовыми значениями разных типов.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is RuntimeValue other)
        {
            return value switch
            {
                bool s => other.value switch
                {
                    bool b => b == s,
                    _ => false
                },
                float d => other.value switch
                {
                    float d2 => Math.Abs(d2 - d) < DoubleTolerance,
                    int i => Math.Abs(i - d) < DoubleTolerance,
                    _ => false
                },
                int i => other.value switch
                {
                    int j => i == j,
                    float d => Math.Abs(d - i) < DoubleTolerance,
                    _ => false
                },
                string s => other.value switch
                {
                    string s2 => s.Equals(s2),
                    _ => false
                },
                _ => throw new NotImplementedException()
            };
        }

        return false;
    }

    public RuntimeValueType GetValueType() => type;

    public RuntimeValue WithConstant(bool isConstant = true)
    {
        return value switch
        {
            int i => new RuntimeValue(i, isConstant),
            float d => new RuntimeValue(d, isConstant),
            bool b => new RuntimeValue(b, isConstant),
            string s => new RuntimeValue(s, isConstant),
            _ => throw new NotImplementedException()
        };
    }
}
