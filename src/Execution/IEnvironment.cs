using Runtime;

namespace Execution;

/// <summary>
/// Представляет окружение для выполнения программы.
/// </summary>
public interface IEnvironment
{
    /// <summary>
    /// Вызывается при вызове функции вывода в программе.
    /// </summary>
    public void PrintValue(string value);

    /// <summary>
    /// Вызывается при вводе значения в программу
    /// </summary>
    public RuntimeValue ReadValue(RuntimeValueType type);
}