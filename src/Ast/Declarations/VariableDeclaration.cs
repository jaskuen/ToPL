using Ast.Expressions;

namespace Ast.Declarations;

public class VariableDeclaration : Declaration
{
    public VariableDeclaration(bool isConst, VariableType variableType, Dictionary<string, Expression?> namesToValues)
        : this(isConst, new TypeReference(variableType), namesToValues)
    {
    }

    public VariableDeclaration(bool isConst, TypeReference type, Dictionary<string, Expression?> namesToValues)
    {
        IsConst = isConst;
        Type = type;
        NamesToValues = namesToValues;
    }

    public bool IsConst { get; }

    public TypeReference Type { get; }

    public VariableType VariableType => Type.Kind;

    public Dictionary<string, Expression?> NamesToValues { get; }

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}
