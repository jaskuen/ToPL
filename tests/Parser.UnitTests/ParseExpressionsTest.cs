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
        string program = $$"""
                          void main() {
                            return {{expression}} ;
                          }
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
            case float doubleValue:
                Assert.True(
                    Math.Abs(result.ToFloat() - doubleValue) < 0.001,
                    $"Expected double {expectedValue}, but got {result.ToFloat()}");
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
            { "12345", 12345 },
            { "3.14159", 3.14159f },
            { "0.5", 0.5f },
            { "true", "True" },
            { "false", "False" },
            { "\"Привет, мир!\"", "Привет, мир!" },
            { "\"\\n\\t\\r\\\"\\\\\"", "\n\t\r\"\\" },

            // Встроенные функции
            { "abs(-123.45)", 123.45f }, // abs должна вернуть тот же тип
            { "abs(-5)", 5 },
            { "round(4.5)", 5 },
            { "ceil(4.1)", 5 },
            { "floor(4.7)", 4 },
            { "min(3, 5)", 3 },         // В min/max параметры должны быть одного типа
            { "min(3.0, 5.0)", 3.0f },
            { "max(3, 5)", 5 },
            { "max(3.0, 5.0)", 5.0f },
            { """length("Hello, world!")""", 13 },
            { """substring("Hello, world!", 7, 5)""", "world" },

            // Унарные операции
            { "-10", -10 },
            { "-10.0", -10.0f },
            { "+5", 5 },
            { "+5.0", 5.0f },
            { "!true", "False" },
            { "not true", "False" },
            { "!(!true)", "True" },
            { "not (not true)", "True" },

            // Логические операторы и сравнения
            { "10 == 5 * 2", "True" },
            { "5 != 10", "True" },
            { "10 >= 10", "True" },
            { "5 < 10", "True" },
            { "true && (1 == 1)", "True" },
            { "true and (1 == 1)", "True" },
            { "false || 10 >= 5", "True" },
            { "false or 10 >= 5", "True" },

            // Приоритеты и ассоциативность
            { "2 + 3 * 4", 14 },
            { "-5 * 2", -10 },
            { "10 + -3", 7 },
            { "10 - 2 >= 5", "True" },
            { "true && 5 >= 2", "True" },
            { "false || true && false", "False" },
            { "(2 + 3) * 4", 20 },
            { "-(3 + 1) * ceil(1.5)", -8 },
        };
    }
}