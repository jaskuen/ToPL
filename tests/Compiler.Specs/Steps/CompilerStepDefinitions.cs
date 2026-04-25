using System.Text;

using Compiler.Specs.Drivers;

using Reqnroll;

using TestLibrary;
using TestLibrary.Helpers;

namespace Compiler.Specs.Steps;

[Binding]
public sealed class CompilerStepDefinitions : IDisposable
{
    private const int RunTimeoutSeconds = 20;
    private static readonly TimeSpan RunTimeout = TimeSpan.FromSeconds(RunTimeoutSeconds);
    private readonly StringBuilder programInput = new();

    private readonly CompilerTestDriver compilerTestDriver = new();
    private TempFile? compiledProgram;
    private string lastProgramOutput = string.Empty;
    private int lastProgramExitCode = -1;

    [Given(@"^я скомпилировал программу ""(.*)""$")]
    public async Task ПустьЯСкомпилировалПрограмму(string name)
    {
        // Получаем путь к файлу с исходным кодом.
        string path = Samples.GetSampleProgramPath(name);
        Assert.True(File.Exists(path), $"Source code file {path} does not exist");

        // Компилируем программу в исполняемый файл (это будет временный файл).
        compiledProgram ??= TempFile.CreateEmpty("program-", ".exe");
        compilerTestDriver.RunCompiler(path, compiledProgram.Path);

        string dllPath = Path.ChangeExtension(compiledProgram.Path, "dll");
        await DotnetIlVerifyRunner.Run(dllPath);
    }

    [When("я ввожу (.*)")]
    public void КогдаЯВвожу(string input)
    {
        programInput.Append(input);
    }

    [When(@"я ввожу текст:")]
    public void КогдаЯВвожуТекст(string input)
    {
        programInput.Append(input);
    }

    [When("^(?:я )?выполняю программу$")]
    public async Task КогдаВыполняюПрограмму()
    {
        // Проверяем, что программа была скомпилирована.
        Assert.NotNull(compiledProgram);
        string dllPath = Path.ChangeExtension(compiledProgram.Path, "dll");
        Assert.True(File.Exists(dllPath), $"Dll file {dllPath} does not exist");

        // Запускаем программу с таймаутом и сохраняем её вывод.
        CancellationTokenSource cts = new(RunTimeout);
        lastProgramOutput = await DotnetConsoleProgramRunner.RunAndReadOutputWithCheck(
            dllPath, programInput.ToString(), cts.Token
        );

        // Код возврата нулевой — иначе используемый метод запуска выбросил бы исключение.
        lastProgramExitCode = 0;
    }

    [When("^(?:я )?выполняю программу с перехватом ошибок$")]
    public async Task WhenЯВыполняюПрограммуСПерехватомОшибок()
    {
        // Проверяем, что программа была скомпилирована.
        Assert.NotNull(compiledProgram);
        string dllPath = Path.ChangeExtension(compiledProgram.Path, "dll");
        Assert.True(File.Exists(dllPath), $"Dll file {dllPath} does not exist");

        // Запускаем программу с таймаутом и сохраняем её вывод и код возврата.
        CancellationTokenSource cts = new(RunTimeout);
        (lastProgramExitCode, lastProgramOutput) = await DotnetConsoleProgramRunner.RunAndReadOutput(
            dllPath, programInput.ToString(), cts.Token
        );
    }

    [Then("^(?:я )?увижу вывод (.*)$")]
    public void ТогдаЯУвижуВывод(string expected)
    {
        Assert.Equal(
            ToUnixLineEnds(expected),
            ToUnixLineEnds(lastProgramOutput)
        );
    }

    [Then("^(?:я )?увижу вывод:$")]
    public void ТогдаЯУвижуВыводМногострочный(string expected)
    {
        Assert.Equal(
            ToUnixLineEnds(expected),
            ToUnixLineEnds(lastProgramOutput)
        );
    }

    [Then(@"^(?:я )?получу код возврата (\d+)$")]
    public void ТогдаЯПолучуКодВозврата(int exitCode)
    {
        Assert.Equal(exitCode, lastProgramExitCode);
    }

    public void Dispose()
    {
        compiledProgram?.Dispose();
    }

    private string ToUnixLineEnds(string text)
    {
        return text.Replace("\r\n", "\n");
    }
}