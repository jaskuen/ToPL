namespace Ast.Statements;

public sealed class ContinueStatement : Statement
{
    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}