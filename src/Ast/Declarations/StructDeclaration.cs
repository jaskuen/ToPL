namespace Ast.Declarations;

public class StructDeclaration(string name, IReadOnlyList<StructFieldDeclaration> fields) : Declaration
{
    public string Name { get; } = name;

    public IReadOnlyList<StructFieldDeclaration> Fields { get; } = fields;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}
