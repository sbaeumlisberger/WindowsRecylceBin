namespace WindowsRecycleBin.Test;

internal static class TestUtils
{
    private static readonly string TestFoldersPath = Path.Combine(Environment.CurrentDirectory, "TestFolders");

    static TestUtils()
    {
        if (Directory.Exists(TestFoldersPath))
        {
            Directory.Delete(TestFoldersPath, true);
        }
    }

    internal static string CreateTestFolder()
    {
        string path = Path.Combine(TestFoldersPath, Guid.NewGuid().ToString());
        Directory.CreateDirectory(path);
        return path;
    }
}