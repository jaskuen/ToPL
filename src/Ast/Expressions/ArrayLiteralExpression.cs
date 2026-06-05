namespace Ast.Expressions;

public class ArrayLiteralExpression(IReadOnlyList<Expression> elements) : Expression
{
    public IReadOnlyList<Expression> Elements { get; } = elements;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}
