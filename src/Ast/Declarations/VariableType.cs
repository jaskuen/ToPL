namespace Ast.Declarations;

public enum VariableType
{
    /// <summary>
    /// Целочисленное значение
    /// </summary>
    Int,

    /// <summary>
    /// Нецелочисленное значение
    /// </summary>
    Float,

    /// <summary>
    /// Булевское значение
    /// </summary>
    Boolean,

    /// <summary>
    /// Строковое значение
    /// </summary>
    String,

    /// <summary>
    /// Пустое значение
    /// </summary>
    Void,

    /// <summary>
    /// Пользовательская структура
    /// </summary>
    Struct,
}
