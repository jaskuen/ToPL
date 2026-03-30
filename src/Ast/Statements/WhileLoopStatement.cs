using Ast.Expressions;

namespace Ast.Statements;

public sealed class WhileLoopStatement(Expression condition, ScopeStatement body) : Statement, IAstNodeWithReturn
{
    public Expression Condition { get; } = condition;

    public ScopeStatement Body { get; } = body;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }

    public bool AcceptWithReturn(IAstVisitor visitor)
    {
        return visitor.Visit(this);
    }
}