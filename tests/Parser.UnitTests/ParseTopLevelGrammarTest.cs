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
                          возврати {variableName} поклон
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
            { "благодать x даруй 5 поклон", "x", new RuntimeValue("5") },

            // Объявление нескольких переменных, присваивание одной значения (другая = 0)
            { "благодать x, y даруй 2 + 3 * 4 поклон", "y", new RuntimeValue("14") },

            // Множественное присваивание
            { "благодать a даруй 5, b даруй 10 поклон", "a", new RuntimeValue("5") },
            { "благодать a даруй 5, b даруй 10 поклон", "b", new RuntimeValue("10") },

            // Функция в качестве значения переменной
            { "кадило f даруй синус(пи/2) поклон", "f", new RuntimeValue("1") },

            // Неявное преобразование Int -> Double
            { "кадило x даруй 3 поклон", "x", new RuntimeValue("3") },

            // Сравнение Int и Double
            { "верующий x даруй 5 паче 3.0 поклон", "x", new RuntimeValue("True") },

            // Вычисление по короткой схеме
            { "верующий x даруй истинно или 3.0 поклон", "x", new RuntimeValue("True") },
            { "верующий x даруй лукаво и 3.0 поклон", "x", new RuntimeValue("False") },

            // Префиксный инкремент
            {
                """
                благодать счетчик даруй 4 поклон
                возгласи(приумножу счетчик) поклон
                """,
                "счетчик", new RuntimeValue("5")
            },

            // Префиксный декремент
            {
                """
                благодать счетчик даруй 4 поклон
                возгласи(умалю счетчик) поклон
                """,
                "счетчик", new RuntimeValue("3")
            },

            // Префиксный инкремент в выражении
            {
                """
                благодать x даруй 4 поклон
                благодать y даруй (приумножу x) * 2 поклон
                """,
                "y", new RuntimeValue("10")
            },

            // Постфиксный инкремент
            {
                """
                благодать x даруй 4 поклон
                возгласи(x приумножу) поклон
                """,
                "x", new RuntimeValue("4")
            },

            // Постфиксный декремент
            {
                """
                благодать x даруй 4 поклон
                возгласи(x умалю) поклон
                """,
                "x", new RuntimeValue("4")
            },

            // Постфиксный инкремент в выражении
            {
                """
                благодать x даруй 4 поклон
                благодать y даруй (x приумножу) * 2 поклон
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
            "а даруй б поклон", // Нет объявления переменных.
            "благодать а даруй б поклон", // Одна из переменных не объявлена
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
            "благодать а даруй 5", // Разбор объявления переменной без конца инструкции
            "благодать даруй 5 поклон", // Разбор объявления переменной с пустым идентификатором
            "благодать x поклон", // Разбор объявления переменной с пустым выражением
            "благодать 123 даруй 321", // Разбор объявления переменной с неправильным идентификатором
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
            "благодать а даруй 5.0 поклон", // Double -> Int
            "благодать а даруй \"5\" поклон", // String -> Int
            "благодать а даруй лукаво поклон", // Boolean -> Int
            "кадило а даруй \"5.0\" поклон", // String -> Double
            "кадило а даруй лукаво поклон", // Boolean -> Double
            "верующий а даруй 3 паче \"2\" поклон", // Сравнение Int и String
            "верующий а даруй 3 паче лукаво поклон", // Сравнение Int и Boolean
            "верующий ",
        };
    }
}