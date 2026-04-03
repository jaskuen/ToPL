using Execution;

using Runtime;

namespace Parser.UnitTests;

public class ParseTopLevelGrammarTest
{
    private readonly Context context;
    private readonly FakeEnvironment environment;

    public ParseTopLevelGrammarTest()
    {
        context = new Context();
        environment = new FakeEnvironment([]);
    }

    [Theory]
    [MemberData(nameof(GetCanParseTopLevelStatementsData))]
    public void Can_parse_top_level_statements(string code, string variableName, RuntimeValue expected)
    {
        string program = $"""
                          главная воистину
                          {code}
                          возврати {variableName};
                          аминь
                          """;

        Parser parser = new(context, environment, program);
        parser.ParseProgram();

        // Проверяем вычисленный результат.
        RuntimeValue actual = environment.Results[0];
        Assert.Equal(expected, actual);
    }

    public static TheoryData<string, string, RuntimeValue> GetCanParseTopLevelStatementsData()
    {
        return new TheoryData<string, string, RuntimeValue>
        {
            // Объявление одной переменной
            { "int x = 5;", "x", new RuntimeValue("5") },

            // Объявление нескольких переменных, присваивание одной значения (другая = 0)
            { "int x, y = 2 + 3 * 4;", "y", new RuntimeValue("14") },

            // Множественное присваивание
            { "int a = 5, b = 10;", "a", new RuntimeValue("5") },
            { "int a = 5, b = 10;", "b", new RuntimeValue("10") },

            // Функция в качестве значения переменной
            { "float f = min(1.0, 5.0);", "f", new RuntimeValue("1") },

            // Неявное преобразование Int -> Double
            { "float x = 3;", "x", new RuntimeValue("3") },

            // Сравнение Int и Double
            { "bool x = 5 >= 3.0;", "x", new RuntimeValue("True") },

            // Вычисление по короткой схеме
            { "bool x = true || 3.0;", "x", new RuntimeValue("True") },
            { "bool x = false && 3.0;", "x", new RuntimeValue("False") },

            // Префиксный инкремент
            {
                """
                int counter = 4;
                write(++counter);
                """,
                "counter", new RuntimeValue("5")
            },

            // Префиксный декремент
            {
                """
                int counter = 4;
                write(--counter);
                """,
                "counter", new RuntimeValue("3")
            },

            // Префиксный инкремент в выражении
            {
                """
                int x = 4;
                int y = (++x) * 2;
                """,
                "y", new RuntimeValue("10")
            },

            // Постфиксный инкремент
            {
                """
                int x = 4;
                write(x++);
                """,
                "x", new RuntimeValue("4")
            },

            // Постфиксный декремент
            {
                """
                int x = 4;
                write(x--);
                """,
                "x", new RuntimeValue("4")
            },

            // Постфиксный инкремент в выражении
            {
                """
                int x = 4;
                int y = (x++) * 2;
                """,
                "y", new RuntimeValue("8")
            },
        };
    }

    [Theory]
    [MemberData(nameof(GetThrowsOnUndefinedVariableData))]
    public void Throws_on_undefined_variable(string code)
    {
        Parser parser = new(context, environment, code);
        Assert.Throws<UnexpectedLexemeException>(() => parser.ParseProgram());
    }

    public static TheoryData<string> GetThrowsOnUndefinedVariableData()
    {
        return new TheoryData<string>
        {
            "a = b;",       // Нет объявления переменных.
            "int a = b;",   // Одна из переменных не объявлена
        };
    }

    [Theory]
    [MemberData(nameof(GetThrowsOnSyntaxErrorsData))]
    public void Throws_on_syntax_errors(string code)
    {
        Parser parser = new(context, environment, code);
        Assert.ThrowsAny<Exception>(() => parser.ParseProgram());
    }

    public static TheoryData<string> GetThrowsOnSyntaxErrorsData()
    {
        return new TheoryData<string>
        {
            "int а = 5",        // Разбор объявления переменной без конца инструкции
            "int = 5;",         // Разбор объявления переменной с пустым идентификатором
            "int x;",           // Разбор объявления переменной с пустым выражением
            "int 123 = 321",    // Разбор объявления переменной с неправильным идентификатором
        };
    }

    [Theory]
    [MemberData(nameof(GetThrowsOnTypeErrorsData))]
    public void Throws_on_incorrect_types(string code)
    {
        Parser parser = new(context, environment, code);
        Assert.ThrowsAny<Exception>(() => parser.ParseProgram());
    }

    public static TheoryData<string> GetThrowsOnTypeErrorsData()
    {
        return new TheoryData<string>
        {
            "int а = 5.0;", // Double -> Int
            "int а = \"5\";", // String -> Int
            "int а = false;", // Boolean -> Int
            "float а = \"5.0\";", // String -> Double
            "float а = false;", // Boolean -> Double
            "bool а = 3 >= \"2\";", // Сравнение Int и String
            "bool а = 3 >= false;", // Сравнение Int и Boolean
            "bool ",
        };
    }
}