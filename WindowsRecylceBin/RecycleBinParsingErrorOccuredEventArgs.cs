using System;

namespace WindowsRecylceBin;

public class RecycleBinParsingErrorOccuredEventArgs
{
    public string MetadataFilePath { get; }

    public Exception Exception { get; }

    internal RecycleBinParsingErrorOccuredEventArgs(string metadataFilePath, Exception exception)
    {
        MetadataFilePath = metadataFilePath;
        Exception = exception;
    }
}
