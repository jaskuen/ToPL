using System.Reflection.Emit;
using Execution;
using MsilBackend;
using MsilCodegen;

namespace Compiler.Drivers;

public class CompilerDriver
{
    public void Compile(string inputPath, string outputPath)
    {
        string code = File.ReadAllText(inputPath);

        Parser.Parser parser = new(new Context(), new ConsoleEnvironment(), code);
        Ast.ProgramUnit program = parser.ParseProgramAst();

        ExecutableBuilder executableBuilder = new(outputPath);
        MsilCodegenPass codegenPass = new(executableBuilder.ModuleBuilder);
        MethodBuilder mainMethod = codegenPass.GenerateProgramCode(program);
        executableBuilder.Save(mainMethod);
    }
}
