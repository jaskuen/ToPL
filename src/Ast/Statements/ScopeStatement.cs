namespace Ast.Statements;

public class ScopeStatement(List<AstNode> statements) : Statement, IAstNodeWithReturn
{
    public IReadOnlyList<AstNode> Statements => statements;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }

    public bool AcceptWithReturn(IAstVisitor visitor)
    {
        return visitor.Visit(this);
    }
}