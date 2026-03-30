using Ast.Declarations;
using Ast.Expressions;
using Ast.Statements;

namespace Ast;

public interface IAstVisitor
{
    public void Visit(LiteralExpression expression);

    public void Visit(BinaryOperationExpression expression);

    public void Visit(FunctionDeclaration declaration);

    public void Visit(VariableDeclaration declaration);

    public void Visit(AssignmentStatement statement);

    public void Visit(UnaryOperationExpression expression);

    public bool Visit(ForLoopStatement statement);

    public void Visit(FunctionCallExpression expression);

    public void Visit(BuiltinFunctionCallExpression expression);

    public void Visit(BuiltinConstantExpression expression);

    public bool Visit(WhileLoopStatement statement);

    public bool Visit(SwitchStatement statement);

    public void Visit(InputStatement statement);

    public void Visit(OutputStatement statement);

    public bool Visit(IfElseStatement statement);

    public bool Visit(ScopeStatement statement);

    public void Visit(BreakStatement statement);

    public void Visit(ContinueStatement statement);

    public void Visit(ReturnStatement statement);

    public void Visit(EmptyStatement declaration);

    public void Visit(VariableExpression declaration);
}