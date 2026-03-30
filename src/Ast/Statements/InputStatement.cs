namespace Ast.Statements;

public sealed class InputStatement(List<string> names) : Statement
{
    public List<string> Names => names;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}