namespace Ast.Expressions;

public class BinaryOperationExpression(Expression left, BinaryOperation? operation = null, Expression? right = null)
    : Expression
{
    public Expression Left { get; } = left;

    // Nullable для возможности перехода от UnaryOperation
    public BinaryOperation? Operation { get; } = operation;

    public Expression? Right { get; } = right;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}