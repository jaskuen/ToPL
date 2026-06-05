using System.Globalization;

using Ast.Declarations;

namespace Runtime;

public class RuntimeValue
{
    private const float FloatTolerance = 0.001f;
    private readonly object value;
    private readonly RuntimeValueType type;
    private readonly TypeReference typeReference;

    public RuntimeValue(int value, bool isConstant = false)
    {
        this.value = value;
        IsConstant = isConstant;
        type = RuntimeValueType.Int;
        typeReference = TypeReference.Int;
    }

    public RuntimeValue(float value, bool isConstant = false)
    {
        this.value = value;
        IsConstant = isConstant;
        type = RuntimeValueType.Float;
        typeReference = TypeReference.Float;
    }

    public RuntimeValue(bool value, bool isConstant = false)
    {
        this.value = value;
        IsConstant = isConstant;
        type = RuntimeValueType.Boolean;
        typeReference = TypeReference.Boolean;
    }

    public RuntimeValue(string value, bool isConstant = false)
    {
        this.value = value;
        IsConstant = isConstant;
        type = RuntimeValueType.String;
        typeReference = TypeReference.String;
    }

    public RuntimeValue(RuntimeArrayValue value, bool isConstant = false)
    {
        this.value = value;
        IsConstant = isConstant;
        type = RuntimeValueType.Array;
        typeReference = TypeReference.ArrayOf(value.ElementType);
    }

    public RuntimeValue(RuntimeStructValue value, bool isConstant = false)
    {
        this.value = value;
        IsConstant = isConstant;
        type = RuntimeValueType.Struct;
        typeReference = TypeReference.Struct(value.TypeName);
    }

    public RuntimeValue(TypeReference type, bool isConstant = false)
    {
        IsConstant = isConstant;
        typeReference = type;

        if (type.IsArray)
        {
            this.type = RuntimeValueType.Array;
            value = new RuntimeArrayValue(type.ElementType, []);
            return;
        }

        this.type = ToRuntimeValueType(type);
        value = type.Kind switch
        {
            VariableType.Int => 0,
            VariableType.Float => 0.0f,
            VariableType.Boolean => false,
            VariableType.String => string.Empty,
            VariableType.Struct => new RuntimeStructValue(
                type.StructName ?? string.Empty,
                new Dictionary<string, TypeReference>(),
                new Dictionary<string, RuntimeValue>()),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public RuntimeValue(RuntimeValueType type, bool isConstant = false)
        : this(ToTypeReference(type), isConstant)
    {
    }

    public bool IsConstant { get; }

    public static RuntimeValue operator +(RuntimeValue left, RuntimeValue right)
    {
        return left.value switch
        {
            bool => throw new Exception("Cannot sum a bool value."),
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
            bool => throw new Exception("Cannot subtract a bool value."),
            string => throw new Exception("Cannot subtract a string value."),
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
            bool => throw new Exception("Cannot subtract a bool value."),
            string => throw new Exception("Cannot subtract a string value."),
            float d => new RuntimeValue(-d),
            int i => new RuntimeValue(-i),
            _ => throw new NotImplementedException()
        };
    }

    public static RuntimeValue operator *(RuntimeValue left, RuntimeValue right)
    {
        return left.value switch
        {
            bool => throw new Exception("Cannot multiply a bool value."),
            string => throw new Exception("Cannot multiply a string value."),
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
            bool => throw new Exception("Cannot divide a bool value."),
            string => throw new Exception("Cannot divide a string value."),
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
            bool => throw new Exception("Cannot find module of a bool value."),
            string => throw new Exception("Cannot find module of a string value."),
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
            float d => Math.Abs(d) < FloatTolerance,
            _ => throw new NotImplementedException()
        };
    }

    public static bool operator false(RuntimeValue value)
    {
        return value.value switch
        {
            bool b => !b,
            int i => i == 0,
            float d => Math.Abs(d) > FloatTolerance,
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
                float d2 => d - d2 > FloatTolerance,
                int i => d - i > FloatTolerance,
                _ => throw new Exception("Incorrect comparison parameters.")
            },
            int i => right.value switch
            {
                int j => i > j,
                float d => i - d > FloatTolerance,
                _ => throw new Exception("Incorrect comparison parameters.")
            },
            _ => throw new NotImplementedException()
        };
    }

    public static bool operator <(RuntimeValue left, RuntimeValue right) => !(left > right) && !left.Equals(right);

    public static bool operator >=(RuntimeValue left, RuntimeValue right) => !(left < right);

    public static bool operator <=(RuntimeValue left, RuntimeValue right) => !(left > right);

    public bool ToBoolean()
    {
        return value switch
        {
            bool s => s,
            float d => Math.Abs(d) < FloatTolerance,
            int i => i != 0,
            _ => throw new NotImplementedException()
        };
    }

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

    public RuntimeArrayValue ToArray()
    {
        return value is RuntimeArrayValue array
            ? array
            : throw new InvalidOperationException($"Expected array, got {type}.");
    }

    public RuntimeStructValue ToStruct()
    {
        return value is RuntimeStructValue structure
            ? structure
            : throw new InvalidOperationException($"Expected struct, got {type}.");
    }

    public override string ToString()
    {
        return value switch
        {
            bool b => b ? "True" : "False",
            float d => d.ToString("0.####", CultureInfo.InvariantCulture),
            int i => i.ToString(CultureInfo.InvariantCulture),
            string s => s,
            RuntimeArrayValue array => "{" + string.Join(", ", array.Values.Select(v => v.ToString())) + "}",
            RuntimeStructValue structure => structure.TypeName,
            _ => throw new NotImplementedException()
        };
    }

    public override int GetHashCode() => value.GetHashCode();

    public override bool Equals(object? obj)
    {
        if (obj is not RuntimeValue other)
        {
            return false;
        }

        return value switch
        {
            bool s => other.value is bool b && b == s,
            float d => other.value switch
            {
                float d2 => Math.Abs(d2 - d) < FloatTolerance,
                int i => Math.Abs(i - d) < FloatTolerance,
                _ => false
            },
            int i => other.value switch
            {
                int j => i == j,
                float d => Math.Abs(d - i) < FloatTolerance,
                _ => false
            },
            string s => other.value is string s2 && s.Equals(s2),
            RuntimeArrayValue s => ReferenceEquals(s, other.value),
            RuntimeStructValue s => ReferenceEquals(s, other.value),
            _ => throw new NotImplementedException()
        };
    }

    public RuntimeValueType GetValueType() => type;

    public TypeReference GetTypeReference() => typeReference;

    public RuntimeValue WithConstant(bool isConstant = true)
    {
        return value switch
        {
            int i => new RuntimeValue(i, isConstant),
            float d => new RuntimeValue(d, isConstant),
            bool b => new RuntimeValue(b, isConstant),
            string s => new RuntimeValue(s, isConstant),
            RuntimeArrayValue array => new RuntimeValue(array, isConstant),
            RuntimeStructValue structure => new RuntimeValue(structure, isConstant),
            _ => throw new NotImplementedException()
        };
    }

    private static RuntimeValueType ToRuntimeValueType(TypeReference type)
    {
        if (type.IsArray)
        {
            return RuntimeValueType.Array;
        }

        return type.Kind switch
        {
            VariableType.Int => RuntimeValueType.Int,
            VariableType.Float => RuntimeValueType.Float,
            VariableType.Boolean => RuntimeValueType.Boolean,
            VariableType.String => RuntimeValueType.String,
            VariableType.Struct => RuntimeValueType.Struct,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private static TypeReference ToTypeReference(RuntimeValueType type)
    {
        return type switch
        {
            RuntimeValueType.Int => TypeReference.Int,
            RuntimeValueType.Float => TypeReference.Float,
            RuntimeValueType.Boolean => TypeReference.Boolean,
            RuntimeValueType.String => TypeReference.String,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
