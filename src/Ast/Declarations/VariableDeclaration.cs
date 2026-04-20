using Ast.Expressions;
using Ast.Statements;

namespace Ast.Declarations;

public class VariableDeclaration(bool isConst, VariableType variableType, Dictionary<string, Expression?> namesToValues)
    : Declaration
{
    public bool IsConst { get; } = isConst;

    public VariableType VariableType { get; } = variableType;

    public Dictionary<string, Expression?> NamesToValues => namesToValues;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}