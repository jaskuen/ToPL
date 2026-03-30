using Ast.Expressions;

namespace Ast.Statements;

public sealed class AssignmentStatement(string name, Expression value) : Statement
{
    public string Name { get; } = name;

    public Expression Value { get; } = value;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}