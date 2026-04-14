namespace Lexer.UnitTests.Helpers;

/// <summary>
/// Фабрика токенов для удобного написания тестов.
/// Использование: new[] { Lex.Id("x"), Lex.Plus, Lex.Int(1) }.
/// </summary>
public static class Lex
{
    // Литералы
    public static Token Id(string v) => new(TokenType.Identifier, new TokenValue(v));

    public static Token Int(int v) => new(TokenType.IntLiteral, new TokenValue(v));

    public static Token Flt(float v) => new(TokenType.FloatLiteral, new TokenValue(v));

    public static Token Str(string v) => new(TokenType.StringLiteral, new TokenValue(v));

    public static Token Err(string v = "") => new(TokenType.Error, new TokenValue(v));

    // Логика
    public static Token True => new(TokenType.True);

    public static Token False => new(TokenType.False);

    public static Token Not => new(TokenType.LogicalNot);

    public static Token And => new(TokenType.LogicalAnd);

    public static Token Or => new(TokenType.LogicalOr);

    // Типы и модификаторы
    public static Token KwInt => new(TokenType.Int);

    public static Token KwFloat => new(TokenType.Float);

    public static Token KwStr => new(TokenType.String);

    public static Token KwBool => new(TokenType.Bool);

    public static Token KwVoid => new(TokenType.Void);

    public static Token KwConst => new(TokenType.Const);

    public static Token KwMain => new(TokenType.Main);

    // Управление потоком
    public static Token KwIf => new(TokenType.If);

    public static Token KwElse => new(TokenType.Else);

    public static Token KwWhile => new(TokenType.While);

    public static Token KwBreak => new(TokenType.Break);

    public static Token KwContinue => new(TokenType.Continue);

    public static Token KwReturn => new(TokenType.Return);

    // Ввод/Вывод
    public static Token KwRead => new(TokenType.Read);

    public static Token KwWrite => new(TokenType.Write);

    // Встроенные функции
    public static Token FnAbs => new(TokenType.Abs);

    public static Token FnRound => new(TokenType.Round);

    public static Token FnCeil => new(TokenType.Ceil);

    public static Token FnFloor => new(TokenType.Floor);

    public static Token FnMin => new(TokenType.Min);

    public static Token FnMax => new(TokenType.Max);

    public static Token FnLength => new(TokenType.Length);

    public static Token FnSubstring => new(TokenType.Substring);

    // Арифметика
    public static Token Plus => new(TokenType.Plus);

    public static Token Minus => new(TokenType.Minus);

    public static Token Mul => new(TokenType.Multiply);

    public static Token Div => new(TokenType.Divide);

    public static Token Mod => new(TokenType.Modulo);

    public static Token Inc => new(TokenType.Increment);

    public static Token Dec => new(TokenType.Decrement);

    public static Token Assign => new(TokenType.Assignment);

    // Сравнение
    public static Token Eq => new(TokenType.Equal);

    public static Token NotEq => new(TokenType.NotEqual);

    public static Token Lt => new(TokenType.LessThan);

    public static Token LtEq => new(TokenType.LessThanOrEqual);

    public static Token Gt => new(TokenType.GreaterThan);

    public static Token GtEq => new(TokenType.GreaterThanOrEqual);

    // Синтаксис
    public static Token LPar => new(TokenType.OpenParenthesis);

    public static Token RPar => new(TokenType.CloseParenthesis);

    public static Token LBrace => new(TokenType.OpenBrace);

    public static Token RBrace => new(TokenType.CloseBrace);

    public static Token Comma => new(TokenType.Comma);

    public static Token Semi => new(TokenType.Semicolon);

    public static Token Tok(TokenType t) => new(t);
}