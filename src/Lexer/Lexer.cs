using System.Globalization;

namespace Lexer;

public class Lexer
{
    private const char StringBoundChar = '\"';

    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        { "воистину", TokenType.OpenBrace },
        { "аминь", TokenType.CloseBrace },
        { "поклон", TokenType.Semicolon },
        { "аще", TokenType.If },
        { "илиже", TokenType.Else },
        { "ничтоже", TokenType.Void },
        { "благодать", TokenType.Int },
        { "кадило", TokenType.Float },
        { "верующий", TokenType.Bool },
        { "словеса", TokenType.String },
        { "доколе", TokenType.While },
        //{ "повторити", TokenType.For },
        { "возврати", TokenType.Return },
        { "отрешити", TokenType.Break },
        { "уповаю", TokenType.Continue },
        { "даруй", TokenType.Assignment },
        { "яко", TokenType.Equal },
        { "негоже", TokenType.NotEqual },
        { "не", TokenType.LogicalNot },
        { "паче", TokenType.GreaterThanOrEqual },
        { "меньше", TokenType.LessThanOrEqual },
        { "велий", TokenType.GreaterThan },
        { "малый", TokenType.LessThan },
        { "или", TokenType.LogicalOr },
        { "и", TokenType.LogicalAnd },
        { "истинно", TokenType.True },
        { "лукаво", TokenType.False },
        { "возгласи", TokenType.Write },
        { "внемли", TokenType.Read },
        { "приумножу", TokenType.Increment },
        { "умалю", TokenType.Decrement },
        //{ "егда", TokenType.Case },
        //{ "изберется", TokenType.Switch },
        //{ "поеликуже", TokenType.Default },
    };

    private readonly TextScanner scanner;

    public Lexer(string sql)
    {
        scanner = new TextScanner(sql);
    }

    /// <summary>
    ///  Распознаёт следующий токен.
    ///  Дополнительные правила:
    ///   1) Если ввод закончился, то возвращаем токен EndOfFile
    ///   2) Пробельные символы пропускаются.
    /// </summary>
    public Token ParseToken()
    {
        SkipWhiteSpacesAndComments();

        if (scanner.IsEnd())
        {
            return new Token(TokenType.End);
        }

        char c = scanner.Peek();
        if (char.IsLetter(c) || c == '_')
        {
            return ParseIdentifierOrKeyword();
        }

        if (char.IsAsciiDigit(c))
        {
            return ParseNumericLiteral();
        }

        if (c == StringBoundChar)
        {
            return ParseStringLiteral();
        }

        switch (c)
        {
            case ',':
                scanner.Advance();
                return new Token(TokenType.Comma);
            case '+':
                scanner.Advance();
                return new Token(TokenType.Plus);
            case '-':
                scanner.Advance();
                return new Token(TokenType.Minus);
            case '*':
                scanner.Advance();
                return new Token(TokenType.Multiply);
            case '%':
                scanner.Advance();
                return new Token(TokenType.Modulo);
            case '/':
                scanner.Advance();
                return new Token(TokenType.Divide);
            case '(':
                scanner.Advance();
                return new Token(TokenType.OpenParenthesis);
            case ')':
                scanner.Advance();
                return new Token(TokenType.CloseParenthesis);
            //case '[':
            //    scanner.Advance();
            //    return new Token(TokenType.OpenArrayParenthesis);
            //case ']':
            //    scanner.Advance();
            //    return new Token(TokenType.CloseArrayParenthesis);
        }

        scanner.Advance();
        return new Token(TokenType.Error, new TokenValue(c.ToString()));
    }

    /// <summary>
    ///  Распознаёт идентификаторы и ключевые слова.
    ///  Идентификаторы обрабатываются по правилам:
    ///     identifier = [letter | '_' ], { letter | digit | '_' } ;
    ///     letter = "a" | "b" | .. | "z" | unicode_letter ;
    ///     digit = "0" | "1" | .. | "9" ;
    ///     unicode_letter — любая буква Unicode.
    /// </summary>
    private Token ParseIdentifierOrKeyword()
    {
        string value = scanner.Peek().ToString();
        scanner.Advance();

        for (char c = scanner.Peek(); char.IsLetter(c) || c == '_' || char.IsAsciiDigit(c); c = scanner.Peek())
        {
            value += c;
            scanner.Advance();
        }

        // Проверяем на совпадение с ключевым словом.
        if (Keywords.TryGetValue(value.ToLower(CultureInfo.InvariantCulture), out TokenType type))
        {
            return new Token(type);
        }

        // Возвращаем токен идентификатора.
        return new Token(TokenType.Identifier, new TokenValue(value));
    }

    /// <summary>
    ///  Распознаёт литерал числа по правилам:
    ///     number = digits_sequence, [ ".", digits_sequence ] ;
    ///     digits_sequence = digit { digit } ;
    ///     digit = "0" | "1" | ... | "9" .
    /// </summary>
    private Token ParseNumericLiteral()
    {
        double value = GetDigitValue(scanner.Peek());
        scanner.Advance();

        // Читаем целую часть числа.
        for (char c = scanner.Peek(); char.IsAsciiDigit(c); c = scanner.Peek())
        {
            value = value * 10 + GetDigitValue(c);
            scanner.Advance();
        }

        // Читаем дробную часть числа.
        if (scanner.Peek() == '.')
        {
            scanner.Advance();
            double factor = 0.1;
            for (char c = scanner.Peek(); char.IsAsciiDigit(c); c = scanner.Peek())
            {
                scanner.Advance();
                value += factor * GetDigitValue(c);
                factor *= 0.1;
            }

            return new Token(TokenType.FloatLiteral, new TokenValue(value));
        }

        return new Token(TokenType.IntLiteral, new TokenValue((int)value));

        // Локальная функция для получения числа из символа цифры.
        int GetDigitValue(char c)
        {
            return c - '0';
        }
    }

    /// <summary>
    ///  Распознаёт литерал строки по правилам:
    ///     string = quote, { string_element }, quote ;
    ///     quote = """ ;
    ///     string_element = char | escape_sequence ;
    ///     char = ^"'" .
    /// </summary>
    private Token ParseStringLiteral()
    {
        scanner.Advance();

        string contents = "";
        while (scanner.Peek() != StringBoundChar)
        {
            if (scanner.IsEnd())
            {
                // Ошибка: строка, не закрытая кавычкой.
                return new Token(TokenType.Error, new TokenValue(contents));
            }

            if (scanner.Peek() == '\r' && scanner.Peek(1) == '\n')
            {
                // Ошибка: перенос в строке без escape-последовательности
                return new Token(TokenType.Error, new TokenValue(contents));
            }

            // Проверяем наличие escape-последовательности.
            if (TryParseStringLiteralEscapeSequence(out char unescaped))
            {
                contents += unescaped;
            }
            else
            {
                contents += scanner.Peek();
                scanner.Advance();
            }
        }

        scanner.Advance();

        return new Token(TokenType.StringLiteral, new TokenValue(contents));
    }

    /// <summary>
    ///  Распознаёт escape-последовательности по правилам:
    ///     escape_sequence = "\", "\" | "\", "n" | "\", "r" | "\", "t" | "\", """ ;
    ///  Возвращает null при появлении неизвестных escape-последовательностей.
    /// </summary>
    private bool TryParseStringLiteralEscapeSequence(out char unescaped)
    {
        if (scanner.Peek() == '\\')
        {
            scanner.Advance();
            if (scanner.Peek() == '\"')
            {
                scanner.Advance();
                unescaped = '\"';
                return true;
            }

            if (scanner.Peek() == '\\')
            {
                scanner.Advance();
                unescaped = '\\';
                return true;
            }

            if (scanner.Peek() == 'n')
            {
                scanner.Advance();
                unescaped = '\n';
                return true;
            }

            if (scanner.Peek() == 'r')
            {
                scanner.Advance();
                unescaped = '\r';
                return true;
            }

            if (scanner.Peek() == 't')
            {
                scanner.Advance();
                unescaped = '\t';
                return true;
            }
        }

        unescaped = '\0';
        return false;
    }

    /// <summary>
    ///  Пропускает пробельные символы и комментарии, пока не встретит что-либо иное.
    /// </summary>
    private void SkipWhiteSpacesAndComments()
    {
        do
        {
            SkipWhiteSpaces();
        }
        while (TryParseSingleLineComment());
    }

    /// <summary>
    ///  Пропускает пробельные символы, пока не встретит иной символ.
    /// </summary>
    private void SkipWhiteSpaces()
    {
        while (char.IsWhiteSpace(scanner.Peek()))
        {
            scanner.Advance();
        }
    }

    /// <summary>
    ///  Пропускает однострочный комментарий в виде `#...текст`,
    ///  пока не встретит конец строки (его оставляет).
    /// </summary>
    private bool TryParseSingleLineComment()
    {
        if (scanner.Peek() == '#')
        {
            do
            {
                scanner.Advance();
            }
            while (scanner.Peek() != '\n' && scanner.Peek() != '\r' && scanner.Peek() != '\0');

            return true;
        }

        return false;
    }
}