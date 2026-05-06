using System.Globalization;

using Runtime;

namespace Execution;

public class ConsoleEnvironment : IEnvironment
{
    public void PrintValue(string value)
    {
        Console.Write(value);
    }

    public RuntimeValue ReadValue(RuntimeValueType type)
    {
        string? value = Console.ReadLine();

        if (value == null)
        {
            return new RuntimeValue(type);
        }

        return type switch
        {
            RuntimeValueType.Int => new RuntimeValue(int.Parse(value, CultureInfo.InvariantCulture)),
            RuntimeValueType.Double => new RuntimeValue(
                float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture)),
            RuntimeValueType.String => new RuntimeValue(value),
            RuntimeValueType.Boolean => new RuntimeValue(bool.Parse(value)),
            _ => throw new Exception("Unknown value type")
        };
    }
}
