using System.CommandLine;

using Compiler.Drivers;

public static class RootCommandFactory
{
    public static RootCommand Create()
    {
        RootCommand command = new(
            "Compiles W program into .NET executable"
        );

        Argument<string> inputPathArgument = new("input")
        {
            Description = "Path to W program source file", Arity = ArgumentArity.ExactlyOne,
        };
        command.Add(inputPathArgument);

        Argument<string> outputPathArgument = new("output")
        {
            Description = "Output path for generated .NET executable", Arity = ArgumentArity.ExactlyOne,
        };
        command.Add(outputPathArgument);

        command.SetAction((result) =>
        {
            string inputPath = result.GetRequiredValue(inputPathArgument);
            string outputPath = result.GetRequiredValue(outputPathArgument);

            return Compile(inputPath, outputPath);
        });

        return command;
    }

    private static int Compile(string inputPath, string outputPath)
    {
        CompilerDriver driver = new();
        driver.Compile(inputPath, outputPath);

        return 0;
    }
}