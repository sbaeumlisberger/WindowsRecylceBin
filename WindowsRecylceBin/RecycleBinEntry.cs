using System;

namespace WindowsRecylceBin;

/// <param name="OriginalFilePath">The original file path.</param>
/// <param name="DeletedAt">The timestap when the file was deleted.</param>
/// <param name="MetadataFilePath">The path of the metadata file in the recycle bin.</param>
/// <param name="BackupFilePath">The path of the backup file in the recycle bin.</param>
public record RecycleBinEntry(
    string OriginalFilePath, 
    DateTime DeletedAt, 
    string MetadataFilePath,
    string BackupFilePath);