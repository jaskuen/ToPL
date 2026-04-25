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
        List<TokenType> types =
        [
            TokenType.Void, TokenType.Bool, TokenType.Float, TokenType.Int, TokenType.String
        ];

        List<FunctionDeclaration> functions = [];
        Token type = tokens.Peek();

        while (types.Contains(type.Type) && tokens.Peek(1).Type != TokenType.Main)
        {
            FunctionDeclaration functionDeclaration = ParseFunctionDeclaration();
            functions.Add(functionDeclaration);
            type = tokens.Peek();
        }

        VariableType mainType = TokenTypeToVariableType(tokens.Peek().Type);
        tokens.Advance();
        Match(TokenType.Main);
        Match(TokenType.OpenParenthesis);
        Match(TokenType.CloseParenthesis);
        ScopeStatement scope = ParseScope();

        Match(TokenType.End);
        return new ProgramUnit(functions, mainType, scope);
    }

    public void ParseProgram()
    {
        ProgramUnit program = ParseProgramAst();

        foreach (FunctionDeclaration function in program.Functions)
        {
            function.Accept(astEvaluator);
        }

        RuntimeValue result = astEvaluator.Evaluate(program.MainBody, program.MainType == VariableType.Void);
        environment.PrintValue($"{result}");
    }

    private FunctionDeclaration ParseFunctionDeclaration()
    {
        TokenType functionType = tokens.Peek().Type;
        tokens.Advance();
        string name = tokens.Peek().Value!.ToString();
        tokens.Advance();
        Dictionary<string, VariableType> parameters = ParseFunctionParameters();
        Statement scope = ParseScope();

        return new FunctionDeclaration(TokenTypeToVariableType(functionType), name, parameters, scope);
    }

    private Dictionary<string, VariableType> ParseFunctionParameters()
    {
        Dictionary<string, VariableType> parameters = [];
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

            VariableType variableType = TokenTypeToVariableType(tokens.Peek().Type);
            tokens.Advance();
            string name = tokens.Peek().Value!.ToString();
            tokens.Advance();

            parameters.Add(name, variableType);
        }

        tokens.Advance();
        return parameters;
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
                if (tokens.Peek(1).Type == TokenType.Assignment)
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
        TokenType type = tokens.Peek().Type;
        if (type == TokenType.Const)
        {
            tokens.Advance();
            TokenType variableType = tokens.Peek().Type;
            tokens.Advance();
            return new VariableDeclaration(
                true,
                TokenTypeToVariableType(variableType),
                ParseVariableDeclarationList(type));
        }

        tokens.Advance();
        return new VariableDeclaration(false, TokenTypeToVariableType(type), ParseVariableDeclarationList(type));
    }

    /// <summary>
    /// Разбирает правило списка переменных в их объявлении
    /// Правило:
    /// variable_decl_list = variable_decl_item, {",", variable_decl_item} ;
    /// </summary>
    private Dictionary<string, Expression?> ParseVariableDeclarationList(TokenType type)
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
        string name = tokens.Peek().Value!.ToString();
        tokens.Advance();
        Expression expression = ParseAssignmentTail();

        return new AssignmentStatement(name, expression);
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
        Expression result = ParseExpression();
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
            elseScope = ParseScope();
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
    /// for_statement = "повторити", "(", assignment_expression, ",", logical_or_expression, ",", assignment_expression, ")", scope;
    /// </summary>
    private ForLoopStatement ParseForLoopStatement()
    {
        Match(TokenType.For);
        Match(TokenType.OpenParenthesis);

        // пока пропускаем тип
        tokens.Advance();
        string name = tokens.Peek().Value!.ToString();
        tokens.Advance();
        Match(TokenType.Assignment);
        Expression startValue = ParseExpression();
        Match(TokenType.Comma);
        Expression condition = ParseExpression();
        Match(TokenType.Comma);
        Expression assignment = ParseExpression();
        Match(TokenType.CloseParenthesis);

        return new ForLoopStatement(
            name,
            startValue,
            condition,
            assignment,
            ParseScope());
    }

    /// <summary>
    /// Выполняет операцию и возвращает результат.
    /// Поддерживает правила:
    /// expression = logical_or_expression ;
    /// </summary>
    private Expression ParseExpression()
    {
        return ParseLogicalOrExpression();
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
    private BinaryOperationExpression ParseLogicalOrExpression()
    {
        BinaryOperationExpression value = ParseLogicalAndExpression();
        while (true)
        {
            switch (tokens.Peek().Type)
            {
                case TokenType.LogicalOr:
                    tokens.Advance();
                    return new BinaryOperationExpression(value, BinaryOperation.Or, ParseLogicalAndExpression());
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
    private BinaryOperationExpression ParseLogicalAndExpression()
    {
        BinaryOperationExpression value = ParseEqualityExpression();
        while (true)
        {
            switch (tokens.Peek().Type)
            {
                case TokenType.LogicalAnd:
                    tokens.Advance();
                    return new BinaryOperationExpression(value, BinaryOperation.And, ParseEqualityExpression());
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
    private BinaryOperationExpression ParseEqualityExpression()
    {
        BinaryOperationExpression value = ParseComparisonExpression();
        while (true)
        {
            switch (tokens.Peek().Type)
            {
                case TokenType.Equal:
                    tokens.Advance();
                    return new BinaryOperationExpression(value, BinaryOperation.Equal, ParseComparisonExpression());
                case TokenType.NotEqual:
                    tokens.Advance();
                    return new BinaryOperationExpression(value, BinaryOperation.NotEqual, ParseComparisonExpression());
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
    private BinaryOperationExpression ParseComparisonExpression()
    {
        BinaryOperationExpression value = ParseAdditiveExpression();
        while (true)
        {
            switch (tokens.Peek().Type)
            {
                case TokenType.GreaterThan:
                    tokens.Advance();
                    return new BinaryOperationExpression(value, BinaryOperation.GreaterThan, ParseAdditiveExpression());
                case TokenType.LessThan:
                    tokens.Advance();
                    return new BinaryOperationExpression(value, BinaryOperation.LessThan, ParseAdditiveExpression());
                case TokenType.GreaterThanOrEqual:
                    tokens.Advance();
                    return new BinaryOperationExpression(
                        value,
                        BinaryOperation.GreaterThanOrEqual,
                        ParseAdditiveExpression());
                case TokenType.LessThanOrEqual:
                    tokens.Advance();
                    return new BinaryOperationExpression(
                        value,
                        BinaryOperation.LessThanOrEqual,
                        ParseAdditiveExpression());
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
    private BinaryOperationExpression ParseAdditiveExpression()
    {
        BinaryOperationExpression value = ParseMultiplicativeExpression();
        while (true)
        {
            switch (tokens.Peek().Type)
            {
                case TokenType.Plus:
                    tokens.Advance();
                    return new BinaryOperationExpression(value, BinaryOperation.Plus, ParseMultiplicativeExpression());
                case TokenType.Minus:
                    tokens.Advance();
                    return new BinaryOperationExpression(value, BinaryOperation.Minus, ParseMultiplicativeExpression());
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
    private BinaryOperationExpression ParseMultiplicativeExpression()
    {
        UnaryOperationExpression value = ParseUnaryExpression();
        while (true)
        {
            switch (tokens.Peek().Type)
            {
                case TokenType.Multiply:
                    tokens.Advance();
                    return new BinaryOperationExpression(value, BinaryOperation.Multiply, ParseUnaryExpression());
                case TokenType.Divide:
                    tokens.Advance();
                    return new BinaryOperationExpression(value, BinaryOperation.Divide, ParseUnaryExpression());
                case TokenType.Modulo:
                    tokens.Advance();
                    return new BinaryOperationExpression(value, BinaryOperation.Modulo, ParseUnaryExpression());
                default:
                    return new BinaryOperationExpression(value);
            }
        }
    }

    /// <summary>
    ///  Разбирает одну унарную операцию.
    ///  Правило:
    ///     unary_expression = [("не" | "+" | "-" | "приумножу" | "умалю")], postfix_expression ;
    /// </summary>
    private UnaryOperationExpression ParseUnaryExpression(bool isStatement = false)
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
        switch (tokens.Peek().Type)
        {
            case TokenType.Increment:
                tokens.Advance();
                return new UnaryOperationExpression(UnaryOperation.Increment, value, true, !isStatement);
            case TokenType.Decrement:
                tokens.Advance();
                return new UnaryOperationExpression(UnaryOperation.Decrement, value, true, !isStatement);
            default:
                return new UnaryOperationExpression(null, value, true);
        }
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

    /// <summary>
    /// Разбирает список выражений, разделённый запятыми.
    /// Правила:
    ///     expression_list = expression, { ",", expression } ;
    /// </summary>
    private List<Expression> ParseArgumentList()
    {
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

    private VariableType TokenTypeToVariableType(TokenType tokenType)
    {
        return tokenType switch
        {
            TokenType.Bool => VariableType.Boolean,
            TokenType.String => VariableType.String,
            TokenType.Int => VariableType.Int,
            TokenType.Float => VariableType.Double,
            TokenType.Void => VariableType.Void,
            _ => throw new UnexpectedLexemeException(tokenType, tokens.Peek())
        };
    }
}