namespace Ast.Expressions;

public class StructLiteralExpression(string typeName, IReadOnlyList<Expression> values) : Expression
{
    public string TypeName { get; } = typeName;

    public IReadOnlyList<Expression> Values { get; } = values;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}
