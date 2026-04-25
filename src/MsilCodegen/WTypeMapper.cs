using Ast.Declarations;

namespace MsilCodegen;

public class WTypeMapper
{
    public Type MapType(VariableType type)
    {
        return type switch
        {
            VariableType.Int => typeof(int),
            VariableType.Double => typeof(float),
            VariableType.Boolean => typeof(bool),
            VariableType.String => typeof(string),
            VariableType.Void => typeof(void),
            _ => throw new NotSupportedException($"W type {type} cannot be converted into .NET type"),
        };
    }
}
