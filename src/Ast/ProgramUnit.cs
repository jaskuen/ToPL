using Ast.Declarations;
using Ast.Statements;

namespace Ast;

public sealed class ProgramUnit(
    IReadOnlyList<FunctionDeclaration> functions,
    VariableType mainType,
    ScopeStatement mainBody)
{
    public IReadOnlyList<FunctionDeclaration> Functions { get; } = functions;

    public VariableType MainType { get; } = mainType;

    public ScopeStatement MainBody { get; } = mainBody;
}
