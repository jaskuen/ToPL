using System.Text;

using Compiler.Specs.Drivers;

using Reqnroll;

using TestLibrary;
using TestLibrary.Helpers;

using Xunit;

namespace PsTiger.Tests.Compiler.Specs.Steps;

[Binding]
public sealed class CompilerStepDefinitions : IDisposable
{
    private const int RunTimeoutSeconds = 20;
    private static readonly TimeSpan RunTimeout = TimeSpan.FromSeconds(RunTimeoutSeconds);

    private readonly CompilerTestDriver _compilerTestDriver = new();
    private TempFile? _compiledProgram;

    private readonly StringBuilder _programInput = new();
    private string _lastProgramOutput = string.Empty;
    private int _lastProgramExitCode = -1;

    [Given(@"^я скомпилировал программу ""(.*)""$")]
    public async Task ПустьЯСкомпилировалПрограмму(string name)
    {
        // Получаем путь к файлу с исходным кодом.
        string path = Samples.GetSampleProgramPath(name);
        Assert.True(File.Exists(path), $"Source code file {path} does not exist");

        // Компилируем программу в исполняемый файл (это будет временный файл).
        _compiledProgram ??= TempFile.CreateEmpty("program-", ".exe");
        _compilerTestDriver.RunCompiler(path, _compiledProgram.Path);

        await DotnetIlVerifyRunner.Run(_compiledProgram.Path);
    }

    [When(@"я ввожу (.*)")]
    public void КогдаЯВвожу(string input)
    {
        _programInput.Append(input);
    }

    [When(@"я ввожу текст:")]
    public void КогдаЯВвожуТекст(string input)
    {
        _programInput.Append(input);
    }

    [When("^(?:я )?выполняю программу$")]
    public async Task КогдаВыполняюПрограмму()
    {
        // Проверяем, что программа была скомпилирована.
        Assert.NotNull(_compiledProgram);
        Assert.True(File.Exists(_compiledProgram.Path), $"Executable file {_compiledProgram.Path} does not exist");

        // Запускаем программу с таймаутом и сохраняем её вывод.
        CancellationTokenSource cts = new(RunTimeout);
        _lastProgramOutput = await DotnetConsoleProgramRunner.RunAndReadOutputWithCheck(
            _compiledProgram.Path, _programInput.ToString(), cts.Token
        );

        // Код возврата нулевой — иначе используемый метод запуска выбросил бы исключение.
        _lastProgramExitCode = 0;
    }

    [When("^(?:я )?выполняю программу с перехватом ошибок$")]
    public async Task WhenЯВыполняюПрограммуСПерехватомОшибок()
    {
        // Проверяем, что программа была скомпилирована.
        Assert.NotNull(_compiledProgram);
        Assert.True(File.Exists(_compiledProgram.Path), $"Executable file {_compiledProgram.Path} does not exist");

        // Запускаем программу с таймаутом и сохраняем её вывод и код возврата.
        CancellationTokenSource cts = new(RunTimeout);
        (_lastProgramExitCode, _lastProgramOutput) = await DotnetConsoleProgramRunner.RunAndReadOutput(
            _compiledProgram.Path, _programInput.ToString(), cts.Token
        );
    }

    [Then("^(?:я )?увижу вывод (.*)$")]
    public void ТогдаЯУвижуВывод(string expected)
    {
        Assert.Equal(
            ToUnixLineEnds(expected),
            ToUnixLineEnds(_lastProgramOutput)
        );
    }

    [Then("^(?:я )?увижу вывод:$")]
    public void ТогдаЯУвижуВыводМногострочный(string expected)
    {
        Assert.Equal(
            ToUnixLineEnds(expected),
            ToUnixLineEnds(_lastProgramOutput)
        );
    }

    [Then(@"^(?:я )?получу код возврата (\d+)$")]
    public void ТогдаЯПолучуКодВозврата(int exitCode)
    {
        Assert.Equal(exitCode, _lastProgramExitCode);
    }

    public void Dispose()
    {
        _compiledProgram?.Dispose();
    }

    private string ToUnixLineEnds(string text)
    {
        return text.Replace("\r\n", "\n");
    }
}