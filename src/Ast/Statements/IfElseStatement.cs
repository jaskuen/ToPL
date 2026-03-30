using Ast.Expressions;

namespace Ast.Statements;

public sealed class IfElseStatement(Expression condition, ScopeStatement thenBranch, ScopeStatement? elseBranch = null)
    : Statement, IAstNodeWithReturn
{
    public Expression Condition { get; } = condition;

    public ScopeStatement ThenBranch { get; } = thenBranch;

    public ScopeStatement? ElseBranch { get; } = elseBranch;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }

    public bool AcceptWithReturn(IAstVisitor visitor)
    {
        return visitor.Visit(this);
    }
}