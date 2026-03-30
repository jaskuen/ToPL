using System.Text;
using Xunit.Abstractions;

namespace Lexer.UnitTests;

public class LexicalStatsTest
{
    [Fact]
    public void Can_analyze_file_correctly()
    {
        string program = """
                         благодать главная() 
                         воистину
                             возгласи("Внемли, чадо, введи первое число: ") поклон
                             благодать а даруй 0 поклон
                             внемли(а) поклон
                             
                             возгласи("Теперь второе число введи: ") поклон
                             благодать б даруй 0 поклон
                             внемли(б) поклон
                             
                             благодать сумма даруй а + б поклон
                             возгласи("Чиселко ", а, " да ", б, " дадут вместе ", сумма) поклон
                         аминь
                         """;

        string path = Path.GetTempFileName();
        File.WriteAllText(path, program, Encoding.UTF8);

        string expected = """
                           keywords: 9
                           identifier: 11
                           number literals: 2
                           string literals: 5
                           operators: 4
                           other lexemes: 27
                           """;

        Assert.Equal(expected, LexicalStats.CollectFromFile(path));
    }
}