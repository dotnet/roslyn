### VB Embedded Runtime inherits overflow checking from the compilation

See https://github.com/dotnet/roslyn/issues/6941. Some VB runtime methods are specified to throw an `OverflowException` when a converted value overflows the target type. When compiling a VB project with the runtime embedded (`/vbruntime*`), the compiler includes the necessary VB runtime helpers into the assembly that is produced.  These runtime helpers inherit the overflow checking behavior of the VB.NET project that they are embedded into.  As a result, if you both embed the runtime and have overflow checking disabled (`/removeintchecks+`), you will not get the specified exceptions from the runtime helpers.  Although technically it is a bug, it has long been the behavior of VB.NET and we have found that customers would be broken by having it fixed, so we do not expect to change this behavior.

``` vb
Sub Main()
    Dim s As SByte = -128
    Dim o As Object = s
    Dim b = CByte(o) ' Should throw OverflowException but does not if you compile with /vbruntime* /removeintchecks+
    Console.WriteLine(b)
End Sub
```
