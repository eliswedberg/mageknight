using System.Text.Json;

namespace MageKnightOnline.Tests;

public class DefinitionParityTests
{
    [Fact]
    public void RuntimeDefinitions_ContainEverySpecDefinition()
    {
        var root = GetRepositoryRoot();
        var specDir = Path.Combine(root, "spec", "definitions");
        var runtimeDir = Path.Combine(root, "src", "MageKnightOnline.Web", "wwwroot", "data", "definitions");

        var specFiles = Directory.GetFiles(specDir, "*.json")
            .Select(Path.GetFileName)
            .OrderBy(name => name)
            .ToArray();
        var runtimeFiles = Directory.GetFiles(runtimeDir, "*.json")
            .Select(Path.GetFileName)
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal(specFiles, runtimeFiles);
    }

    [Theory]
    [MemberData(nameof(DefinitionFiles))]
    public void Definitions_AreValidJson(string relativePath)
    {
        var root = GetRepositoryRoot();
        var path = Path.Combine(root, relativePath);

        using var document = JsonDocument.Parse(File.ReadAllText(path));

        Assert.True(document.RootElement.ValueKind is JsonValueKind.Object or JsonValueKind.Array);
    }

    public static IEnumerable<object[]> DefinitionFiles()
    {
        var root = GetRepositoryRoot();
        foreach (var directory in new[]
                 {
                     Path.Combine(root, "spec", "definitions"),
                     Path.Combine(root, "src", "MageKnightOnline.Web", "wwwroot", "data", "definitions")
                 })
        {
            foreach (var path in Directory.GetFiles(directory, "*.json").OrderBy(path => path))
            {
                yield return new object[] { Path.GetRelativePath(root, path) };
            }
        }
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "spec")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
