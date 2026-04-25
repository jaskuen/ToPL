namespace Lexer;

public enum TokenType
{
    /// <summary>
    /// Ошибка лексического анализа (недопустимый символ и т.д.).
    /// </summary>
    Error,

    /// <summary>
    /// Конец файла (EOF).
    /// </summary>
    End,

    /// <summary>
    /// Литерал целого числа (int_literal, 32-битное знаковое).
    /// </summary>
    IntLiteral,

    /// <summary>
    /// Литерал числа с плавающей точкой (float_literal, 32-битное одинарной точности).
    /// </summary>
    FloatLiteral,

    /// <summary>
    /// Строковый литерал (string_literal).
    /// </summary>
    StringLiteral,

    /// <summary>
    /// Логическая истина (литерал 'true').
    /// </summary>
    True,

    /// <summary>
    /// Логическая ложь (литерал 'false').
    /// </summary>
    False,

    /// <summary>
    /// Идентификатор (имя переменной или функции).
    /// </summary>
    Identifier,

    /// <summary>
    /// Ключевое слово 'int'.
    /// </summary>
    Int,

    /// <summary>
    /// Ключевое слово 'float'.
    /// </summary>
    Float,

    /// <summary>
    /// Ключевое слово 'string'.
    /// </summary>
    String,

    /// <summary>
    /// Ключевое слово 'bool'.
    /// </summary>
    Bool,

    /// <summary>
    /// Ключевое слово 'void'.
    /// </summary>
    Void,

    /// <summary>
    /// Ключевое слово 'main' (точка входа).
    /// </summary>
    Main,

    /// <summary>
    /// Ключевое слово 'const' (объявление константы).
    /// </summary>
    Const,

    /// <summary>
    /// Ключевое слово 'if'.
    /// </summary>
    If,

    /// <summary>
    /// Ключевое слово 'else'.
    /// </summary>
    Else,

    /// <summary>
    /// Ключевое слово 'while'.
    /// </summary>
    While,

    /// <summary>
    /// Ключевое слово 'for'.
    /// </summary>
    For,

    /// <summary>
    /// Ключевое слово 'break'.
    /// </summary>
    Break,

    /// <summary>
    /// Ключевое слово 'continue'.
    /// </summary>
    Continue,

    /// <summary>
    /// Ключевое слово 'return'.
    /// </summary>
    Return,

    /// <summary>
    /// Ключевое слово 'read'.
    /// </summary>
    Read,

    /// <summary>
    /// Ключевое слово 'write'.
    /// </summary>
    Write,

    /// <summary>
    /// Встроенная функция 'abs'.
    /// </summary>
    Abs,

    /// <summary>
    /// Встроенная функция 'round'.
    /// </summary>
    Round,

    /// <summary>
    /// Встроенная функция 'ceil'.
    /// </summary>
    Ceil,

    /// <summary>
    /// Встроенная функция 'floor'.
    /// </summary>
    Floor,

    /// <summary>
    /// Встроенная функция 'min'.
    /// </summary>
    Min,

    /// <summary>
    /// Встроенная функция 'max'.
    /// </summary>
    Max,

    /// <summary>
    /// Встроенная функция 'length'.
    /// </summary>
    Length,

    /// <summary>
    /// Встроенная функция 'substring'.
    /// </summary>
    Substring,

    /// <summary>
    /// Оператор '+' (Сложение, конкатенация, унарный плюс).
    /// </summary>
    Plus,

    /// <summary>
    /// Оператор '-' (Вычитание, унарный минус).
    /// </summary>
    Minus,

    /// <summary>
    /// Оператор '*' (Умножение).
    /// </summary>
    Multiply,

    /// <summary>
    /// Оператор '/' (Деление).
    /// </summary>
    Divide,

    /// <summary>
    /// Оператор '%' (Остаток от деления).
    /// </summary>
    Modulo,

    /// <summary>
    /// Оператор '++' (Инкремент).
    /// </summary>
    Increment,

    /// <summary>
    /// Оператор '--' (Декремент).
    /// </summary>
    Decrement,

    /// <summary>
    /// Оператор '==' или 'equals' (Равенство).
    /// </summary>
    Equal,

    /// <summary>
    /// Оператор '!=' (Неравенство).
    /// </summary>
    NotEqual,

    /// <summary>
    /// Оператор '<' (Меньше).
    /// </summary>
    LessThan,

    /// <summary>
    /// Оператор '<=' (Меньше или равно).
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// Оператор '>' (Больше).
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Оператор '>=' (Больше или равно).
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Оператор '&&' или 'and' (Логическое И).
    /// </summary>
    LogicalAnd,

    /// <summary>
    /// Оператор '||' или 'or' (Логическое ИЛИ).
    /// </summary>
    LogicalOr,

    /// <summary>
    /// Оператор '!' или 'not' (Логическое НЕ).
    /// </summary>
    LogicalNot,

    /// <summary>
    /// Оператор '=' (Присваивание).
    /// </summary>
    Assignment,

    /// <summary>
    /// Открывающая круглая скобка '('.
    /// </summary>
    OpenParenthesis,

    /// <summary>
    /// Закрывающая круглая скобка ')'.
    /// </summary>
    CloseParenthesis,

    /// <summary>
    /// Открывающая фигурная скобка '{' (Начало блока кода).
    /// </summary>
    OpenBrace,

    /// <summary>
    /// Закрывающая фигурная скобка '}' (Конец блока кода).
    /// </summary>
    CloseBrace,

    /// <summary>
    /// Запятая ','.
    /// </summary>
    Comma,

    /// <summary>
    /// Точка с запятой ';'.
    /// </summary>
    Semicolon,
}