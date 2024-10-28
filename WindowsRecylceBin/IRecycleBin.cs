using System.Collections.Generic;
using System.IO;

namespace WindowsRecylceBin;

/// <summary>
/// Provides access to the Windows recycle bin and allows to restore deleted files.
/// To create an instance of this interface, use the static methods <see cref="RecycleBin.ForCurrentUser">RecycleBin.ForCurrentUser()</see> or <see cref="RecycleBin.For">RecycleBin.For(SecurityIdentifier sid)</see>.
/// </summary>
public interface IRecycleBin
{
    /// <summary>
    /// Enumerates all entries in the recycle bin.
    /// </summary>
    IEnumerable<RecycleBinEntry> EnumerateEntries();

    /// <summary>
    /// Gets all entries in the recycle bin.
    /// </summary>
    List<RecycleBinEntry> GetEntries();

    /// <summary>
    /// Restores the specified recycle bin entry.
    /// </summary>
    /// <param name="entry">The entry to restore.</param>
    void Restore(RecycleBinEntry entry);

    /// <summary>
    /// Restores the specified file or directory. If more than one matching entry is found, the entry with the most recent deletion time is restored.
    /// </summary>
    /// <param name="originalFilePath">The orginal path of the file or directory to restore.</param>
    /// <exception cref="IOException">Thrown when the recycle bin contains no matching entry.</exception>
    void Restore(string originalFilePath);

    /// <summary>
    /// Deletes the specified recycle bin entry pernamently.
    /// </summary>
    /// <param name="entry">The entry to delete.</param>
    void DeletePernamently(RecycleBinEntry entry);

    /// <summary>
    /// Empties the recycle bin. This will delete all entries pernamently.
    /// </summary>
    void Empty();
}