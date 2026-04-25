using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace MsilBackend;

public class ExecutableBuilder
{
    private readonly string _executablePath;
    private readonly string _assemblyPath;
    private readonly string _runtimeConfigPath;
    private readonly PersistedAssemblyBuilder _assemblyBuilder;
    private readonly ModuleBuilder _moduleBuilder;

    public ExecutableBuilder(string executablePath)
    {
        _executablePath = executablePath;
        _assemblyPath = Path.ChangeExtension(executablePath, "dll");
        _runtimeConfigPath = Path.ChangeExtension(executablePath, "runtimeconfig.json");
        AssemblyName assemblyName = new(Path.GetFileNameWithoutExtension(_assemblyPath));

        _assemblyBuilder = new PersistedAssemblyBuilder(
            assemblyName,
            coreAssembly: typeof(object).Assembly
        );

        _moduleBuilder = _assemblyBuilder.DefineDynamicModule(Path.GetFileName(_assemblyPath));
    }

    public ModuleBuilder ModuleBuilder => _moduleBuilder;

    public void Save(MethodBuilder mainMethod)
    {
        // Генерируем метаданные. После этого токены методов становятся валидными.
        MetadataBuilder metadataBuilder = _assemblyBuilder.GenerateMetadata(
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
        CreateExecutableFile(_assemblyPath, peBuilder);
        AppHostBuilder.CreateAppHost(_executablePath, Path.GetFileName(_assemblyPath));
        SetExecutePermissions(_executablePath);
        RuntimeConfigGenerator.SaveRuntimeConfig(_runtimeConfigPath);
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
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute
            );
        }
    }
}
