using Ast.Declarations;
using Ast.Statements;

namespace Ast;

public sealed class ProgramUnit(
    IReadOnlyList<StructDeclaration> structs,
    IReadOnlyList<FunctionDeclaration> functions,
    TypeReference mainType,
    ScopeStatement mainBody)
{
    public IReadOnlyList<StructDeclaration> Structs { get; } = structs;

    public IReadOnlyList<FunctionDeclaration> Functions { get; } = functions;

    public TypeReference MainType { get; } = mainType;

    public ScopeStatement MainBody { get; } = mainBody;
}
