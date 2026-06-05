using Ast;
using Ast.Declarations;
using Ast.Expressions;
using Ast.Statements;

using Execution;

using Lexer;

using Runtime;

namespace Parser;

/// <summary>
/// Выполняет синтаксический разбор вывода результата операций
/// над числовыми / логическими значениями или самих значений.
/// Грамматика описана в файле `docs/specification/expressions-grammar.md`.
/// </summary>
public class Parser
{
    private readonly IEnvironment environment;
    private readonly TokenStream tokens;
    private readonly AstEvaluator astEvaluator;

    public Parser(Context context, IEnvironment environment, string code)
    {
        this.environment = environment;
        astEvaluator = new AstEvaluator(context, this.environment);
        tokens = new TokenStream(code);
    }

    public ProgramUnit ParseProgramAst()
    {
        List<StructDeclaration> structs = [];
        List<FunctionDeclaration> functions = [];

        while (tokens.Peek().Type != TokenType.End && tokens.Peek(1).Type != TokenType.Main)
        {
            if (tokens.Peek().Type == TokenType.Struct)
            {
                structs.Add(ParseStructDeclaration());
                continue;
            }

            functions.Add(ParseFunctionDeclaration());
        }

        TypeReference mainType = ParseType();
        Match(TokenType.Main);
        Match(TokenType.OpenParenthesis);
        Match(TokenType.CloseParenthesis);
        ScopeStatement scope = ParseScope();

        Match(TokenType.End);
        return new ProgramUnit(structs, functions, mainType, scope);
    }

    public void ParseProgram()
    {
        ProgramUnit program = ParseProgramAst();

        foreach (FunctionDeclaration function in program.Functions)
        {
            function.Accept(astEvaluator);
        }

        foreach (StructDeclaration structure in program.Structs)
        {
            structure.Accept(astEvaluator);
        }

        RuntimeValue result = astEvaluator.Evaluate(program.MainBody, program.MainType.Kind == VariableType.Void);
        environment.PrintValue($"{result}");
    }

    private FunctionDeclaration ParseFunctionDeclaration()
    {
        TypeReference functionType = ParseType();
        Token nameToken = tokens.Peek();
        Match(TokenType.Identifier);
        string name = nameToken.Value!.ToString();
        Dictionary<string, TypeReference> parameters = ParseFunctionParameters();
        Statement scope = ParseScope();

        return new FunctionDeclaration(functionType, name, parameters, scope);
    }

    private Dictionary<string, TypeReference> ParseFunctionParameters()
    {
        Dictionary<string, TypeReference> parameters = [];
        bool isFirst = true;
        Match(TokenType.OpenParenthesis);
        while (tokens.Peek().Type != TokenType.CloseParenthesis)
        {
            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                Match(TokenType.Comma);
            }

            TypeReference variableType = ParseType();
            string name = tokens.Peek().Value!.ToString();
            tokens.Advance();

            parameters.Add(name, variableType);
        }

        tokens.Advance();
        return parameters;
    }

    private StructDeclaration ParseStructDeclaration()
    {
        Match(TokenType.Struct);
        string name = tokens.Peek().Value!.ToString();
        Match(TokenType.Identifier);
        Match(TokenType.OpenBrace);

        List<StructFieldDeclaration> fields = [];
        while (tokens.Peek().Type != TokenType.CloseBrace)
        {
            TypeReference fieldType = ParseType();
            string fieldName = tokens.Peek().Value!.ToString();
            Match(TokenType.Identifier);
            Match(TokenType.Semicolon);
            fields.Add(new StructFieldDeclaration(fieldType, fieldName));
        }

        Match(TokenType.CloseBrace);
        if (tokens.Peek().Type == TokenType.Semicolon)
        {
            tokens.Advance();
        }

        return new StructDeclaration(name, fields);
    }

    /// <summary>
    /// Разбирает инструкцию области видимости
    /// Поддерживает правило:
    /// scope = "воистину", {statement}, "аминь" ;
    /// </summary>
    private ScopeStatement ParseScope()
    {
        Match(TokenType.OpenBrace);
        List<AstNode> statements = [];
        do
        {
            AstNode statement = ParseStatement();

            // Проверяем, есть ли еще инструкции после точки с запятой
            if (tokens.Peek().Type == TokenType.Semicolon)
            {
                Match(TokenType.Semicolon);
            }

            statements.Add(statement);
        }
        while (tokens.Peek().Type != TokenType.CloseBrace);

        Match(TokenType.CloseBrace);

        return new ScopeStatement(statements);
    }

    /// <summary>
    /// Разбирает инструкцию выражения
    /// Поддерживает правило:
    /// statement = variable_declaration
    ///           | assignment_statement
    ///           | input_statement
    ///           | output_statement
    ///           | return_statement
    ///           | function_call_statement
    ///           | empty_statement
    ///           | if_statement
    ///           | while_statement
    ///           | switch_statement
    ///           | for_statement
    ///           | break_statement
    ///           | continue_statement
    ///               ;
    /// </summary>
    private AstNode ParseStatement()
    {
        switch (tokens.Peek().Type)
        {
            case TokenType.Int:
            case TokenType.Float:
            case TokenType.Bool:
            case TokenType.String:
            case TokenType.Const:
                return ParseVariableDeclaration();
            case TokenType.Identifier:
                if (tokens.Peek(1).Type == TokenType.Identifier)
                {
                    return ParseVariableDeclaration();
                }

                if (CanStartAssignmentStatement())
                {
                    return ParseAssignmentStatement();
                }

                if (tokens.Peek(1).Type is TokenType.Increment or TokenType.Decrement)
                {
                    return ParsePostfixExpression(true);
                }

                return ParseIdentifierSuffix();
            case TokenType.Read:
                return ParseInputStatement();
            case TokenType.Write:
                return ParseOutputStatement();
            case TokenType.Return:
                return ParseReturnStatement();
            case TokenType.If:
                return ParseIfStatement();
            case TokenType.While:
                return ParseWhileLoopStatement();
            case TokenType.For:
                return ParseForLoopStatement();
            case TokenType.Break:
                tokens.Advance();
                Match(TokenType.Semicolon);
                return new BreakStatement();
            case TokenType.Continue:
                tokens.Advance();
                Match(TokenType.Semicolon);
                return new ContinueStatement();
            case TokenType.Semicolon:
                tokens.Advance();
                Match(TokenType.Semicolon);
                return new EmptyStatement();
            case TokenType.Increment:
            case TokenType.Decrement:
                AstNode result = ParseUnaryExpression(true);
                Match(TokenType.Semicolon);
                return result;
            default:
                throw new Exception($"Unexpected token: {tokens.Peek().Type}");
        }
    }

    /// <summary>
    /// Разбирает правило объявления переменной
    /// Правило:
    /// variable_declaration = type, variable_decl_list, "поклон" ;
    /// </summary>
    private VariableDeclaration ParseVariableDeclaration()
    {
        if (tokens.Peek().Type == TokenType.Const)
        {
            tokens.Advance();
            TypeReference variableType = ParseType();
            return new VariableDeclaration(
                true,
                variableType,
                ParseVariableDeclarationList());
        }

        TypeReference type = ParseType();
        return new VariableDeclaration(false, type, ParseVariableDeclarationList());
    }

    /// <summary>
    /// Разбирает правило списка переменных в их объявлении
    /// Правило:
    /// variable_decl_list = variable_decl_item, {",", variable_decl_item} ;
    /// </summary>
    private Dictionary<string, Expression?> ParseVariableDeclarationList()
    {
        Dictionary<string, Expression?> namesToValues = [];
        KeyValuePair<string, Expression?> pair = ParseVariableDeclarationItem();

        namesToValues.Add(pair.Key, pair.Value);
        while (tokens.Peek().Type == TokenType.Comma)
        {
            tokens.Advance();
            pair = ParseVariableDeclarationItem();

            namesToValues.Add(pair.Key, pair.Value);
        }

        return namesToValues;
    }

    /// <summary>
    /// Разбирает правило одного элемента списка переменных в их объявлении
    /// Правило:
    /// variable_decl_item = identifier, [variable_initializer] ;
    /// </summary>
    private KeyValuePair<string, Expression?> ParseVariableDeclarationItem()
    {
        string name = tokens.Peek().Value!.ToString();
        Match(TokenType.Identifier);
        if (tokens.Peek().Type == TokenType.Assignment)
        {
            return new KeyValuePair<string, Expression?>(name, ParseVariableInitializer());
        }

        return new KeyValuePair<string, Expression?>(name, null);
    }

    /// <summary>
    /// Разбирает правило инициализации элемента из списка переменных в их объявлении
    /// Правило:
    /// variable_initializer = "даруй", expression ;
    /// </summary>
    private Expression ParseVariableInitializer()
    {
        Match(TokenType.Assignment);
        return ParseExpression();
    }

    /// <summary>
    /// Разбирает правило присваивания значения переменной
    /// Правило:
    /// assignment_statement = identifier, assignment_tail ;
    /// </summary>
    private AssignmentStatement ParseAssignmentStatement()
    {
        Expression target = ParsePostfixExpression().Expression;
        Match(TokenType.Assignment);
        AssignmentStatement assignment = new(target, ParseExpression());
        Match(TokenType.Semicolon);

        return assignment;
    }

    private Expression ParseAssignmentExpression()
    {
        Expression target = ParseLogicalOrExpression();
        if (tokens.Peek().Type != TokenType.Assignment)
        {
            return target;
        }

        tokens.Advance();
        return new AssignmentExpression(target, ParseAssignmentExpression());
    }

    /// <summary>
    /// Разбирает правило функции ввода в программу
    /// Правило:
    /// input_statement = "внемли", "(", input_arguments, ")", "поклон" ;
    /// </summary>
    private InputStatement ParseInputStatement()
    {
        Match(TokenType.Read);
        Match(TokenType.OpenParenthesis);
        List<string> list = ParseInputStatementArguments();
        Match(TokenType.CloseParenthesis);
        Match(TokenType.Semicolon);

        return new InputStatement(list);
    }

    /// <summary>
    /// Разбирает правило агругментов в функции ввода
    /// Правило:
    /// input_arguments = identifier, {",", identifier} ;
    /// </summary>
    private List<string> ParseInputStatementArguments()
    {
        List<string> list = [];

        if (tokens.Peek().Type == TokenType.CloseParenthesis)
        {
            return list;
        }

        string name = tokens.Peek().Value!.ToString();

        tokens.Advance();

        list.Add(name);

        while (tokens.Peek().Type == TokenType.Comma)
        {
            tokens.Advance();

            // Не идентификатор внутри функции ввода
            if (tokens.Peek().Type != TokenType.Identifier)
            {
                throw new UnexpectedLexemeException(TokenType.Identifier, tokens.Peek());
            }

            name = tokens.Peek().Value!.ToString();
            tokens.Advance();

            list.Add(name);
        }

        return list;
    }

    /// <summary>
    /// Разбирает правило фргументов в функции вывода
    /// Правило:
    /// output_statement = "возгласи", "(", output_arguments, ")", "поклон" ;
    /// </summary>
    private OutputStatement ParseOutputStatement()
    {
        Match(TokenType.Write);
        Match(TokenType.OpenParenthesis);
        List<Expression> items = ParseOutputStatementArguments();
        Match(TokenType.CloseParenthesis);
        Match(TokenType.Semicolon);

        return new OutputStatement(items);
    }

    /// <summary>
    /// Разбирает правило аргументов функции вывода
    /// Правило:
    /// output_arguments = output_item, {",", output_item} ;
    /// </summary>
    private List<Expression> ParseOutputStatementArguments()
    {
        List<Expression> expressions = [];
        Expression value = ParseOutputItem();

        expressions.Add(value);
        while (tokens.Peek().Type == TokenType.Comma)
        {
            tokens.Advance();
            value = ParseOutputItem();
            expressions.Add(value);
        }

        return expressions;
    }

    /// <summary>
    /// Разбирает правило элемента аргумента функции вывода
    /// Правило:
    /// output_item = string | expression ;
    /// </summary>
    private Expression ParseOutputItem()
    {
        return ParseExpression();
    }

    private ReturnStatement ParseReturnStatement()
    {
        Match(TokenType.Return);
        Expression? result = tokens.Peek().Type == TokenType.Semicolon ? null : ParseExpression();
        Match(TokenType.Semicolon);
        return new ReturnStatement(result);
    }

    /// <summary>
    /// if_statement = "аще", "(", logical_or_expression, ")", scope, ["илиже", scope];
    /// </summary>
    private IfElseStatement ParseIfStatement()
    {
        Match(TokenType.If);
        Match(TokenType.OpenParenthesis);
        Expression condition = ParseLogicalOrExpression();
        Match(TokenType.CloseParenthesis);
        ScopeStatement thenScope = ParseScope();
        ScopeStatement? elseScope = null;
        if (tokens.Peek().Type == TokenType.Else)
        {
            tokens.Advance();
            elseScope = tokens.Peek().Type == TokenType.If
                ? new ScopeStatement([ParseIfStatement()])
                : ParseScope();
        }

        return new IfElseStatement(condition, thenScope, elseScope);
    }

    /// <summary>
    /// while_statement = "доколе", "(", logical_or_expression, ")", scope;
    /// </summary>
    private WhileLoopStatement ParseWhileLoopStatement()
    {
        Match(TokenType.While);
        Match(TokenType.OpenParenthesis);
        Expression condition = ParseExpression();
        Match(TokenType.CloseParenthesis);
        ScopeStatement body = ParseScope();

        return new WhileLoopStatement(condition, body);
    }

    /// <summary>
    /// for_statement = "for", "(", [for_init], ";", [expression], ";", [for_post], ")", scope;
    /// </summary>
    private ForLoopStatement ParseForLoopStatement()
    {
        Match(TokenType.For);
        Match(TokenType.OpenParenthesis);

        AstNode? initializer = ParseForInitializer();
        Match(TokenType.Semicolon);
        Expression? condition = tokens.Peek().Type == TokenType.Semicolon ? null : ParseExpression();
        Match(TokenType.Semicolon);
        AstNode? post = tokens.Peek().Type == TokenType.CloseParenthesis ? null : ParseForPost();
        Match(TokenType.CloseParenthesis);

        return new ForLoopStatement(
            initializer,
            condition,
            post,
            ParseScope());
    }

    private AstNode? ParseForInitializer()
    {
        return tokens.Peek().Type switch
        {
            TokenType.Semicolon => null,
            TokenType.Int or TokenType.Float or TokenType.Bool or TokenType.String or TokenType.Const =>
                ParseVariableDeclaration(),
            TokenType.Identifier when CanStartAssignmentStatement() => ParseAssignmentExpression(),
            TokenType.Identifier when tokens.Peek(1).Type is TokenType.Increment or TokenType.Decrement =>
                ParsePostfixExpression(true),
            TokenType.Increment or TokenType.Decrement => ParseUnaryExpression(true),
            _ => ParseExpression(),
        };
    }

    private AstNode? ParseForPost()
    {
        return tokens.Peek().Type switch
        {
            TokenType.CloseParenthesis => null,
            TokenType.Identifier when CanStartAssignmentStatement() => ParseAssignmentExpression(),
            TokenType.Identifier when tokens.Peek(1).Type is TokenType.Increment or TokenType.Decrement =>
                ParsePostfixExpression(true),
            TokenType.Increment or TokenType.Decrement => ParseUnaryExpression(true),
            _ => ParseExpression(),
        };
    }

    /// <summary>
    /// Выполняет операцию и возвращает результат.
    /// Поддерживает правила:
    /// expression = logical_or_expression ;
    /// </summary>
    private Expression ParseExpression()
    {
        return ParseAssignmentExpression();
    }

    /// <summary>
    /// Выполняет операцию присваивания.
    /// Правила:
    ///    assignment_tail = "даруй", expression ;
    /// </summary>
    private Expression ParseAssignmentTail()
    {
        Match(TokenType.Assignment);
        Expression value = ParseExpression();
        Match(TokenType.Semicolon);
        return value;
    }

    /// <summary>
    /// Выполняет логическую операцию "или".
    /// Правила:
    ///    logical_or_expression = logical_and_expression, {"или", logical_and_expression} ;
    /// </summary>
    private Expression ParseLogicalOrExpression()
    {
        Expression value = ParseLogicalAndExpression();
        while (true)
        {
            switch (tokens.Peek().Type)
            {
                case TokenType.LogicalOr:
                    tokens.Advance();
                    value = new BinaryOperationExpression(value, BinaryOperation.Or, ParseLogicalAndExpression());
                    break;
                default:
                    return value;
            }
        }
    }

    /// <summary>
    /// Выполняет логическую операцию "и".
    /// Правила:
    ///    logical_and_expression = equality_expression, {"и", equality_expression} ;
    /// </summary>
    private Expression ParseLogicalAndExpression()
    {
        Expression value = ParseEqualityExpression();
        while (true)
        {
            switch (tokens.Peek().Type)
            {
                case TokenType.LogicalAnd:
                    tokens.Advance();
                    value = new BinaryOperationExpression(value, BinaryOperation.And, ParseEqualityExpression());
                    break;
                default:
                    return value;
            }
        }
    }

    /// <summary>
    /// Выполняет логическую операцию равенства / неравенства.
    /// Правила:
    ///    equality_expression = comparison_expression, {("яко" | "негоже"), comparison_expression} ;
    /// </summary>
    private Expression ParseEqualityExpression()
    {
        Expression value = ParseComparisonExpression();
        while (true)
        {
            switch (tokens.Peek().Type)
            {
                case TokenType.Equal:
                    tokens.Advance();
                    value = new BinaryOperationExpression(value, BinaryOperation.Equal, ParseComparisonExpression());
                    break;
                case TokenType.NotEqual:
                    tokens.Advance();
                    value = new BinaryOperationExpression(value, BinaryOperation.NotEqual, ParseComparisonExpression());
                    break;
                default:
                    return value;
            }
        }
    }

    /// <summary>
    ///  Разбирает одну операцию сравнения.
    ///  Правила:
    ///     comparison_expression = additive_expression, {
    ///        ("велий" | "малый" | "паче" | "меньше"), additive_expression
    ///     } ;
    /// </summary>
    private Expression ParseComparisonExpression()
    {
        Expression value = ParseAdditiveExpression();
        while (true)
        {
            switch (tokens.Peek().Type)
            {
                case TokenType.GreaterThan:
                    tokens.Advance();
                    value = new BinaryOperationExpression(value, BinaryOperation.GreaterThan, ParseAdditiveExpression());
                    break;
                case TokenType.LessThan:
                    tokens.Advance();
                    value = new BinaryOperationExpression(value, BinaryOperation.LessThan, ParseAdditiveExpression());
                    break;
                case TokenType.GreaterThanOrEqual:
                    tokens.Advance();
                    value = new BinaryOperationExpression(
                        value,
                        BinaryOperation.GreaterThanOrEqual,
                        ParseAdditiveExpression());
                    break;
                case TokenType.LessThanOrEqual:
                    tokens.Advance();
                    value = new BinaryOperationExpression(
                        value,
                        BinaryOperation.LessThanOrEqual,
                        ParseAdditiveExpression());
                    break;
                default:
                    return value;
            }
        }
    }

    /// <summary>
    ///  Разбирает одну операцию сложения / вычитания.
    ///  Правила:
    ///     additive_expression = multiplicative_expression, {("+" | "-"), multiplicative_expression} ;
    /// </summary>
    private Expression ParseAdditiveExpression()
    {
        Expression value = ParseMultiplicativeExpression();
        while (true)
        {
            switch (tokens.Peek().Type)
            {
                case TokenType.Plus:
                    tokens.Advance();
                    value = new BinaryOperationExpression(value, BinaryOperation.Plus, ParseMultiplicativeExpression());
                    break;
                case TokenType.Minus:
                    tokens.Advance();
                    value = new BinaryOperationExpression(value, BinaryOperation.Minus, ParseMultiplicativeExpression());
                    break;
                default:
                    return value;
            }
        }
    }

    /// <summary>
    ///  Разбирает одну операцию умножения / деления / остатка от деления.
    ///  Правила:
    ///     multiplicative_expression = unary_expression, {("*" | "/" | "%"), unary_expression} ;
    /// </summary>
    private Expression ParseMultiplicativeExpression()
    {
        Expression value = ParseUnaryExpression();
        while (true)
        {
            switch (tokens.Peek().Type)
            {
                case TokenType.Multiply:
                    tokens.Advance();
                    value = new BinaryOperationExpression(value, BinaryOperation.Multiply, ParseUnaryExpression());
                    break;
                case TokenType.Divide:
                    tokens.Advance();
                    value = new BinaryOperationExpression(value, BinaryOperation.Divide, ParseUnaryExpression());
                    break;
                case TokenType.Modulo:
                    tokens.Advance();
                    value = new BinaryOperationExpression(value, BinaryOperation.Modulo, ParseUnaryExpression());
                    break;
                default:
                    return value;
            }
        }
    }

    /// <summary>
    ///  Разбирает одну унарную операцию.
    ///  Правило:
    ///     unary_expression = [("не" | "+" | "-" | "приумножу" | "умалю")], postfix_expression ;
    /// </summary>
    private Expression ParseUnaryExpression(bool isStatement = false)
    {
        switch (tokens.Peek().Type)
        {
            case TokenType.LogicalNot:
                tokens.Advance();
                return new UnaryOperationExpression(UnaryOperation.Not, ParsePostfixExpression());
            case TokenType.Plus:
                tokens.Advance();
                return new UnaryOperationExpression(UnaryOperation.Plus, ParsePostfixExpression());
            case TokenType.Minus:
                tokens.Advance();
                return new UnaryOperationExpression(UnaryOperation.Minus, ParsePostfixExpression());
            case TokenType.Increment:
                tokens.Advance();
                return new UnaryOperationExpression(UnaryOperation.Increment, ParsePostfixExpression(isStatement));
            case TokenType.Decrement:
                tokens.Advance();
                return new UnaryOperationExpression(UnaryOperation.Decrement, ParsePostfixExpression(isStatement));
            default:
                return ParsePostfixExpression();
        }
    }

    /// <summary>
    ///  Разбирает одну постфиксную операцию.
    ///  Правило:
    ///     postfix_expression = primary_expression, [("приумножу" | "умалю")] ;
    /// </summary>
    private UnaryOperationExpression ParsePostfixExpression(bool isStatement = false)
    {
        Expression value = ParsePrimaryExpression();
        bool keepParsing = true;
        while (keepParsing)
        {
            switch (tokens.Peek().Type)
            {
                case TokenType.OpenBracket:
                    tokens.Advance();
                    Expression index = ParseExpression();
                    Match(TokenType.CloseBracket);
                    value = new ArrayAccessExpression(value, index);
                    break;
                case TokenType.Dot:
                    tokens.Advance();
                    string fieldName = tokens.Peek().Value!.ToString();
                    Match(TokenType.Identifier);
                    value = new FieldAccessExpression(value, fieldName);
                    break;
                case TokenType.Increment:
                    tokens.Advance();
                    return new UnaryOperationExpression(UnaryOperation.Increment, value, true, !isStatement);
                case TokenType.Decrement:
                    tokens.Advance();
                    return new UnaryOperationExpression(UnaryOperation.Decrement, value, true, !isStatement);
                default:
                    keepParsing = false;
                    break;
            }
        }

        return new UnaryOperationExpression(null, value, true);
    }

    /// <summary>
    ///  Разбирает простейшую часть выражения.
    ///  Правила:
    ///     primary_expression = literal
    ///                        | constant_token
    ///                        | "(", expression, ")"
    ///                        | builtin_math_call
    ///                        | identifier_suffix;
    /// </summary>
    private Expression ParsePrimaryExpression()
    {
        Token t = tokens.Peek();
        switch (t.Type)
        {
            case TokenType.FloatLiteral:
                tokens.Advance();
                return new LiteralExpression(t.Value!.ToFloat());
            case TokenType.IntLiteral:
                tokens.Advance();
                return new LiteralExpression(t.Value!.ToInt());
            case TokenType.StringLiteral:
                tokens.Advance();
                return new LiteralExpression(t.Value!.ToString());
            case TokenType.True:
                tokens.Advance();
                return new LiteralExpression(true);
            case TokenType.False:
                tokens.Advance();
                return new LiteralExpression(false);
            case TokenType.Identifier:
                return ParseIdentifierSuffix();
            case TokenType.Abs:
            case TokenType.Round:
            case TokenType.Ceil:
            case TokenType.Floor:
            case TokenType.Min:
            case TokenType.Max:
            case TokenType.Length:
            case TokenType.Substring:
                return ParseBuiltinFunctionCall();
            case TokenType.OpenBrace:
                return ParseArrayLiteral();
            case TokenType.OpenParenthesis:
                {
                    tokens.Advance();
                    Expression value = ParseExpression();
                    Match(TokenType.CloseParenthesis);
                    return value;
                }

            default:
                throw new UnexpectedLexemeException(TokenType.IntLiteral, t);
        }
    }

    /// <summary>
    ///  Разбирает вызов идентификатора / функции.
    ///  Правила:
    ///     identifier_suffix = identifier, [function_arguments] ;
    /// </summary>
    private Expression ParseIdentifierSuffix()
    {
        string name = tokens.Peek().Value!.ToString();
        tokens.Advance();
        if (tokens.Peek().Type == TokenType.OpenBrace)
        {
            return ParseStructLiteral(name);
        }

        if (tokens.Peek().Type == TokenType.OpenParenthesis)
        {
            Match(TokenType.OpenParenthesis);
            List<Expression> arguments = ParseArgumentList();
            Match(TokenType.CloseParenthesis);

            if (BuiltinFunctions.ContainsFunctionWithName(name))
            {
                return new BuiltinFunctionCallExpression(name, arguments);
            }

            return new FunctionCallExpression(name, arguments);
        }

        // Значение переменной
        return new VariableExpression(name);
    }

    private Expression ParseBuiltinFunctionCall()
    {
        string name = tokens.Peek().Type.ToString().ToLowerInvariant();
        tokens.Advance();
        Match(TokenType.OpenParenthesis);
        List<Expression> arguments = ParseArgumentList();
        Match(TokenType.CloseParenthesis);
        return new BuiltinFunctionCallExpression(name, arguments);
    }

    private ArrayLiteralExpression ParseArrayLiteral()
    {
        Match(TokenType.OpenBrace);
        List<Expression> elements = [];
        if (tokens.Peek().Type != TokenType.CloseBrace)
        {
            elements.Add(ParseExpression());
            while (tokens.Peek().Type == TokenType.Comma)
            {
                tokens.Advance();
                elements.Add(ParseExpression());
            }
        }

        Match(TokenType.CloseBrace);
        return new ArrayLiteralExpression(elements);
    }

    private StructLiteralExpression ParseStructLiteral(string typeName)
    {
        Match(TokenType.OpenBrace);
        List<Expression> values = [];
        if (tokens.Peek().Type != TokenType.CloseBrace)
        {
            values.Add(ParseExpression());
            while (tokens.Peek().Type == TokenType.Comma)
            {
                tokens.Advance();
                values.Add(ParseExpression());
            }
        }

        Match(TokenType.CloseBrace);
        return new StructLiteralExpression(typeName, values);
    }

    /// <summary>
    /// Разбирает список выражений, разделённый запятыми.
    /// Правила:
    ///     expression_list = expression, { ",", expression } ;
    /// </summary>
    private List<Expression> ParseArgumentList()
    {
        if (tokens.Peek().Type == TokenType.CloseParenthesis)
        {
            return [];
        }

        List<Expression> values =
        [
            ParseExpression(),
        ];
        while (tokens.Peek().Type == TokenType.Comma)
        {
            tokens.Advance();
            values.Add(ParseExpression());
        }

        return values;
    }

    /// <summary>
    /// Пропускает ожидаемую лексему либо бросает исключение, если встретит иную лексему.
    /// </summary>
    private void Match(TokenType expected)
    {
        Token t = tokens.Peek();
        if (t.Type != expected)
        {
            throw new UnexpectedLexemeException(expected, t);
        }

        tokens.Advance();
    }

    private TypeReference ParseType()
    {
        TypeReference type = tokens.Peek().Type switch
        {
            TokenType.Bool => TypeReference.Boolean,
            TokenType.String => TypeReference.String,
            TokenType.Int => TypeReference.Int,
            TokenType.Float => TypeReference.Float,
            TokenType.Void => TypeReference.Void,
            TokenType.Identifier => TypeReference.Struct(tokens.Peek().Value!.ToString()),
            _ => throw new UnexpectedLexemeException(TokenType.Int, tokens.Peek())
        };

        tokens.Advance();
        if (tokens.Peek().Type == TokenType.OpenBracket)
        {
            tokens.Advance();
            Match(TokenType.CloseBracket);
            type = TypeReference.ArrayOf(type);
        }

        return type;
    }

    private bool CanStartAssignmentStatement()
    {
        int depth = 0;
        int offset = 0;
        while (true)
        {
            TokenType type = tokens.Peek(offset).Type;
            if (type == TokenType.End || type == TokenType.Semicolon || type == TokenType.Comma ||
                type == TokenType.CloseParenthesis)
            {
                return false;
            }

            if (type is TokenType.OpenBracket or TokenType.OpenParenthesis)
            {
                depth++;
            }
            else if (type is TokenType.CloseBracket)
            {
                depth--;
            }
            else if (type == TokenType.Assignment && depth == 0)
            {
                return true;
            }

            offset++;
        }
    }

    private VariableType TokenTypeToVariableType(TokenType tokenType)
    {
        return tokenType switch
        {
            TokenType.Bool => VariableType.Boolean,
            TokenType.String => VariableType.String,
            TokenType.Int => VariableType.Int,
            TokenType.Float => VariableType.Float,
            TokenType.Void => VariableType.Void,
            _ => throw new UnexpectedLexemeException(tokenType, tokens.Peek())
        };
    }
}
