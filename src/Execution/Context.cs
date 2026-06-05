using Ast.Declarations;

using Runtime;

namespace Execution;

/// <summary>
/// Контекст выполнения программы (все переменные и другие символы).
/// </summary>
public class Context
{
    private readonly Stack<Scope> scopes = [];
    private readonly Dictionary<string, FunctionDeclaration> functions = [];
    private readonly Dictionary<string, StructDeclaration> structs = [];

    public void PushScope(Scope scope)
    {
        scopes.Push(scope);
    }

    public void PopScope()
    {
        scopes.Pop();
    }

    /// <summary>
    /// Возвращает значение переменной или константы.
    /// </summary>
    public RuntimeValue GetValue(string name)
    {
        foreach (Scope s in scopes)
        {
            if (s.TryGetVariable(name, out RuntimeValue variable))
            {
                return variable;
            }
        }

        throw new ArgumentException($"Variable '{name}' is not defined");
    }

    /// <summary>
    /// Возвращает тип данных переменной или константы.
    /// </summary>
    public RuntimeValueType GetValueType(string name)
    {
        RuntimeValue value = GetValue(name);
        return value.GetValueType();
    }

    /// <summary>
    /// Присваивает (изменяет) значение переменной.
    /// </summary>
    public void AssignVariable(string name, RuntimeValue value)
    {
        foreach (Scope s in scopes.Reverse())
        {
            if (s.TryAssignVariable(name, value))
            {
                return;
            }
        }

        throw new ArgumentException($"Variable '{name}' is not defined");
    }

    /// <summary>
    /// Определяет переменную в текущей области видимости.
    /// </summary>
    public void DefineVariable(string name, RuntimeValue value)
    {
        if (!scopes.Peek().TryDefineVariable(name, value))
        {
            throw new ArgumentException($"Variable '{name}' is already defined in this scope");
        }
    }

    public FunctionDeclaration GetFunction(string name)
    {
        if (functions.TryGetValue(name, out FunctionDeclaration? function))
        {
            return function;
        }

        throw new ArgumentException($"Function '{name}' is not defined");
    }

    public void DefineFunction(FunctionDeclaration function)
    {
        if (!functions.TryAdd(function.Name, function))
        {
            throw new ArgumentException($"Function '{function.Name}' is already defined");
        }
    }

    public StructDeclaration GetStruct(string name)
    {
        if (structs.TryGetValue(name, out StructDeclaration? structure))
        {
            return structure;
        }

        throw new ArgumentException($"Struct '{name}' is not defined");
    }

    public void DefineStruct(StructDeclaration structure)
    {
        if (!structs.TryAdd(structure.Name, structure))
        {
            throw new ArgumentException($"Struct '{structure.Name}' is already defined");
        }
    }
}
