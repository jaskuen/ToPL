namespace Ast.Expressions;

public sealed class FunctionCallExpression(string name, List<Expression> arguments) : Expression
{
    public string Name { get; } = name;

    public IReadOnlyList<Expression> Arguments => arguments;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}