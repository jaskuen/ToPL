using Ast.Expressions;

namespace Ast.Statements;

public class ReturnStatement(Expression value) : Statement
{
    public Expression Value { get; } = value;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}