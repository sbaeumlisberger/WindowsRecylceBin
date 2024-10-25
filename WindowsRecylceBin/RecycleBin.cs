using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace WindowsRecylceBin;

public class RecycleBin : IRecycleBin
{
    private const string MetadataFilePrefix = "$I";
    private const string BackupFilePrefix = "$R";

    private readonly string recycleBinPath;

    private RecycleBin(SecurityIdentifier sid)
    {
        recycleBinPath = Path.Combine(@"C:\$Recycle.Bin", sid.ToString());

        if (!Directory.Exists(recycleBinPath))
        {
            throw new Exception($"""Recycle bin for SID "{sid}" does not exist.""");
        }
    }

    public static RecycleBin ForCurrentUser()
    {
        var sid = WindowsIdentity.GetCurrent().Owner ?? throw new Exception("Could not retrieve SID for current user.");
        return new RecycleBin(sid);
    }

    public static RecycleBin For(SecurityIdentifier sid)
    {
        return new RecycleBin(sid);
    }

    public IEnumerable<RecycleBinEntry> EnumerateEntries()
    {
        return Directory.EnumerateFiles(recycleBinPath, MetadataFilePrefix + "*").Select(ParseMetadataFile);
    }

    public List<RecycleBinEntry> GetEntries()
    {
        return EnumerateEntries().ToList();
    }

    public void Restore(RecycleBinEntry entry)
    {
        ValidateRecycleBinEntryParam(nameof(entry), entry);
        MoveFileOrDirectory(entry.BackupFilePath, entry.OriginalFilePath);
        DeleteFileOrDirectory(entry.MetadataFilePath);
    }

    public void Restore(string filePath)
    {
        var entry = GetEntries()
            .Where(entry => entry.OriginalFilePath == filePath)
            .OrderByDescending(entry => entry.DeletedAt)
            .FirstOrDefault() ?? throw new Exception($"""Could not find recycle bin entry for "{filePath}".""");

        Restore(entry);
    }

    public void DeletePernamently(RecycleBinEntry entry)
    {
        ValidateRecycleBinEntryParam(nameof(entry), entry);
        DeleteFileOrDirectory(entry.BackupFilePath);
        DeleteFileOrDirectory(entry.MetadataFilePath);
    }

    public void Empty()
    {
        foreach (var path in Directory.EnumerateFileSystemEntries(recycleBinPath))
        {
            DeleteFileOrDirectory(path);
        }
    }

    private RecycleBinEntry ParseMetadataFile(string metadataFilePath)
    {
        var metadataBytes = ReadFile(metadataFilePath);

        DateTime deletedAt = DateTime.FromFileTime(BitConverter.ToInt64(metadataBytes, 16));

        int fileNameLength = BitConverter.ToInt32(metadataBytes, 24);
        string orginalFilePath = Encoding.Unicode.GetString(metadataBytes, 28, fileNameLength * 2 - 2); // UTF-16 null-terminated

        string backupFileName = BackupFilePrefix + Path.GetFileName(metadataFilePath).Substring(MetadataFilePrefix.Length);
        string backupFilePath = Path.Combine(recycleBinPath, backupFileName);

        return new RecycleBinEntry(orginalFilePath, deletedAt, metadataFilePath, backupFilePath);
    }

    private void ValidateRecycleBinEntryParam(string paramName, RecycleBinEntry entry)
    {
        if (!entry.MetadataFilePath.StartsWith(recycleBinPath))
        {
            throw new ArgumentOutOfRangeException(paramName, "Invalid recycle bin entry");
        }
    }

    private static byte[] ReadFile(string filePath)
    {
        using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var memoryStream = new MemoryStream();
        fileStream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    private static void DeleteFileOrDirectory(string path)
    {
        if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
        {
            Directory.Delete(path, true);
        }
        else
        {
            File.Delete(path);
        }
    }

    private static void MoveFileOrDirectory(string sourcePath, string destinationPath)
    {
        if (File.GetAttributes(sourcePath).HasFlag(FileAttributes.Directory))
        {
            Directory.Move(sourcePath, destinationPath);
        }
        else
        {
            File.Move(sourcePath, destinationPath);
        }
    }
}