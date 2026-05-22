using Ast.Expressions;

namespace Ast.Statements;

public sealed class ForLoopStatement(
    AstNode? initializer,
    Expression? condition,
    AstNode? post,
    ScopeStatement body)
    : Statement, IAstNodeWithReturn
{
    public AstNode? Initializer { get; } = initializer;

    public Expression? Condition { get; } = condition;

    public AstNode? Post { get; } = post;

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
