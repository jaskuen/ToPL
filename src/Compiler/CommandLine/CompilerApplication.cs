using System.CommandLine;

namespace Compiler.CommandLine;

/// <summary>
/// Представляет консольное приложение для компилятора языка W.
/// </summary>
public static class CompilerApplication
{
    public static int Run(
        string[] args,
        TextWriter stdoutWriter,
        TextWriter stderrWriter
    )
    {
        RootCommand command = RootCommandFactory.Create();
        InvocationConfiguration configuration = new()
        {
            Output = stdoutWriter, Error = stderrWriter,
        };
        return command.Parse(args).Invoke(configuration);
    }
}