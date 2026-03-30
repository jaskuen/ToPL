namespace Ast;

public abstract class AstNode
{
    public abstract void Accept(IAstVisitor visitor);
}