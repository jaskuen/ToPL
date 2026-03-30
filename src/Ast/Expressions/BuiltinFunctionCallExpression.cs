namespace Ast.Expressions;

public class BuiltinFunctionCallExpression(
    string functionName,
    List<Expression> arguments) : Expression
{
    public string FunctionName { get; } = functionName;

    public List<Expression> Arguments => arguments;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}