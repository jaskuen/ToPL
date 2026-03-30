using System.Text;

namespace ExampleLib;

public static class FileUtil
{
    /// <summary>
    /// Сортирует строки в указанном файле.
    /// Перезаписывает файл, но не атомарно: ошибка ввода-вывода при записи приведёт к потере данных.
    /// </summary>
    public static void SortFileLines(string path)
    {
        // Читаем и сортируем строки файла.
        List<string> lines = File.ReadLines(path, Encoding.UTF8).ToList();
        lines.Sort();

        // Перезаписываем файл с нуля (режим Truncate).
        using FileStream file = File.Open(path, FileMode.Truncate, FileAccess.Write);
        for (int i = 0, iMax = lines.Count; i < iMax; ++i)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(lines[i]);
            file.Write(bytes);
            if (i != iMax - 1)
            {
                file.Write("\n"u8);
            }
        }
    }

    /// <summary>
    /// Добавляет номера строк (начиная с 1) в указанном файле.
    /// </summary>
    public static void AddLineNumbers(string path)
    {
        List<string> lines = File.ReadLines(path, Encoding.UTF8).ToList();

        using FileStream file = File.Open(path, FileMode.Truncate, FileAccess.Write);
        for (int i = 0, iMax = lines.Count; i < iMax; ++i)
        {
            byte[] bytes = Encoding.UTF8.GetBytes($"{i + 1}. {lines[i]}");
            file.Write(bytes);
            if (i != iMax - 1)
            {
                file.Write("\n"u8);
            }
        }
    }
}