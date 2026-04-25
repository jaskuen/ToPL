using Runtime;

namespace Execution;

public class Scope
{
    private readonly Dictionary<string, RuntimeValue> variables = [];

    /// <summary>
    /// Читает переменную из этой области видимости.
    /// Возвращает false, если переменная не объявлена в этой области видимости.
    /// </summary>
    public bool TryGetVariable(string name, out RuntimeValue value)
    {
        if (BuiltinConstants.ContainsBuiltinConstant(name))
        {
            value = BuiltinConstants.GetConstant(name)!;
            return true;
        }

        if (variables.TryGetValue(name, out RuntimeValue? v))
        {
            value = v;
            return true;
        }

        value = new RuntimeValue(0);
        return false;
    }

    /// <summary>
    /// Присваивает переменную в этой области видимости.
    /// Возвращает false, если переменная не объявлена в этой области видимости.
    /// </summary>
    public bool TryAssignVariable(string name, RuntimeValue value)
    {
        if (variables.TryGetValue(name, out RuntimeValue? currentValue))
        {
            if (currentValue.IsConstant)
            {
                throw new InvalidOperationException($"Constant '{name}' cannot be assigned.");
            }

            variables[name] = value;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Объявляет переменную в этой области видимости.
    /// Возвращает false, если переменная уже объявлена в этой области видимости.
    /// </summary>
    public bool TryDefineVariable(string name, RuntimeValue value)
    {
        return variables.TryAdd(name, value);
    }
}
