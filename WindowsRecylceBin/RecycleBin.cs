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
            throw new IOException($"""Recycle bin for SID "{sid}" does not exist.""");
        }
    }

    /// <summary>
    /// Gets the recycle bin for the current user.
    /// </summary>
    public static RecycleBin ForCurrentUser()
    {
        var sid = WindowsIdentity.GetCurrent().Owner ?? throw new Exception("Could not retrieve SID for current user.");
        return new RecycleBin(sid);
    }

    /// <summary>
    /// Gets the recycle bin for the specified SID.
    /// </summary>
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
        File.Delete(entry.MetadataFilePath);
    }

    public void Restore(string orginalFilePath)
    {
        var entry = GetEntries()
            .Where(entry => entry.OriginalFilePath == orginalFilePath)
            .OrderByDescending(entry => entry.DeletedAt)
            .FirstOrDefault() ?? throw new IOException($"""Could not find recycle bin entry for "{orginalFilePath}".""");

        Restore(entry);
    }

    public void DeletePernamently(RecycleBinEntry entry)
    {
        ValidateRecycleBinEntryParam(nameof(entry), entry);
        DeleteFileOrDirectory(entry.BackupFilePath);
        File.Delete(entry.MetadataFilePath);
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
        /*
         * Metadata file structure:
         * 
         * Windows Vista, 7 and 8:
         * 
         * Offset  Size  Description
         *  0        8   Header
         *  8        8   File size
         * 16        8   Deleted at (FILETIME)
         * 24      520   Original file path (UTF-16, null-terminated)
         * 
         * Windows 10 and 11:
         * 
         * Offset  Size  Description
         *  0        8   Header
         *  8        8   File size
         * 16        8   Deleted at (FILETIME)
         * 24        4   File path length (in characters)
         * 28      var   Original file path (UTF-16, null-terminated)
         */

        bool IsWindows10orLaterFormat(byte[] metadataBytes)
        {
            if (metadataBytes.Length != 544)
            {
                return true;
            }

            int filePathLengthInBytes = BitConverter.ToInt32(metadataBytes, 24) * 2;

            return filePathLengthInBytes == 544 - 28;
        }

        var metadataBytes = ReadFile(metadataFilePath);

        DateTime deletedAt = DateTime.FromFileTime(BitConverter.ToInt64(metadataBytes, 16));

        string orginalFilePath;

        if (IsWindows10orLaterFormat(metadataBytes))
        {
            int numberOfBytesToDecode = BitConverter.ToInt32(metadataBytes, 24) * 2 - 2;
            orginalFilePath = Encoding.Unicode.GetString(metadataBytes, 28, numberOfBytesToDecode);
        }
        else
        {
            orginalFilePath = Encoding.Unicode.GetString(metadataBytes, 24, metadataBytes.Length - 24).TrimEnd((char)0);
        }

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
        byte[] buffer = new byte[fileStream.Length];
        fileStream.Read(buffer, 0, buffer.Length);
        return buffer;
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
        if(Path.GetDirectoryName(destinationPath) is string parent)
        {
            Directory.CreateDirectory(parent);
        }

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