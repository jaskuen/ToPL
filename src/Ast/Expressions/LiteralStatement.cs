namespace Ast.Expressions;

public class LiteralExpression(object value) : Expression
{
    public object Value { get; } = value;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}