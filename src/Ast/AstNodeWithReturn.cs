namespace Ast;

public interface IAstNodeWithReturn
{
    public bool AcceptWithReturn(IAstVisitor visitor);
}