namespace Grammar.UnitTests;

public class GrammarTests
{
    public static TheoryData<string> ValidPrograms => new()
    {
        // Минимум
        "главная воистину аминь",

        // Простая программа с переменными и вызовом
        """
        главная
        воистину
            благодать а даруй 5 поклон
            возгласи("Здравствуй") поклон
        аминь
        """,

        // Объявления и арифметика
        """
        главная
        воистину
            # корректное объявление переменных
            благодать число даруй 42 поклон
            словеса текст даруй "мир" поклон
        аминь
        """,

        // Арифметические операции
        """
        главная
        воистину
            кадило радиус даруй 5.0 поклон
            кадило площадь даруй пи * радиус * радиус поклон
        аминь
        """,

        // Условный оператор
        """
        главная
        воистину
            верующий флаг даруй истинно поклон
            аще(флаг)
            воистину
                возгласи("Истина") поклон
            аминь
            илиже
            воистину
                возгласи("Ложь") поклон
            аминь
        аминь
        """,

        // Конструкция выбора (switch)
        """
        главная
        воистину
            словеса операция даруй "+" поклон
            изберется (операция)
            воистину
                егда "+"
                воистину
                    возгласи("Сложение") поклон
                    отрешити поклон
                аминь
                
                егда "-"
                воистину
                    возгласи("Вычитание") поклон
                    отрешити поклон
                аминь
                
                поеликуже
                воистину
                    возгласи("Неизвестная операция") поклон
                    отрешити поклон
                аминь
            аминь
        аминь
        """,

        // Встроенные функции
        """
        главная
        воистину
            кадило результат даруй преисподняя(3.7) поклон
            возврати результат поклон
        аминь
        """,
        """
        главная
        воистину
            кадило результат даруй небеса(2.3) поклон
            возврати результат поклон
        аминь
        """,
        """
        главная
        воистину
            кадило результат даруй колобок(4.5) поклон
            возврати результат поклон
        аминь
        """,
        """
        главная
        воистину
            кадило результат даруй синус(пи/2) поклон
            возврати результат поклон
        аминь
        """
    };

    [Theory]
    [MemberData(nameof(ValidPrograms))]
    public void Validate_ValidProgram_HasNoErrors(string sourceCode)
    {
        // Arrange
        LanguageValidator validator = new();

        // Act
        bool isValid = validator.Validate(sourceCode);

        // Assert
        Assert.True(isValid, $"Ошибки: {string.Join(", ", validator.Errors)}");
        Assert.Empty(validator.Errors);
    }

    public static TheoryData<string> InvalidProgramsMissingStructure => new()
    {
        "главная воистину ", // отсутствует аминь
        "главная аминь", // отсутствует воистину
        "неизвестное_ключевое_слово", // неизвестное слово вне контекста
        "", // пустой ввод
        "воистину\nаминь", // без главная
        "главная\nаминь" // без воистину
    };

    [Theory]
    [MemberData(nameof(InvalidProgramsMissingStructure))]
    public void Validate_MissingStructure_ReturnsSyntaxError(string sourceCode)
    {
        // Arrange
        LanguageValidator validator = new();

        // Act
        bool isValid = validator.Validate(sourceCode);

        // Assert
        Assert.False(isValid, "Ожидался невалидный код, но валидатор вернул true");
        Assert.NotEmpty(validator.Errors);
    }

    public static TheoryData<string> InvalidProgramsSyntax => new()
    {
        // Пропущен поклон
        """
        главная
        воистину
            благодать число даруй 42
            возгласи("Привет") поклон
        аминь
        """,

        // Непарные скобки
        """
        главная
        воистину
            аще(условие воистину
                возврати 0 поклон
            аминь
        аминь
        """,

        // Нарушение структуры switch
        """
        главная
        воистину
            изберется операция
            воистину
                егда "+"
                    # ОШИБКА: тут должно быть воистину
                    возгласи("Сложение") поклон
                    возврати 0 поклон
            благодать х даруй 1 поклон
        аминь
        """
    };

    [Theory]
    [MemberData(nameof(InvalidProgramsSyntax))]
    public void Validate_SyntaxError_ReturnsAtLeastOneError(string sourceCode)
    {
        // Arrange
        LanguageValidator validator = new();

        // Act
        bool isValid = validator.Validate(sourceCode);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(validator.Errors);
    }

    public static TheoryData<string> InvalidProgramsLexical => new()
    {
        // Незакрытая строка с переносом
        """
        главная
        воистину
            словеса текст даруй "начало
            конец" поклон
        аминь
        """,

        // Недопустимый символ в идентификаторе
        """
        главная
        воистину
            благодать число@1 даруй 10 поклон
        аминь
        """
    };

    [Theory]
    [MemberData(nameof(InvalidProgramsLexical))]
    public void Validate_LexicalError_ReturnsAtLeastOneError(string sourceCode)
    {
        // Arrange
        LanguageValidator validator = new();

        // Act
        bool isValid = validator.Validate(sourceCode);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(validator.Errors);
    }

    private static TheoryData<string> InvalidExpressions => new()
    {
        "3 + * 5", // некорректный оператор
        "умалю", // декремент без операнда
        "преисподняя(,)", // некорректный вызов
        "синус()" // отсутствует аргумент
    };

    public static TheoryData<string> InvalidExpressionPrograms => BuildInvalidExpressionPrograms();

    private static TheoryData<string> BuildInvalidExpressionPrograms()
    {
        TheoryData<string> data = new();
        foreach (object[]? expr in InvalidExpressions)
        {
            data.Add($"""
                      главная
                      воистину
                          благодать результат даруй {expr} поклон
                      аминь
                      """);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(InvalidExpressionPrograms))]
    public void Validate_InvalidExpressionInProgram_ReturnsError(string sourceCode)
    {
        // Arrange
        LanguageValidator validator = new();

        // Act
        bool isValid = validator.Validate(sourceCode);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(validator.Errors);
    }

    public static TheoryData<string> SumNumbersProgram => new()
    {
        """
            главная
        воистину
            # объявление и инициализация чисел
            благодать число1 даруй 0 поклон
            благодать число2 даруй 0 поклон

            # ввод чисел
            возгласи("Введите первое число: ") поклон
            внемли(число1) поклон

            возгласи("Введите второе число: ") поклон
            внемли(число2) поклон

            # вычисление суммы
            благодать сумма даруй число1 + число2 поклон

            # вывод суммы
            возгласи("Сумма: ", сумма) поклон
        аминь
        """
    };

    [Theory]
    [MemberData(nameof(SumNumbersProgram))]
    public void Validate_SumTwoNumbersProgram_HasNoErrors(string sourceCode)
    {
        // Arrange
        LanguageValidator validator = new();

        // Act
        bool isValid = validator.Validate(sourceCode);

        // Assert
        Assert.True(isValid, $"Ошибки: {string.Join(", ", validator.Errors)}");
        Assert.Empty(validator.Errors);
    }

    public static TheoryData<string> CircleSquareProgram => new()
    {
        """
        главная
        воистину
            # обявление радиуса - число с плавающей точкой 
            кадило радиус даруй 0.0 поклон

            # ввод радиуса
            возгласи("Введи радиус круга: ") поклон
            внемли(радиус) поклон

            # вычисление площади круга
            # пи - встроенное число = 3.141592
            кадило площадьКруга даруй пи * радиус * радиус поклон

            возгласи("Площадь круга = ", площадьКруга) поклон
        аминь
        """
    };

    [Theory]
    [MemberData(nameof(CircleSquareProgram))]
    public void Validate_CircleSquareProgram_HasNoErrors(string sourceCode)
    {
        // Arrange
        LanguageValidator validator = new();

        // Act
        bool isValid = validator.Validate(sourceCode);

        // Assert
        Assert.True(isValid, $"Ошибки: {string.Join(", ", validator.Errors)}");
        Assert.Empty(validator.Errors);
    }

    public static TheoryData<string> MilesToKilometersProgram => new()
    {
        """
        главная
        воистину
            # объявление переменных
            кадило km, miles, mileToKm поклон
            mileToKm даруй 1.609344 поклон

            возгласи("Введите расстояние в милях: ") поклон
            внемли(miles) поклон

            km даруй miles * mileToKm поклон
            возгласи("Расстояние в милях ", miles, " = ", km, " км") поклон
        аминь
        """
    };

    [Theory]
    [MemberData(nameof(MilesToKilometersProgram))]
    public void Validate_MilesToKilometersProgram_HasNoErrors(string sourceCode)
    {
        // Arrange
        LanguageValidator validator = new();

        // Act
        bool isValid = validator.Validate(sourceCode);

        // Assert
        Assert.True(isValid, $"Ошибки: {string.Join(", ", validator.Errors)}");
        Assert.Empty(validator.Errors);
    }

    public static TheoryData<string> MultipleTableProgram => new()
    {
        """
        главная
        воистину
            благодать N даруй 0 поклон
            возгласи("Введите N: ") поклон
            внемли(N) поклон

            # Объявляем i ДО цикла
            благодать i даруй 0 поклон

            # for (assign; expr; assign)
            повторити (i даруй 1, i меньше N, i даруй i + 1)
            воистину
                благодать j даруй 1 поклон
                доколе (j меньше N)
                воистину
                    возгласи(i * j, "\t") поклон
                    # Заменили 'приумножу j' на присваивание
                    j даруй j + 1 поклон
                аминь

                возгласи("\n") поклон
            аминь
        аминь
        """
    };

    [Theory]
    [MemberData(nameof(MultipleTableProgram))]
    public void Validate_MultipleTableProgram_HasNoErrors(string sourceCode)
    {
        // Arrange
        LanguageValidator validator = new();

        // Act
        bool isValid = validator.Validate(sourceCode);

        // Assert
        Assert.True(isValid, $"Ошибки: {string.Join(", ", validator.Errors)}");
        Assert.Empty(validator.Errors);
    }

    public static TheoryData<string> SqrtProgram => new()
    {
        """
        кадило fabs(кадило n)
        воистину
            аще (n малый 0)
            воистину
                возврати -n поклон
            аминь

            возврати n поклон
        аминь

        кадило Sqrt(кадило n, кадило epsilon)
        воистину
            аще (n малый 0)
            воистину
                возгласи("Err") поклон
                возврати -1 поклон
            аминь

            аще (n яко 0)
            воистину
                возврати 0 поклон
            аминь

            кадило x даруй n поклон
            кадило delta даруй epsilon + 1 поклон

            доколе(delta велий epsilon)
            воистину
                кадило nextX даруй 0.5 * (x + n / x) поклон
                delta даруй fabs(nextX - x) поклон
                x даруй nextX поклон
            аминь

            возврати x поклон
        аминь

        главная
        воистину
            кадило X поклон
            возгласи("Введите X: ") поклон
            внемли(X) поклон

            возгласи("Sqrt(X) = ", Sqrt(X, 0.0001)) поклон
        аминь
        """
    };

    [Theory]
    [MemberData(nameof(SqrtProgram))]
    public void Validate_SqrtProgram_HasNoErrors(string sourceCode)
    {
        // Arrange
        LanguageValidator validator = new();

        // Act
        bool isValid = validator.Validate(sourceCode);

        // Assert
        Assert.True(isValid, $"Ошибки: {string.Join(", ", validator.Errors)}");
        Assert.Empty(validator.Errors);
    }

    public static TheoryData<string> QuadraticEquationProgram => new()
    {
        """
        кадило fabs(кадило n)
        воистину
            аще (n малый 0) воистину возврати -n поклон аминь
            возврати n поклон
        аминь

        кадило Sqrt(кадило n, кадило epsilon)
        воистину
            аще (n малый 0) воистину возврати -1 поклон аминь
            аще (n яко 0) воистину возврати 0 поклон аминь

            кадило x даруй n поклон
            кадило delta даруй epsilon + 1 поклон

            доколе(delta велий epsilon)
            воистину
                кадило nextX даруй 0.5 * (x + n / x) поклон
                delta даруй fabs(nextX - x) поклон
                x даруй nextX поклон
            аминь
            возврати x поклон
        аминь

        ничтоже QuadraticEquation(кадило a, кадило b, кадило c)
        воистину
            кадило D даруй b * b - 4 * a * c поклон
            благодать количествоКорней даруй 0 поклон

            аще (D велий 0)
            воистину
                количествоКорней даруй 2 поклон
            аминь
            илиже
            воистину
                аще (D яко 0)
                воистину
                    количествоКорней даруй 1 поклон
                аминь
                илиже
                воистину
                    количествоКорней даруй 0 поклон
                аминь
            аминь

            возгласи("Кол-во корней: ", количествоКорней) поклон

            изберется (количествоКорней)
            воистину
                егда 0
                воистину
                    возгласи("Нет корней") поклон
                    отрешити поклон
                аминь
                
                егда 1
                воистину
                    кадило x даруй -b / (2 * a) поклон
                    возгласи("x = ", x) поклон
                    отрешити поклон
                аминь
                
                егда 2
                воистину
                    кадило кореньD даруй Sqrt(D, 0.00001) поклон
                    кадило x1 даруй (-b + кореньD) / (2 * a) поклон
                    кадило x2 даруй (-b - кореньD) / (2 * a) поклон
                    возгласи("x1 = ", x1) поклон
                    возгласи("x2 = ", x2) поклон
                    отрешити поклон
                аминь
                
                поеликуже
                воистину
                    возгласи("Error") поклон
                    отрешити поклон
                аминь
            аминь
        аминь

        главная
        воистину
            кадило a, b, c поклон
            возгласи("Input: ") поклон
            внемли(a, b, c) поклон

            QuadraticEquation(a, b, c) поклон
        аминь
        """
    };

    [Theory]
    [MemberData(nameof(QuadraticEquationProgram))]
    public void Validate_QuadraticEquationProgram_HasNoErrors(string sourceCode)
    {
        // Arrange
        LanguageValidator validator = new();

        // Act
        bool isValid = validator.Validate(sourceCode);

        // Assert
        Assert.True(isValid, $"Ошибки: {string.Join(", ", validator.Errors)}");
        Assert.Empty(validator.Errors);
    }
}