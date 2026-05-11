using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

using Microsoft.NET.HostModel.AppHost;

using static System.IO.UnixFileMode;

namespace MsilBackend;

public class ExecutableBuilder
{
    private readonly string executablePath;
    private readonly string assemblyPath;
    private readonly string runtimeConfigPath;
    private readonly PersistedAssemblyBuilder assemblyBuilder;
    private readonly ModuleBuilder moduleBuilder;

    public ExecutableBuilder(string executablePath)
    {
        this.executablePath = executablePath;
        assemblyPath = Path.ChangeExtension(executablePath, "dll");
        runtimeConfigPath = Path.ChangeExtension(executablePath, "runtimeconfig.json");
        AssemblyName assemblyName = new(Path.GetFileNameWithoutExtension(assemblyPath));

        assemblyBuilder = new PersistedAssemblyBuilder(
            assemblyName,
            coreAssembly: typeof(object).Assembly
        );

        moduleBuilder = assemblyBuilder.DefineDynamicModule(Path.GetFileName(assemblyPath));
    }

    public ModuleBuilder ModuleBuilder => moduleBuilder;

    public void Save(MethodBuilder mainMethod)
    {
        // Генерируем метаданные. После этого токены методов становятся валидными.
        MetadataBuilder metadataBuilder = assemblyBuilder.GenerateMetadata(
            out BlobBuilder ilStream,
            out BlobBuilder fieldData
        );

        // Получаем объект для установки Main() как точки входа.
        // Можно вызывать только после генерации метаданных.
        MethodDefinitionHandle mainHandle = GetMethodDefinitionHandle(mainMethod);

        ManagedPEBuilder peBuilder = new(
            header: PEHeaderBuilder.CreateExecutableHeader(),
            metadataRootBuilder: new MetadataRootBuilder(metadataBuilder),
            ilStream: ilStream,
            mappedFieldData: fieldData,
            entryPoint: mainHandle
        );

        // Сохраняем управляемую сборку и создаём apphost для прямого запуска.
        CreateExecutableFile(assemblyPath, peBuilder);
        string appHostTemplate = FindAppHostTemplate();

        HostWriter.CreateAppHost(
            appHostSourceFilePath: appHostTemplate,
            appHostDestinationFilePath: executablePath,
            appBinaryFilePath: Path.GetFileName(assemblyPath),
            windowsGraphicalUserInterface: false,
            assemblyToCopyResorcesFrom: null
        );
        SetExecutePermissions(executablePath);

        // Генерируем *.runtimeconfig.json для запуска программы утилитой dotnet exec в Linux / Mac OS X.
        RuntimeConfigGenerator.SaveRuntimeConfig(runtimeConfigPath);
    }

    private static void CreateExecutableFile(
        string executablePath,
        ManagedPEBuilder peBuilder
    )
    {
        // Создаём исполняемый файл с указанной точкой входа.
        BlobBuilder peBlob = new();
        peBuilder.Serialize(peBlob);

        // Запись в исполняемый файл,
        using FileStream fileStream = new(executablePath, FileMode.Create, FileAccess.Write);
        peBlob.WriteContentTo(fileStream);
    }

    /// <summary>
    /// Получает объект, необходимый для установки метода Main() как точки входа исполняемого файла.
    /// </summary>
    private static MethodDefinitionHandle GetMethodDefinitionHandle(MethodBuilder mainMethod)
    {
        // Получаем токен метода Main().
        int token = mainMethod.MetadataToken;
        int rowNumber = token & 0x00FFFFFF;
        MethodDefinitionHandle mainHandle = MetadataTokens.MethodDefinitionHandle(rowNumber);
        return mainHandle;
    }

    /// <summary>
    /// Устанавливает UNIX-права на выполнение программы.
    /// </summary>
    private static void SetExecutePermissions(string executablePath)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Добавляем UNIX-права на выполнение для владельца, группы и остальных, то есть -rwxr-xr-x (755).
            File.SetUnixFileMode(
                executablePath,
                UserRead | UserWrite | UserExecute | GroupRead | GroupExecute | OtherRead | OtherExecute
            );
        }
    }

    /// <summary>
    /// Находит путь установки .NET.
    /// </summary>
    private static string FindAppHostTemplate()
    {
        string dotnetRoot =
            Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? GetDefaultDotnetRoot();

        if (string.IsNullOrEmpty(dotnetRoot))
        {
            return string.Empty;
        }

        string executableName =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "apphost.exe"
                : "apphost";

        string packsDirectory = Path.Combine(
            dotnetRoot,
            "packs");

        string hostPackDirectory = Directory
            .EnumerateDirectories(
                packsDirectory,
                "Microsoft.NETCore.App.Host.*")
            .OrderByDescending(x => x)
            .First();

        return Directory
            .EnumerateFiles(
                hostPackDirectory,
                executableName,
                SearchOption.AllDirectories)
            .First();
    }

    /// <summary>
    /// Возвращает стандартный путь установки .NET для текущей ОС.
    /// </summary>
    private static string GetDefaultDotnetRoot()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "dotnet");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "/usr/local/share/dotnet";
        }

        return "/usr/share/dotnet";
    }
}