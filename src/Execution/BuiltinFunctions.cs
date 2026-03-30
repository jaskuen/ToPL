using Runtime;

namespace Execution;

public static class BuiltinFunctions
{
    private static readonly Dictionary<string, Func<List<RuntimeValue>, RuntimeValue>> Functions = new()
    {
        { "преисподняя", Floor },
        { "небеса", Ceiling },
        { "колобок", Round },
        { "синус", Sin },
        { "косинус", Cos },
        { "тангенс", Tan },
        { "верстыСловесные", StrLen },
        { "раздробити", Substring },
        { "кНевысокому", ToLower },
        { "кСловесам", ToStringType },
        { "кБлагодати", ToIntType },
        { "кКадилу", ToDoubleType },
    };

    public static Func<List<RuntimeValue>, RuntimeValue> GetFunction(string name)
    {
        if (!Functions.TryGetValue(name, out Func<List<RuntimeValue>, RuntimeValue>? function))
        {
            throw new ArgumentException($"Unknown builtin function {name}");
        }

        return function;
    }

    public static bool ContainsFunctionWithName(string name)
    {
        return Functions.ContainsKey(name);
    }

    private static RuntimeValue Floor(List<RuntimeValue> arguments)
    {
        if (arguments.Count != 1)
        {
            throw new ArgumentException($"Incorrect arguments count: {string.Join(", ", arguments)}");
        }

        return new RuntimeValue(Math.Floor(arguments[0].ToDouble()));
    }

    private static RuntimeValue Ceiling(List<RuntimeValue> arguments)
    {
        if (arguments.Count != 1)
        {
            throw new ArgumentException($"Incorrect arguments count: {string.Join(", ", arguments)}");
        }

        return new RuntimeValue(Math.Ceiling(arguments[0].ToDouble()));
    }

    private static RuntimeValue Round(List<RuntimeValue> arguments)
    {
        if (arguments.Count != 1)
        {
            throw new ArgumentException($"Incorrect arguments count: {string.Join(", ", arguments)}");
        }

        return new RuntimeValue(Math.Round(arguments[0].ToDouble()));
    }

    private static RuntimeValue Sin(List<RuntimeValue> arguments)
    {
        if (arguments.Count != 1)
        {
            throw new ArgumentException($"Incorrect arguments count: {string.Join(", ", arguments)}");
        }

        return new RuntimeValue(Math.Sin(arguments[0].ToDouble()));
    }

    private static RuntimeValue Cos(List<RuntimeValue> arguments)
    {
        if (arguments.Count != 1)
        {
            throw new ArgumentException($"Incorrect arguments count: {string.Join(", ", arguments)}");
        }

        return new RuntimeValue(Math.Cos(arguments[0].ToDouble()));
    }

    private static RuntimeValue Tan(List<RuntimeValue> arguments)
    {
        if (arguments.Count != 1)
        {
            throw new ArgumentException($"Incorrect arguments count: {string.Join(", ", arguments)}");
        }

        return new RuntimeValue(Math.Tan(arguments[0].ToDouble()));
    }

    private static RuntimeValue Substring(List<RuntimeValue> arguments)
    {
        if (arguments.Count != 3)
        {
            throw new ArgumentException($"Incorrect arguments count: {string.Join(", ", arguments)}");
        }

        string s = arguments[0].ToString();
        int index = arguments[1].ToInt();
        int length = arguments[2].ToInt();
        return new RuntimeValue(s.Substring(index, length));
    }

    private static RuntimeValue StrLen(List<RuntimeValue> arguments)
    {
        if (arguments.Count != 1)
        {
            throw new ArgumentException($"Incorrect arguments count: {string.Join(", ", arguments)}");
        }

        return new RuntimeValue(arguments[0].ToString().Length);
    }

    private static RuntimeValue ToLower(List<RuntimeValue> arguments)
    {
        if (arguments.Count != 1)
        {
            throw new ArgumentException($"Incorrect arguments count: {string.Join(", ", arguments)}");
        }

        return new RuntimeValue(arguments[0].ToString().ToLower());
    }

    private static RuntimeValue ToStringType(List<RuntimeValue> arguments)
    {
        if (arguments.Count != 1)
        {
            throw new ArgumentException($"Incorrect arguments count: {string.Join(", ", arguments)}");
        }

        RuntimeValue value = arguments[0];
        RuntimeValueType type = value.GetValueType();
        if (type is not(RuntimeValueType.Int or RuntimeValueType.Double))
        {
            throw new Exception($"Incorrect ToStringType argument type: {type}");
        }

        return new RuntimeValue(arguments[0].ToString());
    }

    private static RuntimeValue ToIntType(List<RuntimeValue> arguments)
    {
        if (arguments.Count != 1)
        {
            throw new ArgumentException($"Incorrect arguments count: {string.Join(", ", arguments)}");
        }

        RuntimeValue value = arguments[0];
        RuntimeValueType type = value.GetValueType();
        if (type is not RuntimeValueType.String)
        {
            throw new Exception($"Incorrect ToIntType argument type: {type}");
        }

        return new RuntimeValue(arguments[0].ToInt());
    }

    private static RuntimeValue ToDoubleType(List<RuntimeValue> arguments)
    {
        if (arguments.Count != 1)
        {
            throw new ArgumentException($"Incorrect arguments count: {string.Join(", ", arguments)}");
        }

        RuntimeValue value = arguments[0];
        RuntimeValueType type = value.GetValueType();
        if (type is not RuntimeValueType.String)
        {
            throw new Exception($"Incorrect ToDoubleType argument type: {type}");
        }

        return new RuntimeValue(arguments[0].ToDouble());
    }
}