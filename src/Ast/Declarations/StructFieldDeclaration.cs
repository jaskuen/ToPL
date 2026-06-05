namespace Ast.Declarations;

public record StructFieldDeclaration
{
    public StructFieldDeclaration(TypeReference type, string name)
    {
        Type = type;
        Name = name;
    }

    public TypeReference Type { get; init; }

    public string Name { get; init; }
}
