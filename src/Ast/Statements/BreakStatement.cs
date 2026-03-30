namespace Ast.Statements;

public sealed class BreakStatement : Statement
{
    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}