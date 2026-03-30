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
    Double,

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
}