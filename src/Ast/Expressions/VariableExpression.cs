namespace Ast.Expressions;

public class VariableExpression(string name) : Expression
{
    public string Name { get; } = name;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}