using System.Diagnostics;
using System.Text;

namespace TestLibrary.Helpers;

public sealed class ConsoleSubprocessRunner
{
    private readonly string workingDirectory;
    private readonly string? stdin;
    private bool runCalled;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleSubprocessRunner"/> class.
    /// Создаёт экземпляр класса.
    /// </summary>
    /// <param name="workingDirectory">Рабочий каталог запускаемого процесса.</param>
    /// <param name="stdin">Ввод, который будет записан в stdin запускаемого процесса.</param>
    public ConsoleSubprocessRunner(
        string workingDirectory,
        string? stdin = null
    )
    {
        this.workingDirectory = workingDirectory;
        this.stdin = stdin;

        ExitCode = -1;
        Stdout = string.Empty;
        Stderr = string.Empty;
    }

    public int ExitCode { get; private set; }

    public string Stdout { get; private set; }

    public string Stderr { get; private set; }

    /// <summary>
    /// Запускает консольную программу в дочернем процессе и ожидает её завершения.
    /// </summary>
    public async Task Run(
        List<string> command,
        CancellationToken ct
    )
    {
        if (runCalled)
        {
            throw new InvalidOperationException("Cannot run process twice using this method");
        }

        runCalled = true;

        // Запускаем процесс.
        using Process process = new();
        process.StartInfo = CreateStartInfo(command, workingDirectory, redirectStdin: stdin != null);
        process.Start();
        ct.ThrowIfCancellationRequested();

        // Записываем данные в stdin
        if (stdin != null)
        {
            await process.StandardInput.WriteAsync(stdin);
            process.StandardInput.Close();
        }

        ct.ThrowIfCancellationRequested();

        // Начинаем чтение stdout/stderr и затем ожидаем завершения процесса.
        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        Task<string> stderrTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        // Сохраняем код возврата.
        ExitCode = process.ExitCode;

        // Ожидаем завершения чтения stdout/stderr.
        Stdout = await stdoutTask;
        Stderr = await stderrTask;
    }

    /// <summary>
    /// Бросает исключение при ненулевом коде возврата ранее запущенного процесса.
    /// </summary>
    public void ThrowOnNonZeroExitCode()
    {
        if (!runCalled)
        {
            throw new InvalidOperationException("Cannot check exit code before running process");
        }

        if (ExitCode != 0)
        {
            StringBuilder sb = new($"Process exited with code {ExitCode}");
            if (!string.IsNullOrEmpty(Stdout))
            {
                sb.AppendLine();
                sb.AppendLine("Stdout:");
                sb.Append(Stdout);
            }

            if (!string.IsNullOrEmpty(Stderr))
            {
                sb.AppendLine();
                sb.AppendLine("Stderr:");
                sb.Append(Stderr);
            }

            throw new Exception(sb.ToString());
        }
    }

    /// <summary>
    /// Создаёт параметры запуска консольной программы с перенаправлением stdout/stderr
    ///  (которые будут прочитаны этим же классом).
    /// </summary>
    private static ProcessStartInfo CreateStartInfo(List<string> command, string workingDirectory, bool redirectStdin)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = command[0],
            RedirectStandardInput = redirectStdin,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            ErrorDialog = false,
            WorkingDirectory = workingDirectory,
        };
        foreach (string argument in command.Skip(1))
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }
}