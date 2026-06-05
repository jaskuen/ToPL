namespace Ast.Expressions;

public class FieldAccessExpression(Expression target, string fieldName) : Expression
{
    public Expression Target { get; } = target;

    public string FieldName { get; } = fieldName;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}
