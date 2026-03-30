namespace Ast.Expressions;

public enum BinaryOperation
{
    /// <summary>
    /// Сложение
    /// </summary>
    Plus,

    /// <summary>
    /// Вычитание
    /// </summary>
    Minus,

    /// <summary>
    /// Умножение
    /// </summary>
    Multiply,

    /// <summary>
    /// Деление
    /// </summary>
    Divide,

    /// <summary>
    /// Остаток от деления
    /// </summary>
    Modulo,

    /// <summary>
    /// Операция сравнения "больше"
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Операция сравнения "больше или равно"
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Операция сравнения "меньше"
    /// </summary>
    LessThan,

    /// <summary>
    /// Операция сравнения "меньше или равно"
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// Операция сравнения "равно"
    /// </summary>
    Equal,

    /// <summary>
    /// Операция сравнения "не равно"
    /// </summary>
    NotEqual,

    /// <summary>
    /// Логическая операция "или"
    /// </summary>
    Or,

    /// <summary>
    /// Логическая операция "и"
    /// </summary>
    And,
}