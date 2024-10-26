.NET API to access the Windows recycle bin and restore files from it.  

Supports Windows Vista, 7, 8, 10 and 11.  

Example:
```
var recycleBin = RecycleBin.ForCurrentUser();
recycleBin.Restore(@"C:\Users\Sample\Documents\ImportantDocument.txt");
```
