using Runtime;

namespace Execution;

public static class BuiltinFunctions
{
    private static readonly Dictionary<string, Func<List<RuntimeValue>, RuntimeValue>> Functions = new()
    {
        { "floor", Floor },
        { "ceil", Ceiling },
        { "round", Round },
        { "sin", Sin },
        { "cos", Cos },
        { "tan", Tan },
        { "length", StrLen },
        { "substring", Substring },
        { "min", Min },
        { "max", Max },
        { "abs", Abs },
        { "кНевысокому", ToLower },
        { "кСловесам", ToStringType },
        { "кБлагодати", ToIntType },
        { "кКадилу", ToFloatType },
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

        return new RuntimeValue(MathF.Floor(arguments[0].ToFloat()));
    }

    private static RuntimeValue Ceiling(List<RuntimeValue> arguments)
    {
        if (arguments.Count != 1)
        {
            throw new ArgumentException($"Incorrect arguments count: {string.Join(", ", arguments)}");
        }

        return new RuntimeValue(MathF.Ceiling(arguments[0].ToFloat()));
    }

    private static RuntimeValue Round(List<RuntimeValue> arguments)
    {
        if (arguments.Count != 1)
        {
            throw new ArgumentException($"Incorrect arguments count: {string.Join(", ", arguments)}");
        }

        return new RuntimeValue(MathF.Round(arguments[0].ToFloat(), MidpointRounding.AwayFromZero));
    }

    private static RuntimeValue Sin(List<RuntimeValue> arguments)
    {
        if (arguments.Count != 1)
        {
            throw new ArgumentException($"Incorrect arguments count: {string.Join(", ", arguments)}");
        }

        return new RuntimeValue(MathF.Sin(arguments[0].ToFloat()));
    }

    private static RuntimeValue Cos(List<RuntimeValue> arguments)
    {
        if (arguments.Count != 1)
        {
            throw new ArgumentException($"Incorrect arguments count: {string.Join(", ", arguments)}");
        }

        return new RuntimeValue(MathF.Cos(arguments[0].ToFloat()));
    }

    private static RuntimeValue Tan(List<RuntimeValue> arguments)
    {
        if (arguments.Count != 1)
        {
            throw new ArgumentException($"Incorrect arguments count: {string.Join(", ", arguments)}");
        }

        return new RuntimeValue(MathF.Tan(arguments[0].ToFloat()));
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

    private static RuntimeValue Min(List<RuntimeValue> arguments)
    {
        if (arguments.Count != 2)
        {
            throw new ArgumentException($"Incorrect arguments count: {string.Join(", ", arguments)}");
        }

        return new RuntimeValue(Math.Min(arguments[0].ToFloat(), arguments[1].ToFloat()));
    }

    private static RuntimeValue Max(List<RuntimeValue> arguments)
    {
        if (arguments.Count != 2)
        {
            throw new ArgumentException($"Incorrect arguments count: {string.Join(", ", arguments)}");
        }

        return new RuntimeValue(Math.Max(arguments[0].ToFloat(), arguments[1].ToFloat()));
    }

    private static RuntimeValue Abs(List<RuntimeValue> arguments)
    {
        if (arguments.Count != 1)
        {
            throw new ArgumentException($"Incorrect arguments count: {string.Join(", ", arguments)}");
        }

        RuntimeValue value = arguments[0];

        return new RuntimeValue(Math.Abs(arguments[0].ToFloat()));
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
        if (type is not (RuntimeValueType.Int or RuntimeValueType.Float))
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

    private static RuntimeValue ToFloatType(List<RuntimeValue> arguments)
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

        return new RuntimeValue(arguments[0].ToFloat());
    }
}