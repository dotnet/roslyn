# Capturing a crash dump

## Using a registry setting

Create a registry key file (`dump.reg`) with the contents below, then execute it. The settings mean that every crash will produce a full dump (`DumpType`=2) in the folder specified by `DumpFolder`, and at most one will be kept (every subsequent crash will overwrite the file, because `DumpCount`=1).

After this key is set, repro the issue and it should produce a dump file named after the crashing process.

Use the registry editor to delete this key.

```
Windows Registry Editor Version 5.00

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps]
"DumpFolder"="c:\\localdumps"
"DumpCount"=dword:00000001
"DumpType"=dword:00000002
```

More [information](https://msdn.microsoft.com/en-us/library/windows/desktop/bb787181(v=vs.85).aspx)