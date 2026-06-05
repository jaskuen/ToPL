namespace Ast.Declarations;

public record TypeReference
{
    public TypeReference(VariableType kind, bool isArray = false, string? structName = null)
    {
        Kind = kind;
        IsArray = isArray;
        StructName = structName;
    }

    public static readonly TypeReference Int = new(VariableType.Int);
    public static readonly TypeReference Float = new(VariableType.Float);
    public static readonly TypeReference Boolean = new(VariableType.Boolean);
    public static readonly TypeReference String = new(VariableType.String);
    public static readonly TypeReference Void = new(VariableType.Void);

    public VariableType Kind { get; init; }

    public bool IsArray { get; init; }

    public string? StructName { get; init; }

    public TypeReference ElementType
    {
        get
        {
            if (!IsArray)
            {
                throw new InvalidOperationException($"{this} is not an array type.");
            }

            return new TypeReference(Kind, false, StructName);
        }
    }

    public static TypeReference ArrayOf(TypeReference elementType)
    {
        if (elementType.IsArray || elementType.Kind == VariableType.Void)
        {
            throw new InvalidOperationException($"Cannot create array of {elementType}.");
        }

        return elementType with { IsArray = true };
    }

    public static TypeReference Struct(string name) => new(VariableType.Struct, false, name);

    public static implicit operator TypeReference(VariableType kind) => new(kind);

    public static implicit operator VariableType(TypeReference type) => type.Kind;

    public bool IsAssignableFrom(TypeReference actual)
    {
        return Equals(actual) ||
               (!IsArray && !actual.IsArray && Kind == VariableType.Float && actual.Kind == VariableType.Int);
    }

    public override string ToString()
    {
        string name = Kind == VariableType.Struct ? StructName ?? "<anonymous struct>" : Kind.ToString();
        return IsArray ? $"{name}[]" : name;
    }
}
