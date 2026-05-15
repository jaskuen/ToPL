using TestLibrary.Helpers;

namespace TestLibrary.Helpers;

/// <summary>
/// Запускает исполняемое .NET приложение — консольную программу.
/// Это должна быть сборка (Assembly), содержащая класс Program с методом Main.
/// </summary>
public static class DotnetConsoleProgramRunner
{
    /// <summary>
    /// Запускает консольную программу с записью переданного ввода в stdin,
    ///  возвращает её вывод в stdout.
    /// Бросает исключение при ненулевом коде возврата.
    /// </summary>
    public static async Task<string> RunAndReadOutputWithCheck(
        string executablePath, string stdin, CancellationToken ct
    )
    {
        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException($"Executable file not found: {executablePath}");
        }

        string workingDirectory = Path.GetDirectoryName(executablePath)!;
        List<string> command =
        [
            "dotnet",
            "exec",
            executablePath,
        ];

        ConsoleSubprocessRunner runner = new(
            workingDirectory: workingDirectory,
            stdin: stdin
        );

        await runner.Run(command, ct);
        runner.ThrowOnNonZeroExitCode();

        return runner.Stdout;
    }

    /// <summary>
    /// Запускает консольную программу с записью переданного ввода в stdin,
    ///  возвращает её код возврата и вывод в stdout.
    /// </summary>
    public static async Task<(int ExitCode, string Stdout)> RunAndReadOutput(
        string executablePath, string stdin, CancellationToken ct
    )
    {
        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException($"Executable file not found: {executablePath}");
        }

        string workingDirectory = Path.GetDirectoryName(executablePath)!;
        List<string> command =
        [
            "dotnet",
            "exec",
            executablePath,
        ];

        ConsoleSubprocessRunner runner = new(
            workingDirectory: workingDirectory,
            stdin: stdin
        );

        await runner.Run(command, ct);

        return (runner.ExitCode, runner.Stdout);
    }
}