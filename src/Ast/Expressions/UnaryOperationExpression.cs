using Ast.Statements;

namespace Ast.Expressions;

public sealed class UnaryOperationExpression(UnaryOperation? operation, Expression expression, bool isPostfix = false, bool doPushToStack = true)
    : Expression
{
    public UnaryOperation? Operation { get; } = operation;

    public Expression Expression { get; } = expression;

    public bool IsPostfix { get; } = isPostfix;

    public bool DoPushToStack { get; } = doPushToStack;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}