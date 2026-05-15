namespace TestLibrary.Helpers;

public static class DotnetIlVerifyRunner
{
    public static async Task Run(string executablePath, CancellationToken ct = default)
    {
        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException($"Executable file not found: {executablePath}");
        }

        string workingDirectory = Path.GetDirectoryName(executablePath)!;
        string librariesPathPattern = RuntimeFinder.GetRuntimeLibrariesPathPattern();

        List<string> command =
        [
            "ilverify",
            executablePath,
            "-r",
            librariesPathPattern,
        ];

        ConsoleSubprocessRunner runner = new(workingDirectory: workingDirectory);

        await runner.Run(command, ct);
        runner.ThrowOnNonZeroExitCode();
    }
}