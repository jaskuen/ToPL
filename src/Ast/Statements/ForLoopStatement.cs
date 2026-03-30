using Ast.Expressions;

namespace Ast.Statements;

public sealed class ForLoopStatement(
    string iteratorName,
    Expression startValue,
    Expression endCondition,
    Expression stepValueExpression,
    ScopeStatement body)
    : Statement, IAstNodeWithReturn
{
    public string IteratorName { get; } = iteratorName;

    public Expression StartValue { get; } = startValue;

    public Expression EndCondition { get; } = endCondition;

    public Expression StepValueExpression { get; } = stepValueExpression;

    public ScopeStatement Body { get; } = body;

    public override void Accept(IAstVisitor visitor)
    {
        visitor.Visit(this);
    }

    public bool AcceptWithReturn(IAstVisitor visitor)
    {
        return visitor.Visit(this);
    }
}