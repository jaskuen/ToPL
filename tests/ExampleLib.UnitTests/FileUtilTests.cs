using ExampleLib.UnitTests.Helpers;

using Xunit;

namespace ExampleLib.UnitTests;

public class FileUtilTests
{
    [Fact]
    public void CanSortTextFile()
    {
        const string unsorted = """
                                Играют волны — ветер свищет,
                                И мачта гнется и скрыпит…
                                Увы! он счастия не ищет
                                И не от счастия бежит!
                                """;
        const string sorted = """
                              И мачта гнется и скрыпит…
                              И не от счастия бежит!
                              Играют волны — ветер свищет,
                              Увы! он счастия не ищет
                              """;

        using TempFile file = TempFile.Create(unsorted);
        FileUtil.SortFileLines(file.Path);

        string actual = File.ReadAllText(file.Path);
        Assert.Equal(sorted.Replace("\r\n", "\n"), actual);
    }

    [Fact]
    public void CanSortOneLineFile()
    {
        using TempFile file = TempFile.Create("Играют волны — ветер свищет,");
        FileUtil.SortFileLines(file.Path);

        string actual = File.ReadAllText(file.Path);
        Assert.Equal("Играют волны — ветер свищет,", actual);
    }

    [Fact]
    public void CanSortEmptyFile()
    {
        using TempFile file = TempFile.Create("");

        FileUtil.SortFileLines(file.Path);

        string actual = File.ReadAllText(file.Path);
        Assert.Equal("", actual);
    }

    [Fact]
    public void CanNumberTextFileLines()
    {
        const string initial = """
                                Играют волны — ветер свищет,
                                И мачта гнется и скрыпит…
                                Увы! он счастия не ищет
                                И не от счастия бежит!
                                """;
        const string numbered = """
                                1. Играют волны — ветер свищет,
                                2. И мачта гнется и скрыпит…
                                3. Увы! он счастия не ищет
                                4. И не от счастия бежит!
                                """;

        using TempFile file = TempFile.Create(initial);
        FileUtil.AddLineNumbers(file.Path);

        string actual = File.ReadAllText(file.Path);
        Assert.Equal(numbered.Replace("\r\n", "\n"), actual);
    }

    [Fact]
    public void CanNumberOneLineFile()
    {
        using TempFile file = TempFile.Create("Играют волны — ветер свищет,");
        FileUtil.AddLineNumbers(file.Path);

        string actual = File.ReadAllText(file.Path);
        Assert.Equal("1. Играют волны — ветер свищет,", actual);
    }

    [Fact]
    public void CanNumberEmptyFile()
    {
        using TempFile file = TempFile.Create("");

        FileUtil.AddLineNumbers(file.Path);

        string actual = File.ReadAllText(file.Path);

        // В файле нет строк, поэтому нумеровать нечего
        Assert.Equal("", actual);
    }

    [Fact]
    public void ThrowsIfFileNotExists()
    {
        Assert.ThrowsAny<FileNotFoundException>(() => FileUtil.AddLineNumbers("anyNotExistingPath"));
    }
}