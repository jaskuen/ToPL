using Ast.Expressions;

namespace Ast.Statements;

public sealed class OutputStatement(List<Expression> expressions) : Statement
{
    public List<Expression> Expressions => expressions;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}