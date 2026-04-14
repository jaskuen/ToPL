namespace Lexer;

/// <summary>
/// Перечисление общих категорий (типов) лексем.
/// Используется для первичной классификации токенов перед их детализацией в TokenType.
/// См. 01_lexemes.md.
/// </summary>
public enum LexicalType
{
    /// <summary>
    /// Раздел 3: Ключевые слова (int, float, if, read, write, abs и т.д.).
    /// Включает также словесные формы операторов (and, or, equals).
    /// </summary>
    Keyword,

    /// <summary>
    /// Раздел 1: Идентификаторы (имена переменных и пользовательских функций).
    /// </summary>
    Identifier,

    /// <summary>
    /// Раздел 2.1: Целочисленный литерал (int_literal).
    /// </summary>
    IntLiteral,

    /// <summary>
    /// Раздел 2.2: Литерал с плавающей точкой (float_literal).
    /// </summary>
    FloatLiteral,

    /// <summary>
    /// Раздел 2.3: Строковый литерал (string_literal).
    /// </summary>
    StringLiteral,

    /// <summary>
    /// Раздел 2.4: Логический литерал (true, false).
    /// </summary>
    BoolLiteral,

    /// <summary>
    /// Раздел 4: Операторы (+, -, *, /, ==, &&, ! и т.д.).
    /// </summary>
    Operator,

    /// <summary>
    /// Раздел 5: Знаки пунктуации и разделители ( ; , ( ) { } ).
    /// </summary>
    Punctuation,
}