using Ast.Expressions;

namespace Ast.Statements;

public sealed class AssignmentStatement(Expression target, Expression value) : Statement
{
    public AssignmentStatement(string name, Expression value)
        : this(new VariableExpression(name), value)
    {
    }

    public Expression Target { get; } = target;

    public string Name => Target is VariableExpression variable ? variable.Name : string.Empty;

    public Expression Value { get; } = value;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}
