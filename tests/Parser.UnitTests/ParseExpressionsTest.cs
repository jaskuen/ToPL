using Execution;
using Runtime;

namespace Parser.UnitTests;

public class ParseExpressionsTest
{
    private readonly Context context;
    private readonly FakeEnvironment environment;

    public ParseExpressionsTest()
    {
        context = new Context();
        environment = new FakeEnvironment([]);
    }

    [Theory]
    [MemberData(nameof(GetParseTestData))]
    public void Can_parse_expressions(string expression, object expectedValue)
    {
        string program = $"""
                          главная воистину
                          возврати {expression} поклон
                          аминь
                          """;

        Parser parser = new Parser(context, environment, program);
        parser.ParseProgram();

        Assert.Single(environment.Results);
        RuntimeValue result = environment.Results[0];

        switch (expectedValue)
        {
            case bool boolValue:
                Assert.True(
                    result.ToBoolean() == boolValue,
                    $"Expected boolean {expectedValue}, but got {result.ToBoolean()}");
                break;
            case double doubleValue:
                Assert.True(
                    Math.Abs(result.ToDouble() - doubleValue) < 0.001,
                    $"Expected double {expectedValue}, but got {result.ToDouble()}");
                break;
            case int intValue:
                Assert.True(
                    result.ToInt() == intValue,
                    $"Expected int {expectedValue}, but got {result.ToInt()}");
                break;
            case string stringValue:
                Assert.True(
                    result.ToString() == stringValue,
                    $"Expected string {expectedValue}, but got {result}");
                break;
            default:
                Assert.Fail($"Unsupported expected type: {expectedValue.GetType()}");
                break;
        }
    }

    public static TheoryData<string, object> GetParseTestData()
    {
        return new TheoryData<string, object>
        {
            // Литералы и константы
            { "12345", 12345.0 },
            { "3.14159", 3.14159 },
            { "0.5", 0.5 },
            { "истинно", "True" },
            { "лукаво", "False" },
            { "\"Привет, мир!\"", "Привет, мир!" },
            { "\"\\n\\t\\r\\\"\\\\\"", "\n\t\r\"\\" },
            { "пи", Math.PI },
            { "эйлер", Math.E },

            // Встроенные функции
            { "преисподняя(3.9)", 3.0 },
            { "небеса(4.1)", 5.0 },
            { "колобок(5.5)", 6.0 },
            { "синус(30 * (пи / 180))", 0.5 },
            { "1 + тангенс(0)", 1.0 },
            { "преисподняя(синус(15 * (пи / 180)))", 0.0 },
            { "кСловесам(5.1867)", "5.1867" },
            { "раздробити(\"Hello world!\", 6, 5)", "world" },
            { "кНевысокому(\"Hello World!\")", "hello world!" },
            { "верстыСловесные(\"12345678\")", 8 },
            { "кБлагодати(\"135\")", 135 },
            { "кКадилу(\"12.345\")", 12.345 },

            // Унарные операции
            { "-10", -10.0 },
            { "+5", 5.0 },
            { "не истинно", "False" },
            { "не (не истинно)", "True" },

            // Логические операторы и сравнения
            { "10 яко 5 * 2", "True" },
            { "5 негоже 10", "True" },
            { "10 паче 10", "True" },
            { "5 меньше 10", "True" },
            { "истинно и (1 яко 1)", "True" },
            { "лукаво или 10 паче 5", "True" },

            // Приоритеты и ассоциативность
            { "2 + 3 * 4", 14.0 },
            { "-5 * 2", -10.0 },
            { "10 + -3", 7.0 },
            { "10 - 2 паче 5", "True" },
            { "истинно и 5 паче 2", "True" },
            { "лукаво или истинно и лукаво", "False" },
            { "(2 + 3) * 4", 20.0 },
            { "-(пи + 1) * небеса(1.5)", -8.283185307179586 },
        };
    }
}