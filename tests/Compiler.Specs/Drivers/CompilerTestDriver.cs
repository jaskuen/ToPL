using System.Text;

using Compiler.CommandLine;

using Xunit.Sdk;

namespace Compiler.Specs.Drivers;

public class CompilerTestDriver
{
    public void RunCompiler(string inputPath, string outputPath)
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

        if (exitCode != 0)
        {
            StringBuilder sb = new();
            sb.Append("Compilation failed with exit code ");
            sb.Append(exitCode);

            string stdout = stdoutWriter.ToString();
            if (stdout != string.Empty)
            {
                sb.AppendLine();
                sb.AppendLine("Compiler output:");
                sb.Append(stdout);
            }

            string stderr = stderrWriter.ToString();
            if (stderr != string.Empty)
            {
                sb.AppendLine();
                sb.AppendLine("Compiler errors:");
                sb.Append(stderr);
            }

            throw FailException.ForFailure(sb.ToString());
        }
    }
}