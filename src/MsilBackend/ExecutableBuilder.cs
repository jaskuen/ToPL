using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

using static System.IO.UnixFileMode;

namespace MsilBackend;

public class ExecutableBuilder
{
    private readonly string executablePath;
    private readonly PersistedAssemblyBuilder assemblyBuilder;
    private readonly ModuleBuilder moduleBuilder;

    public ExecutableBuilder(string executablePath)
    {
        this.executablePath = executablePath;
        AssemblyName assemblyName = new(Path.GetFileNameWithoutExtension(executablePath));

        assemblyBuilder = new PersistedAssemblyBuilder(
            assemblyName,
            coreAssembly: typeof(object).Assembly
        );

        moduleBuilder = assemblyBuilder.DefineDynamicModule(Path.GetFileName(executablePath));
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

        // Сохраняем исполняемый файл и задаём ему UNIX-права на исполнение.
        CreateExecutableFile(executablePath, peBuilder);
        SetExecutePermissions(executablePath);
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
}