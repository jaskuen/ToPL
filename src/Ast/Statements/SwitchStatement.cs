using Ast.Expressions;

namespace Ast.Statements;

public sealed class SwitchStatement(
    Dictionary<Expression, ScopeStatement> cases,
    Expression expression,
    ScopeStatement defaultCase)
    : Statement, IAstNodeWithReturn
{
    public Expression Expression { get; } = expression;

    public Dictionary<Expression, ScopeStatement> Cases => cases;

    public ScopeStatement DefaultCase { get; } = defaultCase;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }

    public bool AcceptWithReturn(IAstVisitor visitor)
    {
        return visitor.Visit(this);
    }
}