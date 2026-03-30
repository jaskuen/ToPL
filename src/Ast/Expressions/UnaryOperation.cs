namespace Ast.Expressions;

public enum UnaryOperation
{
    /// <summary>
    /// Унарный плюс
    /// </summary>
    Plus,

    /// <summary>
    /// Унарный минус
    /// </summary>
    Minus,

    /// <summary>
    /// Логическое отрицание
    /// </summary>
    Not,

    /// <summary>
    /// Инкремент
    /// </summary>
    Increment,

    /// <summary>
    /// Декремент
    /// </summary>
    Decrement,
}