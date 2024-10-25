using System.Collections.Generic;

namespace WindowsRecylceBin;

public interface IRecycleBin
{
    IEnumerable<RecycleBinEntry> EnumerateEntries();

    List<RecycleBinEntry> GetEntries();

    void Restore(RecycleBinEntry entry);

    void Restore(string filePath);

    void DeletePernamently(RecycleBinEntry entry);

    void Empty();
}