using Ast.Statements;

namespace Ast.Declarations;

public sealed class FunctionDeclaration(
    VariableType type,
    string name,
    Dictionary<string, VariableType> parameters,
    Statement body)
    : Declaration
{
    public VariableType Type { get; } = type;

    public string Name { get; } = name;

    public Dictionary<string, VariableType> Parameters { get; } = parameters;

    public Statement Body { get; } = body;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }
}