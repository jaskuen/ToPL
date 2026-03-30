using Execution;

namespace Interpreter;

public class SaintInterpreter
{
    private readonly Context context;
    private readonly IEnvironment environment;

    public SaintInterpreter()
    {
        context = new Context();
        environment = new ConsoleEnvironment();
    }

    public SaintInterpreter(IEnvironment environment)
    {
        context = new Context();
        this.environment = environment;
    }

    /// <summary>
    /// Выполняет программу на языке Святоскрипте
    /// </summary>
    /// <param name="sourceCode">Исходный код программы</param>
    public void Execute(string sourceCode)
    {
        if (string.IsNullOrEmpty(sourceCode))
        {
            throw new ArgumentException("Source code cannot be null or empty", nameof(sourceCode));
        }

        // Создаем парсер и выполняем программу
        Parser.Parser parser = new Parser.Parser(context, environment, sourceCode);
        parser.ParseProgram();
    }
}