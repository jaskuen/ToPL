using System.Runtime.InteropServices;
using System.Text;

namespace MsilBackend;

public static class AppHostBuilder
{
    private const string Placeholder = "c3ab8ff13720e8ad9047dd39466b3c8974e592c2fa383d4a3960714caef0c4f2";

    public static void CreateAppHost(string executablePath, string assemblyFileName)
    {
        string templatePath = FindAppHostTemplate();
        byte[] bytes = File.ReadAllBytes(templatePath);
        PatchAssemblyPath(bytes, assemblyFileName);
        File.WriteAllBytes(executablePath, bytes);
    }

    private static void PatchAssemblyPath(byte[] bytes, string assemblyFileName)
    {
        byte[] placeholderBytes = Encoding.UTF8.GetBytes(Placeholder);
        byte[] assemblyPathBytes = Encoding.UTF8.GetBytes(assemblyFileName);

        if (assemblyPathBytes.Length > placeholderBytes.Length)
        {
            throw new InvalidOperationException(
                $"Assembly file name '{assemblyFileName}' is too long for .NET apphost template.");
        }

        int index = FindBytes(bytes, placeholderBytes);
        if (index < 0)
        {
            throw new InvalidOperationException("Cannot find apphost assembly path placeholder.");
        }

        Array.Copy(assemblyPathBytes, 0, bytes, index, assemblyPathBytes.Length);
        Array.Clear(bytes, index + assemblyPathBytes.Length, placeholderBytes.Length - assemblyPathBytes.Length);
    }

    private static string FindAppHostTemplate()
    {
        string runtimeIdentifier = RuntimeInformation.RuntimeIdentifier;
        string executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "apphost.exe" : "apphost";

        foreach (string dotnetRoot in GetDotnetRootCandidates())
        {
            string sdkRoot = Path.Combine(dotnetRoot, "sdk");
            if (Directory.Exists(sdkRoot))
            {
                string? sdkTemplatePath = Directory
                    .EnumerateFiles(sdkRoot, executableName, SearchOption.AllDirectories)
                    .Where(path => path.Contains($"{Path.DirectorySeparatorChar}AppHostTemplate{Path.DirectorySeparatorChar}"))
                    .OrderByDescending(path => path)
                    .FirstOrDefault();

                if (sdkTemplatePath != null)
                {
                    return sdkTemplatePath;
                }
            }

            string hostPackRoot = Path.Combine(dotnetRoot, "packs", $"Microsoft.NETCore.App.Host.{runtimeIdentifier}");
            if (Directory.Exists(hostPackRoot))
            {
                string? hostPackTemplatePath = Directory
                    .EnumerateFiles(hostPackRoot, executableName, SearchOption.AllDirectories)
                    .OrderByDescending(path => path)
                    .FirstOrDefault();

                if (hostPackTemplatePath != null)
                {
                    return hostPackTemplatePath;
                }
            }
        }

        throw new FileNotFoundException("Cannot find .NET apphost template for current runtime.");
    }

    private static IEnumerable<string> GetDotnetRootCandidates()
    {
        string? dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (!string.IsNullOrEmpty(dotnetRoot))
        {
            yield return dotnetRoot;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            yield return "/usr/local/share/dotnet";
        }
        else
        {
            yield return "/usr/share/dotnet";
            yield return "/usr/lib/dotnet";
        }
    }

    private static int FindBytes(byte[] haystack, byte[] needle)
    {
        for (int i = 0; i <= haystack.Length - needle.Length; i++)
        {
            bool matches = true;
            for (int j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                return i;
            }
        }

        return -1;
    }
}
