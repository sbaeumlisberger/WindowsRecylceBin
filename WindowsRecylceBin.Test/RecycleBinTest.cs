using Windows.Storage;
using WindowsRecylceBin;

namespace WindowsRecycleBin.Test;

public class RecycleBinTest
{
    private readonly RecycleBin recycleBin = RecycleBin.ForCurrentUser();

    [Fact]
    public async Task GetEntries()
    {
        var testDirectoryPath = TestUtils.CreateTestFolder();
        string prefix = Guid.NewGuid().ToString();
        await CreateAndDeleteFileAsync(testDirectoryPath, prefix);
        await CreateAndDeleteFileAsync(testDirectoryPath, prefix);
        await CreateAndDeleteFileAsync(testDirectoryPath, prefix);

        var recycleBinEntries = FilterByPrefix(recycleBin.GetEntries(), prefix);

        Assert.Equal(3, recycleBinEntries.Count);
    }

    [Fact]
    public async Task Restore_File()
    {
        var testDirectoryPath = TestUtils.CreateTestFolder();
        string testFilePath = Path.Combine(testDirectoryPath, "test.txt");
        File.WriteAllText(testFilePath, "test");
        var creationTime = File.GetCreationTime(testFilePath);
        var storageFile = await StorageFile.GetFileFromPathAsync(testFilePath);
        await storageFile.DeleteAsync();
        Assert.False(File.Exists(testFilePath));

        recycleBin.Restore(testFilePath);

        Assert.True(File.Exists(testFilePath));
        Assert.Equal("test", File.ReadAllText(testFilePath));
        Assert.Equal(creationTime, File.GetCreationTime(testFilePath));
    }

    [Fact]
    public async Task Restore_FileWithAttribute()
    {
        var testDirectoryPath = TestUtils.CreateTestFolder();
        string testFilePath = Path.Combine(testDirectoryPath, "test.txt");
        File.WriteAllText(testFilePath, "test");
        File.SetAttributes(testFilePath, System.IO.FileAttributes.Temporary);
        var storageFile = await StorageFile.GetFileFromPathAsync(testFilePath);
        await storageFile.DeleteAsync();
        Assert.False(File.Exists(testFilePath));

        recycleBin.Restore(testFilePath);

        Assert.True(File.Exists(testFilePath));
        Assert.True(File.GetAttributes(testFilePath).HasFlag(System.IO.FileAttributes.Temporary));
    }

    [Fact]
    public async Task Restore_Directory()
    {
        var testDirectoryPath = TestUtils.CreateTestFolder();
        string testFilePath = Path.Combine(testDirectoryPath, "test.txt");
        File.WriteAllText(testFilePath, "test");
        var storageFolder = await StorageFolder.GetFolderFromPathAsync(testDirectoryPath);
        await storageFolder.DeleteAsync();
        Assert.False(Directory.Exists(testDirectoryPath));
        Assert.False(File.Exists(testFilePath));

        recycleBin.Restore(testDirectoryPath);

        Assert.True(Directory.Exists(testDirectoryPath));
        Assert.True(File.Exists(testFilePath));
    }


    [Fact]
    public async Task DeletePernamently()
    {
        var testDirectoryPath = TestUtils.CreateTestFolder();
        string prefix = Guid.NewGuid().ToString();
        await CreateAndDeleteFileAsync(testDirectoryPath, prefix);
        await CreateAndDeleteFileAsync(testDirectoryPath, prefix);
        await CreateAndDeleteFileAsync(testDirectoryPath, prefix);
        var recycleBinEntries = FilterByPrefix(recycleBin.GetEntries(), prefix);

        recycleBin.DeletePernamently(recycleBinEntries[0]);
        recycleBinEntries = FilterByPrefix(recycleBin.GetEntries(), prefix);

        Assert.Equal(2, recycleBinEntries.Count);
    }

    private async Task CreateAndDeleteFileAsync(string directoryPath, string fileNamePrefix)
    {
        string testFilePath = Path.Combine(directoryPath, fileNamePrefix + "-" + Guid.NewGuid().ToString());
        File.WriteAllText(testFilePath, "test");
        var storageFile = await StorageFile.GetFileFromPathAsync(testFilePath);
        await storageFile.DeleteAsync();
    }

    private List<RecycleBinEntry> FilterByPrefix(List<RecycleBinEntry> recycleBinEntries, string prefix)
    {
        return recycleBinEntries.Where(entry => Path.GetFileName(entry.OriginalFilePath).StartsWith(prefix)).ToList();
    }
}
