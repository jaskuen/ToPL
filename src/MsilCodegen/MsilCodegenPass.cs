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
    private readonly Stack<LoopLabels> loops = [];
    private readonly Dictionary<string, MethodBuilder> functionBuilders = [];
    private readonly Dictionary<string, FunctionDeclaration> functionDeclarations = [];
    private readonly Dictionary<string, TypeBuilder> structBuilders = [];
    private readonly Dictionary<string, StructDeclaration> structDeclarations = [];
    private readonly Dictionary<string, Dictionary<string, FieldBuilder>> structFields = [];

    private TypeBuilder programTypeBuilder = null!;
    private ILGenerator il = null!;
    private TypeReference currentReturnType = TypeReference.Void;

    public MsilCodegenPass(ModuleBuilder moduleBuilder)
    {
        this.moduleBuilder = moduleBuilder;
    }

    public MethodBuilder GenerateProgramCode(ProgramUnit program)
    {
        programTypeBuilder = moduleBuilder.DefineType(
            "Program",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class);

        foreach (StructDeclaration structure in program.Structs)
        {
            DefineStructShell(structure);
        }

        foreach (StructDeclaration structure in program.Structs)
        {
            DefineStructFields(structure);
        }

        foreach (StructDeclaration structure in program.Structs)
        {
            structBuilders[structure.Name].CreateType();
        }

        foreach (FunctionDeclaration function in program.Functions)
        {
            DefineFunction(function);
        }

        foreach (FunctionDeclaration function in program.Functions)
        {
            GenerateFunction(function);
        }

        MethodBuilder mainMethod = programTypeBuilder.DefineMethod(
            "Main",
            MethodAttributes.Public | MethodAttributes.Static,
            MapClrType(program.MainType),
            Type.EmptyTypes);

        il = mainMethod.GetILGenerator();
        currentReturnType = program.MainType;
        BeginScope();
        EmitScopeBody(program.MainBody);
        EndScope();

        EmitDefaultReturn(program.MainType);
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
        GenerateFunction(declaration);
    }

    public void Visit(VariableDeclaration declaration)
    {
        foreach ((string name, Expression? initialValue) in declaration.NamesToValues)
        {
            Type clrType = MapClrType(declaration.Type);
            LocalBuilder local = il.DeclareLocal(clrType);
            EmitInitialValue(declaration.Type, initialValue, declaration.IsConst, name);
            il.Emit(OpCodes.Stloc, local);
            scopes.Peek().Add(name, new LocalValue(local, declaration.Type, declaration.IsConst));
        }
    }

    public void Visit(StructDeclaration declaration)
    {
        if (!structDeclarations.ContainsKey(declaration.Name))
        {
            DefineStructShell(declaration);
            DefineStructFields(declaration);
        }
    }

    public void Visit(AssignmentStatement statement)
    {
        EmitAssignment(statement.Target, statement.Value, false);
    }

    public void Visit(UnaryOperationExpression expression)
    {
        EmitExpression(expression);
    }

    public bool Visit(ForLoopStatement statement)
    {
        BeginScope();
        EmitStatementNode(statement.Initializer);

        Label conditionLabel = il.DefineLabel();
        Label postLabel = il.DefineLabel();
        Label endLabel = il.DefineLabel();

        il.MarkLabel(conditionLabel);
        if (statement.Condition is not null)
        {
            VariableType conditionType = EmitExpression(statement.Condition);
            EnsureType(conditionType, VariableType.Boolean, "for condition");
            il.Emit(OpCodes.Brfalse, endLabel);
        }

        loops.Push(new LoopLabels(endLabel, postLabel));
        EmitNestedScope(statement.Body);
        loops.Pop();

        il.MarkLabel(postLabel);
        EmitStatementNode(statement.Post);
        il.Emit(OpCodes.Br, conditionLabel);
        il.MarkLabel(endLabel);

        EndScope();
        return false;
    }

    public void Visit(FunctionCallExpression expression)
    {
        EmitExpression(expression);
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
        Label conditionLabel = il.DefineLabel();
        Label endLabel = il.DefineLabel();

        il.MarkLabel(conditionLabel);
        VariableType conditionType = EmitExpression(statement.Condition);
        EnsureType(conditionType, VariableType.Boolean, "while condition");
        il.Emit(OpCodes.Brfalse, endLabel);

        loops.Push(new LoopLabels(endLabel, conditionLabel));
        EmitNestedScope(statement.Body);
        loops.Pop();

        il.Emit(OpCodes.Br, conditionLabel);
        il.MarkLabel(endLabel);
        return false;
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
        Label elseLabel = il.DefineLabel();
        Label endLabel = il.DefineLabel();

        VariableType conditionType = EmitExpression(statement.Condition);
        EnsureType(conditionType, VariableType.Boolean, "if condition");
        il.Emit(OpCodes.Brfalse, elseLabel);

        EmitNestedScope(statement.ThenBranch);
        il.Emit(OpCodes.Br, endLabel);

        il.MarkLabel(elseLabel);
        if (statement.ElseBranch is not null)
        {
            EmitNestedScope(statement.ElseBranch);
        }

        il.MarkLabel(endLabel);
        return false;
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
        if (loops.Count == 0)
        {
            throw new NotSupportedException("Break statements are allowed only inside loops.");
        }

        il.Emit(OpCodes.Br, loops.Peek().BreakLabel);
    }

    public void Visit(ContinueStatement statement)
    {
        if (loops.Count == 0)
        {
            throw new NotSupportedException("Continue statements are allowed only inside loops.");
        }

        il.Emit(OpCodes.Br, loops.Peek().ContinueLabel);
    }

    public void Visit(ReturnStatement statement)
    {
        if (currentReturnType == VariableType.Void)
        {
            if (statement.Value is not null)
            {
                VariableType valueType = EmitExpression(statement.Value);
                if (valueType != VariableType.Void)
                {
                    il.Emit(OpCodes.Pop);
                }
            }

            il.Emit(OpCodes.Ret);
            return;
        }

        if (statement.Value is null)
        {
            throw new NotSupportedException($"Function returning {currentReturnType} requires a return value.");
        }

        VariableType returnType = EmitExpression(statement.Value);
        EmitConversion(returnType, currentReturnType);
        il.Emit(OpCodes.Ret);
    }

    public void Visit(EmptyStatement declaration)
    {
    }

    public void Visit(VariableExpression declaration)
    {
        EmitExpression(declaration);
    }

    public void Visit(ArrayLiteralExpression expression)
    {
        EmitExpression(expression);
    }

    public void Visit(StructLiteralExpression expression)
    {
        EmitExpression(expression);
    }

    public void Visit(ArrayAccessExpression expression)
    {
        EmitExpression(expression);
    }

    public void Visit(FieldAccessExpression expression)
    {
        EmitExpression(expression);
    }

    public void Visit(AssignmentExpression expression)
    {
        EmitExpression(expression);
    }

    private void EmitScopeBody(ScopeStatement statement)
    {
        foreach (AstNode node in statement.Statements)
        {
            EmitStatementNode(node);
        }
    }

    private void EmitNestedScope(ScopeStatement statement)
    {
        BeginScope();
        EmitScopeBody(statement);
        EndScope();
    }

    private void EmitStatementNode(AstNode? node)
    {
        if (node is null)
        {
            return;
        }

        if (node is Expression expression)
        {
            VariableType expressionType = EmitExpression(expression);
            if (expressionType != VariableType.Void)
            {
                il.Emit(OpCodes.Pop);
            }

            return;
        }

        node.Accept(this);
    }

    private TypeReference EmitExpression(Expression expression)
    {
        return expression switch
        {
            LiteralExpression literal => EmitLiteral(literal),
            VariableExpression variable => EmitVariable(variable),
            UnaryOperationExpression unary => EmitUnaryOperation(unary),
            BinaryOperationExpression binary => EmitBinaryOperation(binary),
            BuiltinFunctionCallExpression builtin => EmitBuiltinFunctionCall(builtin),
            FunctionCallExpression function => EmitFunctionCall(function),
            AssignmentExpression assignment => EmitAssignment(assignment.Target, assignment.Value, true),
            ArrayLiteralExpression arrayLiteral => EmitArrayLiteral(arrayLiteral),
            StructLiteralExpression structLiteral => EmitStructLiteral(structLiteral),
            ArrayAccessExpression arrayAccess => EmitArrayAccess(arrayAccess),
            FieldAccessExpression fieldAccess => EmitFieldAccess(fieldAccess),
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
                return VariableType.Float;
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

    private TypeReference EmitArrayLiteral(ArrayLiteralExpression expression, TypeReference? expectedElementType = null)
    {
        if (expression.Elements.Count == 0 && expectedElementType is null)
        {
            throw new NotSupportedException("Empty array literals require target type inference and are not supported by MSIL backend.");
        }

        TypeReference elementType = expectedElementType ?? InferExpressionType(expression.Elements[0]);
        il.Emit(OpCodes.Ldc_I4, expression.Elements.Count);
        il.Emit(OpCodes.Newarr, MapClrType(elementType));
        for (int i = 0; i < expression.Elements.Count; i++)
        {
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4, i);
            VariableType valueType = EmitExpression(expression.Elements[i]);
            EmitConversion(valueType, elementType);
            EmitStoreArrayElement(elementType);
        }

        return TypeReference.ArrayOf(elementType);
    }

    private TypeReference EmitStructLiteral(StructLiteralExpression expression)
    {
        if (!structBuilders.TryGetValue(expression.TypeName, out TypeBuilder? builder) ||
            !structDeclarations.TryGetValue(expression.TypeName, out StructDeclaration? declaration))
        {
            throw new NotSupportedException($"Struct {expression.TypeName} is not defined.");
        }

        if (declaration.Fields.Count != expression.Values.Count)
        {
            throw new NotSupportedException(
                $"Struct {expression.TypeName} expected {declaration.Fields.Count} values, got {expression.Values.Count}.");
        }

        ConstructorInfo constructor = builder.GetConstructor(Type.EmptyTypes)
                                      ?? throw new InvalidOperationException($"Struct {expression.TypeName} has no default constructor.");
        il.Emit(OpCodes.Newobj, constructor);
        for (int i = 0; i < declaration.Fields.Count; i++)
        {
            StructFieldDeclaration field = declaration.Fields[i];
            il.Emit(OpCodes.Dup);

            VariableType valueType = EmitExpression(expression.Values[i]);
            EmitConversion(valueType, field.Type);

            il.Emit(OpCodes.Stfld, structFields[expression.TypeName][field.Name]);
        }

        return TypeReference.Struct(expression.TypeName);
    }

    private TypeReference EmitArrayAccess(ArrayAccessExpression expression)
    {
        TypeReference arrayType = InferExpressionType(expression.Target);
        if (!arrayType.IsArray)
        {
            throw new NotSupportedException($"Indexing requires array, got {arrayType}.");
        }

        TypeReference arrayClrType = arrayType;
        LocalBuilder arrayLocal = il.DeclareLocal(MapClrType(arrayClrType));
        LocalBuilder indexLocal = il.DeclareLocal(typeof(int));

        EmitExpression(expression.Target);
        il.Emit(OpCodes.Stloc, arrayLocal);
        TypeReference indexType = EmitExpression(expression.Index);

        if (indexType.Kind is not VariableType.Int)
        {
            throw new NotSupportedException($"Array access requires int index type, got {indexType.Kind}.");
        }

        il.Emit(OpCodes.Stloc, indexLocal);
        EmitArrayBoundsCheck(arrayLocal, indexLocal);

        il.Emit(OpCodes.Ldloc, arrayLocal);
        il.Emit(OpCodes.Ldloc, indexLocal);
        EmitLoadArrayElement(arrayType.ElementType);
        return arrayType.ElementType;
    }

    private TypeReference EmitFieldAccess(FieldAccessExpression expression)
    {
        TypeReference targetType = InferExpressionType(expression.Target);
        if (targetType.Kind != VariableType.Struct || targetType.StructName is null)
        {
            throw new NotSupportedException($"Field access requires struct, got {targetType}.");
        }

        StructFieldDeclaration field = structDeclarations[targetType.StructName].Fields
            .First(f => f.Name == expression.FieldName);
        EmitExpression(expression.Target);
        il.Emit(OpCodes.Ldfld, structFields[targetType.StructName][expression.FieldName]);
        return field.Type;
    }

    private TypeReference EmitVariable(VariableExpression expression)
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
        return expression.DoPushToStack ? variable.Type : VariableType.Void;
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

        VariableType resultType = leftType == VariableType.Float || rightType == VariableType.Float
            ? VariableType.Float
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

        // short-circuit evaluation
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
            il.Emit(
                OpCodes.Call,
                GetMethod(typeof(string ), nameof(string.Concat ), [typeof(string ), typeof(string )]));
            return VariableType.String;
        }

        if (expression.Operation is BinaryOperation.Equal or BinaryOperation.NotEqual)
        {
            EnsureType(leftType, VariableType.String, expression.Operation.Value);
            EnsureType(rightType, VariableType.String, expression.Operation.Value);
            EmitExpression(expression.Left);
            EmitExpression(expression.Right!);
            il.Emit(
                OpCodes.Call,
                GetMethod(typeof(string ), nameof(string.Equals ), [typeof(string ), typeof(string )]));
            if (expression.Operation == BinaryOperation.NotEqual)
            {
                EmitLogicalNot();
            }

            return VariableType.Boolean;
        }

        if (IsComparison(expression.Operation!.Value))
        {
            EnsureType(leftType, VariableType.String, expression.Operation.Value);
            EnsureType(rightType, VariableType.String, expression.Operation.Value);
            EmitExpression(expression.Left);
            EmitExpression(expression.Right!);
            il.Emit(
                OpCodes.Call,
                GetMethod(typeof(string ), nameof(string.CompareOrdinal ), [typeof(string ), typeof(string )]));
            il.Emit(OpCodes.Ldc_I4_0);
            EmitComparison(expression.Operation.Value);
            return VariableType.Boolean;
        }

        throw new NotSupportedException($"String operation {expression.Operation} is not supported.");
    }

    private VariableType EmitBuiltinFunctionCall(BuiltinFunctionCallExpression expression)
    {
        return expression.FunctionName switch
        {
            "floor" => EmitUnaryMathF(expression, nameof(MathF.Floor )),
            "ceil" => EmitUnaryMathF(expression, nameof(MathF.Ceiling )),
            "round" => EmitRound(expression),
            "sin" => EmitUnaryMathF(expression, nameof(MathF.Sin )),
            "cos" => EmitUnaryMathF(expression, nameof(MathF.Cos )),
            "tan" => EmitUnaryMathF(expression, nameof(MathF.Tan )),
            "length" => EmitLength(expression),
            "substring" => EmitSubstring(expression),
            "min" => EmitBinaryMathF(expression, nameof(MathF.Min )),
            "max" => EmitBinaryMathF(expression, nameof(MathF.Max )),
            "abs" => EmitUnaryMathF(expression, nameof(MathF.Abs )),
            _ => throw new NotSupportedException($"Builtin function {expression.FunctionName} is not supported."),
        };
    }

    private VariableType EmitFunctionCall(FunctionCallExpression expression)
    {
        if (!functionBuilders.TryGetValue(expression.Name, out MethodBuilder? method) ||
            !functionDeclarations.TryGetValue(expression.Name, out FunctionDeclaration? declaration))
        {
            throw new NotSupportedException($"User function {expression.Name} is not defined.");
        }

        if (expression.Arguments.Count != declaration.Parameters.Count)
        {
            throw new NotSupportedException(
                $"Function {expression.Name} expected {declaration.Parameters.Count} arguments, got {expression.Arguments.Count}.");
        }

        int index = 0;
        foreach (VariableType parameterType in declaration.Parameters.Values)
        {
            VariableType argumentType = EmitExpression(expression.Arguments[index]);
            EmitConversion(argumentType, parameterType);
            index++;
        }

        il.Emit(OpCodes.Call, method);
        return declaration.Type;
    }

    private VariableType EmitUnaryMathF(BuiltinFunctionCallExpression expression, string methodName)
    {
        EnsureArgumentsCount(expression, 1);
        VariableType argumentType = EmitExpression(expression.Arguments[0]);
        EmitConversion(argumentType, VariableType.Float);
        il.Emit(OpCodes.Call, GetMethod(typeof(MathF ), methodName, [typeof(float )]));
        return VariableType.Float;
    }

    private VariableType EmitBinaryMathF(BuiltinFunctionCallExpression expression, string methodName)
    {
        EnsureArgumentsCount(expression, 2);
        VariableType leftType = EmitExpression(expression.Arguments[0]);
        EmitConversion(leftType, VariableType.Float);
        VariableType rightType = EmitExpression(expression.Arguments[1]);
        EmitConversion(rightType, VariableType.Float);
        il.Emit(OpCodes.Call, GetMethod(typeof(MathF ), methodName, [typeof(float ), typeof(float )]));
        return VariableType.Float;
    }

    private VariableType EmitRound(BuiltinFunctionCallExpression expression)
    {
        EnsureArgumentsCount(expression, 1);
        VariableType argumentType = EmitExpression(expression.Arguments[0]);
        EmitConversion(argumentType, VariableType.Float);
        il.Emit(OpCodes.Ldc_I4, (int)MidpointRounding.AwayFromZero);
        il.Emit(
            OpCodes.Call,
            GetMethod(typeof(MathF ), nameof(MathF.Round ), [typeof(float ), typeof(MidpointRounding )]));
        return VariableType.Float;
    }

    private VariableType EmitLength(BuiltinFunctionCallExpression expression)
    {
        EnsureArgumentsCount(expression, 1);
        VariableType argumentType = EmitExpression(expression.Arguments[0]);
        EnsureType(argumentType, VariableType.String, expression.FunctionName);
        il.Emit(OpCodes.Callvirt, GetMethod(typeof(string ), "get_Length", Type.EmptyTypes));
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
        il.Emit(
            OpCodes.Callvirt,
            GetMethod(typeof(string ), nameof(string.Substring ), [typeof(int ), typeof(int )]));
        return VariableType.String;
    }

    private void EmitInitialValue(TypeReference type, Expression? initialValue, bool isConst, string name)
    {
        if (initialValue != null)
        {
            Expression unwrappedInitialValue = UnwrapTransparentUnary(initialValue);
            TypeReference valueType = unwrappedInitialValue is ArrayLiteralExpression arrayLiteral && type.IsArray
                ? EmitArrayLiteral(arrayLiteral, type.ElementType)
                : EmitExpression(initialValue);
            EmitConversion(valueType, type);
            return;
        }

        if (isConst)
        {
            throw new NotSupportedException($"Constant {name} requires an initial value.");
        }

        if (type.IsArray)
        {
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Newarr, MapClrType(type.ElementType));
            return;
        }

        switch (type.Kind)
        {
            case VariableType.Int:
                il.Emit(OpCodes.Ldc_I4_0);
                break;
            case VariableType.Float:
                il.Emit(OpCodes.Ldc_R4, 0.0f);
                break;
            case VariableType.Boolean:
                il.Emit(OpCodes.Ldc_I4_0);
                break;
            case VariableType.String:
                il.Emit(OpCodes.Ldstr, string.Empty);
                break;
            case VariableType.Struct:
                ConstructorInfo constructor = MapClrType(type).GetConstructor(Type.EmptyTypes)
                                              ?? throw new InvalidOperationException($"Struct {type} has no default constructor.");
                il.Emit(OpCodes.Newobj, constructor);
                break;
            default:
                throw new NotSupportedException($"Default value for {type} is not supported.");
        }
    }

    private void EmitConsoleRead(VariableType type)
    {
        il.Emit(OpCodes.Call, GetMethod(typeof(Console ), nameof(Console.ReadLine ), Type.EmptyTypes));
        switch (type)
        {
            case VariableType.Int:
                il.Emit(OpCodes.Call, GetMethod(typeof(int ), nameof(int.Parse ), [typeof(string )]));
                break;
            case VariableType.Float:
                EmitInvariantCulture();
                il.Emit(
                    OpCodes.Call,
                    GetMethod(typeof(float ), nameof(float.Parse ), [typeof(string ), typeof(IFormatProvider )]));
                break;
            case VariableType.Boolean:
                il.Emit(OpCodes.Call, GetMethod(typeof(bool ), nameof(bool.Parse ), [typeof(string )]));
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
        il.Emit(OpCodes.Call, GetMethod(typeof(Console ), nameof(Console.Write ), [typeof(string )]));
    }

    private void EmitNumericOperands(
        Expression left,
        VariableType leftType,
        Expression right,
        VariableType rightType,
        VariableType? targetType = null)
    {
        VariableType resultType = targetType ?? (leftType == VariableType.Float || rightType == VariableType.Float
            ? VariableType.Float
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

        if (from == VariableType.Int && to == VariableType.Float)
        {
            il.Emit(OpCodes.Conv_R4);
            return;
        }

        if (from == VariableType.Float && to == VariableType.Int)
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
                il.Emit(OpCodes.Call, GetMethod(typeof(Convert ), nameof(Convert.ToString ), [typeof(int )]));
                break;
            case VariableType.Float:
                EmitInvariantCulture();
                il.Emit(
                    OpCodes.Call,
                    GetMethod(
                        typeof(Convert ),
                        nameof(Convert.ToString ),
                        [typeof(float ), typeof(IFormatProvider )]));
                break;
            case VariableType.Boolean:
                il.Emit(OpCodes.Call, GetMethod(typeof(Convert), nameof(Convert.ToString), [typeof(bool )]));
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

        if (type == VariableType.Float)
        {
            il.Emit(OpCodes.Ldc_R4, 1.0f);
            return;
        }

        throw new NotSupportedException($"Cannot emit one for {type}.");
    }

    private void EmitArrayBoundsCheck(LocalBuilder arrayLocal, LocalBuilder indexLocal)
    {
        Label throwLabel = il.DefineLabel();
        Label okLabel = il.DefineLabel();

        il.Emit(OpCodes.Ldloc, indexLocal);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Blt, throwLabel);

        il.Emit(OpCodes.Ldloc, indexLocal);
        il.Emit(OpCodes.Ldloc, arrayLocal);
        il.Emit(OpCodes.Ldlen);
        il.Emit(OpCodes.Conv_I4);
        il.Emit(OpCodes.Blt, okLabel);

        il.MarkLabel(throwLabel);
        il.Emit(OpCodes.Ldstr, "Array index is out of bounds.");
        ConstructorInfo exceptionConstructor = typeof(IndexOutOfRangeException).GetConstructor([typeof(string)])
                                               ?? throw new IndexOutOfRangeException(
                                                   "Cannot find IndexOutOfRangeException(string).");
        il.Emit(OpCodes.Newobj, exceptionConstructor);
        il.Emit(OpCodes.Throw);

        il.MarkLabel(okLabel);
    }

    private void EmitStoreArrayElement(TypeReference elementType)
    {
        if (elementType.IsArray || elementType.Kind == VariableType.Struct || elementType.Kind == VariableType.String)
        {
            il.Emit(OpCodes.Stelem_Ref);
            return;
        }

        switch (elementType.Kind)
        {
            case VariableType.Int:
                il.Emit(OpCodes.Stelem_I4);
                break;
            case VariableType.Float:
                il.Emit(OpCodes.Stelem_R4);
                break;
            case VariableType.Boolean:
                il.Emit(OpCodes.Stelem_I1);
                break;
            default:
                throw new NotSupportedException($"Array element type {elementType} is not supported.");
        }
    }

    private void EmitLoadArrayElement(TypeReference elementType)
    {
        if (elementType.IsArray || elementType.Kind == VariableType.Struct || elementType.Kind == VariableType.String)
        {
            il.Emit(OpCodes.Ldelem_Ref);
            return;
        }

        switch (elementType.Kind)
        {
            case VariableType.Int:
                il.Emit(OpCodes.Ldelem_I4);
                break;
            case VariableType.Float:
                il.Emit(OpCodes.Ldelem_R4);
                break;
            case VariableType.Boolean:
                il.Emit(OpCodes.Ldelem_U1);
                break;
            default:
                throw new NotSupportedException($"Array element type {elementType} is not supported.");
        }
    }

    private TypeReference InferExpressionType(Expression expression)
    {
        return expression switch
        {
            LiteralExpression literal => InferLiteralType(literal),
            VariableExpression variable => FindVariable(variable.Name).Type,
            UnaryOperationExpression unary => InferUnaryType(unary),
            BinaryOperationExpression binary => InferBinaryType(binary),
            BuiltinFunctionCallExpression builtin => InferBuiltinFunctionType(builtin),
            BuiltinConstantExpression => VariableType.Float,
            FunctionCallExpression function => InferFunctionType(function),
            AssignmentExpression assignment => InferExpressionType(assignment.Target),
            ArrayLiteralExpression arrayLiteral => InferArrayLiteralType(arrayLiteral),
            StructLiteralExpression structLiteral => TypeReference.Struct(structLiteral.TypeName),
            ArrayAccessExpression arrayAccess => InferExpressionType(arrayAccess.Target).ElementType,
            FieldAccessExpression fieldAccess => InferFieldAccessType(fieldAccess),
            _ => throw new NotSupportedException($"Expression {expression.GetType().Name} is not supported."),
        };
    }

    private TypeReference InferLiteralType(LiteralExpression literal)
    {
        return literal.Value switch
        {
            int => VariableType.Int,
            float => VariableType.Float,
            bool => VariableType.Boolean,
            string => VariableType.String,
            _ => throw new NotSupportedException($"Literal {literal.Value.GetType().Name} is not supported."),
        };
    }

    private TypeReference InferUnaryType(UnaryOperationExpression unary)
    {
        return unary.Operation == UnaryOperation.Not
            ? VariableType.Boolean
            : InferExpressionType(unary.Expression);
    }

    private TypeReference InferBinaryType(BinaryOperationExpression binary)
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

        return leftType == VariableType.Float || rightType == VariableType.Float
            ? VariableType.Float
            : VariableType.Int;
    }

    private TypeReference InferBuiltinFunctionType(BuiltinFunctionCallExpression expression)
    {
        return expression.FunctionName switch
        {
            "length" => VariableType.Int,
            "substring" => VariableType.String,
            _ => VariableType.Float,
        };
    }

    private TypeReference InferFunctionType(FunctionCallExpression expression)
    {
        if (functionDeclarations.TryGetValue(expression.Name, out FunctionDeclaration? declaration))
        {
            return declaration.ReturnType;
        }

        throw new NotSupportedException($"User function {expression.Name} is not defined.");
    }

    private TypeReference InferArrayLiteralType(ArrayLiteralExpression expression)
    {
        if (expression.Elements.Count == 0)
        {
            throw new NotSupportedException("Empty array literal type cannot be inferred.");
        }

        return TypeReference.ArrayOf(InferExpressionType(expression.Elements[0]));
    }

    private TypeReference InferFieldAccessType(FieldAccessExpression expression)
    {
        TypeReference targetType = InferExpressionType(expression.Target);
        if (targetType.Kind != VariableType.Struct || targetType.StructName is null)
        {
            throw new NotSupportedException($"Field access requires struct, got {targetType}.");
        }

        return structDeclarations[targetType.StructName].Fields.First(f => f.Name == expression.FieldName).Type;
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
        if (type is not(VariableType.Int or VariableType.Float))
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
        MethodInfo? getter = typeof(CultureInfo ).GetProperty(nameof(CultureInfo.InvariantCulture ))?.GetMethod;
        if (getter == null)
        {
            throw new InvalidOperationException("Cannot find CultureInfo.InvariantCulture getter.");
        }

        il.Emit(OpCodes.Call, getter);
    }

    private void EmitDefaultReturn(VariableType returnType)
    {
        if (returnType != VariableType.Void)
        {
            EmitDefaultValue(returnType);
        }

        il.Emit(OpCodes.Ret);
    }

    private void EmitDefaultStructReturn(string structName)
    {
        EmitDefaultStructLiteral(structName);

        il.Emit(OpCodes.Ret);
    }

    private void EmitDefaultValue(VariableType type)
    {
        switch (type)
        {
            case VariableType.Int:
            case VariableType.Boolean:
                il.Emit(OpCodes.Ldc_I4_0);
                break;
            case VariableType.Float:
                il.Emit(OpCodes.Ldc_R4, 0.0f);
                break;
            case VariableType.String:
                il.Emit(OpCodes.Ldstr, string.Empty);
                break;
            case VariableType.Void:
                break;
            default:
                throw new NotSupportedException($"Default value for {type} is not supported.");
        }
    }

    private void EmitDefaultStructLiteral(string structName)
    {
        if (!structBuilders.TryGetValue(structName, out TypeBuilder? builder) ||
            !structDeclarations.TryGetValue(structName, out StructDeclaration? declaration))
        {
            throw new NotSupportedException($"Struct {structName} is not defined.");
        }

        ConstructorInfo constructor = builder.GetConstructor(Type.EmptyTypes)
                                      ?? throw new InvalidOperationException($"Struct {structName} has no default constructor.");
        il.Emit(OpCodes.Newobj, constructor);
    }

    private Type MapClrType(TypeReference type)
    {
        if (type.IsArray)
        {
            return MapClrType(type.ElementType).MakeArrayType();
        }

        if (type.Kind == VariableType.Struct)
        {
            if (type.StructName is null || !structBuilders.TryGetValue(type.StructName, out TypeBuilder? builder))
            {
                throw new NotSupportedException($"Struct type {type.StructName} is not defined.");
            }

            return builder;
        }

        return typeMapper.MapType(type.Kind);
    }

    private void DefineStructShell(StructDeclaration structure)
    {
        if (structBuilders.ContainsKey(structure.Name))
        {
            return;
        }

        TypeBuilder builder = moduleBuilder.DefineType(
            structure.Name,
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);
        builder.DefineDefaultConstructor(MethodAttributes.Public);
        structBuilders.Add(structure.Name, builder);
        structDeclarations.Add(structure.Name, structure);
        structFields.Add(structure.Name, []);
    }

    private void DefineStructFields(StructDeclaration structure)
    {
        TypeBuilder builder = structBuilders[structure.Name];
        Dictionary<string, FieldBuilder> fields = structFields[structure.Name];
        foreach (StructFieldDeclaration field in structure.Fields)
        {
            if (fields.ContainsKey(field.Name))
            {
                continue;
            }

            fields.Add(field.Name, builder.DefineField(field.Name, MapClrType(field.Type), FieldAttributes.Public));
        }
    }

    private TypeReference EmitAssignment(Expression target, Expression value, bool pushAssignedValue)
    {
        target = UnwrapTransparentUnary(target);
        switch (target)
        {
            case VariableExpression variableExpression:
                LocalValue variable = FindVariable(variableExpression.Name);
                EnsureMutable(variableExpression.Name, variable);
                TypeReference valueType = EmitExpression(value);
                EmitConversion(valueType, variable.Type);
                if (pushAssignedValue)
                {
                    il.Emit(OpCodes.Dup);
                }

                il.Emit(OpCodes.Stloc, variable.Local);
                return pushAssignedValue ? variable.Type : VariableType.Void;
            case ArrayAccessExpression arrayAccess:
                TypeReference arrayType = InferExpressionType(arrayAccess.Target);
                if (!arrayType.IsArray)
                {
                    throw new NotSupportedException($"Index assignment requires array, got {arrayType}.");
                }

                LocalBuilder arrayLocal = il.DeclareLocal(MapClrType(arrayType));
                LocalBuilder indexLocal = il.DeclareLocal(typeof(int));

                EmitExpression(arrayAccess.Target);
                il.Emit(OpCodes.Stloc, arrayLocal);
                TypeReference indexType = EmitExpression(arrayAccess.Index);
                EmitConversion(indexType, VariableType.Int);
                il.Emit(OpCodes.Stloc, indexLocal);
                EmitArrayBoundsCheck(arrayLocal, indexLocal);

                il.Emit(OpCodes.Ldloc, arrayLocal);
                il.Emit(OpCodes.Ldloc, indexLocal);
                TypeReference arrayValueType = EmitExpression(value);
                EmitConversion(arrayValueType, arrayType.ElementType);
                LocalBuilder? arrayTemp = null;
                if (pushAssignedValue)
                {
                    arrayTemp = il.DeclareLocal(MapClrType(arrayType.ElementType));
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Stloc, arrayTemp);
                }

                EmitStoreArrayElement(arrayType.ElementType);
                if (arrayTemp is not null)
                {
                    il.Emit(OpCodes.Ldloc, arrayTemp);
                }

                return pushAssignedValue ? arrayType.ElementType : VariableType.Void;
            case FieldAccessExpression fieldAccess:
                TypeReference targetType = InferExpressionType(fieldAccess.Target);
                if (targetType.Kind != VariableType.Struct || targetType.StructName is null)
                {
                    throw new NotSupportedException($"Field assignment requires struct, got {targetType}.");
                }

                StructFieldDeclaration field = structDeclarations[targetType.StructName].Fields
                    .First(f => f.Name == fieldAccess.FieldName);
                EmitExpression(fieldAccess.Target);
                TypeReference fieldValueType = EmitExpression(value);
                EmitConversion(fieldValueType, field.Type);
                LocalBuilder? fieldTemp = null;
                if (pushAssignedValue)
                {
                    fieldTemp = il.DeclareLocal(MapClrType(field.Type));
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Stloc, fieldTemp);
                }

                il.Emit(OpCodes.Stfld, structFields[targetType.StructName][fieldAccess.FieldName]);
                if (fieldTemp is not null)
                {
                    il.Emit(OpCodes.Ldloc, fieldTemp);
                }

                return pushAssignedValue ? field.Type : VariableType.Void;
            default:
                throw new NotSupportedException($"Assignment to {target.GetType().Name} is not supported by MSIL backend yet.");
        }
    }

    private static Expression UnwrapTransparentUnary(Expression expression)
    {
        return expression is UnaryOperationExpression { Operation: null } unary ? unary.Expression : expression;
    }

    private void DefineFunction(FunctionDeclaration function)
    {
        Type[] parameterTypes = function.Parameters.Values.Select(MapClrType).ToArray();
        MethodBuilder method = programTypeBuilder.DefineMethod(
            function.Name,
            MethodAttributes.Private | MethodAttributes.Static,
            MapClrType(function.ReturnType),
            parameterTypes);

        functionBuilders.Add(function.Name, method);
        functionDeclarations.Add(function.Name, function);
    }

    private void GenerateFunction(FunctionDeclaration function)
    {
        if (!functionBuilders.TryGetValue(function.Name, out MethodBuilder? method))
        {
            DefineFunction(function);
            method = functionBuilders[function.Name];
        }

        // Нужно для переключения контекста с Main функции
        ILGenerator previousIl = il;
        TypeReference previousReturnType = currentReturnType;

        il = method.GetILGenerator();
        currentReturnType = function.ReturnType;
        BeginScope();

        int argumentIndex = 0;
        foreach ((string name, TypeReference type) in function.Parameters)
        {
            LocalBuilder local = il.DeclareLocal(MapClrType(type));
            il.Emit(OpCodes.Ldarg, argumentIndex);
            il.Emit(OpCodes.Stloc, local);
            scopes.Peek().Add(name, new LocalValue(local, type, false));
            argumentIndex++;
        }

        if (function.Body is ScopeStatement scope)
        {
            EmitScopeBody(scope);
        }
        else
        {
            EmitStatementNode(function.Body);
        }

        EndScope();

        if (function.ReturnType.StructName is not null)
        {
            EmitDefaultStructReturn(function.ReturnType.StructName);
        }
        else
        {
            EmitDefaultReturn(function.ReturnType);
        }

        il = previousIl;
        currentReturnType = previousReturnType;
    }

    private sealed class LocalValue(LocalBuilder local, TypeReference type, bool isConst)
    {
        public LocalBuilder Local { get; } = local;

        public TypeReference Type { get; } = type;

        public bool IsConst { get; } = isConst;
    }

    private sealed class LoopLabels(Label breakLabel, Label continueLabel)
    {
        public Label BreakLabel { get; } = breakLabel;

        public Label ContinueLabel { get; } = continueLabel;
    }
}
