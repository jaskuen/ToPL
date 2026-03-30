using Ast.Expressions;
using Ast.Statements;

namespace Ast.Declarations;

public class VariableDeclaration(VariableType variableType, Dictionary<string, Expression?> namesToValues)
    : Declaration
{
    public VariableType VariableType { get; } = variableType;

    public Dictionary<string, Expression?> NamesToValues => namesToValues;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}