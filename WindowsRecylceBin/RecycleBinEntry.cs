using System;

namespace WindowsRecylceBin;

public record RecycleBinEntry(string OriginalFilePath, DateTime DeletedAt, string MetadataFilePath, string BackupFilePath);