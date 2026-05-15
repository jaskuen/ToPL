using System.Globalization;

namespace Lexer;

public class Lexer
{
    private const char StringBoundChar = '\"';

    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        { ";", TokenType.Semicolon },
        { "if", TokenType.If },
        { "else", TokenType.Else },
        { "void", TokenType.Void },
        { "int", TokenType.Int },
        { "float", TokenType.Float },
        { "bool", TokenType.Bool },
        { "string", TokenType.String },
        { "while", TokenType.While },
        { "for", TokenType.For },
        { "return", TokenType.Return },
        { "break", TokenType.Break },
        { "continue", TokenType.Continue },
        { "equals", TokenType.Equal },
        { "not", TokenType.LogicalNot },
        { "or", TokenType.LogicalOr },
        { "and", TokenType.LogicalAnd },
        { "true", TokenType.True },
        { "false", TokenType.False },
        { "write", TokenType.Write },
        { "read", TokenType.Read },
        { "const", TokenType.Const },
        { "main",  TokenType.Main },
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
            case ';':
                scanner.Advance();
                return new Token(TokenType.Semicolon);
            case ',':
                scanner.Advance();
                return new Token(TokenType.Comma);
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
            case '{':
                scanner.Advance();
                return new Token(TokenType.OpenBrace);
            case '}':
                scanner.Advance();
                return new Token(TokenType.CloseBrace);
            case '!':
            case '>':
            case '<':
            case '|':
            case '&':
            case '=':
            case '+':
            case '-':
                return ParseOperand();
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

    private Token ParseOperand()
    {
        char c = scanner.Peek();
        scanner.Advance();
        switch (c)
        {
            case '!':
                if (scanner.Peek() != '=')
                {
                    return new Token(TokenType.LogicalNot);
                }

                scanner.Advance();
                return new Token(TokenType.NotEqual);

            case '>':
                if (scanner.Peek() != '=')
                {
                    return new Token(TokenType.GreaterThan);
                }

                scanner.Advance();
                return new Token(TokenType.GreaterThanOrEqual);

            case '<':
                if (scanner.Peek() != '=')
                {
                    return new Token(TokenType.LessThan);
                }

                scanner.Advance();
                return new Token(TokenType.LessThanOrEqual);
            case '|':
                if (scanner.Peek() == '|')
                {
                    scanner.Advance();
                    return new Token(TokenType.LogicalOr);
                }

                break;
            case '&':
                if (scanner.Peek() == '&')
                {
                    scanner.Advance();
                    return new Token(TokenType.LogicalAnd);
                }

                break;
            case '+':
                if (scanner.Peek() == '+')
                {
                    scanner.Advance();
                    return new Token(TokenType.Increment);
                }

                return new Token(TokenType.Plus);
            case '-':
                if (scanner.Peek() == '-')
                {
                    scanner.Advance();
                    return new Token(TokenType.Decrement);
                }

                return new Token(TokenType.Minus);
            case '=':
                if (scanner.Peek() == '=')
                {
                    scanner.Advance();
                    return new Token(TokenType.Equal);
                }

                return new Token(TokenType.Assignment);
        }

        return new Token(TokenType.Error, new TokenValue(c.ToString()));
    }

    /// <summary>
    ///  Распознаёт литерал числа по правилам:
    ///     number = digits_sequence, [ ".", digits_sequence ] ;
    ///     digits_sequence = digit { digit } ;
    ///     digit = "0" | "1" | ... | "9" .
    /// </summary>
    private Token ParseNumericLiteral()
    {
        float value = GetDigitValue(scanner.Peek());
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
            float factor = 0.1f;
            for (char c = scanner.Peek(); char.IsAsciiDigit(c); c = scanner.Peek())
            {
                scanner.Advance();
                value += factor * GetDigitValue(c);
                factor *= 0.1f;
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

            if (scanner.Peek() == '\r' ||
                (scanner.Peek() == '\r' && scanner.Peek(1) == '\n'))
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
        if (scanner.Peek() == '/' && scanner.Peek(1) == '/')
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
