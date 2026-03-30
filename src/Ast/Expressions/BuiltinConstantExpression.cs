namespace Ast.Expressions;

public class BuiltinConstantExpression(string name) : Expression
{
    public string Name { get; } = name;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}