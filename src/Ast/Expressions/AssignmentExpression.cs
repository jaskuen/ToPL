namespace Ast.Expressions;

public class AssignmentExpression(Expression target, Expression value) : Expression
{
    public Expression Target { get; } = target;

    public Expression Value { get; } = value;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}
