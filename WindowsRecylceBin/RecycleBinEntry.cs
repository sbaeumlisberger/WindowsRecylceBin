using System;

namespace WindowsRecylceBin;

/// <summary>Represents an entry of the Windows recycle bin.</summary>
public record RecycleBinEntry
{
    /// <summary>
    /// The original file path.
    /// </summary>
    public string OriginalFilePath { get; }

    /// <summary>
    /// The timestap when the file was deleted.
    /// </summary>
    public DateTime DeletedAt { get; }

    /// <summary>
    /// The path of the metadata file in the recycle bin.
    /// </summary>
    public string MetadataFilePath { get; }

    /// <summary>
    /// The path of the backup file in the recycle bin.
    /// </summary>
    public string BackupFilePath { get; }

    internal RecycleBinEntry(string originalFilePath, DateTime deletedAt, string metadataFilePath, string backupFilePath)
    {
        OriginalFilePath = originalFilePath;
        DeletedAt = deletedAt;
        MetadataFilePath = metadataFilePath;
        BackupFilePath = backupFilePath;
    }
}