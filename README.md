.NET API to access the Windows recycle bin and restore files from it.

https://www.nuget.org/packages/WindowsRecylceBin

Example:
```
var recycleBin = RecycleBin.ForCurrentUser();
recycleBin.Restore(@"C:\Users\Sample\Documents\ImportantDocument.txt");
```
