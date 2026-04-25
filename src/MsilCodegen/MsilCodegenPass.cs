using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

using Ast;
using Ast.Declarations;
using Ast.Expressions;
using Ast.Statements;

namespace MsilCodegen;

public class MsilCodegenPass : IAstVisitor
{
    private readonly ModuleBuilder moduleBuilder;
    private readonly WTypeMapper typeMapper = new();
    private readonly Stack<Dictionary<string, LocalValue>> scopes = [];

    private TypeBuilder programTypeBuilder = null!;
    private ILGenerator il = null!;

    public MsilCodegenPass(ModuleBuilder moduleBuilder)
    {
        this.moduleBuilder = moduleBuilder;
    }

    public MethodBuilder GenerateProgramCode(ProgramUnit program)
    {
        programTypeBuilder = moduleBuilder.DefineType(
            "Program",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class);

        MethodBuilder mainMethod = programTypeBuilder.DefineMethod(
            "Main",
            MethodAttributes.Public | MethodAttributes.Static,
            typeof(void),
            Type.EmptyTypes);

        il = mainMethod.GetILGenerator();
        BeginScope();
        EmitScopeBody(program.MainBody);
        EndScope();

        il.Emit(OpCodes.Ret);
        programTypeBuilder.CreateType();

        return mainMethod;
    }

    public void Visit(LiteralExpression expression)
    {
        EmitExpression(expression);
    }

    public void Visit(BinaryOperationExpression expression)
    {
        EmitExpression(expression);
    }

    public void Visit(FunctionDeclaration declaration)
    {
        throw new NotSupportedException("User functions are not supported by the MSIL backend yet.");
    }

    public void Visit(VariableDeclaration declaration)
    {
        foreach ((string name, Expression? initialValue) in declaration.NamesToValues)
        {
            Type clrType = typeMapper.MapType(declaration.VariableType);
            LocalBuilder local = il.DeclareLocal(clrType);
            EmitInitialValue(declaration.VariableType, initialValue, declaration.IsConst, name);
            il.Emit(OpCodes.Stloc, local);
            scopes.Peek().Add(name, new LocalValue(local, declaration.VariableType, declaration.IsConst));
        }
    }

    public void Visit(AssignmentStatement statement)
    {
        LocalValue variable = FindVariable(statement.Name);
        EnsureMutable(statement.Name, variable);
        VariableType valueType = EmitExpression(statement.Value);
        EmitConversion(valueType, variable.Type);
        il.Emit(OpCodes.Stloc, variable.Local);
    }

    public void Visit(UnaryOperationExpression expression)
    {
        EmitExpression(expression);
    }

    public bool Visit(ForLoopStatement statement)
    {
        throw new NotSupportedException("For loops are outside the third compiler iteration.");
    }

    public void Visit(FunctionCallExpression expression)
    {
        throw new NotSupportedException("User functions are not supported by the MSIL backend yet.");
    }

    public void Visit(BuiltinFunctionCallExpression expression)
    {
        EmitExpression(expression);
    }

    public void Visit(BuiltinConstantExpression expression)
    {
        EmitExpression(expression);
    }

    public bool Visit(WhileLoopStatement statement)
    {
        throw new NotSupportedException("While loops are outside the third compiler iteration.");
    }

    public bool Visit(SwitchStatement statement)
    {
        throw new NotSupportedException("Switch statements are outside the third compiler iteration.");
    }

    public void Visit(InputStatement statement)
    {
        if (statement.Names.Count == 0)
        {
            EmitConsoleRead(VariableType.String);
            il.Emit(OpCodes.Pop);
            return;
        }

        foreach (string name in statement.Names)
        {
            LocalValue variable = FindVariable(name);
            EnsureMutable(name, variable);
            EmitConsoleRead(variable.Type);
            il.Emit(OpCodes.Stloc, variable.Local);
        }
    }

    public void Visit(OutputStatement statement)
    {
        foreach (Expression expression in statement.Expressions)
        {
            VariableType type = EmitExpression(expression);
            EmitConsoleWrite(type);
        }
    }

    public bool Visit(IfElseStatement statement)
    {
        throw new NotSupportedException("If statements are outside the third compiler iteration.");
    }

    public bool Visit(ScopeStatement statement)
    {
        BeginScope();
        EmitScopeBody(statement);
        EndScope();
        return false;
    }

    public void Visit(BreakStatement statement)
    {
        throw new NotSupportedException("Break statements are outside the third compiler iteration.");
    }

    public void Visit(ContinueStatement statement)
    {
        throw new NotSupportedException("Continue statements are outside the third compiler iteration.");
    }

    public void Visit(ReturnStatement statement)
    {
        VariableType valueType = EmitExpression(statement.Value);
        if (valueType != VariableType.Void)
        {
            il.Emit(OpCodes.Pop);
        }

        il.Emit(OpCodes.Ret);
    }

    public void Visit(EmptyStatement declaration)
    {
    }

    public void Visit(VariableExpression declaration)
    {
        EmitExpression(declaration);
    }

    private void EmitScopeBody(ScopeStatement statement)
    {
        foreach (AstNode node in statement.Statements)
        {
            node.Accept(this);
        }
    }

    private VariableType EmitExpression(Expression expression)
    {
        return expression switch
        {
            LiteralExpression literal => EmitLiteral(literal),
            VariableExpression variable => EmitVariable(variable),
            UnaryOperationExpression unary => EmitUnaryOperation(unary),
            BinaryOperationExpression binary => EmitBinaryOperation(binary),
            BuiltinFunctionCallExpression builtin => EmitBuiltinFunctionCall(builtin),
            BuiltinConstantExpression builtinConstant => EmitBuiltinConstant(builtinConstant),
            FunctionCallExpression => throw new NotSupportedException(
                "User functions are not supported by the MSIL backend yet."),
            _ => throw new NotSupportedException($"Expression {expression.GetType().Name} is not supported."),
        };
    }

    private VariableType EmitLiteral(LiteralExpression expression)
    {
        switch (expression.Value)
        {
            case int value:
                il.Emit(OpCodes.Ldc_I4, value);
                return VariableType.Int;
            case float value:
                il.Emit(OpCodes.Ldc_R4, value);
                return VariableType.Double;
            case bool value:
                il.Emit(value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                return VariableType.Boolean;
            case string value:
                il.Emit(OpCodes.Ldstr, value);
                return VariableType.String;
            default:
                throw new NotSupportedException($"Literal {expression.Value.GetType().Name} is not supported.");
        }
    }

    private VariableType EmitVariable(VariableExpression expression)
    {
        LocalValue variable = FindVariable(expression.Name);
        il.Emit(OpCodes.Ldloc, variable.Local);
        return variable.Type;
    }

    private VariableType EmitUnaryOperation(UnaryOperationExpression expression)
    {
        if (expression.Operation is UnaryOperation.Increment or UnaryOperation.Decrement)
        {
            return EmitIncrementOrDecrement(expression);
        }

        VariableType type = EmitExpression(expression.Expression);
        switch (expression.Operation)
        {
            case null:
            case UnaryOperation.Plus:
                return type;
            case UnaryOperation.Minus:
                EnsureNumeric(type, expression.Operation.Value);
                il.Emit(OpCodes.Neg);
                return type;
            case UnaryOperation.Not:
                EnsureType(type, VariableType.Boolean, expression.Operation.Value);
                EmitLogicalNot();
                return VariableType.Boolean;
            default:
                throw new NotSupportedException($"Unary operation {expression.Operation} is not supported.");
        }
    }

    private VariableType EmitIncrementOrDecrement(UnaryOperationExpression expression)
    {
        VariableExpression variableExpression = UnwrapVariableExpression(expression.Expression);
        LocalValue variable = FindVariable(variableExpression.Name);
        EnsureMutable(variableExpression.Name, variable);
        EnsureNumeric(variable.Type, expression.Operation!.Value);

        il.Emit(OpCodes.Ldloc, variable.Local);
        if (expression is { IsPostfix: true, DoPushToStack: true })
        {
            il.Emit(OpCodes.Dup);
        }

        EmitOne(variable.Type);
        il.Emit(expression.Operation == UnaryOperation.Increment ? OpCodes.Add : OpCodes.Sub);
        if (expression is { IsPostfix: false, DoPushToStack: true })
        {
            il.Emit(OpCodes.Dup);
        }

        il.Emit(OpCodes.Stloc, variable.Local);
        return variable.Type;
    }

    private VariableType EmitBinaryOperation(BinaryOperationExpression expression)
    {
        if (expression.Operation == null || expression.Right == null)
        {
            return EmitExpression(expression.Left);
        }

        if (expression.Operation is BinaryOperation.And or BinaryOperation.Or)
        {
            return EmitLogicalBinaryOperation(expression);
        }

        VariableType leftType = InferExpressionType(expression.Left);
        VariableType rightType = InferExpressionType(expression.Right);

        if (leftType == VariableType.String || rightType == VariableType.String)
        {
            return EmitStringBinaryOperation(expression, leftType, rightType);
        }

        if (IsComparison(expression.Operation.Value))
        {
            EmitNumericOperands(expression.Left, leftType, expression.Right, rightType);
            EmitComparison(expression.Operation.Value);
            return VariableType.Boolean;
        }

        VariableType resultType = leftType == VariableType.Double || rightType == VariableType.Double
            ? VariableType.Double
            : VariableType.Int;
        EmitNumericOperands(expression.Left, leftType, expression.Right, rightType, resultType);

        switch (expression.Operation)
        {
            case BinaryOperation.Plus:
                il.Emit(OpCodes.Add);
                break;
            case BinaryOperation.Minus:
                il.Emit(OpCodes.Sub);
                break;
            case BinaryOperation.Multiply:
                il.Emit(OpCodes.Mul);
                break;
            case BinaryOperation.Divide:
                il.Emit(OpCodes.Div);
                break;
            case BinaryOperation.Modulo:
                il.Emit(OpCodes.Rem);
                break;
            default:
                throw new NotSupportedException($"Binary operation {expression.Operation} is not supported.");
        }

        return resultType;
    }

    private VariableType EmitLogicalBinaryOperation(BinaryOperationExpression expression)
    {
        Label shortcutLabel = il.DefineLabel();
        Label endLabel = il.DefineLabel();

        VariableType leftType = EmitExpression(expression.Left);
        EnsureType(leftType, VariableType.Boolean, expression.Operation!.Value);

        if (expression.Operation == BinaryOperation.And)
        {
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Brfalse, shortcutLabel);
            il.Emit(OpCodes.Pop);
        }
        else
        {
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Brtrue, shortcutLabel);
            il.Emit(OpCodes.Pop);
        }

        VariableType rightType = EmitExpression(expression.Right!);
        EnsureType(rightType, VariableType.Boolean, expression.Operation.Value);
        il.Emit(OpCodes.Br, endLabel);
        il.MarkLabel(shortcutLabel);
        il.MarkLabel(endLabel);
        return VariableType.Boolean;
    }

    private VariableType EmitStringBinaryOperation(
        BinaryOperationExpression expression,
        VariableType leftType,
        VariableType rightType)
    {
        if (expression.Operation == BinaryOperation.Plus)
        {
            EmitExpression(expression.Left);
            EmitToString(leftType);
            EmitExpression(expression.Right!);
            EmitToString(rightType);
            il.Emit(OpCodes.Call, GetMethod(typeof(string), nameof(string.Concat), [typeof(string), typeof(string)]));
            return VariableType.String;
        }

        if (expression.Operation is BinaryOperation.Equal or BinaryOperation.NotEqual)
        {
            EnsureType(leftType, VariableType.String, expression.Operation.Value);
            EnsureType(rightType, VariableType.String, expression.Operation.Value);
            EmitExpression(expression.Left);
            EmitExpression(expression.Right!);
            il.Emit(OpCodes.Call, GetMethod(typeof(string), nameof(string.Equals), [typeof(string), typeof(string)]));
            if (expression.Operation == BinaryOperation.NotEqual)
            {
                EmitLogicalNot();
            }

            return VariableType.Boolean;
        }

        throw new NotSupportedException($"String operation {expression.Operation} is not supported.");
    }

    private VariableType EmitBuiltinFunctionCall(BuiltinFunctionCallExpression expression)
    {
        return expression.FunctionName switch
        {
            "floor" => EmitUnaryMathF(expression, nameof(MathF.Floor)),
            "ceil" => EmitUnaryMathF(expression, nameof(MathF.Ceiling)),
            "round" => EmitRound(expression),
            "sin" => EmitUnaryMathF(expression, nameof(MathF.Sin)),
            "cos" => EmitUnaryMathF(expression, nameof(MathF.Cos)),
            "tan" => EmitUnaryMathF(expression, nameof(MathF.Tan)),
            "length" => EmitLength(expression),
            "substring" => EmitSubstring(expression),
            "min" => EmitBinaryMathF(expression, nameof(MathF.Min)),
            "max" => EmitBinaryMathF(expression, nameof(MathF.Max)),
            "abs" => EmitUnaryMathF(expression, nameof(MathF.Abs)),
            "кСловесам" => EmitConvertToString(expression),
            "кБлагодати" => EmitParseConversion(expression, typeof(int), nameof(int.Parse), VariableType.Int),
            "кКадилу" => EmitParseConversion(expression, typeof(float), nameof(float.Parse), VariableType.Double),
            "кНевысокому" => EmitToLower(expression),
            _ => throw new NotSupportedException($"Builtin function {expression.FunctionName} is not supported."),
        };
    }

    private VariableType EmitBuiltinConstant(BuiltinConstantExpression expression)
    {
        return expression.Name switch
        {
            "пи" => EmitFloatConstant(MathF.PI),
            "эйлер" => EmitFloatConstant(MathF.E),
            _ => throw new NotSupportedException($"Builtin constant {expression.Name} is not supported."),
        };
    }

    private VariableType EmitUnaryMathF(BuiltinFunctionCallExpression expression, string methodName)
    {
        EnsureArgumentsCount(expression, 1);
        VariableType argumentType = EmitExpression(expression.Arguments[0]);
        EmitConversion(argumentType, VariableType.Double);
        il.Emit(OpCodes.Call, GetMethod(typeof(MathF), methodName, [typeof(float)]));
        return VariableType.Double;
    }

    private VariableType EmitBinaryMathF(BuiltinFunctionCallExpression expression, string methodName)
    {
        EnsureArgumentsCount(expression, 2);
        VariableType leftType = EmitExpression(expression.Arguments[0]);
        EmitConversion(leftType, VariableType.Double);
        VariableType rightType = EmitExpression(expression.Arguments[1]);
        EmitConversion(rightType, VariableType.Double);
        il.Emit(OpCodes.Call, GetMethod(typeof(MathF), methodName, [typeof(float), typeof(float)]));
        return VariableType.Double;
    }

    private VariableType EmitRound(BuiltinFunctionCallExpression expression)
    {
        EnsureArgumentsCount(expression, 1);
        VariableType argumentType = EmitExpression(expression.Arguments[0]);
        EmitConversion(argumentType, VariableType.Double);
        il.Emit(OpCodes.Ldc_I4, (int)MidpointRounding.AwayFromZero);
        il.Emit(OpCodes.Call, GetMethod(typeof(MathF), nameof(MathF.Round), [typeof(float), typeof(MidpointRounding)]));
        return VariableType.Double;
    }

    private VariableType EmitLength(BuiltinFunctionCallExpression expression)
    {
        EnsureArgumentsCount(expression, 1);
        VariableType argumentType = EmitExpression(expression.Arguments[0]);
        EnsureType(argumentType, VariableType.String, expression.FunctionName);
        il.Emit(OpCodes.Callvirt, GetMethod(typeof(string), "get_Length", Type.EmptyTypes));
        return VariableType.Int;
    }

    private VariableType EmitSubstring(BuiltinFunctionCallExpression expression)
    {
        EnsureArgumentsCount(expression, 3);
        VariableType sourceType = EmitExpression(expression.Arguments[0]);
        EnsureType(sourceType, VariableType.String, expression.FunctionName);
        VariableType startType = EmitExpression(expression.Arguments[1]);
        EmitConversion(startType, VariableType.Int);
        VariableType lengthType = EmitExpression(expression.Arguments[2]);
        EmitConversion(lengthType, VariableType.Int);
        il.Emit(OpCodes.Callvirt, GetMethod(typeof(string), nameof(string.Substring), [typeof(int), typeof(int)]));
        return VariableType.String;
    }

    private VariableType EmitConvertToString(BuiltinFunctionCallExpression expression)
    {
        EnsureArgumentsCount(expression, 1);
        VariableType argumentType = EmitExpression(expression.Arguments[0]);
        EmitToString(argumentType);
        return VariableType.String;
    }

    private VariableType EmitParseConversion(
        BuiltinFunctionCallExpression expression,
        Type targetType,
        string methodName,
        VariableType resultType)
    {
        EnsureArgumentsCount(expression, 1);
        VariableType argumentType = EmitExpression(expression.Arguments[0]);
        EnsureType(argumentType, VariableType.String, expression.FunctionName);
        il.Emit(OpCodes.Call, GetMethod(targetType, methodName, [typeof(string)]));
        return resultType;
    }

    private VariableType EmitToLower(BuiltinFunctionCallExpression expression)
    {
        EnsureArgumentsCount(expression, 1);
        VariableType argumentType = EmitExpression(expression.Arguments[0]);
        EnsureType(argumentType, VariableType.String, expression.FunctionName);
        il.Emit(OpCodes.Callvirt, GetMethod(typeof(string), nameof(string.ToLower), Type.EmptyTypes));
        return VariableType.String;
    }

    private VariableType EmitFloatConstant(float value)
    {
        il.Emit(OpCodes.Ldc_R4, value);
        return VariableType.Double;
    }

    private void EmitInitialValue(VariableType type, Expression? initialValue, bool isConst, string name)
    {
        if (initialValue != null)
        {
            VariableType valueType = EmitExpression(initialValue);
            EmitConversion(valueType, type);
            return;
        }

        if (isConst)
        {
            throw new NotSupportedException($"Constant {name} requires an initial value.");
        }

        switch (type)
        {
            case VariableType.Int:
                il.Emit(OpCodes.Ldc_I4_0);
                break;
            case VariableType.Double:
                il.Emit(OpCodes.Ldc_R4, 0.0f);
                break;
            case VariableType.Boolean:
                il.Emit(OpCodes.Ldc_I4_0);
                break;
            case VariableType.String:
                il.Emit(OpCodes.Ldstr, string.Empty);
                break;
            default:
                throw new NotSupportedException($"Default value for {type} is not supported.");
        }
    }

    private void EmitConsoleRead(VariableType type)
    {
        il.Emit(OpCodes.Call, GetMethod(typeof(Console), nameof(Console.ReadLine), Type.EmptyTypes));
        switch (type)
        {
            case VariableType.Int:
                il.Emit(OpCodes.Call, GetMethod(typeof(int), nameof(int.Parse), [typeof(string)]));
                break;
            case VariableType.Double:
                EmitInvariantCulture();
                il.Emit(OpCodes.Call,
                    GetMethod(typeof(float), nameof(float.Parse), [typeof(string), typeof(IFormatProvider)]));
                break;
            case VariableType.Boolean:
                il.Emit(OpCodes.Call, GetMethod(typeof(bool), nameof(bool.Parse), [typeof(string)]));
                break;
            case VariableType.String:
                break;
            default:
                throw new NotSupportedException($"Input for {type} is not supported.");
        }
    }

    private void EmitConsoleWrite(VariableType type)
    {
        EmitToString(type);
        il.Emit(OpCodes.Call, GetMethod(typeof(Console), nameof(Console.Write), [typeof(string)]));
    }

    private void EmitNumericOperands(
        Expression left,
        VariableType leftType,
        Expression right,
        VariableType rightType,
        VariableType? targetType = null)
    {
        VariableType resultType = targetType ?? (leftType == VariableType.Double || rightType == VariableType.Double
            ? VariableType.Double
            : VariableType.Int);
        EnsureNumeric(leftType, "binary operation");
        EnsureNumeric(rightType, "binary operation");

        EmitExpression(left);
        EmitConversion(leftType, resultType);
        EmitExpression(right);
        EmitConversion(rightType, resultType);
    }

    private void EmitComparison(BinaryOperation operation)
    {
        switch (operation)
        {
            case BinaryOperation.Equal:
                il.Emit(OpCodes.Ceq);
                break;
            case BinaryOperation.NotEqual:
                il.Emit(OpCodes.Ceq);
                EmitLogicalNot();
                break;
            case BinaryOperation.GreaterThan:
                il.Emit(OpCodes.Cgt);
                break;
            case BinaryOperation.LessThan:
                il.Emit(OpCodes.Clt);
                break;
            case BinaryOperation.GreaterThanOrEqual:
                il.Emit(OpCodes.Clt);
                EmitLogicalNot();
                break;
            case BinaryOperation.LessThanOrEqual:
                il.Emit(OpCodes.Cgt);
                EmitLogicalNot();
                break;
            default:
                throw new NotSupportedException($"Comparison {operation} is not supported.");
        }
    }

    private void EmitConversion(VariableType from, VariableType to)
    {
        if (from == to)
        {
            return;
        }

        if (from == VariableType.Int && to == VariableType.Double)
        {
            il.Emit(OpCodes.Conv_R4);
            return;
        }

        if (from == VariableType.Double && to == VariableType.Int)
        {
            il.Emit(OpCodes.Conv_I4);
            return;
        }

        throw new NotSupportedException($"Conversion from {from} to {to} is not supported.");
    }

    private void EmitToString(VariableType type)
    {
        switch (type)
        {
            case VariableType.Int:
                il.Emit(OpCodes.Call, GetMethod(typeof(Convert), nameof(Convert.ToString), [typeof(int)]));
                break;
            case VariableType.Double:
                EmitInvariantCulture();
                il.Emit(OpCodes.Call,
                    GetMethod(typeof(Convert), nameof(Convert.ToString), [typeof(float), typeof(IFormatProvider)]));
                break;
            case VariableType.Boolean:
                il.Emit(OpCodes.Call, GetMethod(typeof(Convert), nameof(Convert.ToString), [typeof(bool)]));
                break;
            case VariableType.String:
                break;
            default:
                throw new NotSupportedException($"Conversion from {type} to string is not supported.");
        }
    }

    private void EmitLogicalNot()
    {
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ceq);
    }

    private void EmitOne(VariableType type)
    {
        if (type == VariableType.Int)
        {
            il.Emit(OpCodes.Ldc_I4_1);
            return;
        }

        if (type == VariableType.Double)
        {
            il.Emit(OpCodes.Ldc_R4, 1.0f);
            return;
        }

        throw new NotSupportedException($"Cannot emit one for {type}.");
    }

    private VariableType InferExpressionType(Expression expression)
    {
        return expression switch
        {
            LiteralExpression literal => InferLiteralType(literal),
            VariableExpression variable => FindVariable(variable.Name).Type,
            UnaryOperationExpression unary => InferUnaryType(unary),
            BinaryOperationExpression binary => InferBinaryType(binary),
            BuiltinFunctionCallExpression builtin => InferBuiltinFunctionType(builtin),
            BuiltinConstantExpression => VariableType.Double,
            FunctionCallExpression => throw new NotSupportedException(
                "User functions are not supported by the MSIL backend yet."),
            _ => throw new NotSupportedException($"Expression {expression.GetType().Name} is not supported."),
        };
    }

    private VariableType InferLiteralType(LiteralExpression literal)
    {
        return literal.Value switch
        {
            int => VariableType.Int,
            float => VariableType.Double,
            bool => VariableType.Boolean,
            string => VariableType.String,
            _ => throw new NotSupportedException($"Literal {literal.Value.GetType().Name} is not supported."),
        };
    }

    private VariableType InferUnaryType(UnaryOperationExpression unary)
    {
        return unary.Operation == UnaryOperation.Not
            ? VariableType.Boolean
            : InferExpressionType(unary.Expression);
    }

    private VariableType InferBinaryType(BinaryOperationExpression binary)
    {
        if (binary.Operation == null || binary.Right == null)
        {
            return InferExpressionType(binary.Left);
        }

        if (binary.Operation is BinaryOperation.And or BinaryOperation.Or || IsComparison(binary.Operation.Value))
        {
            return VariableType.Boolean;
        }

        VariableType leftType = InferExpressionType(binary.Left);
        VariableType rightType = InferExpressionType(binary.Right);
        if (binary.Operation == BinaryOperation.Plus &&
            (leftType == VariableType.String || rightType == VariableType.String))
        {
            return VariableType.String;
        }

        return leftType == VariableType.Double || rightType == VariableType.Double
            ? VariableType.Double
            : VariableType.Int;
    }

    private VariableType InferBuiltinFunctionType(BuiltinFunctionCallExpression expression)
    {
        return expression.FunctionName switch
        {
            "length" or "кБлагодати" => VariableType.Int,
            "substring" or "кСловесам" or "кНевысокому" => VariableType.String,
            _ => VariableType.Double,
        };
    }

    private void BeginScope()
    {
        scopes.Push([]);
        il.BeginScope();
    }

    private void EndScope()
    {
        il.EndScope();
        scopes.Pop();
    }

    private LocalValue FindVariable(string name)
    {
        foreach (Dictionary<string, LocalValue> scope in scopes)
        {
            if (scope.TryGetValue(name, out LocalValue? variable))
            {
                return variable;
            }
        }

        throw new InvalidOperationException($"Variable {name} is not defined.");
    }

    private static void EnsureMutable(string name, LocalValue variable)
    {
        if (variable.IsConst)
        {
            throw new NotSupportedException($"Constant {name} cannot be changed.");
        }
    }

    private static VariableExpression UnwrapVariableExpression(Expression expression)
    {
        return expression switch
        {
            VariableExpression variable => variable,
            UnaryOperationExpression { Expression: VariableExpression variable } => variable,
            _ => throw new NotSupportedException("Increment and decrement can be applied only to variables."),
        };
    }

    private static bool IsComparison(BinaryOperation operation)
    {
        return operation is BinaryOperation.Equal
            or BinaryOperation.NotEqual
            or BinaryOperation.GreaterThan
            or BinaryOperation.GreaterThanOrEqual
            or BinaryOperation.LessThan
            or BinaryOperation.LessThanOrEqual;
    }

    private static void EnsureNumeric(VariableType type, object operation)
    {
        if (type is not (VariableType.Int or VariableType.Double))
        {
            throw new NotSupportedException($"Operation {operation} cannot be applied to {type}.");
        }
    }

    private static void EnsureType(VariableType actual, VariableType expected, object operation)
    {
        if (actual != expected)
        {
            throw new NotSupportedException($"Operation {operation} expected {expected}, got {actual}.");
        }
    }

    private static void EnsureArgumentsCount(BuiltinFunctionCallExpression expression, int expected)
    {
        if (expression.Arguments.Count != expected)
        {
            throw new NotSupportedException(
                $"Builtin function {expression.FunctionName} expected {expected} arguments, got {expression.Arguments.Count}.");
        }
    }

    private static MethodInfo GetMethod(Type type, string methodName, Type[] parameterTypes)
    {
        MethodInfo? method = type.GetMethod(methodName, parameterTypes);
        if (method == null)
        {
            string parameterTypeNames = string.Join(", ", parameterTypes.Select(t => t.Name));
            throw new InvalidOperationException($"Cannot find method {type.Name}.{methodName}({parameterTypeNames}).");
        }

        return method;
    }

    private void EmitInvariantCulture()
    {
        MethodInfo? getter = typeof(CultureInfo).GetProperty(nameof(CultureInfo.InvariantCulture))?.GetMethod;
        if (getter == null)
        {
            throw new InvalidOperationException("Cannot find CultureInfo.InvariantCulture getter.");
        }

        il.Emit(OpCodes.Call, getter);
    }

    private sealed class LocalValue(LocalBuilder local, VariableType type, bool isConst)
    {
        public LocalBuilder Local { get; } = local;

        public VariableType Type { get; } = type;

        public bool IsConst { get; } = isConst;
    }
}