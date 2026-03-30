using Parser;

namespace Interpreter;

public static class Program
{
    public static int Main(string[] args)
    {
        // Проверяем, что передан ровно один аргумент - путь к файлу
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: PsKaleidoscope.Interpreter <file-path>");
            return 1;
        }

        string filePath = args[0];

        try
        {
            // Проверяем существование файла
            if (!File.Exists(filePath))
            {
                Console.Error.WriteLine($"Error: File '{filePath}' not found.");
                return 1;
            }

            // Читаем исходный код целиком
            string sourceCode = File.ReadAllText(filePath);

            // Выполняем программу
            SaintInterpreter interpreter = new SaintInterpreter();
            interpreter.Execute(sourceCode);

            return 0;
        }
        catch (UnexpectedLexemeException ex)
        {
            Console.Error.WriteLine($"Parse error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}