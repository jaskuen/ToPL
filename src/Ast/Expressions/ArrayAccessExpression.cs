namespace Ast.Expressions;

public class ArrayAccessExpression(Expression target, Expression index) : Expression
{
    public Expression Target { get; } = target;

    public Expression Index { get; } = index;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}
