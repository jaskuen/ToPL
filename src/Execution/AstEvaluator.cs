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
                value = EvaluateExpression(v, declaration.Type);
            }
            else
            {
                if (declaration.IsConst)
                {
                    throw new Exception(
                        $"Constant variable declaration {name} requires a value");
                }

                value = CreateDefaultValue(declaration.Type);
            }

            context.DefineVariable(name, declaration.IsConst ? value.WithConstant() : value);
        }
    }

    public void Visit(StructDeclaration declaration)
    {
        context.DefineStruct(declaration);
    }

    public void Visit(AssignmentStatement statement)
    {
        AssignTarget(statement.Target, EvaluateExpression(statement.Value));
    }

    public void Visit(UnaryOperationExpression expression)
    {
        // Инкремент / декремент - значит у нас должно быть выражение - переменная
        if (expression.Operation is UnaryOperation.Increment or UnaryOperation.Decrement)
        {
            Expression target = UnwrapTransparentUnary(expression.Expression);
            target.Accept(this);
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

            AssignTarget(target, value);
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
            if (statement.Initializer is not null)
            {
                int stackSize = values.Count;
                statement.Initializer.Accept(this);
                if (values.Count > stackSize)
                {
                    values.Pop();
                }
            }

            while (true)
            {
                if (statement.Condition is not null)
                {
                    statement.Condition.Accept(this);
                    RuntimeValue endCondition = values.Pop();
                    if (!endCondition)
                    {
                        break;
                    }
                }

                context.PushScope(new Scope());

                // Выполняем тело цикла и отбрасываем результат.
                returnsValue = statement.Body.AcceptWithReturn(this);

                context.PopScope();

                if (returnsValue)
                {
                    break;
                }

                if (statement.Post is not null)
                {
                    int stackSize = values.Count;
                    statement.Post.Accept(this);
                    if (values.Count > stackSize)
                    {
                        values.Pop();
                    }
                }
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
            foreach ((string name, TypeReference type) in Enumerable.Reverse(function.Parameters))
            {
                context.DefineVariable(name, Coerce(values.Pop(), type));
            }

            // Исполняем функцию
            function.Body.Accept(this);

            if (function.ReturnType.Kind != VariableType.Void)
            {
                RuntimeValue value = values.Peek();
                values.Pop();
                values.Push(Coerce(value, function.ReturnType));
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

            if (valueType == RuntimeValueType.Int && variableType == RuntimeValueType.Float)
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
        statement.Value?.Accept(this);
    }

    public void Visit(EmptyStatement declaration)
    {
    }

    public void Visit(VariableExpression declaration)
    {
        values.Push(context.GetValue(declaration.Name));
    }

    public void Visit(ArrayLiteralExpression expression)
    {
        TypeReference? elementType = null;
        List<RuntimeValue> elements = [];
        foreach (Expression element in expression.Elements)
        {
            RuntimeValue value = EvaluateExpression(element);
            elementType ??= value.GetTypeReference();
            elements.Add(Coerce(value, elementType));
        }

        values.Push(new RuntimeValue(new RuntimeArrayValue(elementType ?? TypeReference.Int, elements)));
    }

    public void Visit(StructLiteralExpression expression)
    {
        StructDeclaration declaration = context.GetStruct(expression.TypeName);
        if (declaration.Fields.Count != expression.Values.Count)
        {
            throw new Exception(
                $"Struct {expression.TypeName} expects {declaration.Fields.Count} values, got {expression.Values.Count}.");
        }

        Dictionary<string, TypeReference> fieldTypes = [];
        Dictionary<string, RuntimeValue> fieldValues = [];
        for (int i = 0; i < declaration.Fields.Count; i++)
        {
            StructFieldDeclaration field = declaration.Fields[i];
            fieldTypes.Add(field.Name, field.Type);
            fieldValues.Add(field.Name, EvaluateExpression(expression.Values[i], field.Type));
        }

        values.Push(new RuntimeValue(new RuntimeStructValue(expression.TypeName, fieldTypes, fieldValues)));
    }

    public void Visit(ArrayAccessExpression expression)
    {
        RuntimeArrayValue array = EvaluateExpression(expression.Target).ToArray();
        int index = EvaluateExpression(expression.Index, TypeReference.Int).ToInt();
        values.Push(array.Get(index));
    }

    public void Visit(FieldAccessExpression expression)
    {
        RuntimeStructValue structure = EvaluateExpression(expression.Target).ToStruct();
        values.Push(structure.GetField(expression.FieldName));
    }

    public void Visit(AssignmentExpression expression)
    {
        RuntimeValue value = AssignTarget(expression.Target, EvaluateExpression(expression.Value));
        values.Push(value);
    }

    private RuntimeValue EvaluateExpression(Expression expression, TypeReference? expectedType = null)
    {
        expression.Accept(this);
        RuntimeValue value = values.Pop();
        return expectedType is null ? value : Coerce(value, expectedType);
    }

    private RuntimeValue AssignTarget(Expression target, RuntimeValue value)
    {
        Expression unwrapped = UnwrapTransparentUnary(target);
        switch (unwrapped)
        {
            case VariableExpression variable:
                TypeReference variableType = context.GetValue(variable.Name).GetTypeReference();
                RuntimeValue coercedValue = Coerce(value, variableType);
                context.AssignVariable(variable.Name, coercedValue);
                return coercedValue;
            case ArrayAccessExpression arrayAccess:
                RuntimeArrayValue array = EvaluateExpression(arrayAccess.Target).ToArray();
                int index = EvaluateExpression(arrayAccess.Index, TypeReference.Int).ToInt();
                RuntimeValue arrayValue = Coerce(value, array.ElementType);
                array.Set(index, arrayValue);
                return arrayValue;
            case FieldAccessExpression fieldAccess:
                RuntimeStructValue structure = EvaluateExpression(fieldAccess.Target).ToStruct();
                if (!structure.FieldTypes.TryGetValue(fieldAccess.FieldName, out TypeReference? fieldType))
                {
                    throw new Exception($"Struct {structure.TypeName} has no field {fieldAccess.FieldName}.");
                }

                RuntimeValue fieldValue = Coerce(value, fieldType);
                structure.SetField(fieldAccess.FieldName, fieldValue);
                return fieldValue;
            default:
                throw new Exception($"Expression {target.GetType().Name} cannot be assigned.");
        }
    }

    private RuntimeValue Coerce(RuntimeValue value, TypeReference expectedType)
    {
        if (expectedType.IsAssignableFrom(value.GetTypeReference()))
        {
            if (!expectedType.IsArray &&
                expectedType.Kind == VariableType.Float &&
                value.GetTypeReference().Kind == VariableType.Int)
            {
                return new RuntimeValue(value.ToFloat());
            }

            return value;
        }

        throw new Exception($"Type mismatch: Expected {expectedType}, got {value.GetTypeReference()}");
    }

    private RuntimeValue CreateDefaultValue(TypeReference type)
    {
        if (type.IsArray)
        {
            return new RuntimeValue(new RuntimeArrayValue(type.ElementType, []));
        }

        if (type.Kind == VariableType.Struct)
        {
            StructDeclaration declaration = context.GetStruct(type.StructName ?? string.Empty);
            Dictionary<string, TypeReference> fieldTypes = [];
            Dictionary<string, RuntimeValue> fieldValues = [];
            foreach (StructFieldDeclaration field in declaration.Fields)
            {
                fieldTypes.Add(field.Name, field.Type);
                fieldValues.Add(field.Name, CreateDefaultValue(field.Type));
            }

            return new RuntimeValue(new RuntimeStructValue(declaration.Name, fieldTypes, fieldValues));
        }

        return new RuntimeValue(type);
    }

    private static Expression UnwrapTransparentUnary(Expression expression)
    {
        return expression is UnaryOperationExpression { Operation: null } unary ? unary.Expression : expression;
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
                return value is RuntimeValueType.Int or RuntimeValueType.Float;
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
        return value is RuntimeValueType.Int or RuntimeValueType.Float;
    }

    private RuntimeValueType ToRuntimeValueType(VariableType value)
    {
        return value switch
        {
            VariableType.Int => RuntimeValueType.Int,
            VariableType.Float => RuntimeValueType.Float,
            VariableType.String => RuntimeValueType.String,
            VariableType.Boolean => RuntimeValueType.Boolean,
            _ => throw new Exception($"Unexpected value type: {value}"),
        };
    }
}
