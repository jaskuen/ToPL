using Ast.Statements;

namespace Ast.Declarations;

public sealed class FunctionDeclaration(
    TypeReference type,
    string name,
    Dictionary<string, TypeReference> parameters,
    Statement body)
    : Declaration
{
    public TypeReference ReturnType { get; } = type;

    public VariableType Type => ReturnType.Kind;

    public string Name { get; } = name;

    public Dictionary<string, TypeReference> Parameters { get; } = parameters;

    public Statement Body { get; } = body;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}
