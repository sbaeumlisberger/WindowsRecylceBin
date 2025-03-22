using System.Security.Principal;
using Windows.Storage;
using WindowsRecylceBin;
using FileAttributes = System.IO.FileAttributes;

namespace WindowsRecycleBin.Test;

public class RecycleBinTest
{
    private const string MetadataFilePrefix = "$I";
    private const string BackupFilePrefix = "$R";

    private readonly RecycleBin recycleBin = RecycleBin.ForCurrentUser();

    [Fact]
    public async Task GetEntries()
    {
        var testDirectoryPath = TestUtils.CreateTestFolder();
        string prefix = Guid.NewGuid().ToString();
        await CreateAndMoveFileWithPrefixToRecycleBinAsync(testDirectoryPath, prefix);
        await CreateAndMoveFileWithPrefixToRecycleBinAsync(testDirectoryPath, prefix);
        await CreateAndMoveFileWithPrefixToRecycleBinAsync(testDirectoryPath, prefix);

        var recycleBinEntries = recycleBin.GetEntries();

        Assert.Equal(3, FilterByPrefix(recycleBinEntries, prefix).Count);
    }

    [Fact]
    public async Task ParsingErrorsAreIgnoredAndReported()
    {
        // setup
        var testDirectoryPath = TestUtils.CreateTestFolder();
        string prefix = Guid.NewGuid().ToString();
        await CreateAndMoveFileWithPrefixToRecycleBinAsync(testDirectoryPath, prefix);
        await CreateAndMoveFileWithPrefixToRecycleBinAsync(testDirectoryPath, prefix);
        await CreateAndMoveFileWithPrefixToRecycleBinAsync(testDirectoryPath, prefix);

        string recycleBinPath = Path.Combine(Path.GetPathRoot(testDirectoryPath)!, "$Recycle.Bin", WindowsIdentity.GetCurrent().Owner!.Value);
        string invalidEntryBackupFile = Path.Combine(recycleBinPath, BackupFilePrefix + "invalid");
        File.WriteAllText(invalidEntryBackupFile, "test");
        string invalidEntryMetadataFile = Path.Combine(recycleBinPath, MetadataFilePrefix + "invalid");
        File.WriteAllText(invalidEntryMetadataFile, "invalid");

        List<RecycleBinParsingErrorOccuredEventArgs> errors = new List<RecycleBinParsingErrorOccuredEventArgs>();
        recycleBin.ParsingErrorOccured += (s, e) => errors.Add(e);

        // act
        var recycleBinEntries = recycleBin.GetEntries();

        // assert
        Assert.Equal(3, FilterByPrefix(recycleBinEntries, prefix).Count);
        Assert.Contains(errors, e => e.MetadataFilePath == invalidEntryMetadataFile);

        // cleanup
        File.Delete(invalidEntryBackupFile);
        File.Delete(invalidEntryMetadataFile);
    }

    [Fact]
    public async Task Restore_File()
    {
        var testDirectoryPath = TestUtils.CreateTestFolder();
        string testFilePath = Path.Combine(testDirectoryPath, "test.txt");
        CreateFile(testFilePath, "test");
        var creationTime = File.GetCreationTime(testFilePath);
        await MoveFileToRecycleBinAsync(testFilePath);

        recycleBin.Restore(testFilePath);

        Assert.True(File.Exists(testFilePath));
        Assert.Equal("test", File.ReadAllText(testFilePath));
        Assert.Equal(creationTime, File.GetCreationTime(testFilePath));
    }

    [Fact]
    public async Task Restore_HandlesSpecialCharsInPath()
    {
        var testDirectoryPath = TestUtils.CreateTestFolder();
        string testFilePath = Path.Combine(testDirectoryPath, "§$%&()=`'_;üöä€ß.txt");
        CreateFile(testFilePath, "test");
        await MoveFileToRecycleBinAsync(testFilePath);

        recycleBin.Restore(testFilePath);

        Assert.True(File.Exists(testFilePath));
        Assert.Equal("test", File.ReadAllText(testFilePath));
    }

    [Fact]
    public async Task Restore_ThrowsExceptionOnConflict()
    {
        var testDirectoryPath = TestUtils.CreateTestFolder();
        string testFilePath = Path.Combine(testDirectoryPath, "test.txt");
        CreateFile(testFilePath, "test");
        await MoveFileToRecycleBinAsync(testFilePath);
        CreateFile(testFilePath, "conflict");

        Assert.Throws<IOException>(() => recycleBin.Restore(testFilePath));

        Assert.Contains(recycleBin.GetEntries(), entry => entry.OriginalFilePath == testFilePath);
    }

    [Fact]
    public async Task Restore_PrefersLastDeletedFile()
    {
        var testDirectoryPath = TestUtils.CreateTestFolder();
        string testFilePath = Path.Combine(testDirectoryPath, "test.txt");
        CreateFile(testFilePath, "firstDeleted");
        await MoveFileToRecycleBinAsync(testFilePath);
        CreateFile(testFilePath, "lastDeleted");
        await MoveFileToRecycleBinAsync(testFilePath);

        recycleBin.Restore(testFilePath);

        Assert.True(File.Exists(testFilePath));
        Assert.Equal("lastDeleted", File.ReadAllText(testFilePath));
    }

    [Fact]
    public async Task Restore_CreatesParentDirectory()
    {
        var testDirectoryPath = TestUtils.CreateTestFolder();
        string parentDirectory = Path.Combine(testDirectoryPath, "ParentDirectory");
        string testFilePath = Path.Combine(parentDirectory, "test.txt");
        CreateFile(testFilePath, "test");
        await MoveFileToRecycleBinAsync(testFilePath);
        Directory.Delete(parentDirectory);

        recycleBin.Restore(testFilePath);

        Assert.True(Directory.Exists(parentDirectory));
        Assert.True(File.Exists(testFilePath));
        Assert.Equal("test", File.ReadAllText(testFilePath));
    }

    [Fact]
    public async Task Restore_PreservesFileAttributes()
    {
        var testDirectoryPath = TestUtils.CreateTestFolder();
        string testFilePath = Path.Combine(testDirectoryPath, "test.txt");
        CreateFile(testFilePath, "test", FileAttributes.Temporary);
        await MoveFileToRecycleBinAsync(testFilePath);

        recycleBin.Restore(testFilePath);

        Assert.True(File.Exists(testFilePath));
        Assert.True(File.GetAttributes(testFilePath).HasFlag(FileAttributes.Temporary));
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
        await CreateAndMoveFileWithPrefixToRecycleBinAsync(testDirectoryPath, prefix);
        await CreateAndMoveFileWithPrefixToRecycleBinAsync(testDirectoryPath, prefix);
        await CreateAndMoveFileWithPrefixToRecycleBinAsync(testDirectoryPath, prefix);
        var recycleBinEntries = FilterByPrefix(recycleBin.GetEntries(), prefix);

        recycleBin.DeletePernamently(recycleBinEntries[0]);
        recycleBinEntries = FilterByPrefix(recycleBin.GetEntries(), prefix);

        Assert.Equal(2, recycleBinEntries.Count);
    }

    [Fact]
    public void Restore_File_Windows7()
    {
        PrepareWindows7Test();
        string testFilePath = @"C:\WindowsRecycleBinTest\New Text Document.txt";
        if (File.Exists(testFilePath)) { File.Delete(testFilePath); }

        recycleBin.Restore(testFilePath);

        Assert.True(File.Exists(testFilePath));
        Assert.Equal("test", File.ReadAllText(testFilePath));
    }

    [Fact]
    public void Restore_Directory_Windows7()
    {
        PrepareWindows7Test();
        string testDirectoryPath = @"C:\WindowsRecycleBinTest\New folder";
        if (Directory.Exists(testDirectoryPath)) { Directory.Delete(testDirectoryPath, true); }

        recycleBin.Restore(testDirectoryPath);

        Assert.True(Directory.Exists(testDirectoryPath));
        Assert.True(File.Exists(Path.Combine(testDirectoryPath, "New Text Document.txt")));
    }

    [Fact(Skip = "Skipped because this test empties the entire recycle bin.")]
    public async Task Empty()
    {
        var testDirectoryPath = TestUtils.CreateTestFolder();
        string prefix = Guid.NewGuid().ToString();
        await CreateAndMoveFileWithPrefixToRecycleBinAsync(testDirectoryPath, prefix);
        await CreateAndMoveFileWithPrefixToRecycleBinAsync(testDirectoryPath, prefix);
        await CreateAndMoveFileWithPrefixToRecycleBinAsync(testDirectoryPath, prefix);

        recycleBin.Empty();

        Assert.Empty(recycleBin.GetEntries());
    }

    private void PrepareWindows7Test()
    {
        string recycleBinPath = Path.Combine(@"C:\$Recycle.Bin", WindowsIdentity.GetCurrent().Owner!.ToString());
        string windows7TestDataPath = Path.Combine(Environment.CurrentDirectory, "Resources", "Windows7");
        Directory.EnumerateFiles(windows7TestDataPath, "*", SearchOption.AllDirectories).ToList().ForEach(path =>
        {
            string dstPath = Path.Combine(recycleBinPath, path.Substring(windows7TestDataPath.Length + 1));
            Directory.CreateDirectory(Path.GetDirectoryName(dstPath)!);
            File.Copy(path, dstPath, true);
        });
    }

    private async Task CreateAndMoveFileWithPrefixToRecycleBinAsync(string directoryPath, string fileNamePrefix)
    {
        string filePath = Path.Combine(directoryPath, fileNamePrefix + "-" + Guid.NewGuid().ToString());
        CreateFile(filePath, "test");
        await MoveFileToRecycleBinAsync(filePath);
    }

    private void CreateFile(string filePath, string content, FileAttributes fileAttributes = FileAttributes.None)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, content);
        if (fileAttributes != FileAttributes.None)
        {
            File.SetAttributes(filePath, fileAttributes);
        }
    }

    private async Task MoveFileToRecycleBinAsync(string filePath)
    {
        var storageFile = await StorageFile.GetFileFromPathAsync(filePath);
        await storageFile.DeleteAsync();
    }

    private List<RecycleBinEntry> FilterByPrefix(List<RecycleBinEntry> recycleBinEntries, string prefix)
    {
        return recycleBinEntries.Where(entry => Path.GetFileName(entry.OriginalFilePath).StartsWith(prefix)).ToList();
    }
}
