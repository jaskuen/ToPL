using Ast;
using Ast.Declarations;
using Ast.Expressions;
using Ast.Statements;

namespace MsilCodegen;

public class MsilCodegenPass : IAstVisitor
{
    public void Visit(LiteralExpression expression)
    {
        throw new NotImplementedException();
    }

    public void Visit(BinaryOperationExpression expression)
    {
        throw new NotImplementedException();
    }

    public void Visit(FunctionDeclaration declaration)
    {
        throw new NotImplementedException();
    }

    public void Visit(VariableDeclaration declaration)
    {
        throw new NotImplementedException();
    }

    public void Visit(AssignmentStatement statement)
    {
        throw new NotImplementedException();
    }

    public void Visit(UnaryOperationExpression expression)
    {
        throw new NotImplementedException();
    }

    public bool Visit(ForLoopStatement statement)
    {
        throw new NotImplementedException();
    }

    public void Visit(FunctionCallExpression expression)
    {
        throw new NotImplementedException();
    }

    public void Visit(BuiltinFunctionCallExpression expression)
    {
        throw new NotImplementedException();
    }

    public void Visit(BuiltinConstantExpression expression)
    {
        throw new NotImplementedException();
    }

    public bool Visit(WhileLoopStatement statement)
    {
        throw new NotImplementedException();
    }

    public bool Visit(SwitchStatement statement)
    {
        throw new NotImplementedException();
    }

    public void Visit(InputStatement statement)
    {
        throw new NotImplementedException();
    }

    public void Visit(OutputStatement statement)
    {
        throw new NotImplementedException();
    }

    public bool Visit(IfElseStatement statement)
    {
        throw new NotImplementedException();
    }

    public bool Visit(ScopeStatement statement)
    {
        throw new NotImplementedException();
    }

    public void Visit(BreakStatement statement)
    {
        throw new NotImplementedException();
    }

    public void Visit(ContinueStatement statement)
    {
        throw new NotImplementedException();
    }

    public void Visit(ReturnStatement statement)
    {
        throw new NotImplementedException();
    }

    public void Visit(EmptyStatement declaration)
    {
        throw new NotImplementedException();
    }

    public void Visit(VariableExpression declaration)
    {
        throw new NotImplementedException();
    }
}