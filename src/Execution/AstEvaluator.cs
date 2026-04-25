using Ast;
using Ast.Declarations;
using Ast.Expressions;
using Ast.Statements;

using Runtime;

namespace Execution;

public class AstEvaluator : IAstVisitor
{
    private readonly IEnvironment environment;
    private readonly Context context;
    private readonly Stack<RuntimeValue> values = [];

    public AstEvaluator(Context context, IEnvironment environment)
    {
        this.context = context;
        this.environment = environment;
    }

    public RuntimeValue Evaluate(AstNode node, bool isMainFunctionVoid)
    {
        if (values.Count > 0)
        {
            throw new InvalidOperationException(
                $"Evaluation stack must be empty, but contains {values.Count} values: {string.Join(", ", values)}"
            );
        }

        node.Accept(this);

        if (!isMainFunctionVoid && values.Count == 0)
        {
            throw new InvalidOperationException(
                "Evaluator logical error: the stack has no evaluation result"
            );
        }

        if (isMainFunctionVoid && values.Count == 0)
        {
            return new RuntimeValue(0);
        }

        return values.Count switch
        {
            > 1 => throw new InvalidOperationException(
                $"Evaluator logical error: expected 1 value, got {values.Count} values: {string.Join(", ", values)}"
            ),
            _ => values.Pop(),
        };
    }

    public void Visit(LiteralExpression expression)
    {
        RuntimeValue value = expression.Value switch
        {
            int i => new RuntimeValue(i),
            float d => new RuntimeValue(d),
            string s => new RuntimeValue(s),
            bool b => new RuntimeValue(b),
            _ => throw new Exception(
                $"Unknown expression type met while trying to visit {nameof(LiteralExpression)}"),
        };
        values.Push(value);
    }

    public void Visit(BinaryOperationExpression expression)
    {
        expression.Left.Accept(this);
        if (expression.Operation == null || expression.Right == null)
        {
            return;
        }

        // Вычисление по короткой схеме
        if (expression.Operation is BinaryOperation.And or BinaryOperation.Or)
        {
            RuntimeValue value = values.Peek();

            if (value.GetValueType() != RuntimeValueType.Boolean)
            {
                throw new Exception(
                    $"Failed to perform short-circuit evaluation: value type is {value.GetValueType()}");
            }

            if ((expression.Operation is BinaryOperation.And && !value.ToBoolean()) ||
                (expression.Operation is BinaryOperation.Or && value.ToBoolean()))
            {
                return;
            }
        }

        expression.Right.Accept(this);
        RuntimeValue right = values.Pop();
        RuntimeValue left = values.Pop();

        if (!CanDoOperation(left.GetValueType(), expression.Operation!.Value, right.GetValueType()))
        {
            throw new Exception(
                $"Failed to perform binary operation: left: {left.GetValueType()}, " +
                $"right: {right.GetValueType()}, operator: {expression.Operation!.Value}");
        }

        switch (expression.Operation)
        {
            case BinaryOperation.Plus:
                values.Push(left + right);
                break;
            case BinaryOperation.Minus:
                values.Push(left - right);
                break;
            case BinaryOperation.Multiply:
                values.Push(left * right);
                break;
            case BinaryOperation.Divide:
                values.Push(left / right);
                break;
            case BinaryOperation.Modulo:
                values.Push(left % right);
                break;
            case BinaryOperation.GreaterThan:
                values.Push(new RuntimeValue(left > right));
                break;
            case BinaryOperation.GreaterThanOrEqual:
                values.Push(new RuntimeValue(left >= right));
                break;
            case BinaryOperation.LessThan:
                values.Push(new RuntimeValue(left < right));
                break;
            case BinaryOperation.LessThanOrEqual:
                values.Push(new RuntimeValue(left <= right));
                break;
            case BinaryOperation.Equal:
                values.Push(new RuntimeValue(Equals(left, right)));
                break;
            case BinaryOperation.NotEqual:
                values.Push(new RuntimeValue(!Equals(left, right)));
                break;
            case BinaryOperation.Or:
                values.Push(new RuntimeValue(left.ToBoolean() || right.ToBoolean()));
                break;
            case BinaryOperation.And:
                values.Push(new RuntimeValue(left.ToBoolean() && right.ToBoolean()));
                break;
            default:
                throw new NotImplementedException();
        }
    }

    public void Visit(FunctionDeclaration declaration)
    {
        context.DefineFunction(declaration);
    }

    public void Visit(VariableDeclaration declaration)
    {
        foreach ((string name, Expression? v) in declaration.NamesToValues)
        {
            RuntimeValue value;
            if (v is not null)
            {
                v.Accept(this);
                value = values.Pop();

                RuntimeValueType valueType = value.GetValueType();
                RuntimeValueType variableType = ToRuntimeValueType(declaration.VariableType);

                if (valueType == RuntimeValueType.Int && variableType == RuntimeValueType.Double)
                {
                    value = new RuntimeValue(value.ToFloat());
                }
                else if (valueType != variableType)
                {
                    throw new Exception(
                        $"Variable declaration type mismatch: Expected {variableType}, got {valueType}");
                }
            }
            else
            {
                if (declaration.IsConst)
                {
                    throw new Exception(
                        $"Constant variable declaration {name} requires a value");
                }

                value = declaration.VariableType switch
                {
                    VariableType.Int => new RuntimeValue(RuntimeValueType.Int),
                    VariableType.Double => new RuntimeValue(RuntimeValueType.Double),
                    VariableType.Boolean => new RuntimeValue(RuntimeValueType.Boolean),
                    VariableType.String => new RuntimeValue(RuntimeValueType.String),
                    _ => throw new InvalidOperationException()
                };
            }

            context.DefineVariable(name, declaration.IsConst ? value.WithConstant() : value);
        }
    }

    public void Visit(AssignmentStatement statement)
    {
        statement.Value.Accept(this);
        RuntimeValue value = values.Pop();

        RuntimeValueType expressionValueType = value.GetValueType();
        RuntimeValueType actualType = context.GetValueType(statement.Name);

        if (expressionValueType != actualType)
        {
            throw new Exception(
                $"Variable assign type mismatch: Expected {actualType}, got {expressionValueType}");
        }

        context.AssignVariable(statement.Name, value);
    }

    public void Visit(UnaryOperationExpression expression)
    {
        // Инкремент / декремент - значит у нас должно быть выражение - переменная
        if (expression.Operation is UnaryOperation.Increment or UnaryOperation.Decrement)
        {
            if (expression.Expression.GetType() != typeof(VariableExpression) &&
                (expression.Expression as UnaryOperationExpression)?.Expression.GetType() !=
                typeof(VariableExpression))
            {
                throw new Exception("Cannot use this operation not on a variable");
            }

            VariableExpression variable = expression.Expression is VariableExpression
                ? (expression.Expression as VariableExpression)!
                : ((expression.Expression as UnaryOperationExpression)!.Expression as VariableExpression)!;
            string name = variable.Name;

            variable.Accept(this);
            RuntimeValue value = expression is { IsPostfix: true, DoPushToStack: true } ? values.Peek() : values.Pop();

            if (!CanDoOperation(value.GetValueType(), expression.Operation))
            {
                throw new Exception(
                    $"Failed to perform unary operation: value: {value.GetValueType()}, operation: {expression.Operation}");
            }

            if (expression.Operation == UnaryOperation.Increment)
            {
                value += new RuntimeValue(1);
            }
            else
            {
                value -= new RuntimeValue(1);
            }

            context.AssignVariable(name, value);
            if (expression is { IsPostfix: false, DoPushToStack: true })
            {
                values.Push(value);
            }

            return;
        }

        expression.Expression.Accept(this);
        RuntimeValue value2 = values.Peek();

        if (!CanDoOperation(value2.GetValueType(), expression.Operation))
        {
            throw new Exception(
                $"Failed to perform unary operation: value: {value2.GetValueType()}, operation: {expression.Operation}");
        }

        switch (expression.Operation)
        {
            case UnaryOperation.Minus:
                values.Push(-values.Pop());
                break;
            case UnaryOperation.Not:
                values.Push(new RuntimeValue(!values.Pop()));
                break;
            case UnaryOperation.Plus:
            case null:
                break;
            default:
                throw new NotImplementedException();
        }
    }

    public bool Visit(ForLoopStatement statement)
    {
        bool returnsValue = false;

        context.PushScope(new Scope());
        try
        {
            // Вычисляем начальное значение переменной-итератора.
            statement.StartValue.Accept(this);
            RuntimeValue iteratorValue = values.Pop();

            // Определяем переменную-итератор и добавляем в стек вероятное значение цикла
            context.DefineVariable(statement.IteratorName, iteratorValue);
            while (true)
            {
                // Вычисляем выражение-условие, проверяем, верно ли оно.
                statement.EndCondition.Accept(this);
                RuntimeValue endCondition = values.Pop();
                if (!endCondition)
                {
                    break;
                }

                context.PushScope(new Scope());

                // Выполняем тело цикла и отбрасываем результат.
                returnsValue = statement.Body.AcceptWithReturn(this);

                context.PopScope();

                if (returnsValue)
                {
                    break;
                }

                // Выполняем инкремент итератора.
                statement.StepValueExpression.Accept(this);
                values.Pop();
            }
        }
        finally
        {
            context.PopScope();
        }

        return returnsValue;
    }

    public void Visit(FunctionCallExpression expression)
    {
        FunctionDeclaration function = context.GetFunction(expression.Name);

        // NOTE: вычисляем аргументы и временно сохраняем их в стеке.
        foreach (Expression argument in expression.Arguments)
        {
            argument.Accept(this);
        }

        context.PushScope(new Scope());
        try
        {
            // Определяем параметры, извлекая их из стека в обратном порядке.
            foreach ((string name, VariableType _) in Enumerable.Reverse(function.Parameters))
            {
                context.DefineVariable(name, values.Pop());
            }

            // Исполняем функцию
            function.Body.Accept(this);

            if (function.Type != VariableType.Void)
            {
                RuntimeValue value = values.Peek();
                RuntimeValueType valueType = value.GetValueType();
                RuntimeValueType returnType = ToRuntimeValueType(function.Type);

                if (valueType == RuntimeValueType.Int && returnType == RuntimeValueType.Double)
                {
                    values.Push(new RuntimeValue(values.Pop().ToFloat()));
                }
                else if (valueType != returnType)
                {
                    throw new Exception(
                        $"Incorrect function return type: Expected {ToRuntimeValueType(function.Type)},  got {value.GetValueType()}");
                }
            }
        }
        finally
        {
            context.PopScope();
        }
    }

    public void Visit(BuiltinFunctionCallExpression expression)
    {
        List<RuntimeValue> arguments = [];

        // NOTE: вычисляем аргументы и сохраняем значения.
        foreach (Expression argument in expression.Arguments)
        {
            argument.Accept(this);
            arguments.Add(values.Pop());
        }

        Func<List<RuntimeValue>, RuntimeValue> function = BuiltinFunctions.GetFunction(expression.FunctionName);

        values.Push(function(arguments));
    }

    public void Visit(BuiltinConstantExpression expression)
    {
        values.Push(BuiltinConstants.GetConstant(expression.Name));
    }

    public bool Visit(WhileLoopStatement statement)
    {
        bool returnsValue = false;

        context.PushScope(new Scope());
        try
        {
            while (true)
            {
                // Вычисляем выражение-условие, проверяем, неверно ли оно.
                statement.Condition.Accept(this);
                RuntimeValue condition = values.Pop();
                if (!condition)
                {
                    break;
                }

                // Выполняем тело цикла и отбрасываем результат.
                returnsValue = statement.Body.AcceptWithReturn(this);

                if (returnsValue)
                {
                    break;
                }
            }
        }
        finally
        {
            context.PopScope();
        }

        return returnsValue;
    }

    public bool Visit(SwitchStatement statement)
    {
        bool returnsValue = false;

        context.PushScope(new Scope());
        try
        {
            // Вычисляем значение switch
            statement.Expression.Accept(this);
            RuntimeValue value = values.Pop();

            bool mustUseDefaultCase = true;
            foreach ((Expression e, ScopeStatement s) in statement.Cases)
            {
                e.Accept(this);
                RuntimeValue value2 = values.Pop();
                if (value.Equals(value2))
                {
                    returnsValue = s.AcceptWithReturn(this);

                    mustUseDefaultCase = false;
                    break;
                }
            }

            if (mustUseDefaultCase)
            {
                if (statement.DefaultCase is null)
                {
                    throw new Exception("Default case not found");
                }

                returnsValue = statement.DefaultCase.AcceptWithReturn(this);
            }
        }
        finally
        {
            context.PopScope();
        }

        return returnsValue;
    }

    public void Visit(InputStatement statement)
    {
        int count = statement.Names.Count;
        for (int i = 0; i < count; i++)
        {
            string variableName = statement.Names[i];
            RuntimeValueType variableType = context.GetValueType(variableName);

            RuntimeValue value = environment.ReadValue(variableType);
            RuntimeValueType valueType = value.GetValueType();

            if (valueType == RuntimeValueType.Int && variableType == RuntimeValueType.Double)
            {
                context.AssignVariable(variableName, new RuntimeValue(value.ToFloat()));
            }
            else if (valueType != variableType)
            {
                throw new Exception(
                    $"Variable assign via input statement type mismatch: Expected {variableType}, got {valueType}");
            }

            context.AssignVariable(variableName, value);
        }
    }

    public void Visit(OutputStatement statement)
    {
        string result = string.Empty;
        foreach (Expression e in statement.Expressions)
        {
            e.Accept(this);
            RuntimeValue value = values.Pop();

            result += value.ToString();
        }

        environment.PrintValue(result);
    }

    public bool Visit(IfElseStatement statement)
    {
        statement.Condition.Accept(this);

        RuntimeValue conditionValue = values.Pop();
        if (conditionValue)
        {
            return statement.ThenBranch.AcceptWithReturn(this);
        }

        return statement.ElseBranch?.AcceptWithReturn(this) ?? false;
    }

    public bool Visit(ScopeStatement statement)
    {
        bool doBreak = false;
        context.PushScope(new Scope());
        foreach (AstNode s in statement.Statements)
        {
            switch (s)
            {
                case BreakStatement:
                case ContinueStatement:
                    doBreak = true;
                    break;
                case ReturnStatement:
                    s.Accept(this);

                    context.PopScope();
                    return true;
                case IAstNodeWithReturn statementWithReturn:
                    bool returnsValue = statementWithReturn.AcceptWithReturn(this);
                    if (returnsValue)
                    {
                        context.PopScope();

                        return true;
                    }

                    break;
                default:
                    s.Accept(this);
                    break;
            }

            if (doBreak)
            {
                break;
            }
        }

        context.PopScope();

        return false;
    }

    public void Visit(BreakStatement statement)
    {
    }

    public void Visit(ContinueStatement statement)
    {
    }

    public void Visit(ReturnStatement statement)
    {
        statement.Value.Accept(this);
    }

    public void Visit(EmptyStatement declaration)
    {
    }

    public void Visit(VariableExpression declaration)
    {
        values.Push(context.GetValue(declaration.Name));
    }

    private bool CanDoOperation(RuntimeValueType value, UnaryOperation? operation)
    {
        switch (operation)
        {
            case UnaryOperation.Not:
                return value == RuntimeValueType.Boolean;
            case UnaryOperation.Plus:
            case UnaryOperation.Minus:
            case UnaryOperation.Increment:
            case UnaryOperation.Decrement:
                return value is RuntimeValueType.Int or RuntimeValueType.Double;
            case null:
                return true;
            default:
                throw new Exception($"Unexpected unary operation: {operation}");
        }
    }

    private bool CanDoOperation(RuntimeValueType first, BinaryOperation operation, RuntimeValueType second)
    {
        switch (operation)
        {
            case BinaryOperation.Minus:
            case BinaryOperation.Divide:
            case BinaryOperation.Multiply:
            case BinaryOperation.Modulo:
            case BinaryOperation.GreaterThan:
            case BinaryOperation.GreaterThanOrEqual:
            case BinaryOperation.LessThan:
            case BinaryOperation.LessThanOrEqual:
                return IsNumericType(first) && IsNumericType(second);
            case BinaryOperation.Plus:
                return (IsNumericType(first) && IsNumericType(second)) ||
                       (first is RuntimeValueType.String && second is RuntimeValueType.String);
            case BinaryOperation.Or:
            case BinaryOperation.And:
                return first is RuntimeValueType.Boolean && second is RuntimeValueType.Boolean;
            case BinaryOperation.Equal:
            case BinaryOperation.NotEqual:
                return first == second || (IsNumericType(first) && IsNumericType(second));
            default:
                throw new Exception($"Unexpected binary operation: {operation}");
        }
    }

    private bool IsNumericType(RuntimeValueType value)
    {
        return value is RuntimeValueType.Int or RuntimeValueType.Double;
    }

    private RuntimeValueType ToRuntimeValueType(VariableType value)
    {
        return value switch
        {
            VariableType.Int => RuntimeValueType.Int,
            VariableType.Double => RuntimeValueType.Double,
            VariableType.String => RuntimeValueType.String,
            VariableType.Boolean => RuntimeValueType.Boolean,
            _ => throw new Exception($"Unexpected value type: {value}"),
        };
    }
}
