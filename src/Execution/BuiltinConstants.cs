using Runtime;

namespace Execution;

public static class BuiltinConstants
{
    private static readonly Dictionary<string, RuntimeValue> Constants = new()
    {
        {
            "пи", Pi()
        },
        {
            "эйлер", Euler()
        },
    };

    public static RuntimeValue GetConstant(string name)
    {
        if (!Constants.TryGetValue(name, out RuntimeValue? constant))
        {
            throw new ArgumentException($"Unknown builtin const {name}");
        }

        return constant;
    }

    public static bool ContainsBuiltinConstant(string name)
    {
        return Constants.ContainsKey(name);
    }

    private static RuntimeValue Pi()
    {
        return new RuntimeValue(MathF.PI);
    }

    private static RuntimeValue Euler()
    {
        return new RuntimeValue(MathF.E);
    }
}