namespace TestLibrary.Helpers;

/// <summary>
///  Представляет временный файл, создаваемый на время работы теста.
/// </summary>
public sealed class TempFile : IDisposable
{
    private const int MaxCreateAttempts = 5;

    private TempFile(string path)
    {
        Path = path;
    }

    /// <summary>
    /// Возвращает путь к временному файлу.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Создаёт пустой временный файл с гарантированно уникальным именем.
    /// </summary>
    public static TempFile CreateEmpty(string prefix, string suffix)
    {
        for (int i = 0; i < MaxCreateAttempts; ++i)
        {
            string path = BuildRandomFileName(prefix, suffix);
            if (TryCreateNewFile(path))
            {
                return new TempFile(path);
            }
        }

        throw new InvalidOperationException(
            $"Failed to create temporary file with prefix {prefix} and suffix {suffix}"
        );
    }

    public void Dispose()
    {
        File.Delete(Path);
    }

    private static string BuildRandomFileName(string prefix, string suffix)
    {
        string random = System.IO.Path.GetRandomFileName();
        string name = prefix + random[..^4] + suffix;

        return System.IO.Path.Combine(System.IO.Path.GetTempPath(), name);
    }

    /// <summary>
    /// Создаёт новый файл, если он ещё не существует.
    /// </summary>
    /// <returns>Возвращает true, если файл удалось создать.</returns>
    private static bool TryCreateNewFile(string path)
    {
        try
        {
            using (new FileStream(
                       path,
                       FileMode.CreateNew,
                       FileAccess.ReadWrite,
                       FileShare.None))
            {
            }

            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }
}