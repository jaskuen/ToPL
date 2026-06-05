using System.Text;

using Compiler.CommandLine;

using Xunit.Sdk;

namespace Compiler.Specs.Drivers;

public sealed class CompilerResult
{
    public int ExitCode { get; set; }

    public string StdOut { get; set; } = string.Empty;

    public string StdErr { get; set; } = string.Empty;
}

public class CompilerTestDriver
{
    public void RunCompiler(string inputPath, string outputPath)
    {
        CompilerResult result = RunCompilerWithResult(inputPath, outputPath);

        if (result.ExitCode != 0)
        {
            StringBuilder sb = new();
            sb.Append("Compilation failed with exit code ");
            sb.Append(result.ExitCode);

            if (result.StdOut != string.Empty)
            {
                sb.AppendLine();
                sb.AppendLine("Compiler output:");
                sb.Append(result.StdOut);
            }

            if (result.StdErr != string.Empty)
            {
                sb.AppendLine();
                sb.AppendLine("Compiler errors:");
                sb.Append(result.StdErr);
            }

            throw FailException.ForFailure(sb.ToString());
        }
    }

    public CompilerResult RunCompilerWithResult(string inputPath, string outputPath)
    {
        StringWriter stdoutWriter = new();
        StringWriter stderrWriter = new();

        int exitCode = CompilerApplication.Run(
            args:
            [
                inputPath,
                outputPath,
            ],
            stdoutWriter: stdoutWriter,
            stderrWriter: stderrWriter
        );

        return new CompilerResult
        {
            ExitCode = exitCode,
            StdOut = stdoutWriter.ToString(),
            StdErr = stderrWriter.ToString(),
        };
    }
}