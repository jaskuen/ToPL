using System.Runtime.InteropServices;

namespace TestLibrary.Helpers;

/// <summary>
/// Выполняет поиск рантайма .NET в текущей системе.
/// </summary>
public static class RuntimeFinder
{
    /// <summary>
    /// Имя рантайма для запуска консольных приложений.
    /// Такой рантайм является частью фреймворка .NET и служит для запуска простых приложений, не
    ///   являющихся ни сервисами на ASP.NET Core, ни десктопными приложениями для Windows.
    /// </summary>
    private const string AppRuntimeName = "Microsoft.NETCore.App";

    /// <summary>
    /// Возвращает шаблон пути к базовым библиотекам фреймворка .NET,
    ///  используемых для консольных приложений.
    /// Пример: "/usr/lib/dotnet/shared/Microsoft.NETCore.App/10.0.1/*.dll".
    /// </summary>
    /// <remarks>
    /// Используется версия .NET, на которой запущен текущий процесс.
    /// </remarks>
    public static string GetRuntimeLibrariesPathPattern()
    {
        // Получаем версию текущего рантайма.
        string runtimeVersion = Environment.Version.ToString();

        // Для каждого возможного корневого каталога .NET формируем пути к каталогу с библиотеками фреймворка .NET.
        List<string> candidates = GetDotnetRootPaths()
            .ConvertAll(root => Path.Combine(root, "shared", AppRuntimeName, runtimeVersion));

        string? libraryDir = candidates.FirstOrDefault(Directory.Exists);
        if (string.IsNullOrEmpty(libraryDir))
        {
            throw new NotSupportedException(
                $"Cannot find .NET library directory for framework {runtimeVersion}, no one of paths exists: {string.Join(", ", candidates)}"
            );
        }

        // Формируем шаблон пути к библиотекам фреймворка .NET.
        return Path.Combine(libraryDir, "*.dll");
    }

    /// <summary>
    /// Получает список корневых каталогов .NET, существующих в текущей системе.
    /// </summary>
    private static List<string> GetDotnetRootPaths()
    {
        List<string> candidates = [];

        // Добавляем путь, указанный в переменной DOTNET_ROOT.
        string? customRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (!string.IsNullOrEmpty(customRoot))
        {
            candidates.Add(customRoot);
        }

        // Добавляем стандартные варианты корневого кататлога .NET в зависимости от системы.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet"));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            candidates.Add("/usr/local/share/dotnet");
        }
        else
        {
            candidates.Add("/usr/share/dotnet");
            candidates.Add("/usr/lib/dotnet");
        }

        // Устраняем дубликаты и несуществующие каталоги.
        List<string> results = candidates.Distinct().Where(Directory.Exists).ToList();
        if (results.Count == 0)
        {
            throw new NotSupportedException(
                "Cannot find .NET root directory, no one of paths exists: " + string.Join(", ", candidates)
            );
        }

        return results;
    }
}