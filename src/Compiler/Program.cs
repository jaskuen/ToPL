using Compiler.Drivers;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: Compiler <input.w> <output.exe>");
    return 1;
}

try
{
    CompilerDriver driver = new();
    driver.Compile(args[0], args[1]);
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
