.NET API to access the Windows recycle bin and restore files from it.  

Supports Windows Vista, 7, 8, 10 and 11.  

https://www.nuget.org/packages/WindowsRecylceBin

Example:
```
var recycleBin = RecycleBin.ForCurrentUser();
recycleBin.Restore(@"C:\Users\Sample\Documents\ImportantDocument.txt");
```

For more details visit the [API documentation](https://sbaeumlisberger.github.io/WindowsRecylceBin/api/WindowsRecylceBin.html)
