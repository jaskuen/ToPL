using System.Text;
using Xunit.Abstractions;

namespace Lexer.UnitTests;

public class LexicalStatsTest
{
    [Fact]
    public void Can_analyze_file_correctly()
    {
        string program = """
                         void main()
                         {
                            write("Введи первое число: ");
                            int a = 0;
                            read(a);

                            write("Введи второе число: ");
                            int b = 0;
                            read(b);

                            int summ = a + b;
                            write("Ответ: ", a, " + ", b, " = ", summ);
                         }
                         """;

        string path = Path.GetTempFileName();
        File.WriteAllText(path, program, Encoding.UTF8);

        string expected = """
                          keywords: 10
                          identifiers: 10
                          int literals: 2
                          float literals: 0
                          string literals: 5
                          bool literals: 0
                          operators: 4
                          punctuations: 27
                          comments: 0
                          errors: 0
                          """;

        Assert.Equal(expected, LexicalStats.CollectFromFile(path));
    }
}