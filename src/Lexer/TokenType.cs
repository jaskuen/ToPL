namespace Lexer;

public enum TokenType
{
    /// <summary>
    ///  Недопустимая лексема.
    /// </summary>
    Error,

    /// <summary>
    /// Конец чтения
    /// </summary>
    End,

    /// <summary>
    /// Начало блока кода
    /// </summary>
    CodeBlockBegin,

    /// <summary>
    /// Конец блока кода
    /// </summary>
    CodeBlockEnd,

    /// <summary>
    /// Возврат значения
    /// </summary>
    Return,

    /// <summary>
    /// Оператор прерывания
    /// </summary>
    Break,

    /// <summary>
    /// Оператор перехода к следующей итерации
    /// </summary>
    Continue,

    /// <summary>
    /// Оператор присваивания
    /// </summary>
    Assignment,

    /// <summary>
    /// Оператор условия
    /// </summary>
    If,

    /// <summary>
    /// Оператор "иначе"
    /// </summary>
    Else,

    /// <summary>
    /// "Пустой" возвратный тип
    /// </summary>
    Void,

    /// <summary>
    /// Тип "целое число"
    /// </summary>
    NumericType,

    /// <summary>
    /// Тип "число с плавающей точкой"
    /// </summary>
    FloatType,

    /// <summary>
    /// Тип "строка"
    /// </summary>
    StringType,

    /// <summary>
    /// Логический тип
    /// </summary>
    BooleanType,

    /// <summary>
    ///  Идентификатор (имя символа)
    /// </summary>
    Identifier,

    /// <summary>
    ///  Литерал целого числа
    /// </summary>
    IntLiteral,

    /// <summary>
    ///  Литерал числа с плавающей точкой
    /// </summary>
    DoubleLiteral,

    /// <summary>
    ///  Литерал строки
    /// </summary>
    StringLiteral,

    /// <summary>
    ///  Оператор сложения
    /// </summary>
    PlusSign,

    /// <summary>
    ///  Оператор вычитания
    /// </summary>
    MinusSign,

    /// <summary>
    ///  Оператор умножения
    /// </summary>
    MultiplySign,

    /// <summary>
    ///  Оператор деления
    /// </summary>
    DivideSign,

    /// <summary>
    /// Оператор остатка от деления
    /// </summary>
    ModuloSign,

    /// <summary>
    /// Инкремент
    /// </summary>
    Increment,

    /// <summary>
    /// Декремент
    /// </summary>
    Decrement,

    /// <summary>
    /// Оператор равенства
    /// </summary>
    Equal,

    /// <summary>
    /// Оператор неравенства
    /// </summary>
    NotEqual,

    /// <summary>
    /// Логическая истина
    /// </summary>
    True,

    /// <summary>
    /// Логическая ложь
    /// </summary>
    False,

    /// <summary>
    ///  Оператор сравнения "меньше".
    /// </summary>
    LessThan,

    /// <summary>
    ///  Оператор сравнения "меньше или равно".
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    ///  Оператор сравнения "больше".
    /// </summary>
    GreaterThan,

    /// <summary>
    ///  Оператор сравнения "больше или равно".
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Логическое "и"
    /// </summary>
    And,

    /// <summary>
    /// Логическое "или"
    /// </summary>
    Or,

    /// <summary>
    /// Логическое "не"
    /// </summary>
    Not,

    /// <summary>
    ///  Открывающая круглая скобка '('.
    /// </summary>
    OpenParenthesis,

    /// <summary>
    ///  Закрывающая круглая скобка ')'.
    /// </summary>
    CloseParenthesis,

    /// <summary>
    /// Открывающая квадратная скобка '['.
    /// </summary>
    OpenArrayParenthesis,

    /// <summary>
    /// Закрывающая квадратная скобка ']'.
    /// </summary>
    CloseArrayParenthesis,

    /// <summary>
    ///  Запятая ','
    /// </summary>
    Comma,

    /// <summary>
    ///  Разделитель 'поклон'
    /// </summary>
    Semicolon,

    /// <summary>
    /// Оператор 'изберется'
    /// </summary>
    Switch,

    /// <summary>
    /// Оператор 'егда'
    /// </summary>
    Case,

    /// <summary>
    /// Оператор 'поеликуже'
    /// </summary>
    Default,

    /// <summary>
    /// Оператор цикла 'доколе'
    /// </summary>
    While,

    /// <summary>
    /// Оператор цикла 'повторити'
    /// </summary>
    For,

    /// <summary>
    /// Чтение из консоли
    /// </summary>
    ConsoleRead,

    /// <summary>
    /// Вывод в консоль
    /// </summary>
    ConsoleWrite,
}