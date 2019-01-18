' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class NoncompliantOverloadingInMetadata
        Inherits BasicTestBase

        <Fact()>
        Public Sub NamespaceOfTypesDifferByCase_1()
            Dim customIL = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) }
.assembly extern Microsoft.VisualBasic { .ver 10:0:0:0 .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly '<<GeneratedFileName>>'
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
}
.module '<<GeneratedFileName>>.dll'

.class public abstract auto ansi sealed beforefieldinit extensions.C
       extends [mscorlib]System.Object
{
  .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  Goo(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.3
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method C::Goo

} // end of class extensions.C

.class public abstract auto ansi sealed beforefieldinit Extensions.C
       extends [mscorlib]System.Object
{
  .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  Goo(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method C::Goo

} // end of class Extensions.C

.class public abstract auto ansi sealed beforefieldinit Extensions.D
       extends [mscorlib]System.Object
{
  .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  Goo(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.2
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method D::Goo

} // end of class Extensions.D
]]>


            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Imports Extensions
 
Module Program
    Sub Main
        Dim x As Integer = 1
        x.Goo
        Goo(x)
        Extensions.C.Goo(x)
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False)

            ' x.Goo               - !!! Breaking change, Dev10 calls Extensions.D.Goo
            ' Goo(x)              - Same error in Dev10.
            ' Extensions.C.Goo(x) - Dev10 reports two errors:
            '                           error BC31429: 'C' is ambiguous because multiple kinds of members with this name exist in namespace 'Extensions'.
            '                           error BC30560: 'C' is ambiguous in the namespace 'Extensions'.
            '                       BC31429 looks redundant and inaccurate.
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30521: Overload resolution failed because no accessible 'Goo' is most specific for these arguments:
    Extension method 'Public Sub Goo()' defined in 'C': Not most specific.
    Extension method 'Public Sub Goo()' defined in 'C': Not most specific.
    Extension method 'Public Sub Goo()' defined in 'D': Not most specific.
        x.Goo
          ~~~
BC30562: 'Goo' is ambiguous between declarations in Modules 'extensions.C, Extensions.C, Extensions.D'.
        Goo(x)
        ~~~
BC30560: 'C' is ambiguous in the namespace 'extensions'.
        Extensions.C.Goo(x)
        ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub NamespaceOfTypesDifferByCase_2()
            Dim customIL = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) }
.assembly extern Microsoft.VisualBasic { .ver 10:0:0:0 .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly '<<GeneratedFileName>>'
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
}
.module '<<GeneratedFileName>>.dll'

.class private abstract auto ansi sealed beforefieldinit extensions.C
       extends [mscorlib]System.Object
{
  .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  Goo(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.3
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method C::Goo

} // end of class extensions.C

.class public abstract auto ansi sealed beforefieldinit Extensions.C
       extends [mscorlib]System.Object
{
  .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  Goo(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method C::Goo

} // end of class Extensions.C

.class public abstract auto ansi sealed beforefieldinit Extensions.D
       extends [mscorlib]System.Object
{
  .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  Goo(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.2
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method D::Goo

} // end of class Extensions.D
]]>


            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Imports Extensions
 
Module Program
    Sub Main
        Dim x As Integer = 1
        x.Goo
        Goo(x)
        Extensions.C.Goo(x)
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30521: Overload resolution failed because no accessible 'Goo' is most specific for these arguments:
    Extension method 'Public Sub Goo()' defined in 'C': Not most specific.
    Extension method 'Public Sub Goo()' defined in 'D': Not most specific.
        x.Goo
          ~~~
BC30562: 'Goo' is ambiguous between declarations in Modules 'Extensions.C, Extensions.D'.
        Goo(x)
        ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub NamespaceOfTypesDifferByCase_3()
            Dim customIL = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) }
.assembly extern Microsoft.VisualBasic { .ver 10:0:0:0 .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly '<<GeneratedFileName>>'
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
}
.module '<<GeneratedFileName>>.dll'

.class public abstract auto ansi sealed beforefieldinit extensions.C
       extends [mscorlib]System.Object
{
  .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  Goo(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.3
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method C::Goo

} // end of class extensions.C

.class public abstract auto ansi sealed beforefieldinit Extensions.C
       extends [mscorlib]System.Object
{
  .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute::.ctor() = ( 01 00 00 00 ) 
} // end of class Extensions.C
]]>


            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Imports Extensions
 
Module Program
    Sub Main
        Dim x As Integer = 1
        x.Goo
        Goo(x)
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe)

            ' x.Goo      - Dev10 reports error BC30456: 'Goo' is not a member of 'Integer'.
            ' Goo(x)     - no change, works
            CompileAndVerify(compilation1, expectedOutput:="3" & Environment.NewLine & "3")

            Dim compilation2 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Imports Extensions
 
Module Program
    Sub Main
        Dim x As Integer = 1
        Extensions.C.Goo(x)
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe)

            ' Dev10 reports two errors:
            '     error BC31429: 'C' is ambiguous because multiple kinds of members with this name exist in namespace 'Extensions'.
            '     error BC30560: 'C' is ambiguous in the namespace 'Extensions'.
            ' BC31429 looks redundant and inaccurate.
            CompilationUtils.AssertTheseDiagnostics(compilation2,
<expected>
BC30560: 'C' is ambiguous in the namespace 'extensions'.
        Extensions.C.Goo(x)
        ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub TypesDifferByCase_1()
            Dim customIL = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) }
.assembly extern Microsoft.VisualBasic { .ver 10:0:0:0 .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly '<<GeneratedFileName>>'
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
}
.module '<<GeneratedFileName>>.dll'

.class public abstract auto ansi sealed beforefieldinit Extensions.c
       extends [mscorlib]System.Object
{
  .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  Goo(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.3
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method C::Goo

} // end of class extensions.C

.class public abstract auto ansi sealed beforefieldinit Extensions.C
       extends [mscorlib]System.Object
{
  .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  Goo(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method C::Goo

} // end of class Extensions.C

.class public abstract auto ansi sealed beforefieldinit Extensions.D
       extends [mscorlib]System.Object
{
  .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  Goo(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.2
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method D::Goo

} // end of class Extensions.D
]]>


            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Imports Extensions
 
Module Program
    Sub Main
        Dim x As Integer = 1
        x.Goo
        Goo(x)
        Extensions.C.Goo(x)
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False)

            ' x.Goo               - !!! Breaking change, Dev10 calls Extensions.D.Goo
            ' Goo(x)              - Same error in Dev10.
            ' Extensions.C.Goo(x) - Dev10 reports two errors:
            '                           error BC31429: 'C' is ambiguous because multiple kinds of members with this name exist in namespace 'Extensions'.
            '                           error BC30560: 'C' is ambiguous in the namespace 'Extensions'.
            '                       BC31429 looks redundant and inaccurate.
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30521: Overload resolution failed because no accessible 'Goo' is most specific for these arguments:
    Extension method 'Public Sub Goo()' defined in 'c': Not most specific.
    Extension method 'Public Sub Goo()' defined in 'C': Not most specific.
    Extension method 'Public Sub Goo()' defined in 'D': Not most specific.
        x.Goo
          ~~~
BC30562: 'Goo' is ambiguous between declarations in Modules 'Extensions.c, Extensions.C, Extensions.D'.
        Goo(x)
        ~~~
BC30560: 'c' is ambiguous in the namespace 'Extensions'.
        Extensions.C.Goo(x)
        ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub TypesDifferByCase_2()
            Dim customIL = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) }
.assembly extern Microsoft.VisualBasic { .ver 10:0:0:0 .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly '<<GeneratedFileName>>'
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
}
.module '<<GeneratedFileName>>.dll'

.class private abstract auto ansi sealed beforefieldinit Extensions.c
       extends [mscorlib]System.Object
{
  .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  Goo(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.3
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method C::Goo

} // end of class extensions.C

.class public abstract auto ansi sealed beforefieldinit Extensions.C
       extends [mscorlib]System.Object
{
  .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  Goo(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method C::Goo

} // end of class Extensions.C

.class public abstract auto ansi sealed beforefieldinit Extensions.D
       extends [mscorlib]System.Object
{
  .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  Goo(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.2
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method D::Goo

} // end of class Extensions.D
]]>


            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Imports Extensions
 
Module Program
    Sub Main
        Dim x As Integer = 1
        x.Goo
        Goo(x)
        Extensions.C.Goo(x)
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30521: Overload resolution failed because no accessible 'Goo' is most specific for these arguments:
    Extension method 'Public Sub Goo()' defined in 'C': Not most specific.
    Extension method 'Public Sub Goo()' defined in 'D': Not most specific.
        x.Goo
          ~~~
BC30562: 'Goo' is ambiguous between declarations in Modules 'Extensions.C, Extensions.D'.
        Goo(x)
        ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub TypesDifferByCase_3()
            Dim customIL = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) }
.assembly extern Microsoft.VisualBasic { .ver 10:0:0:0 .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly '<<GeneratedFileName>>'
{
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
}
.module '<<GeneratedFileName>>.dll'

.class public abstract auto ansi sealed beforefieldinit Extensions.c
       extends [mscorlib]System.Object
{
  .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  Goo(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.3
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method C::Goo

} // end of class extensions.C

.class public abstract auto ansi sealed beforefieldinit Extensions.C
       extends [mscorlib]System.Object
{
  .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute::.ctor() = ( 01 00 00 00 ) 
} // end of class Extensions.C
]]>


            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Imports Extensions
 
Module Program
    Sub Main
        Dim x As Integer = 1
        x.Goo
        Goo(x)
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe)

            ' x.Goo      - Dev10 reports error BC30456: 'Goo' is not a member of 'Integer'.
            ' Goo(x)     - no change, works
            CompileAndVerify(compilation1, expectedOutput:="3" & Environment.NewLine & "3")

            Dim compilation2 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Imports Extensions
 
Module Program
    Sub Main
        Dim x As Integer = 1
        Extensions.C.Goo(x)
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe)

            ' Dev10 reports two errors:
            '     error BC31429: 'C' is ambiguous because multiple kinds of members with this name exist in namespace 'Extensions'.
            '     error BC30560: 'C' is ambiguous in the namespace 'Extensions'.
            ' BC31429 looks redundant and inaccurate.
            CompilationUtils.AssertTheseDiagnostics(compilation2,
<expected>
BC30560: 'c' is ambiguous in the namespace 'Extensions'.
        Extensions.C.Goo(x)
        ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub MembersDifferByCaseAndAccessibility_1()
            Dim customIL = <![CDATA[
.class public auto ansi beforefieldinit Container1
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Bar
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "Container1.Bar"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Bar::.ctor

    .method public hidebysig static void 
            bar1() cil managed
    {
      // Code size       13 (0xd)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  ldstr      "Container1.Bar.bar1"
      IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000b:  nop
      IL_000c:  ret
    } // end of method Bar::bar1
  } // end of class Bar

  .class auto ansi nested family beforefieldinit bar
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "Container1.bar"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method bar::.ctor

    .method public hidebysig static void 
            bar1() cil managed
    {
      // Code size       13 (0xd)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  ldstr      "Container1.bar.bar1"
      IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000b:  nop
      IL_000c:  ret
    } // end of method bar::bar1
  } // end of class bar

  .field public string Baz
  .field family string baz
  .method public hidebysig instance void 
          goo(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.goo"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container1::goo

  .method family hidebysig instance void 
          gOO(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.gOO"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container1::gOO

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       30 (0x1e)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldstr      "Baz"
    IL_0006:  stfld      string Container1::Baz
    IL_000b:  ldarg.0
    IL_000c:  ldstr      "baz"
    IL_0011:  stfld      string Container1::baz
    IL_0016:  ldarg.0
    IL_0017:  call       instance void [mscorlib]System.Object::.ctor()
    IL_001c:  nop
    IL_001d:  ret
  } // end of method Container1::.ctor

} // end of class Container1
]]>


            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program

    Class Test
        Inherits Container1

        Sub Test()
            goo(1)
            System.Console.WriteLine(Baz)

            Dim bar As New Bar()
            MyBase.Bar.bar1()
        End Sub
    End Class

    Sub Main
        Dim c1 As New Container1()
        c1.goo(1)
        System.Console.WriteLine(c1.baz)

        Dim bar As New Container1.Bar()
        Container1.Bar.bar1()

        Dim t1 As New Test()
        t1.Test()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation1, expectedOutput:=
            <![CDATA[
Container1.goo
Baz
Container1.Bar
Container1.Bar.bar1
Container1.goo
Baz
Container1.Bar
Container1.Bar.bar1
]]>)

        End Sub

        <Fact()>
        Public Sub MembersDifferByKindAndAccessibility_1()
            Dim customIL = <![CDATA[
.class public auto ansi beforefieldinit Container1
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Bar
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "Container1.Bar"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Bar::.ctor

    .method public hidebysig static void 
            bar1() cil managed
    {
      // Code size       13 (0xd)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  ldstr      "Container1.Bar.bar1"
      IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000b:  nop
      IL_000c:  ret
    } // end of method Bar::bar1
  } // end of class Bar

  .class auto ansi nested family beforefieldinit baz
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "Container1.baz"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method baz::.ctor

  } // end of class baz

  .field family string bar
  .field family string gOO
  .field public string Baz
  .method public hidebysig instance void 
          goo() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.goo"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container1::goo

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       41 (0x29)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldstr      "bar"
    IL_0006:  stfld      string Container1::bar
    IL_000b:  ldarg.0
    IL_000c:  ldstr      "gOO"
    IL_0011:  stfld      string Container1::gOO
    IL_0016:  ldarg.0
    IL_0017:  ldstr      "Baz"
    IL_001c:  stfld      string Container1::Baz
    IL_0021:  ldarg.0
    IL_0022:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0027:  nop
    IL_0028:  ret
  } // end of method Container1::.ctor

} // end of class Container1
]]>


            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program

    Class Test
        Inherits Container1

        Sub Test()
            goo
            System.Console.WriteLine(Baz)

            Dim bar As New Bar()
            MyBase.Bar.bar1()
        End Sub
    End Class

    Sub Main
        Dim c1 As New Container1()
        c1.goo
        System.Console.WriteLine(c1.baz)

        Dim bar As New Container1.Bar()
        Container1.Bar.bar1()

        Dim t1 As New Test()
        t1.Test()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation1, expectedOutput:=
            <![CDATA[
Container1.goo
Baz
Container1.Bar
Container1.Bar.bar1
Container1.goo
Baz
Container1.Bar
Container1.Bar.bar1
]]>)

        End Sub

        <Fact()>
        Public Sub MembersDifferByKindSameAccessibility_1()
            Dim customIL = <![CDATA[
.class public auto ansi beforefieldinit Container1
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Bar
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "Container1.Bar"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Bar::.ctor

    .method public hidebysig static void 
            bar1() cil managed
    {
      // Code size       13 (0xd)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  ldstr      "Container1.Bar.bar1"
      IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000b:  nop
      IL_000c:  ret
    } // end of method Bar::bar1

  } // end of class Bar

  .field public static string bar
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Container1::.ctor

} // end of class Container1
]]>

            ' New Container1.Bar - Dev10 reports error BC31429: 'Bar' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
            ' Roslyn works fine.
            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program

    Sub Main
        Dim bar As New Container1.Bar()
        bar.bar1()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation1, expectedOutput:=
            <![CDATA[
Container1.Bar
Container1.Bar.bar1
]]>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program

    Sub Main
        Container1.Bar.bar1()
        System.Console.WriteLine(Container1.bar)
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation2,
<expected>
BC31429: 'bar' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
        Container1.Bar.bar1()
        ~~~~~~~~~~~~~~
BC31429: 'bar' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
        System.Console.WriteLine(Container1.bar)
                                 ~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub TypeLessAccessibleThanField_1()
            Dim customIL = <![CDATA[
.class public auto ansi beforefieldinit Container1
       extends [mscorlib]System.Object
{
  .class auto ansi nested family beforefieldinit Bar
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "Container1.Bar"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Bar::.ctor

    .method public hidebysig static void 
            bar1() cil managed
    {
      // Code size       13 (0xd)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  ldstr      "Container1.Bar.bar1"
      IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000b:  nop
      IL_000c:  ret
    } // end of method Bar::bar1

  } // end of class Bar

  .field public static string bar
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Container1::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Container1.bar"
    IL_0005:  stsfld     string Container1::bar
    IL_000a:  ret
  } // end of method Container1::.cctor

} // end of class Container1
]]>

            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program

    Class Test
        Inherits Container1

        Sub Test()
            System.Console.WriteLine("Test.Test")
            Dim bar As New Bar()
            bar.bar1()
            System.Console.WriteLine(MyBase.bar)
        End Sub
    End Class

    Sub Main
        System.Console.WriteLine(Container1.bar)

        Dim t1 As New Test()
        t1.Test()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            ' !!! Difference with Dev10 - less accessible type wins in type-only context.
            CompileAndVerify(compilation1, expectedOutput:=
            <![CDATA[
Container1.bar
Test.Test
Container1.Bar
Container1.Bar.bar1
Container1.bar
]]>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program

    Class Test
        Inherits Container1

        Sub Test()
            MyBase.Bar.bar1()
        End Sub
    End Class

    Sub Main
        Dim bar As New Container1.Bar()
        bar.bar1()
        Container1.Bar.bar1()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation2,
<expected>
BC30456: 'bar1' is not a member of 'String'.
            MyBase.Bar.bar1()
            ~~~~~~~~~~~~~~~
BC30389: 'Container1.Bar' is not accessible in this context because it is 'Protected'.
        Dim bar As New Container1.Bar()
                       ~~~~~~~~~~~~~~
BC30456: 'bar1' is not a member of 'String'.
        Container1.Bar.bar1()
        ~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub NamespaceAndPublicTypeDifferByCase_1()
            Dim customIL = <![CDATA[
.class public auto ansi beforefieldinit Container1.Bar
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "Container1.Bar"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method Bar::.ctor

} // end of class Container1.Bar

.class public auto ansi beforefieldinit Container1.bar.Baz
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "Container1.bar.Baz"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method Baz::.ctor

} // end of class Container1.bar.Baz
]]>

            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program
    Sub Main
        Dim bar1 As New Container1.bar()
        Dim bar2 As New Container1.bar.Baz()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC30560: 'bar' is ambiguous in the namespace 'Container1'.
        Dim bar1 As New Container1.bar()
                        ~~~~~~~~~~~~~~
BC30560: 'bar' is ambiguous in the namespace 'Container1'.
        Dim bar2 As New Container1.bar.Baz()
                        ~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub NamespaceAndFriendTypeDifferByCase_1()
            Dim customIL = <![CDATA[
.class private auto ansi beforefieldinit Container1.Bar
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "Container1.Bar"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method Bar::.ctor

} // end of class Container1.Bar

.class public auto ansi beforefieldinit Container1.bar.Baz
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "Container1.bar.Baz"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method Baz::.ctor

} // end of class Container1.bar.Baz
]]>

            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program
    Sub Main
        Dim bar1 As New Container1.bar()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC30182: Type expected.
        Dim bar1 As New Container1.bar()
                        ~~~~~~~~~~~~~~
</expected>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program
    Sub Main
        Dim bar2 As New Container1.bar.Baz()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation2, expectedOutput:=
            <![CDATA[
Container1.bar.Baz
]]>)
        End Sub

        <Fact()>
        Public Sub NamespaceAndFriendTypeDifferByCase_2()
            Dim customIL = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) }
.assembly extern Microsoft.VisualBasic { .ver 10:0:0:0 .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly '<<GeneratedFileName>>'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 13 43 6F 6E 73 6F 6C 65 41 70 70 6C 69 63   // ...ConsoleApplic
                                                                                                                 61 74 69 6F 6E 31 00 00 )                         // ation1..
}
.module '<<GeneratedFileName>>.dll'

.class private auto ansi beforefieldinit Container4.Container5
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Container6
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "Container4.Container5.Container6"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Container6::.ctor

  } // end of class Container6

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Container5::.ctor

} // end of class Container4.Container5

.class public auto ansi beforefieldinit Container4.ContaineR5.Container6
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "Container4.ContaineR5.Container6"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method Container6::.ctor

} // end of class Container4.ContaineR5.Container6
]]>


            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="ConsoleApplication">
    <file name="a.vb">
Module Program
    Sub Main
        Dim c As New Container4.ContaineR5.Container6()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation1, expectedOutput:=
            <![CDATA[
Container4.ContaineR5.Container6
]]>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="ConsoleApplication1">
    <file name="a.vb">
Module Program
    Sub Main
        Dim c As New Container4.ContaineR5.Container6()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation2, expectedOutput:=
            <![CDATA[
Container4.ContaineR5.Container6
]]>)
        End Sub


        <Fact()>
        Public Sub NamespaceAndTypesDifferByCase_1()
            Dim customIL = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) }
.assembly extern Microsoft.VisualBasic { .ver 10:0:0:0 .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly '<<GeneratedFileName>>'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 13 43 6F 6E 73 6F 6C 65 41 70 70 6C 69 63   // ...ConsoleApplic
                                                                                                                 61 74 69 6F 6E 31 00 00 )                         // ation1..
}
.module '<<GeneratedFileName>>.dll'

.class private auto ansi beforefieldinit bbxxx
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 bbxxx"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method bbxxx::.ctor

} // end of class bbxxx

.class public auto ansi beforefieldinit bbxxy
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 bbxxy"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method bbxxy::.ctor

} // end of class bbxxy

.class private auto ansi beforefieldinit bbxxz
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 bbxxz"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method bbxxz::.ctor

} // end of class bbxxz

.class public auto ansi beforefieldinit bBxxz.Test
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "1 bBxxz"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method Test::.ctor

} // end of class bBxxz.Test

.class public auto ansi beforefieldinit bbxyx
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 bbxyx"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method bbxyx::.ctor

} // end of class bbxyx

.class public auto ansi beforefieldinit bBxyx.Test
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "1 bBxyx"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method Test::.ctor

} // end of class bBxyx.Test

.class private auto ansi beforefieldinit bbxyy
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 bbxyy"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method bbxyy::.ctor

} // end of class bbxyy

.class private auto ansi beforefieldinit Bbxyy
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 Bbxyy"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Bbxyy::.ctor

} // end of class Bbxyy

.class public auto ansi beforefieldinit bBxyy.Test
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "1 bBxyy"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method Test::.ctor

} // end of class bBxyy.Test

.class public auto ansi beforefieldinit bbxyz
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 bbxyz"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method bbxyz::.ctor

} // end of class bbxyz

.class private auto ansi beforefieldinit Bbxyz
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 Bbxyz"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Bbxyz::.ctor

} // end of class Bbxyz

.class public auto ansi beforefieldinit bBxyz.Test
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "1 bBxyz"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method Test::.ctor

} // end of class bBxyz.Test

.class private auto ansi beforefieldinit Bbxzx
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 Bbxzx"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Bbxzx::.ctor

} // end of class Bbxzx

.class public auto ansi beforefieldinit bbxzx
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 bbxzx"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method bbxzx::.ctor

} // end of class bbxzx

.class public auto ansi beforefieldinit bBxzx.Test
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "1 bBxzx"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method Test::.ctor

} // end of class bBxzx.Test

.class private auto ansi beforefieldinit Bbxzy
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 Bbxzy"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Bbxzy::.ctor

} // end of class Bbxzy

.class private auto ansi beforefieldinit BBxzy
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 BBxzy"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method BBxzy::.ctor

} // end of class BBxzy

.class public auto ansi beforefieldinit bbxzy
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 bbxzy"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method bbxzy::.ctor

} // end of class bbxzy

.class public auto ansi beforefieldinit bBxzy.Test
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "1 bBxzy"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method Test::.ctor

} // end of class bBxzy.Test

.class private auto ansi beforefieldinit Bbxzz
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 Bbxzz"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Bbxzz::.ctor

} // end of class Bbxzz

.class public auto ansi beforefieldinit bbxzz
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 bbxzz"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method bbxzz::.ctor

} // end of class bbxzz

.class private auto ansi beforefieldinit BBxzz
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 BBxzz"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method BBxzz::.ctor

} // end of class BBxzz

.class public auto ansi beforefieldinit bBxzz.Test
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "1 bBxzz"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method Test::.ctor

} // end of class bBxzz.Test

.class public auto ansi beforefieldinit bbyxx
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 bbyxx"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method bbyxx::.ctor

} // end of class bbyxx

.class private auto ansi beforefieldinit Bbyxx
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 Bbyxx"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Bbyxx::.ctor

} // end of class Bbyxx

.class private auto ansi beforefieldinit BByxx
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 BByxx"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method BByxx::.ctor

} // end of class BByxx

.class public auto ansi beforefieldinit bByxx.Test
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "1 bByxx"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method Test::.ctor

} // end of class bByxx.Test

.class public auto ansi beforefieldinit bbyxy
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 bbyxy"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method bbyxy::.ctor

} // end of class bbyxy

.class public auto ansi beforefieldinit Bbyxy
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 Bbyxy"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Bbyxy::.ctor

} // end of class Bbyxy

.class private auto ansi beforefieldinit BByxy
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 BByxy"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method BByxy::.ctor

} // end of class BByxy

.class public auto ansi beforefieldinit bByxy.Test
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "1 bByxy"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method Test::.ctor

} // end of class bByxy.Test

.class public auto ansi beforefieldinit bbyxz
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 bbyxz"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method bbyxz::.ctor

} // end of class bbyxz

.class private auto ansi beforefieldinit BByxz
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 BByxz"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method BByxz::.ctor

} // end of class BByxz

.class public auto ansi beforefieldinit Bbyxz
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 Bbyxz"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Bbyxz::.ctor

} // end of class Bbyxz

.class public auto ansi beforefieldinit bByxz.Test
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "1 bByxz"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method Test::.ctor

} // end of class bByxz.Test

.class private auto ansi beforefieldinit BByyx
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 BByyx"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method BByyx::.ctor

} // end of class BByyx

.class public auto ansi beforefieldinit bbyyx
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 bbyyx"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method bbyyx::.ctor

} // end of class bbyyx

.class public auto ansi beforefieldinit Bbyyx
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  nop
      IL_0008:  ldstr      "1 Bbyyx"
      IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ret
    } // end of method Test::.ctor

  } // end of class Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Bbyyx::.ctor

} // end of class Bbyyx

.class public auto ansi beforefieldinit bByyx.Test
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "1 bByyx"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method Test::.ctor

} // end of class bByyx.Test
]]>

            Dim ilAssemblyRef1 As MetadataReference = Nothing

            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="ConsoleApplication">
    <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object

        c = New bbxxx.Test()
        c = New bBxyx.Test()
        c = New bBxyz.Test()
        c = New bBxzx.Test()
        c = New bBxzy.Test()
        c = New bBxzz.Test()
        c = New bByxx.Test()
        c = New bByxy.Test()
        c = New bByxz.Test()
        c = New bByyx.Test()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe, ilReference:=ilAssemblyRef1)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC30389: 'bbxxx' is not accessible in this context because it is 'Friend'.
        c = New bbxxx.Test()
                ~~~~~
BC30554: 'bbxyx' is ambiguous.
        c = New bBxyx.Test()
                ~~~~~
BC30554: 'bbxyz' is ambiguous.
        c = New bBxyz.Test()
                ~~~~~
BC30554: 'bbxzx' is ambiguous.
        c = New bBxzx.Test()
                ~~~~~
BC30554: 'bbxzy' is ambiguous.
        c = New bBxzy.Test()
                ~~~~~
BC30554: 'bbxzz' is ambiguous.
        c = New bBxzz.Test()
                ~~~~~
BC30554: 'bbyxx' is ambiguous.
        c = New bByxx.Test()
                ~~~~~
BC30554: 'bbyxy' is ambiguous.
        c = New bByxy.Test()
                ~~~~~
BC30554: 'bbyxz' is ambiguous.
        c = New bByxz.Test()
                ~~~~~
BC30554: 'bbyyx' is ambiguous.
        c = New bByyx.Test()
                ~~~~~
</expected>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="ConsoleApplication">
    <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object

        c = New bbxxy.Test()
        c = New bBxxz.Test()
        c = New bBxyy.Test()
    End Sub
End Module
    </file>
</compilation>, references:={ilAssemblyRef1}, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation2, expectedOutput:=
            <![CDATA[
1 bbxxy
1 bBxxz
1 bBxyy
]]>)

            Dim compilation3 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="ConsoleApplication1">
    <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object

        c = New bBxyx.Test()
        c = New bBxyz.Test()
        c = New bBxzx.Test()
        c = New bBxzy.Test()
        c = New bBxzz.Test()
        c = New bByxx.Test()
        c = New bByxy.Test()
        c = New bByxz.Test()
        c = New bByyx.Test()
    End Sub
End Module
    </file>
</compilation>, references:={ilAssemblyRef1}, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation3,
<expected>
BC30554: 'bbxyx' is ambiguous.
        c = New bBxyx.Test()
                ~~~~~
BC30554: 'bbxyz' is ambiguous.
        c = New bBxyz.Test()
                ~~~~~
BC30554: 'bbxzx' is ambiguous.
        c = New bBxzx.Test()
                ~~~~~
BC30554: 'bbxzy' is ambiguous.
        c = New bBxzy.Test()
                ~~~~~
BC30554: 'bbxzz' is ambiguous.
        c = New bBxzz.Test()
                ~~~~~
BC30554: 'bbyxx' is ambiguous.
        c = New bByxx.Test()
                ~~~~~
BC30554: 'bbyxy' is ambiguous.
        c = New bByxy.Test()
                ~~~~~
BC30554: 'bbyxz' is ambiguous.
        c = New bByxz.Test()
                ~~~~~
BC30554: 'bbyyx' is ambiguous.
        c = New bByyx.Test()
                ~~~~~
</expected>)

            Dim compilation4 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="ConsoleApplication1">
    <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object

        c = New bbxxx.Test()
        c = New bbxxy.Test()
        c = New bBxxz.Test()
        c = New bBxyy.Test()
    End Sub
End Module
    </file>
</compilation>, references:={ilAssemblyRef1}, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation4, expectedOutput:=
            <![CDATA[
1 bbxxx
1 bbxxy
1 bBxxz
1 bBxyy
]]>)

            Dim vbCompilationToRef = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="VBLibrary">
    <file name="a.vb">
Namespace BBxxx
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("2 BBxxx.Test")
        End Sub
    End Class
End Namespace

Namespace BBxxy
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("2 BBxxy.Test")
        End Sub
    End Class
End Namespace

Namespace BBxxz
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("2 BBxxz.Test")
        End Sub
    End Class
    Public Class Test1
        Public Sub New()
            System.Console.WriteLine("2 BBxxz.Test1")
        End Sub
    End Class
End Namespace

Namespace BBxyx
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("2 BBxyx.Test")
        End Sub
    End Class
End Namespace

Namespace BBxyy
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("2 BBxyy.Test")
        End Sub
    End Class
    Public Class Test1
        Public Sub New()
            System.Console.WriteLine("2 BBxyy.Test1")
        End Sub
    End Class
End Namespace

Namespace BBxyz
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("2 BBxyz.Test")
        End Sub
    End Class
End Namespace

Namespace BBxzx
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("2 BBxzx.Test")
        End Sub
    End Class
End Namespace

Namespace BBxzy
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("2 BBxzy.Test")
        End Sub
    End Class
End Namespace

Namespace BBxzz
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("2 BBxzz.Test")
        End Sub
    End Class
End Namespace

Namespace BByxx
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("2 BByxx.Test")
        End Sub
    End Class
End Namespace

Namespace BByxy
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("2 BByxy.Test")
        End Sub
    End Class
End Namespace

Namespace BByxz
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("2 BByxz.Test")
        End Sub
    End Class
End Namespace

Namespace BByyx
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("2 BByyx.Test")
        End Sub
    End Class
End Namespace
    </file>
</compilation>, TestOptions.ReleaseDll)

            Dim vbCompilationRef = New VisualBasicCompilationReference(vbCompilationToRef)

            If True Then
                Dim compilation5 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="ConsoleApplication">
        <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object

        c = New bbxxy.Test()
        c = New bBxxz.Test()
        c = New bBxyx.Test()
        c = New bBxyy.Test()
        c = New bBxyz.Test()
        c = New bBxzx.Test()
        c = New bBxzy.Test()
        c = New bBxzz.Test()
        c = New bByxx.Test()
        c = New bByxy.Test()
        c = New bByxz.Test()
        c = New bByyx.Test()
    End Sub
End Module
    </file>
    </compilation>, references:={ilAssemblyRef1, vbCompilationRef}, options:=TestOptions.ReleaseExe)

                CompilationUtils.AssertTheseDiagnostics(compilation5,
    <expected>
BC30554: 'bbxxy' is ambiguous.
        c = New bbxxy.Test()
                ~~~~~
BC30560: 'Test' is ambiguous in the namespace 'bBxxz'.
        c = New bBxxz.Test()
                ~~~~~~~~~~
BC30554: 'bbxyx' is ambiguous.
        c = New bBxyx.Test()
                ~~~~~
BC30560: 'Test' is ambiguous in the namespace 'bBxyy'.
        c = New bBxyy.Test()
                ~~~~~~~~~~
BC30554: 'bbxyz' is ambiguous.
        c = New bBxyz.Test()
                ~~~~~
BC30554: 'bbxzx' is ambiguous.
        c = New bBxzx.Test()
                ~~~~~
BC30554: 'bbxzy' is ambiguous.
        c = New bBxzy.Test()
                ~~~~~
BC30554: 'bbxzz' is ambiguous.
        c = New bBxzz.Test()
                ~~~~~
BC30554: 'bbyxx' is ambiguous.
        c = New bByxx.Test()
                ~~~~~
BC30554: 'bbyxy' is ambiguous.
        c = New bByxy.Test()
                ~~~~~
BC30554: 'bbyxz' is ambiguous.
        c = New bByxz.Test()
                ~~~~~
BC30554: 'bbyyx' is ambiguous.
        c = New bByyx.Test()
                ~~~~~
</expected>)

                Dim compilation6 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="ConsoleApplication">
        <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object

        c = New bbxxx.Test()
        c = New bBxxz.Test1()
        c = New bBxyy.Test1()
    End Sub
End Module
    </file>
    </compilation>, references:={ilAssemblyRef1, vbCompilationRef}, options:=TestOptions.ReleaseExe)

                CompileAndVerify(compilation6, expectedOutput:=
                <![CDATA[
2 BBxxx.Test
2 BBxxz.Test1
2 BBxyy.Test1
    ]]>)

                Dim compilation7 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="ConsoleApplication1">
        <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object

        c = New bbxxx.Test()
        c = New bbxxy.Test()
        c = New bBxxz.Test()
        c = New bBxyx.Test()
        c = New bBxyy.Test()
        c = New bBxyz.Test()
        c = New bBxzx.Test()
        c = New bBxzy.Test()
        c = New bBxzz.Test()
        c = New bByxx.Test()
        c = New bByxy.Test()
        c = New bByxz.Test()
        c = New bByyx.Test()
    End Sub
End Module
    </file>
    </compilation>, references:={ilAssemblyRef1, vbCompilationRef}, options:=TestOptions.ReleaseExe)

                CompilationUtils.AssertTheseDiagnostics(compilation7,
    <expected>
BC30554: 'bbxxx' is ambiguous.
        c = New bbxxx.Test()
                ~~~~~
BC30554: 'bbxxy' is ambiguous.
        c = New bbxxy.Test()
                ~~~~~
BC30560: 'Test' is ambiguous in the namespace 'bBxxz'.
        c = New bBxxz.Test()
                ~~~~~~~~~~
BC30554: 'bbxyx' is ambiguous.
        c = New bBxyx.Test()
                ~~~~~
BC30560: 'Test' is ambiguous in the namespace 'bBxyy'.
        c = New bBxyy.Test()
                ~~~~~~~~~~
BC30554: 'bbxyz' is ambiguous.
        c = New bBxyz.Test()
                ~~~~~
BC30554: 'bbxzx' is ambiguous.
        c = New bBxzx.Test()
                ~~~~~
BC30554: 'bbxzy' is ambiguous.
        c = New bBxzy.Test()
                ~~~~~
BC30554: 'bbxzz' is ambiguous.
        c = New bBxzz.Test()
                ~~~~~
BC30554: 'bbyxx' is ambiguous.
        c = New bByxx.Test()
                ~~~~~
BC30554: 'bbyxy' is ambiguous.
        c = New bByxy.Test()
                ~~~~~
BC30554: 'bbyxz' is ambiguous.
        c = New bByxz.Test()
                ~~~~~
BC30554: 'bbyyx' is ambiguous.
        c = New bByyx.Test()
                ~~~~~
</expected>)

                Dim compilation8 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="ConsoleApplication1">
        <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object

        c = New bBxxz.Test1()
        c = New bBxyy.Test1()
    End Sub
End Module
    </file>
    </compilation>, references:={ilAssemblyRef1, vbCompilationRef}, options:=TestOptions.ReleaseExe)

                CompileAndVerify(compilation8, expectedOutput:=
                <![CDATA[
2 BBxxz.Test1
2 BBxyy.Test1
    ]]>)
            End If

            If True Then
                Dim compilation5 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="ConsoleApplication">
        <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object

        c = New bbxxy.Test()
        c = New bBxxz.Test()
        c = New bBxyx.Test()
        c = New bBxyy.Test()
        c = New bBxyz.Test()
        c = New bBxzx.Test()
        c = New bBxzy.Test()
        c = New bBxzz.Test()
        c = New bByxx.Test()
        c = New bByxy.Test()
        c = New bByxz.Test()
        c = New bByyx.Test()
    End Sub
End Module
    </file>
    </compilation>, references:={vbCompilationRef, ilAssemblyRef1}, options:=TestOptions.ReleaseExe)

                CompilationUtils.AssertTheseDiagnostics(compilation5,
    <expected>
BC30554: 'bbxxy' is ambiguous.
        c = New bbxxy.Test()
                ~~~~~
BC30560: 'Test' is ambiguous in the namespace 'BBxxz'.
        c = New bBxxz.Test()
                ~~~~~~~~~~
BC30554: 'bbxyx' is ambiguous.
        c = New bBxyx.Test()
                ~~~~~
BC30560: 'Test' is ambiguous in the namespace 'BBxyy'.
        c = New bBxyy.Test()
                ~~~~~~~~~~
BC30554: 'bbxyz' is ambiguous.
        c = New bBxyz.Test()
                ~~~~~
BC30554: 'bbxzx' is ambiguous.
        c = New bBxzx.Test()
                ~~~~~
BC30554: 'bbxzy' is ambiguous.
        c = New bBxzy.Test()
                ~~~~~
BC30554: 'bbxzz' is ambiguous.
        c = New bBxzz.Test()
                ~~~~~
BC30554: 'bbyxx' is ambiguous.
        c = New bByxx.Test()
                ~~~~~
BC30554: 'bbyxy' is ambiguous.
        c = New bByxy.Test()
                ~~~~~
BC30554: 'bbyxz' is ambiguous.
        c = New bByxz.Test()
                ~~~~~
BC30554: 'bbyyx' is ambiguous.
        c = New bByyx.Test()
                ~~~~~
</expected>)

                Dim compilation6 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="ConsoleApplication">
        <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object

        c = New bbxxx.Test()
        c = New bBxxz.Test1()
        c = New bBxyy.Test1()
    End Sub
End Module
    </file>
    </compilation>, references:={vbCompilationRef, ilAssemblyRef1}, options:=TestOptions.ReleaseExe)

                CompileAndVerify(compilation6, expectedOutput:=
                <![CDATA[
2 BBxxx.Test
2 BBxxz.Test1
2 BBxyy.Test1
    ]]>)

                Dim compilation7 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="ConsoleApplication1">
        <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object

        c = New bbxxx.Test()
        c = New bbxxy.Test()
        c = New bBxxz.Test()
        c = New bBxyx.Test()
        c = New bBxyy.Test()
        c = New bBxyz.Test()
        c = New bBxzx.Test()
        c = New bBxzy.Test()
        c = New bBxzz.Test()
        c = New bByxx.Test()
        c = New bByxy.Test()
        c = New bByxz.Test()
        c = New bByyx.Test()
    End Sub
End Module
    </file>
    </compilation>, references:={vbCompilationRef, ilAssemblyRef1}, options:=TestOptions.ReleaseExe)

                CompilationUtils.AssertTheseDiagnostics(compilation7,
    <expected>
BC30554: 'bbxxx' is ambiguous.
        c = New bbxxx.Test()
                ~~~~~
BC30554: 'bbxxy' is ambiguous.
        c = New bbxxy.Test()
                ~~~~~
BC30560: 'Test' is ambiguous in the namespace 'BBxxz'.
        c = New bBxxz.Test()
                ~~~~~~~~~~
BC30554: 'bbxyx' is ambiguous.
        c = New bBxyx.Test()
                ~~~~~
BC30560: 'Test' is ambiguous in the namespace 'BBxyy'.
        c = New bBxyy.Test()
                ~~~~~~~~~~
BC30554: 'bbxyz' is ambiguous.
        c = New bBxyz.Test()
                ~~~~~
BC30554: 'bbxzx' is ambiguous.
        c = New bBxzx.Test()
                ~~~~~
BC30554: 'bbxzy' is ambiguous.
        c = New bBxzy.Test()
                ~~~~~
BC30554: 'bbxzz' is ambiguous.
        c = New bBxzz.Test()
                ~~~~~
BC30554: 'bbyxx' is ambiguous.
        c = New bByxx.Test()
                ~~~~~
BC30554: 'bbyxy' is ambiguous.
        c = New bByxy.Test()
                ~~~~~
BC30554: 'bbyxz' is ambiguous.
        c = New bByxz.Test()
                ~~~~~
BC30554: 'bbyyx' is ambiguous.
        c = New bByyx.Test()
                ~~~~~
</expected>)

                Dim compilation8 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="ConsoleApplication1">
        <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object

        c = New bBxxz.Test1()
        c = New bBxyy.Test1()
    End Sub
End Module
    </file>
    </compilation>, references:={vbCompilationRef, ilAssemblyRef1}, options:=TestOptions.ReleaseExe)

                CompileAndVerify(compilation8, expectedOutput:=
                <![CDATA[
2 BBxxz.Test1
2 BBxyy.Test1
    ]]>)

            End If

            Dim ilAssemblyRef2 As MetadataReference = Nothing

            Dim compilation9 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="ConsoleApplication1">
    <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object

        c = New bbxxx.Test()
        c = New bbxxy.Test()
        c = New bBxxz.Test()
        c = New bBxyx.Test()
        c = New bBxyy.Test()
        c = New bBxyz.Test()
        c = New bBxzx.Test()
        c = New bBxzy.Test()
        c = New bBxzz.Test()
        c = New bByxx.Test()
        c = New bByxy.Test()
        c = New bByxz.Test()
        c = New bByyx.Test()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe, ilReference:=ilAssemblyRef2)

            compilation9 = compilation9.AddReferences(ilAssemblyRef1, vbCompilationRef)

            CompilationUtils.AssertTheseDiagnostics(compilation9,
<expected>
BC30554: 'bbxxx' is ambiguous.
        c = New bbxxx.Test()
                ~~~~~
BC30554: 'bbxxy' is ambiguous.
        c = New bbxxy.Test()
                ~~~~~
BC30560: 'Test' is ambiguous in the namespace 'bBxxz'.
        c = New bBxxz.Test()
                ~~~~~~~~~~
BC30554: 'bbxyx' is ambiguous.
        c = New bBxyx.Test()
                ~~~~~
BC30560: 'Test' is ambiguous in the namespace 'bBxyy'.
        c = New bBxyy.Test()
                ~~~~~~~~~~
BC30554: 'bbxyz' is ambiguous.
        c = New bBxyz.Test()
                ~~~~~
BC30554: 'bbxzx' is ambiguous.
        c = New bBxzx.Test()
                ~~~~~
BC30554: 'bbxzy' is ambiguous.
        c = New bBxzy.Test()
                ~~~~~
BC30554: 'bbxzz' is ambiguous.
        c = New bBxzz.Test()
                ~~~~~
BC30554: 'bbyxx' is ambiguous.
        c = New bByxx.Test()
                ~~~~~
BC30554: 'bbyxy' is ambiguous.
        c = New bByxy.Test()
                ~~~~~
BC30554: 'bbyxz' is ambiguous.
        c = New bByxz.Test()
                ~~~~~
BC30554: 'bbyyx' is ambiguous.
        c = New bByyx.Test()
                ~~~~~
</expected>)

            Dim compilation10 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="ConsoleApplication1">
    <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object

        c = New bbxxx.Test()
        c = New bbxxy.Test()
        c = New bBxxz.Test()
        c = New bBxyx.Test()
        c = New bBxyy.Test()
        c = New bBxyz.Test()
        c = New bBxzx.Test()
        c = New bBxzy.Test()
        c = New bBxzz.Test()
        c = New bByxx.Test()
        c = New bByxy.Test()
        c = New bByxz.Test()
        c = New bByyx.Test()
    End Sub
End Module
    </file>
</compilation>, references:={ilAssemblyRef2, vbCompilationRef, ilAssemblyRef1}, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation10,
<expected>
BC30554: 'bbxxx' is ambiguous.
        c = New bbxxx.Test()
                ~~~~~
BC30554: 'bbxxy' is ambiguous.
        c = New bbxxy.Test()
                ~~~~~
BC30560: 'Test' is ambiguous in the namespace 'bBxxz'.
        c = New bBxxz.Test()
                ~~~~~~~~~~
BC30554: 'bbxyx' is ambiguous.
        c = New bBxyx.Test()
                ~~~~~
BC30560: 'Test' is ambiguous in the namespace 'bBxyy'.
        c = New bBxyy.Test()
                ~~~~~~~~~~
BC30554: 'bbxyz' is ambiguous.
        c = New bBxyz.Test()
                ~~~~~
BC30554: 'bbxzx' is ambiguous.
        c = New bBxzx.Test()
                ~~~~~
BC30554: 'bbxzy' is ambiguous.
        c = New bBxzy.Test()
                ~~~~~
BC30554: 'bbxzz' is ambiguous.
        c = New bBxzz.Test()
                ~~~~~
BC30554: 'bbyxx' is ambiguous.
        c = New bByxx.Test()
                ~~~~~
BC30554: 'bbyxy' is ambiguous.
        c = New bByxy.Test()
                ~~~~~
BC30554: 'bbyxz' is ambiguous.
        c = New bByxz.Test()
                ~~~~~
BC30554: 'bbyyx' is ambiguous.
        c = New bByyx.Test()
                ~~~~~
</expected>)

            Dim compilation11 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="ConsoleApplication1">
    <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object

        c = New bbxxx.Test()
        c = New bbxxy.Test()
        c = New bBxxz.Test()
        c = New bBxyx.Test()
        c = New bBxyy.Test()
        c = New bBxyz.Test()
        c = New bBxzx.Test()
        c = New bBxzy.Test()
        c = New bBxzz.Test()
        c = New bByxx.Test()
        c = New bByxy.Test()
        c = New bByxz.Test()
        c = New bByyx.Test()
    End Sub
End Module
    </file>
</compilation>, references:={vbCompilationRef, ilAssemblyRef2, ilAssemblyRef1}, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation11,
<expected>
BC30554: 'bbxxx' is ambiguous.
        c = New bbxxx.Test()
                ~~~~~
BC30554: 'bbxxy' is ambiguous.
        c = New bbxxy.Test()
                ~~~~~
BC30560: 'Test' is ambiguous in the namespace 'BBxxz'.
        c = New bBxxz.Test()
                ~~~~~~~~~~
BC30554: 'bbxyx' is ambiguous.
        c = New bBxyx.Test()
                ~~~~~
BC30560: 'Test' is ambiguous in the namespace 'BBxyy'.
        c = New bBxyy.Test()
                ~~~~~~~~~~
BC30554: 'bbxyz' is ambiguous.
        c = New bBxyz.Test()
                ~~~~~
BC30554: 'bbxzx' is ambiguous.
        c = New bBxzx.Test()
                ~~~~~
BC30554: 'bbxzy' is ambiguous.
        c = New bBxzy.Test()
                ~~~~~
BC30554: 'bbxzz' is ambiguous.
        c = New bBxzz.Test()
                ~~~~~
BC30554: 'bbyxx' is ambiguous.
        c = New bByxx.Test()
                ~~~~~
BC30554: 'bbyxy' is ambiguous.
        c = New bByxy.Test()
                ~~~~~
BC30554: 'bbyxz' is ambiguous.
        c = New bByxz.Test()
                ~~~~~
BC30554: 'bbyyx' is ambiguous.
        c = New bByyx.Test()
                ~~~~~
</expected>)

            Dim compilation12 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="ConsoleApplication1">
    <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object

        c = New bBxxz.Test1()
        c = New bBxyy.Test1()
    End Sub
End Module
    </file>
</compilation>, references:={vbCompilationRef, ilAssemblyRef1, ilAssemblyRef2}, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation12, expectedOutput:=
            <![CDATA[
2 BBxxz.Test1
2 BBxyy.Test1
    ]]>)

            Dim compilation13 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="ConsoleApplication1">
    <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object

        c = New bBxxz.Test1()
        c = New bBxyy.Test1()
    End Sub
End Module
    </file>
</compilation>, references:={ilAssemblyRef1, vbCompilationRef, ilAssemblyRef2}, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation13, expectedOutput:=
            <![CDATA[
2 BBxxz.Test1
2 BBxyy.Test1
    ]]>)

            Dim compilation14 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="ConsoleApplication1">
    <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object

        c = New bBxxz.Test1()
        c = New bBxyy.Test1()
    End Sub
End Module
    </file>
</compilation>, references:={ilAssemblyRef1, ilAssemblyRef2, vbCompilationRef}, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation14, expectedOutput:=
            <![CDATA[
2 BBxxz.Test1
2 BBxyy.Test1
    ]]>)

            Dim compilation15 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="ConsoleApplication1">
    <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object

        c = New bbxxx.Test()
        c = New bbxxy.Test()
        c = New bBxxz.Test()
        c = New bBxyx.Test()
        c = New bBxyy.Test()
        c = New bBxyz.Test()
        c = New bBxzx.Test()
        c = New bBxzy.Test()
        c = New bBxzz.Test()
        c = New bByxx.Test()
        c = New bByxy.Test()
        c = New bByxz.Test()
        c = New bByyx.Test()
    End Sub
End Module

Namespace BBxxx
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("3 BBxxx.Test")
        End Sub
    End Class
End Namespace

Namespace BBxxy
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("3 BBxxy.Test")
        End Sub
    End Class
End Namespace

Namespace BBxxz
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("3 BBxxz.Test")
        End Sub
    End Class
End Namespace

Namespace BBxyx
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("3 BBxyx.Test")
        End Sub
    End Class
End Namespace

Namespace BBxyy
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("3 BBxyy.Test")
        End Sub
    End Class
End Namespace

Namespace BBxyz
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("3 BBxyz.Test")
        End Sub
    End Class
End Namespace

Namespace BBxzx
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("3 BBxzx.Test")
        End Sub
    End Class
End Namespace

Namespace BBxzy
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("3 BBxzy.Test")
        End Sub
    End Class
End Namespace

Namespace BBxzz
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("3 BBxzz.Test")
        End Sub
    End Class
End Namespace

Namespace BByxx
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("3 BByxx.Test")
        End Sub
    End Class
End Namespace

Namespace BByxy
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("3 BByxy.Test")
        End Sub
    End Class
End Namespace

Namespace BByxz
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("3 BByxz.Test")
        End Sub
    End Class
End Namespace

Namespace BByyx
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("3 BByyx.Test")
        End Sub
    End Class
End Namespace

    </file>
</compilation>, references:={ilAssemblyRef1, vbCompilationRef, ilAssemblyRef2}, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation15, expectedOutput:=
            <![CDATA[
3 BBxxx.Test
3 BBxxy.Test
3 BBxxz.Test
3 BBxyx.Test
3 BBxyy.Test
3 BBxyz.Test
3 BBxzx.Test
3 BBxzy.Test
3 BBxzz.Test
3 BByxx.Test
3 BByxy.Test
3 BByxz.Test
3 BByyx.Test
    ]]>)

            Dim compilation16 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="ConsoleApplication1">
    <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object

        c = New bbxxx.Test()
        c = New bbxxy.Test()
        c = New bBxxz.Test()
        c = New bBxyx.Test()
        c = New bBxyy.Test()
        c = New bBxyz.Test()
        c = New bBxzx.Test()
        c = New bBxzy.Test()
        c = New bBxzz.Test()
        c = New bByxx.Test()
        c = New bByxy.Test()
        c = New bByxz.Test()
        c = New bByyx.Test()
    End Sub
End Module

Class BBxxx
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("4 BBxxx.Test")
        End Sub
    End Class
End Class

Class BBxxy
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("4 BBxxy.Test")
        End Sub
    End Class
End Class

Class BBxxz
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("4 BBxxz.Test")
        End Sub
    End Class
End Class

Class BBxyx
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("4 BBxyx.Test")
        End Sub
    End Class
End Class

Class BBxyy
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("4 BBxyy.Test")
        End Sub
    End Class
End Class

Class BBxyz
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("4 BBxyz.Test")
        End Sub
    End Class
End Class

Class BBxzx
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("4 BBxzx.Test")
        End Sub
    End Class
End Class

Class BBxzy
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("4 BBxzy.Test")
        End Sub
    End Class
End Class

Class BBxzz
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("4 BBxzz.Test")
        End Sub
    End Class
End Class

Class BByxx
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("4 BByxx.Test")
        End Sub
    End Class
End Class

Class BByxy
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("4 BByxy.Test")
        End Sub
    End Class
End Class

Class BByxz
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("4 BByxz.Test")
        End Sub
    End Class
End Class

Class BByyx
    Public Class Test
        Public Sub New()
            System.Console.WriteLine("4 BByyx.Test")
        End Sub
    End Class
End Class

    </file>
</compilation>, references:={ilAssemblyRef1, vbCompilationRef, ilAssemblyRef2}, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation16, expectedOutput:=
            <![CDATA[
4 BBxxx.Test
4 BBxxy.Test
4 BBxxz.Test
4 BBxyx.Test
4 BBxyy.Test
4 BBxyz.Test
4 BBxzx.Test
4 BBxzy.Test
4 BBxzz.Test
4 BByxx.Test
4 BByxy.Test
4 BByxz.Test
4 BByyx.Test
    ]]>)
        End Sub

        <Fact()>
        Public Sub MembersDifferByKindAndAccessibility_2()
            Dim customIL = <![CDATA[
.class public auto ansi beforefieldinit Container1
       extends [mscorlib]System.Object
{
  .field family string aa1
  .field family string aa2
  .field family string aa3
  .field family string aa4
  .field family string aa5
  .field family string aa6
  .field public string aa10
  .field public string aa20
  .field public string aa30
  .field public string aa40
  .field public string aa50
  .field public string aa60
  .field public string aa100
  .field public string aa200
  .field public string aa300
  .field public string aa400
  .field public string aa500
  .field public string aa600
  .method family hidebysig instance string 
          Aa1() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.Aa1"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::Aa1

  .method public hidebysig instance string 
          aA1(int32 x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.aA1"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::aA1

  .method public hidebysig instance string 
          aA2(int32 x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.aA2"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::aA2

  .method family hidebysig instance string 
          Aa2() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.Aa2"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::Aa2

  .method family hidebysig instance string 
          Aa3() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.Aa3"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::Aa3

  .method public hidebysig instance string 
          aA3(int32 x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.aA3"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::aA3

  .method family hidebysig instance string 
          Aa4() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.Aa4"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::Aa4

  .method public hidebysig instance string 
          aA4(int32 x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.aA4"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::aA4

  .method public hidebysig instance string 
          aA5(int32 x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.aA5"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::aA5

  .method family hidebysig instance string 
          Aa5() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.Aa5"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::Aa5

  .method public hidebysig instance string 
          aA6(int32 x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.aA6"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::aA6

  .method family hidebysig instance string 
          Aa6() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.Aa6"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::Aa6

  .method family hidebysig instance string 
          Aa10() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.Aa10"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::Aa10

  .method public hidebysig instance string 
          aA10(int32 x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.aA10"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::aA10

  .method public hidebysig instance string 
          aA20(int32 x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.aA20"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::aA20

  .method family hidebysig instance string 
          Aa20() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.Aa20"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::Aa20

  .method family hidebysig instance string 
          Aa30() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.Aa30"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::Aa30

  .method public hidebysig instance string 
          aA30(int32 x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.aA30"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::aA30

  .method family hidebysig instance string 
          Aa40() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.Aa40"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::Aa40

  .method public hidebysig instance string 
          aA40(int32 x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.aA40"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::aA40

  .method public hidebysig instance string 
          aA50(int32 x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.aA50"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::aA50

  .method family hidebysig instance string 
          Aa50() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.Aa50"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::Aa50

  .method public hidebysig instance string 
          aA60(int32 x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.aA60"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::aA60

  .method family hidebysig instance string 
          Aa60() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.Aa60"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::Aa60

  .method family hidebysig instance string 
          Aa100() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.Aa100"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::Aa100

  .method family hidebysig instance string 
          aA100(int32 x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.aA100"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::aA100

  .method family hidebysig instance string 
          aA200(int32 x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.aA200"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::aA200

  .method family hidebysig instance string 
          Aa200() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.Aa200"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::Aa200

  .method family hidebysig instance string 
          Aa300() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.Aa300"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::Aa300

  .method family hidebysig instance string 
          aA300(int32 x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.aA300"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::aA300

  .method family hidebysig instance string 
          Aa400() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.Aa400"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::Aa400

  .method family hidebysig instance string 
          aA400(int32 x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.aA400"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::aA400

  .method family hidebysig instance string 
          aA500(int32 x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.aA500"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::aA500

  .method family hidebysig instance string 
          Aa500() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.Aa500"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::Aa500

  .method family hidebysig instance string 
          aA600(int32 x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.aA600"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::aA600

  .method family hidebysig instance string 
          Aa600() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.Aa600"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::Aa600

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       206 (0xce)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldstr      "Container1.aa1"
    IL_0006:  stfld      string Container1::aa1
    IL_000b:  ldarg.0
    IL_000c:  ldstr      "Container1.aa2"
    IL_0011:  stfld      string Container1::aa2
    IL_0016:  ldarg.0
    IL_0017:  ldstr      "Container1.aa3"
    IL_001c:  stfld      string Container1::aa3
    IL_0021:  ldarg.0
    IL_0022:  ldstr      "Container1.aa4"
    IL_0027:  stfld      string Container1::aa4
    IL_002c:  ldarg.0
    IL_002d:  ldstr      "Container1.aa5"
    IL_0032:  stfld      string Container1::aa5
    IL_0037:  ldarg.0
    IL_0038:  ldstr      "Container1.aa6"
    IL_003d:  stfld      string Container1::aa6
    IL_0042:  ldarg.0
    IL_0043:  ldstr      "Container1.aa10"
    IL_0048:  stfld      string Container1::aa10
    IL_004d:  ldarg.0
    IL_004e:  ldstr      "Container1.aa20"
    IL_0053:  stfld      string Container1::aa20
    IL_0058:  ldarg.0
    IL_0059:  ldstr      "Container1.aa30"
    IL_005e:  stfld      string Container1::aa30
    IL_0063:  ldarg.0
    IL_0064:  ldstr      "Container1.aa40"
    IL_0069:  stfld      string Container1::aa40
    IL_006e:  ldarg.0
    IL_006f:  ldstr      "Container1.aa50"
    IL_0074:  stfld      string Container1::aa50
    IL_0079:  ldarg.0
    IL_007a:  ldstr      "Container1.aa60"
    IL_007f:  stfld      string Container1::aa60
    IL_0084:  ldarg.0
    IL_0085:  ldstr      "Container1.aa100"
    IL_008a:  stfld      string Container1::aa100
    IL_008f:  ldarg.0
    IL_0090:  ldstr      "Container1.aa200"
    IL_0095:  stfld      string Container1::aa200
    IL_009a:  ldarg.0
    IL_009b:  ldstr      "Container1.aa300"
    IL_00a0:  stfld      string Container1::aa300
    IL_00a5:  ldarg.0
    IL_00a6:  ldstr      "Container1.aa400"
    IL_00ab:  stfld      string Container1::aa400
    IL_00b0:  ldarg.0
    IL_00b1:  ldstr      "Container1.aa500"
    IL_00b6:  stfld      string Container1::aa500
    IL_00bb:  ldarg.0
    IL_00bc:  ldstr      "Container1.aa600"
    IL_00c1:  stfld      string Container1::aa600
    IL_00c6:  ldarg.0
    IL_00c7:  call       instance void [mscorlib]System.Object::.ctor()
    IL_00cc:  nop
    IL_00cd:  ret
  } // end of method Container1::.ctor

} // end of class Container1
]]>

            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program

    Class Test
        Inherits Container1

        Sub Test()
            System.Console.WriteLine("Test.Test")
            System.Console.WriteLine(Aa10)
            System.Console.WriteLine(aA10(1))

            System.Console.WriteLine(aA20)
            System.Console.WriteLine(aA20(1))

            System.Console.WriteLine(Aa30)
            System.Console.WriteLine(aA30(1))

            System.Console.WriteLine(Aa40)
            System.Console.WriteLine(aA40(1))

            System.Console.WriteLine(aA50)
            System.Console.WriteLine(aA50(1))

            System.Console.WriteLine(aA60)
            System.Console.WriteLine(aA60(1))
        End Sub
    End Class

    Sub Main
        Dim c As New Container1()
        System.Console.WriteLine(c.Aa10)
        System.Console.WriteLine(c.aA10(1))

        System.Console.WriteLine(c.aA20)
        System.Console.WriteLine(c.aA20(1))

        System.Console.WriteLine(c.Aa30)
        System.Console.WriteLine(c.aA30(1))

        System.Console.WriteLine(c.Aa40)
        System.Console.WriteLine(c.aA40(1))

        System.Console.WriteLine(c.aA50)
        System.Console.WriteLine(c.aA50(1))

        System.Console.WriteLine(c.aA60)
        System.Console.WriteLine(c.aA60(1))
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC31429: 'aA10' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
            System.Console.WriteLine(Aa10)
                                     ~~~~
BC31429: 'aA10' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
            System.Console.WriteLine(aA10(1))
                                     ~~~~
BC31429: 'aA20' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
            System.Console.WriteLine(aA20)
                                     ~~~~
BC31429: 'aA20' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
            System.Console.WriteLine(aA20(1))
                                     ~~~~
BC31429: 'aA30' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
            System.Console.WriteLine(Aa30)
                                     ~~~~
BC31429: 'aA30' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
            System.Console.WriteLine(aA30(1))
                                     ~~~~
BC31429: 'aA40' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
            System.Console.WriteLine(Aa40)
                                     ~~~~
BC31429: 'aA40' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
            System.Console.WriteLine(aA40(1))
                                     ~~~~
BC31429: 'aA50' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
            System.Console.WriteLine(aA50)
                                     ~~~~
BC31429: 'aA50' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
            System.Console.WriteLine(aA50(1))
                                     ~~~~
BC31429: 'aA60' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
            System.Console.WriteLine(aA60)
                                     ~~~~
BC31429: 'aA60' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
            System.Console.WriteLine(aA60(1))
                                     ~~~~
BC31429: 'aA10' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
        System.Console.WriteLine(c.Aa10)
                                 ~~~~~~
BC31429: 'aA10' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
        System.Console.WriteLine(c.aA10(1))
                                 ~~~~~~
BC31429: 'aA20' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
        System.Console.WriteLine(c.aA20)
                                 ~~~~~~
BC31429: 'aA20' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
        System.Console.WriteLine(c.aA20(1))
                                 ~~~~~~
BC31429: 'aA30' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
        System.Console.WriteLine(c.Aa30)
                                 ~~~~~~
BC31429: 'aA30' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
        System.Console.WriteLine(c.aA30(1))
                                 ~~~~~~
BC31429: 'aA40' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
        System.Console.WriteLine(c.Aa40)
                                 ~~~~~~
BC31429: 'aA40' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
        System.Console.WriteLine(c.aA40(1))
                                 ~~~~~~
BC31429: 'aA50' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
        System.Console.WriteLine(c.aA50)
                                 ~~~~~~
BC31429: 'aA50' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
        System.Console.WriteLine(c.aA50(1))
                                 ~~~~~~
BC31429: 'aA60' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
        System.Console.WriteLine(c.aA60)
                                 ~~~~~~
BC31429: 'aA60' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
        System.Console.WriteLine(c.aA60(1))
                                 ~~~~~~
</expected>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program

    Class Test
        Inherits Container1

        Sub Test()
            System.Console.WriteLine("Test.Test")
            System.Console.WriteLine(aa100)
            System.Console.WriteLine(aa100(1))

            System.Console.WriteLine(aa200)
            System.Console.WriteLine(aa200(1))

            System.Console.WriteLine(aa300)
            System.Console.WriteLine(aa300(1))

            System.Console.WriteLine(aa400)
            System.Console.WriteLine(aa400(1))

            System.Console.WriteLine(aa500)
            System.Console.WriteLine(aa500(1))

            System.Console.WriteLine(aa600)
            System.Console.WriteLine(aa600(1))
        End Sub
    End Class

    Sub Main
        Dim c As New Container1()
        System.Console.WriteLine(c.aa100)
        System.Console.WriteLine(c.aa100(1))

        System.Console.WriteLine(c.aa200)
        System.Console.WriteLine(c.aa200(1))

        System.Console.WriteLine(c.aa300)
        System.Console.WriteLine(c.aa300(1))

        System.Console.WriteLine(c.aa400)
        System.Console.WriteLine(c.aa400(1))

        System.Console.WriteLine(c.aa500)
        System.Console.WriteLine(c.aa500(1))

        System.Console.WriteLine(c.aa600)
        System.Console.WriteLine(c.aa600(1))

        Dim t1 As New Test()
        t1.Test()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation2, expectedOutput:=
            <![CDATA[
Container1.aa100
o
Container1.aa200
o
Container1.aa300
o
Container1.aa400
o
Container1.aa500
o
Container1.aa600
o
Test.Test
Container1.aa100
o
Container1.aa200
o
Container1.aa300
o
Container1.aa400
o
Container1.aa500
o
Container1.aa600
o
]]>)

            Dim compilation3 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program

    Class Test
        Inherits Container1

        Sub Test()
            System.Console.WriteLine("Test.Test")
            System.Console.WriteLine(aA1(1))
            System.Console.WriteLine(aA2(1))
            System.Console.WriteLine(aA3(1))
            System.Console.WriteLine(aA4(1))
            System.Console.WriteLine(aA5(1))
            System.Console.WriteLine(aA6(1))
        End Sub
    End Class

    Sub Main
        Dim c As New Container1()
        System.Console.WriteLine(c.aA1(1))
        System.Console.WriteLine(c.aA2(1))
        System.Console.WriteLine(c.aA3(1))
        System.Console.WriteLine(c.aA4(1))
        System.Console.WriteLine(c.aA5(1))
        System.Console.WriteLine(c.aA6(1))

        Dim t1 As New Test()
        t1.Test()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation3, expectedOutput:=
            <![CDATA[
Container1.aA1
Container1.aA2
Container1.aA3
Container1.aA4
Container1.aA5
Container1.aA6
Test.Test
Container1.aA1
Container1.aA2
Container1.aA3
Container1.aA4
Container1.aA5
Container1.aA6
]]>)

            Dim compilation4 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program

    Class Test
        Inherits Container1

        Sub Test()
            System.Console.WriteLine("Test.Test")
            System.Console.WriteLine(Aa1)
            System.Console.WriteLine(aA2)
            System.Console.WriteLine(Aa3)
            System.Console.WriteLine(Aa4)
            System.Console.WriteLine(aA5)
            System.Console.WriteLine(aA6)
        End Sub
    End Class

    Sub Main
        Dim c As New Container1()
        System.Console.WriteLine(c.Aa1)
        System.Console.WriteLine(c.aA2)
        System.Console.WriteLine(c.Aa3)
        System.Console.WriteLine(c.Aa4)
        System.Console.WriteLine(c.aA5)
        System.Console.WriteLine(c.aA6)

        Dim t1 As New Test()
        t1.Test()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation4,
<expected>
BC30455: Argument not specified for parameter 'x' of 'Public Overloads Function aA1(x As Integer) As String'.
            System.Console.WriteLine(Aa1)
                                     ~~~
BC30455: Argument not specified for parameter 'x' of 'Public Overloads Function aA2(x As Integer) As String'.
            System.Console.WriteLine(aA2)
                                     ~~~
BC30455: Argument not specified for parameter 'x' of 'Public Overloads Function aA3(x As Integer) As String'.
            System.Console.WriteLine(Aa3)
                                     ~~~
BC30455: Argument not specified for parameter 'x' of 'Public Overloads Function aA4(x As Integer) As String'.
            System.Console.WriteLine(Aa4)
                                     ~~~
BC30455: Argument not specified for parameter 'x' of 'Public Overloads Function aA5(x As Integer) As String'.
            System.Console.WriteLine(aA5)
                                     ~~~
BC30455: Argument not specified for parameter 'x' of 'Public Overloads Function aA6(x As Integer) As String'.
            System.Console.WriteLine(aA6)
                                     ~~~
BC30455: Argument not specified for parameter 'x' of 'Public Overloads Function aA1(x As Integer) As String'.
        System.Console.WriteLine(c.Aa1)
                                   ~~~
BC30455: Argument not specified for parameter 'x' of 'Public Overloads Function aA2(x As Integer) As String'.
        System.Console.WriteLine(c.aA2)
                                   ~~~
BC30455: Argument not specified for parameter 'x' of 'Public Overloads Function aA3(x As Integer) As String'.
        System.Console.WriteLine(c.Aa3)
                                   ~~~
BC30455: Argument not specified for parameter 'x' of 'Public Overloads Function aA4(x As Integer) As String'.
        System.Console.WriteLine(c.Aa4)
                                   ~~~
BC30455: Argument not specified for parameter 'x' of 'Public Overloads Function aA5(x As Integer) As String'.
        System.Console.WriteLine(c.aA5)
                                   ~~~
BC30455: Argument not specified for parameter 'x' of 'Public Overloads Function aA6(x As Integer) As String'.
        System.Console.WriteLine(c.aA6)
                                   ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub TypesDifferByCase_4()
            Dim customIL = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) }
.assembly extern Microsoft.VisualBasic { .ver 10:0:0:0 .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly '<<GeneratedFileName>>'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 13 43 6F 6E 73 6F 6C 65 41 70 70 6C 69 63   // ...ConsoleApplic
                                                                                                                 61 74 69 6F 6E 31 00 00 )                         // ation1..
}
.module '<<GeneratedFileName>>.dll'

.class public auto ansi beforefieldinit aaxxx
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "aaxxx"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method aaxxx::.ctor

} // end of class aaxxx

.class private auto ansi beforefieldinit Aaxxx
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "Aaxxx"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method Aaxxx::.ctor

} // end of class Aaxxx

.class private auto ansi beforefieldinit aAxxx
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "aAxxx"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method aAxxx::.ctor

} // end of class aAxxx

.class private auto ansi beforefieldinit aaxxy
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "aaxxy"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method aaxxy::.ctor

} // end of class aaxxy

.class public auto ansi beforefieldinit Aaxxy
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "Aaxxy"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method Aaxxy::.ctor

} // end of class Aaxxy

.class private auto ansi beforefieldinit aAxxy
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "aAxxy"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method aAxxy::.ctor

} // end of class aAxxy

.class private auto ansi beforefieldinit aaxxz
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "aaxxz"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method aaxxz::.ctor

} // end of class aaxxz

.class private auto ansi beforefieldinit Aaxxz
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "Aaxxz"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method Aaxxz::.ctor

} // end of class Aaxxz

.class public auto ansi beforefieldinit aAxxz
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "aAxxz"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method aAxxz::.ctor

} // end of class aAxxz

.class private auto ansi beforefieldinit aaxyx
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "aaxyx"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method aaxyx::.ctor

} // end of class aaxyx

.class public auto ansi beforefieldinit Aaxyx
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "Aaxyx"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method Aaxyx::.ctor

} // end of class Aaxyx

.class public auto ansi beforefieldinit aAxyx
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "aAxyx"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method aAxyx::.ctor

} // end of class aAxyx

.class public auto ansi beforefieldinit aaxyy
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "aaxyy"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method aaxyy::.ctor

} // end of class aaxyy

.class private auto ansi beforefieldinit Aaxyy
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "Aaxyy"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method Aaxyy::.ctor

} // end of class Aaxyy

.class public auto ansi beforefieldinit aAxyy
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "aAxyy"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method aAxyy::.ctor

} // end of class aAxyy

.class public auto ansi beforefieldinit aaxyz
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "aaxyz"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method aaxyz::.ctor

} // end of class aaxyz

.class public auto ansi beforefieldinit Aaxyz
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "Aaxyz"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method Aaxyz::.ctor

} // end of class Aaxyz

.class private auto ansi beforefieldinit aAxyz
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "aAxyz"
    IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method aAxyz::.ctor

} // end of class aAxyz
]]>


            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="ConsoleApplication">
    <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object
        c = New aAxyx()
        c = New aAxyy()
        c = New Aaxyz()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC30554: 'Aaxyx' is ambiguous.
        c = New aAxyx()
                ~~~~~
BC30554: 'aaxyy' is ambiguous.
        c = New aAxyy()
                ~~~~~
BC30554: 'aaxyz' is ambiguous.
        c = New Aaxyz()
                ~~~~~
</expected>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="ConsoleApplication">
    <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object
        c = New aaxxx()
        c = New Aaxxy()
        c = New aAxxz()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation2, expectedOutput:=
            <![CDATA[
aaxxx
Aaxxy
aAxxz
]]>)

            Dim compilation3 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="ConsoleApplication">
    <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object
        c = New aAxyx()
        c = New aAxyy()
        c = New Aaxyz()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation3,
<expected>
BC30554: 'Aaxyx' is ambiguous.
        c = New aAxyx()
                ~~~~~
BC30554: 'aaxyy' is ambiguous.
        c = New aAxyy()
                ~~~~~
BC30554: 'aaxyz' is ambiguous.
        c = New Aaxyz()
                ~~~~~
</expected>)

            Dim compilation4 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="ConsoleApplication1">
    <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object
        c = New aaxxx()
        c = New Aaxxy()
        c = New aAxxz()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation4, expectedOutput:=
            <![CDATA[
aaxxx
Aaxxy
aAxxz
]]>)

        End Sub

        <Fact()>
        Public Sub TypesDifferByCase_5()
            Dim customIL = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) }
.assembly extern Microsoft.VisualBasic { .ver 10:0:0:0 .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly '<<GeneratedFileName>>'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 13 43 6F 6E 73 6F 6C 65 41 70 70 6C 69 63   // ...ConsoleApplic
                                                                                                                 61 74 69 6F 6E 31 00 00 )                         // ation1..
}
.module '<<GeneratedFileName>>.dll'

.class public auto ansi beforefieldinit Container
       extends [mscorlib]System.Object
{
    .class nested public auto ansi beforefieldinit aaxxx
           extends [mscorlib]System.Object
    {
      .method public hidebysig specialname rtspecialname 
              instance void  .ctor() cil managed
      {
        // Code size       21 (0x15)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
        IL_0006:  nop
        IL_0007:  nop
        IL_0008:  ldstr      "aaxxx"
        IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
        IL_0012:  nop
        IL_0013:  nop
        IL_0014:  ret
      } // end of method aaxxx::.ctor

    } // end of class aaxxx

    .class nested assembly auto ansi beforefieldinit Aaxxx
           extends [mscorlib]System.Object
    {
      .method public hidebysig specialname rtspecialname 
              instance void  .ctor() cil managed
      {
        // Code size       21 (0x15)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
        IL_0006:  nop
        IL_0007:  nop
        IL_0008:  ldstr      "Aaxxx"
        IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
        IL_0012:  nop
        IL_0013:  nop
        IL_0014:  ret
      } // end of method Aaxxx::.ctor

    } // end of class Aaxxx

    .class nested assembly auto ansi beforefieldinit aAxxx
           extends [mscorlib]System.Object
    {
      .method public hidebysig specialname rtspecialname 
              instance void  .ctor() cil managed
      {
        // Code size       21 (0x15)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
        IL_0006:  nop
        IL_0007:  nop
        IL_0008:  ldstr      "aAxxx"
        IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
        IL_0012:  nop
        IL_0013:  nop
        IL_0014:  ret
      } // end of method aAxxx::.ctor

    } // end of class aAxxx

    .class nested assembly auto ansi beforefieldinit aaxxy
           extends [mscorlib]System.Object
    {
      .method public hidebysig specialname rtspecialname 
              instance void  .ctor() cil managed
      {
        // Code size       21 (0x15)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
        IL_0006:  nop
        IL_0007:  nop
        IL_0008:  ldstr      "aaxxy"
        IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
        IL_0012:  nop
        IL_0013:  nop
        IL_0014:  ret
      } // end of method aaxxy::.ctor

    } // end of class aaxxy

    .class nested public auto ansi beforefieldinit Aaxxy
           extends [mscorlib]System.Object
    {
      .method public hidebysig specialname rtspecialname 
              instance void  .ctor() cil managed
      {
        // Code size       21 (0x15)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
        IL_0006:  nop
        IL_0007:  nop
        IL_0008:  ldstr      "Aaxxy"
        IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
        IL_0012:  nop
        IL_0013:  nop
        IL_0014:  ret
      } // end of method Aaxxy::.ctor

    } // end of class Aaxxy

    .class nested assembly auto ansi beforefieldinit aAxxy
           extends [mscorlib]System.Object
    {
      .method public hidebysig specialname rtspecialname 
              instance void  .ctor() cil managed
      {
        // Code size       21 (0x15)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
        IL_0006:  nop
        IL_0007:  nop
        IL_0008:  ldstr      "aAxxy"
        IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
        IL_0012:  nop
        IL_0013:  nop
        IL_0014:  ret
      } // end of method aAxxy::.ctor

    } // end of class aAxxy

    .class nested assembly auto ansi beforefieldinit aaxxz
           extends [mscorlib]System.Object
    {
      .method public hidebysig specialname rtspecialname 
              instance void  .ctor() cil managed
      {
        // Code size       21 (0x15)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
        IL_0006:  nop
        IL_0007:  nop
        IL_0008:  ldstr      "aaxxz"
        IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
        IL_0012:  nop
        IL_0013:  nop
        IL_0014:  ret
      } // end of method aaxxz::.ctor

    } // end of class aaxxz

    .class nested assembly auto ansi beforefieldinit Aaxxz
           extends [mscorlib]System.Object
    {
      .method public hidebysig specialname rtspecialname 
              instance void  .ctor() cil managed
      {
        // Code size       21 (0x15)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
        IL_0006:  nop
        IL_0007:  nop
        IL_0008:  ldstr      "Aaxxz"
        IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
        IL_0012:  nop
        IL_0013:  nop
        IL_0014:  ret
      } // end of method Aaxxz::.ctor

    } // end of class Aaxxz

    .class nested public auto ansi beforefieldinit aAxxz
           extends [mscorlib]System.Object
    {
      .method public hidebysig specialname rtspecialname 
              instance void  .ctor() cil managed
      {
        // Code size       21 (0x15)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
        IL_0006:  nop
        IL_0007:  nop
        IL_0008:  ldstr      "aAxxz"
        IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
        IL_0012:  nop
        IL_0013:  nop
        IL_0014:  ret
      } // end of method aAxxz::.ctor

    } // end of class aAxxz

    .class nested assembly auto ansi beforefieldinit aaxyx
           extends [mscorlib]System.Object
    {
      .method public hidebysig specialname rtspecialname 
              instance void  .ctor() cil managed
      {
        // Code size       21 (0x15)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
        IL_0006:  nop
        IL_0007:  nop
        IL_0008:  ldstr      "aaxyx"
        IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
        IL_0012:  nop
        IL_0013:  nop
        IL_0014:  ret
      } // end of method aaxyx::.ctor

    } // end of class aaxyx

    .class nested public auto ansi beforefieldinit Aaxyx
           extends [mscorlib]System.Object
    {
      .method public hidebysig specialname rtspecialname 
              instance void  .ctor() cil managed
      {
        // Code size       21 (0x15)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
        IL_0006:  nop
        IL_0007:  nop
        IL_0008:  ldstr      "Aaxyx"
        IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
        IL_0012:  nop
        IL_0013:  nop
        IL_0014:  ret
      } // end of method Aaxyx::.ctor

    } // end of class Aaxyx

    .class nested public auto ansi beforefieldinit aAxyx
           extends [mscorlib]System.Object
    {
      .method public hidebysig specialname rtspecialname 
              instance void  .ctor() cil managed
      {
        // Code size       21 (0x15)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
        IL_0006:  nop
        IL_0007:  nop
        IL_0008:  ldstr      "aAxyx"
        IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
        IL_0012:  nop
        IL_0013:  nop
        IL_0014:  ret
      } // end of method aAxyx::.ctor

    } // end of class aAxyx

    .class nested public auto ansi beforefieldinit aaxyy
           extends [mscorlib]System.Object
    {
      .method public hidebysig specialname rtspecialname 
              instance void  .ctor() cil managed
      {
        // Code size       21 (0x15)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
        IL_0006:  nop
        IL_0007:  nop
        IL_0008:  ldstr      "aaxyy"
        IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
        IL_0012:  nop
        IL_0013:  nop
        IL_0014:  ret
      } // end of method aaxyy::.ctor

    } // end of class aaxyy

    .class nested assembly auto ansi beforefieldinit Aaxyy
           extends [mscorlib]System.Object
    {
      .method public hidebysig specialname rtspecialname 
              instance void  .ctor() cil managed
      {
        // Code size       21 (0x15)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
        IL_0006:  nop
        IL_0007:  nop
        IL_0008:  ldstr      "Aaxyy"
        IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
        IL_0012:  nop
        IL_0013:  nop
        IL_0014:  ret
      } // end of method Aaxyy::.ctor

    } // end of class Aaxyy

    .class nested public auto ansi beforefieldinit aAxyy
           extends [mscorlib]System.Object
    {
      .method public hidebysig specialname rtspecialname 
              instance void  .ctor() cil managed
      {
        // Code size       21 (0x15)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
        IL_0006:  nop
        IL_0007:  nop
        IL_0008:  ldstr      "aAxyy"
        IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
        IL_0012:  nop
        IL_0013:  nop
        IL_0014:  ret
      } // end of method aAxyy::.ctor

    } // end of class aAxyy

    .class nested public auto ansi beforefieldinit aaxyz
           extends [mscorlib]System.Object
    {
      .method public hidebysig specialname rtspecialname 
              instance void  .ctor() cil managed
      {
        // Code size       21 (0x15)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
        IL_0006:  nop
        IL_0007:  nop
        IL_0008:  ldstr      "aaxyz"
        IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
        IL_0012:  nop
        IL_0013:  nop
        IL_0014:  ret
      } // end of method aaxyz::.ctor

    } // end of class aaxyz

    .class nested public auto ansi beforefieldinit Aaxyz
           extends [mscorlib]System.Object
    {
      .method public hidebysig specialname rtspecialname 
              instance void  .ctor() cil managed
      {
        // Code size       21 (0x15)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
        IL_0006:  nop
        IL_0007:  nop
        IL_0008:  ldstr      "Aaxyz"
        IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
        IL_0012:  nop
        IL_0013:  nop
        IL_0014:  ret
      } // end of method Aaxyz::.ctor

    } // end of class Aaxyz

    .class nested assembly auto ansi beforefieldinit aAxyz
           extends [mscorlib]System.Object
    {
      .method public hidebysig specialname rtspecialname 
              instance void  .ctor() cil managed
      {
        // Code size       21 (0x15)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
        IL_0006:  nop
        IL_0007:  nop
        IL_0008:  ldstr      "aAxyz"
        IL_000d:  call       void [mscorlib]System.Console::WriteLine(string)
        IL_0012:  nop
        IL_0013:  nop
        IL_0014:  ret
      } // end of method aAxyz::.ctor

    } // end of class aAxyz

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Container::.ctor

} // end of class Container
]]>


            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="ConsoleApplication">
    <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object
        c = New Container.aAxyx()
        c = New Container.aAxyy()
        c = New Container.Aaxyz()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC31429: 'Aaxyx' is ambiguous because multiple kinds of members with this name exist in class 'Container'.
        c = New Container.aAxyx()
                ~~~~~~~~~~~~~~~
BC31429: 'aaxyy' is ambiguous because multiple kinds of members with this name exist in class 'Container'.
        c = New Container.aAxyy()
                ~~~~~~~~~~~~~~~
BC31429: 'aaxyz' is ambiguous because multiple kinds of members with this name exist in class 'Container'.
        c = New Container.Aaxyz()
                ~~~~~~~~~~~~~~~
</expected>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="ConsoleApplication">
    <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object
        c = New Container.aaxxx()
        c = New Container.Aaxxy()
        c = New Container.aAxxz()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation2, expectedOutput:=
            <![CDATA[
aaxxx
Aaxxy
aAxxz
]]>)

            Dim compilation3 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="ConsoleApplication">
    <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object
        c = New Container.aAxyx()
        c = New Container.aAxyy()
        c = New Container.Aaxyz()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation3,
<expected>
BC31429: 'Aaxyx' is ambiguous because multiple kinds of members with this name exist in class 'Container'.
        c = New Container.aAxyx()
                ~~~~~~~~~~~~~~~
BC31429: 'aaxyy' is ambiguous because multiple kinds of members with this name exist in class 'Container'.
        c = New Container.aAxyy()
                ~~~~~~~~~~~~~~~
BC31429: 'aaxyz' is ambiguous because multiple kinds of members with this name exist in class 'Container'.
        c = New Container.Aaxyz()
                ~~~~~~~~~~~~~~~
</expected>)

            Dim compilation4 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="ConsoleApplication1">
    <file name="a.vb">
Module Program
    Sub Main
        Dim c As Object
        c = New Container.aaxxx()
        c = New Container.Aaxxy()
        c = New Container.aAxxz()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation4, expectedOutput:=
            <![CDATA[
aaxxx
Aaxxy
aAxxz
]]>)

        End Sub

        <Fact()>
        Public Sub MembersDifferByKindAndAccessibility_3()
            Dim customIL = <![CDATA[
.class public auto ansi beforefieldinit Container1
       extends [mscorlib]System.Object
{
  .field family string aAxxx
  .field family string aAxxy
  .field public string aAxxz
  .field public string aAxyx
  .field public string aAxyy
  .field family string aAxyz
  .method family hidebysig specialname instance string 
          get_Aaxxx() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.Aaxxx"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::get_Aaxxx

  .method public hidebysig instance string 
          AAxxx() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.AAxxx"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::AAxxx

  .method public hidebysig specialname instance string 
          get_Aaxxy() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.Aaxxy"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::get_Aaxxy

  .method family hidebysig instance string 
          AAxxy() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.AAxxy"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::AAxxy

  .method family hidebysig specialname instance string 
          get_Aaxxz() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.Aaxxz"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::get_Aaxxz

  .method family hidebysig instance string 
          AAxxz() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.AAxxz"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::AAxxz

  .method public hidebysig specialname instance string 
          get_Aaxyx() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.Aaxyx"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::get_Aaxyx

  .method family hidebysig instance string 
          AAxyx() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.AAxyx"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::AAxyx

  .method family hidebysig specialname instance string 
          get_Aaxyy() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.Aaxyy"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::get_Aaxyy

  .method public hidebysig instance string 
          AAxyy() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.AAxyy"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::AAxyy

  .method public hidebysig specialname instance string 
          get_Aaxyz() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.Aaxyz"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::get_Aaxyz

  .method public hidebysig instance string 
          AAxyz() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.AAxyz"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::AAxyz

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       74 (0x4a)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldstr      "Container1.aAxxx"
    IL_0006:  stfld      string Container1::aAxxx
    IL_000b:  ldarg.0
    IL_000c:  ldstr      "Container1.aAxxy"
    IL_0011:  stfld      string Container1::aAxxy
    IL_0016:  ldarg.0
    IL_0017:  ldstr      "Container1.aAxxz"
    IL_001c:  stfld      string Container1::aAxxz
    IL_0021:  ldarg.0
    IL_0022:  ldstr      "Container1.aAxyx"
    IL_0027:  stfld      string Container1::aAxyx
    IL_002c:  ldarg.0
    IL_002d:  ldstr      "Container1.aAxyy"
    IL_0032:  stfld      string Container1::aAxyy
    IL_0037:  ldarg.0
    IL_0038:  ldstr      "Container1.aAxyz"
    IL_003d:  stfld      string Container1::aAxyz
    IL_0042:  ldarg.0
    IL_0043:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0048:  nop
    IL_0049:  ret
  } // end of method Container1::.ctor

  .property instance string Aaxxx()
  {
    .get instance string Container1::get_Aaxxx()
  } // end of property Container1::Aaxxx
  .property instance string Aaxxy()
  {
    .get instance string Container1::get_Aaxxy()
  } // end of property Container1::Aaxxy
  .property instance string Aaxxz()
  {
    .get instance string Container1::get_Aaxxz()
  } // end of property Container1::Aaxxz
  .property instance string Aaxyx()
  {
    .get instance string Container1::get_Aaxyx()
  } // end of property Container1::Aaxyx
  .property instance string Aaxyy()
  {
    .get instance string Container1::get_Aaxyy()
  } // end of property Container1::Aaxyy
  .property instance string Aaxyz()
  {
    .get instance string Container1::get_Aaxyz()
  } // end of property Container1::Aaxyz
} // end of class Container1
]]>

            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program

    Class Test
        Inherits Container1

        Sub Test()
            System.Console.WriteLine(AAxyx)
            System.Console.WriteLine(AAxyy)
            System.Console.WriteLine(AAxyz)
        End Sub
    End Class

    Sub Main
        Dim c As New Container1()
        System.Console.WriteLine(c.AAxyx)
        System.Console.WriteLine(c.AAxyy)
        System.Console.WriteLine(c.AAxyz)
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC31429: 'Aaxyx' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
            System.Console.WriteLine(AAxyx)
                                     ~~~~~
BC31429: 'AAxyy' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
            System.Console.WriteLine(AAxyy)
                                     ~~~~~
BC31429: 'AAxyz' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
            System.Console.WriteLine(AAxyz)
                                     ~~~~~
BC31429: 'Aaxyx' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
        System.Console.WriteLine(c.AAxyx)
                                 ~~~~~~~
BC31429: 'AAxyy' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
        System.Console.WriteLine(c.AAxyy)
                                 ~~~~~~~
BC31429: 'AAxyz' is ambiguous because multiple kinds of members with this name exist in class 'Container1'.
        System.Console.WriteLine(c.AAxyz)
                                 ~~~~~~~
</expected>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program

    Class Test
        Inherits Container1

        Sub Test()
            System.Console.WriteLine(AAxxx)
            System.Console.WriteLine(Aaxxy)
            System.Console.WriteLine(aAxxz)
        End Sub
    End Class

    Sub Main
        Dim c As New Container1()
        System.Console.WriteLine(c.AAxxx)
        System.Console.WriteLine(c.Aaxxy)
        System.Console.WriteLine(c.aAxxz)

        Dim t1 As New Test()
        t1.Test()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation2, expectedOutput:=
            <![CDATA[
Container1.AAxxx
Container1.Aaxxy
Container1.aAxxz
Container1.AAxxx
Container1.Aaxxy
Container1.aAxxz
]]>)

        End Sub

        <Fact()>
        Public Sub OverloadingInBase_1()
            Dim customIL = <![CDATA[
.class public auto ansi beforefieldinit Container1
       extends [mscorlib]System.Object
{
  .field family string goO
  .method family hidebysig instance void 
          GoO(int32 x) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method Container1::GoO

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Container1::.ctor

} // end of class Container1

.class public auto ansi beforefieldinit Container2
       extends Container1
{
  .field family string goo
  .method public hidebysig instance void 
          Goo() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Container2.Goo"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container2::Goo

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Container1::.ctor()
    IL_0006:  ret
  } // end of method Container2::.ctor

} // end of class Container2
]]>

            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program
    Class Test
        Inherits Container2

        Sub Test()
            Goo(1)
        End Sub
    End Class

    Sub Main
        Dim c As New Container2()
        c.Goo(1)
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC30057: Too many arguments to 'Public Overloads Sub Goo()'.
            Goo(1)
                ~
BC30057: Too many arguments to 'Public Overloads Sub Goo()'.
        c.Goo(1)
              ~
</expected>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program
    Class Test
        Inherits Container2

        Sub Test()
            Goo()
        End Sub
    End Class

    Sub Main
        Dim c As New Container2()
        c.Goo()
        Dim tt As New Test()
        tt.Test()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation2, expectedOutput:=
            <![CDATA[
Container2.Goo
Container2.Goo
]]>)
        End Sub

        <Fact()>
        Public Sub OverloadingInBase_2()
            Dim customIL = <![CDATA[
.class public auto ansi beforefieldinit Container1
       extends [mscorlib]System.Object
{
  .field public string goO
  .method family hidebysig instance void 
          GoO(int32 x) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method Container1::GoO

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Container1::.ctor

} // end of class Container1

.class public auto ansi beforefieldinit Container2
       extends Container1
{
  .field family string goo
  .method public hidebysig instance void 
          Goo() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method Container2::Goo

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Container1::.ctor()
    IL_0006:  ret
  } // end of method Container2::.ctor

} // end of class Container2
]]>

            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program
    Class Test
        Inherits Container2

        Sub Test()
            Goo(1)
        End Sub
    End Class

    Sub Main
        Dim c As New Container2()
        c.Goo(1)
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC30057: Too many arguments to 'Public Overloads Sub Goo()'.
            Goo(1)
                ~
BC30057: Too many arguments to 'Public Overloads Sub Goo()'.
        c.Goo(1)
              ~
</expected>)
        End Sub

        <Fact()>
        Public Sub OverloadingInBase_3()
            Dim customIL = <![CDATA[
.class public auto ansi beforefieldinit Container1
       extends [mscorlib]System.Object
{
  .field family string goO
  .method public hidebysig instance void 
          GoO(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.GoO"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container1::GoO

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Container1::.ctor

} // end of class Container1

.class public auto ansi beforefieldinit Container2
       extends Container1
{
  .field family string goo
  .method public hidebysig instance void 
          Goo() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method Container2::Goo

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Container1::.ctor()
    IL_0006:  ret
  } // end of method Container2::.ctor

} // end of class Container2
]]>

            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program
    Class Test
        Inherits Container2

        Sub Test()
            Goo(1)
        End Sub
    End Class

    Sub Main
        Dim c As New Container2()
        c.Goo(1)
        Dim t As New Test()
        t.Test()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation1, expectedOutput:=
            <![CDATA[
Container1.GoO
Container1.GoO
]]>)
        End Sub

        <Fact()>
        Public Sub OverloadingInBase_4()
            Dim customIL = <![CDATA[
.class public auto ansi beforefieldinit Container1
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void 
          GoO(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.GoO"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container1::GoO

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Container1::.ctor

} // end of class Container1

.class public auto ansi beforefieldinit Container2
       extends Container1
{
  .method public hidebysig instance void 
          GOO(int64 x) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method Container2::GOO

  .method public hidebysig instance void 
          goo(int64 x) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method Container2::goo

  .method public hidebysig instance void 
          gOo(int64 x) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method Container2::gOo

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Container1::.ctor()
    IL_0006:  ret
  } // end of method Container2::.ctor

} // end of class Container2
]]>

            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program
    Sub Main
        Dim c As New Container2()
        c.Goo(Integer.MaxValue)
        Dim m As Long = Integer.MaxValue
        c.Goo(m)
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            ' !!! c.Goo(Integer.MaxValue) - Dev10 reports an error BC31429: 'GOO' is ambiguous because multiple kinds of members with this name exist in class 'Container2'.
            ' !!! c.Goo(m)                - Dev10 reports an error BC31429: 'GOO' is ambiguous because multiple kinds of members with this name exist in class 'Container2'.
            CompileAndVerify(compilation1, expectedOutput:=
            <![CDATA[
Container1.GoO
Container1.GoO
]]>)
        End Sub

        <Fact()>
        Public Sub OverloadingInBase_5()
            Dim customIL = <![CDATA[
.class public auto ansi beforefieldinit Container1
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void 
          GoO(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.GoO"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container1::GoO

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Container1::.ctor

} // end of class Container1

.class public auto ansi beforefieldinit Container2
       extends Container1
{
  .method public hidebysig instance void 
          GOO(int64 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Container2.GOO"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container2::GOO

  .method family hidebysig instance void 
          goo(int64 x) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method Container2::goo

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Container1::.ctor()
    IL_0006:  ret
  } // end of method Container2::.ctor

} // end of class Container2
]]>

            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program
    Class Test
        Inherits Container2

        Sub Test()
            Goo(Long.MaxValue)
            Goo(Integer.MaxValue)
        End Sub
    End Class

    Sub Main
        Dim c As New Container2()
        c.Goo(Long.MaxValue)
        c.Goo(Integer.MaxValue)

        Dim t As New Test()
        t.Test()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation1, expectedOutput:=
            <![CDATA[
Container2.GOO
Container1.GoO
Container2.GOO
Container1.GoO
]]>)
        End Sub

        <Fact()>
        Public Sub OverloadingInBase_6()
            Dim customIL = <![CDATA[
.class public auto ansi beforefieldinit Container1
       extends [mscorlib]System.Object
{
  .field public string GoO
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       19 (0x13)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldstr      "Container1.GoO"
    IL_0006:  stfld      string Container1::GoO
    IL_000b:  ldarg.0
    IL_000c:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0011:  nop
    IL_0012:  ret
  } // end of method Container1::.ctor

} // end of class Container1

.class public auto ansi beforefieldinit Container2
       extends Container1
{
  .method family hidebysig instance string 
          GOO() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container2.GOO"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container2::GOO

  .method family hidebysig instance string 
          goo() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container2.goo"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container2::goo

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Container1::.ctor()
    IL_0006:  ret
  } // end of method Container2::.ctor

} // end of class Container2
]]>

            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program
    Sub Main
        Dim c As New Container2()
        System.Console.WriteLine(c.GoO)
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation1, expectedOutput:=
            <![CDATA[
Container1.GoO
]]>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program
    Class Test
        Inherits Container2

        Sub Test()
            System.Console.WriteLine(Goo)
        End Sub
    End Class

    Sub Main
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation2,
<expected>
BC31429: 'GOO' is ambiguous because multiple kinds of members with this name exist in class 'Container2'.
            System.Console.WriteLine(Goo)
                                     ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub OverloadingInBase_7()
            Dim customIL = <![CDATA[
.class public auto ansi beforefieldinit Container1
       extends [mscorlib]System.Object
{
  .method public hidebysig instance string 
          GoO(int32 x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.GoO"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::GoO

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Container1::.ctor

} // end of class Container1

.class public auto ansi beforefieldinit Container2
       extends Container1
{
  .method family hidebysig instance string 
          GOO() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container2.GOO"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container2::GOO

  .method family hidebysig instance string 
          goo() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container2.goo"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container2::goo

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Container1::.ctor()
    IL_0006:  ret
  } // end of method Container2::.ctor

} // end of class Container2

.class public auto ansi beforefieldinit Container3
       extends Container2
{
  .method public hidebysig instance string 
          gOO(int64 x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container3.gOO"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container3::gOO

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Container2::.ctor()
    IL_0006:  ret
  } // end of method Container3::.ctor

} // end of class Container3
]]>

            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program
    Class Test
        Inherits Container3

        Sub Test()
            System.Console.WriteLine(gOO(Long.MaxValue))
            System.Console.WriteLine(gOO(Integer.MaxValue))
        End Sub
    End Class

    Sub Main()
        Dim c As New Container3()
        System.Console.WriteLine(c.gOO(Long.MaxValue))
        System.Console.WriteLine(c.gOO(Integer.MaxValue))

        Dim t As New Test()
        t.Test()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation1, expectedOutput:=
            <![CDATA[
Container3.gOO
Container1.GoO
Container3.gOO
Container1.GoO
]]>)
        End Sub

        <Fact()>
        Public Sub OverloadingInBase_8()
            Dim customIL = <![CDATA[
.class public auto ansi beforefieldinit Container1
       extends [mscorlib]System.Object
{
  .method public hidebysig instance string 
          GoO(int32 x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container1.GoO"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container1::GoO

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Container1::.ctor

} // end of class Container1

.class public auto ansi beforefieldinit Container2
       extends Container1
{
  .method family hidebysig instance string 
          GOO(int32 x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container2.GOO"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container2::GOO

  .method family hidebysig instance string 
          goo(int32 x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container2.goo"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container2::goo

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Container1::.ctor()
    IL_0006:  ret
  } // end of method Container2::.ctor

} // end of class Container2

.class public auto ansi beforefieldinit Container3
       extends Container2
{
  .method public hidebysig instance string 
          gOO(int64 x) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Container3.gOO"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Container3::gOO

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Container2::.ctor()
    IL_0006:  ret
  } // end of method Container3::.ctor

} // end of class Container3
]]>

            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Program
    Class Test
        Inherits Container3

        Sub Test()
            System.Console.WriteLine(gOO(Long.MaxValue))
            System.Console.WriteLine(gOO(Integer.MaxValue))
        End Sub
    End Class

    Sub Main()
        Dim c As New Container3()
        System.Console.WriteLine(c.gOO(Long.MaxValue))
        System.Console.WriteLine(c.gOO(Integer.MaxValue))

        Dim t As New Test()
        t.Test()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation1, expectedOutput:=
            <![CDATA[
Container3.gOO
Container1.GoO
Container3.gOO
Container1.GoO
]]>)
        End Sub

        <Fact()>
        Public Sub ModuleMembersDifferByCase_1()
            Dim customIL = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) }
.assembly extern Microsoft.VisualBasic { .ver 10:0:0:0 .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
    
.assembly '<<GeneratedFileName>>'
{
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
}
.module '<<GeneratedFileName>>.dll'

.class public abstract auto ansi sealed beforefieldinit Extensions.c
       extends [mscorlib]System.Object
{
  .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  Goo(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.3
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method c::Goo

  .method public hidebysig static void  GoO(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method c::GoO

} // end of class Extensions.c

.class public abstract auto ansi sealed beforefieldinit Extensions.D
       extends [mscorlib]System.Object
{
  .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  Goo(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.2
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method D::Goo

} // end of class Extensions.D

    ]]>


            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Imports Extensions
 
Module Program
    Sub Main
        Dim x As Integer = 1
        Goo(x)
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC30562: 'Goo' is ambiguous between declarations in Modules 'Extensions.c, Extensions.c, Extensions.D'.
        Goo(x)
        ~~~
</expected>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Imports Extensions
 
Module Program
    Sub Main
        Dim x As Integer = 1
        x.Goo()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe)

            ' Dev10 reports error BC30521: Overload resolution failed because no accessible 'Goo' is most specific for these arguments:
            CompilationUtils.AssertTheseDiagnostics(compilation2,
<expected>
BC30521: Overload resolution failed because no accessible 'Goo' is most specific for these arguments:
    Extension method 'Public Sub Goo()' defined in 'c': Not most specific.
    Extension method 'Public Sub GoO()' defined in 'c': Not most specific.
    Extension method 'Public Sub Goo()' defined in 'D': Not most specific.
        x.Goo()
          ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub ModuleMembersDifferByCase_2()
            Dim customIL = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) }
.assembly extern Microsoft.VisualBasic { .ver 10:0:0:0 .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly '<<GeneratedFileName>>'
{
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
}
.module '<<GeneratedFileName>>.dll'

.class public abstract auto ansi sealed beforefieldinit Extensions.c
       extends [mscorlib]System.Object
{
  .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  Goo(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.3
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method c::Goo

  .method public hidebysig static void  GoO(int32 x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method c::GoO

} // end of class Extensions.c
]]>


            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Imports Extensions
 
Module Program
    Sub Main
        Dim x As Integer = 1
        x.Goo()
        Goo(x)
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30521: Overload resolution failed because no accessible 'Goo' is most specific for these arguments:
    Extension method 'Public Sub Goo()' defined in 'c': Not most specific.
    Extension method 'Public Sub GoO()' defined in 'c': Not most specific.
        x.Goo()
          ~~~
BC31429: 'Goo' is ambiguous because multiple kinds of members with this name exist in module 'c'.
        Goo(x)
        ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub ModuleMembersDifferByCase_3()
            Dim customIL = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) }
.assembly extern Microsoft.VisualBasic { .ver 10:0:0:0 .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly '<<GeneratedFileName>>'
{
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
}
.module '<<GeneratedFileName>>.dll'

.class public abstract auto ansi sealed beforefieldinit Extensions.c
       extends [mscorlib]System.Object
{
  .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  Goo(class [mscorlib]System.ValueType x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.3
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method c::Goo

  .method public hidebysig static void  GoO(class [mscorlib]System.ValueType x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method c::GoO

} // end of class Extensions.c

.class public abstract auto ansi sealed beforefieldinit Extensions.D
       extends [mscorlib]System.Object
{
  .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  Goo(object x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.2
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method D::Goo

} // end of class Extensions.D
]]>


            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Imports Extensions
 
Module Program
    Sub Main
        Dim x As Integer = 1
        Goo(x)
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC30562: 'Goo' is ambiguous between declarations in Modules 'Extensions.c, Extensions.c, Extensions.D'.
        Goo(x)
        ~~~
</expected>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Imports Extensions
 
Module Program
    Sub Main
        Dim x As Integer = 1
        x.Goo()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe)

            ' Dev10 reports error BC30521: Overload resolution failed because no accessible 'Goo' is most specific for these arguments:
            CompilationUtils.AssertTheseDiagnostics(compilation2,
<expected>
BC30521: Overload resolution failed because no accessible 'Goo' is most specific for these arguments:
    Extension method 'Public Sub Goo()' defined in 'c': Not most specific.
    Extension method 'Public Sub GoO()' defined in 'c': Not most specific.
        x.Goo()
          ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub ModuleMembersDifferByCase_4()
            Dim customIL = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) }
.assembly extern Microsoft.VisualBasic { .ver 10:0:0:0 .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }


.assembly '<<GeneratedFileName>>'
{
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
}
.module '<<GeneratedFileName>>.dll'

.class public abstract auto ansi sealed beforefieldinit Extensions.c
       extends [mscorlib]System.Object
{
  .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  Goo(object x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.3
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method c::Goo

  .method public hidebysig static void  GoO(object x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method c::GoO

} // end of class Extensions.c

.class public abstract auto ansi sealed beforefieldinit Extensions.D
       extends [mscorlib]System.Object
{
  .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig static void  Goo(class [mscorlib]System.ValueType x) cil managed
  {
    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.2
    IL_0002:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0007:  nop
    IL_0008:  ret
  } // end of method D::Goo

} // end of class Extensions.D
]]>


            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Imports Extensions
 
Module Program
    Sub Main
        Dim x As Integer = 1
        x.Goo()
        Goo(x)
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30562: 'Goo' is ambiguous between declarations in Modules 'Extensions.c, Extensions.c, Extensions.D'.
        Goo(x)
        ~~~
</expected>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Imports Extensions
 
Module Program
    Sub Main
        Dim x As Integer = 1
        x.Goo()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation2, expectedOutput:=
            <![CDATA[
2
]]>)
        End Sub

        <Fact()>
        Public Sub SameKindOverloadingInSameContainer_1()
            Dim customIL = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) }
.assembly extern Microsoft.VisualBasic { .ver 10:0:0:0 .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly '<<GeneratedFileName>>'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 13 43 6F 6E 73 6F 6C 65 41 70 70 6C 69 63   // ...ConsoleApplic
                                                                                                                 61 74 69 6F 6E 31 00 00 )                         // ation1..
}
.module '<<GeneratedFileName>>.dll'

.class public auto ansi beforefieldinit Container31
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void 
          aAxxx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "aAxxx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aAxxx

  .method public hidebysig instance void 
          Aaxxx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Aaxxx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::Aaxxx

  .method assembly hidebysig instance void 
          aAxxy(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "aAxxy"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aAxxy

  .method public hidebysig instance void 
          Aaxxy(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Aaxxy"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::Aaxxy

  .method public hidebysig instance void 
          aAxxz(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "aAxxz"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aAxxz

  .method assembly hidebysig instance void 
          Aaxxz(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Aaxxz"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::Aaxxz

  .method assembly hidebysig instance void 
          aAxyx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "aAxyx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aAxyx

  .method public hidebysig instance void 
          Aaxyx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Aaxyx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::Aaxyx

  .method public hidebysig instance void 
          AAxyx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAxyx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::AAxyx

  .method public hidebysig instance void 
          aAxyy(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "aAxyy"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aAxyy

  .method assembly hidebysig instance void 
          Aaxyy(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Aaxyy"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::Aaxyy

  .method public hidebysig instance void 
          AAxyy(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAxyy"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::AAxyy

  .method public hidebysig instance void 
          aAxyz(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "aAxyz"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aAxyz

  .method public hidebysig instance void 
          Aaxyz(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Aaxyz"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::Aaxyz

  .method assembly hidebysig instance void 
          AAxyz(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAxyz"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::AAxyz

  .method public hidebysig instance void 
          aAxzx(int32 x,
                int32 y) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "aAxzx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aAxzx

  .method public hidebysig instance void 
          Aaxzx(int32 x,
                int32 y) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Aaxzx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::Aaxzx

  .method public hidebysig instance void 
          AAxzx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAxzx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::AAxzx

  .method public hidebysig instance void 
          aaxzx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAxzx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aaxzx

  .method public hidebysig instance void 
          aAxzy(int32 x,
                int32 y) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "aAxzy"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aAxzy

  .method public hidebysig instance void 
          AAxzy(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAxzy"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::AAxzy

  .method public hidebysig instance void 
          Aaxzy(int32 x,
                int32 y) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Aaxzy"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::Aaxzy

  .method public hidebysig instance void 
          aaxzy(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAxzy"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aaxzy

  .method public hidebysig instance void 
          aAxzz(int32 x,
                int32 y) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "aAxzz"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aAxzz

  .method public hidebysig instance void 
          AAxzz(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAxzz"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::AAxzz

  .method public hidebysig instance void 
          aaxzz(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAxzz"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aaxzz

  .method public hidebysig instance void 
          Aaxzz(int32 x,
                int32 y) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Aaxzz"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::Aaxzz

  .method public hidebysig instance void 
          aAyxx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "aAyxx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aAyxx

  .method assembly hidebysig instance void 
          Aayxx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Aayxx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::Aayxx

  .method assembly hidebysig instance void 
          AAyxx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAyxx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::AAyxx

  .method assembly hidebysig instance void 
          aayxx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAyxx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aayxx

  .method assembly hidebysig instance void 
          aAyxy(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "aAyxy"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aAyxy

  .method public hidebysig instance void 
          Aayxy(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Aayxy"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::Aayxy

  .method assembly hidebysig instance void 
          AAyxy(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAyxy"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::AAyxy

  .method assembly hidebysig instance void 
          aayxy(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAyxy"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aayxy

  .method assembly hidebysig instance void 
          aAyxz(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "aAyxz"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aAyxz

  .method assembly hidebysig instance void 
          Aayxz(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Aayxz"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::Aayxz

  .method public hidebysig instance void 
          AAyxz(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAyxz"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::AAyxz

  .method assembly hidebysig instance void 
          aayxz(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAyxz"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aayxz

  .method assembly hidebysig instance void 
          aAyyx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "aAyyx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aAyyx

  .method assembly hidebysig instance void 
          Aayyx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Aayyx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::Aayyx

  .method assembly hidebysig instance void 
          AAyyx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAyyx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::AAyyx

  .method public hidebysig instance void 
          aayyx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAyyx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aayyx

  .method assembly hidebysig instance void 
          aAyyy(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "aAyyy"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aAyyy

  .method assembly hidebysig instance void 
          Aayyy(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Aayyy"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::Aayyy

  .method public hidebysig instance void 
          AAyyy(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAyyy"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::AAyyy

  .method public hidebysig instance void 
          aayyy(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAyyy"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aayyy

  .method assembly hidebysig instance void 
          aAyyz(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "aAyyz"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aAyyz

  .method public hidebysig instance void 
          Aayyz(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Aayyz"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::Aayyz

  .method assembly hidebysig instance void 
          AAyyz(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAyyz"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::AAyyz

  .method public hidebysig instance void 
          aayyz(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAyyz"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aayyz

  .method assembly hidebysig instance void 
          aAyzx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "aAyzx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aAyzx

  .method public hidebysig instance void 
          Aayzx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Aayzx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::Aayzx

  .method public hidebysig instance void 
          AAyzx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAyzx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::AAyzx

  .method assembly hidebysig instance void 
          aayzx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAyzx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aayzx

  .method public hidebysig instance void 
          aAyzy(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "aAyzy"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aAyzy

  .method assembly hidebysig instance void 
          Aayzy(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Aayzy"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::Aayzy

  .method assembly hidebysig instance void 
          AAyzy(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAyzy"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::AAyzy

  .method public hidebysig instance void 
          aayzy(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAyzy"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aayzy

  .method public hidebysig instance void 
          aAyzz(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "aAyzz"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aAyzz

  .method assembly hidebysig instance void 
          Aayzz(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Aayzz"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::Aayzz

  .method public hidebysig instance void 
          AAyzz(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAyzz"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::AAyzz

  .method assembly hidebysig instance void 
          aayzz(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAyzz"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aayzz

  .method public hidebysig instance void 
          aAzxx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "aAzxx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aAzxx

  .method public hidebysig instance void 
          Aazxx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Aazxx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::Aazxx

  .method assembly hidebysig instance void 
          AAzxx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAzxx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::AAzxx

  .method assembly hidebysig instance void 
          aazxx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "AAzxx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container31::aazxx

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Container31::.ctor

} // end of class Container31
]]>


            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="ConsoleApplication">
    <file name="a.vb">
Module Program
    Sub Main
        Dim cc As New Container31

        cc.aAxxx(1)
        cc.aAxyx(1)
        cc.aAxyy(1)
        cc.aAxyz(1)
        cc.aAxzx(1, 2)
        cc.Aaxzx(1)
        cc.aAxzy(1, 2)
        cc.AAxzy(1)
        cc.aAxzz(1, 2)
        cc.AAxzz(1)
        cc.aAyyy(1)
        cc.aAyyz(1)
        cc.aAyzx(1)
        cc.aAyzy(1)
        cc.aAyzz(1)
        cc.aAzxx(1)

        cc.aAxzx()
        cc.aAxzy()
        cc.aAxzz()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC31429: 'aAxxx' is ambiguous because multiple kinds of members with this name exist in class 'Container31'.
        cc.aAxxx(1)
           ~~~~~
BC31429: 'Aaxyx' is ambiguous because multiple kinds of members with this name exist in class 'Container31'.
        cc.aAxyx(1)
           ~~~~~
BC31429: 'aAxyy' is ambiguous because multiple kinds of members with this name exist in class 'Container31'.
        cc.aAxyy(1)
           ~~~~~
BC31429: 'aAxyz' is ambiguous because multiple kinds of members with this name exist in class 'Container31'.
        cc.aAxyz(1)
           ~~~~~
BC31429: 'aAxzx' is ambiguous because multiple kinds of members with this name exist in class 'Container31'.
        cc.aAxzx(1, 2)
           ~~~~~
BC31429: 'AAxzx' is ambiguous because multiple kinds of members with this name exist in class 'Container31'.
        cc.Aaxzx(1)
           ~~~~~
BC31429: 'aAxzy' is ambiguous because multiple kinds of members with this name exist in class 'Container31'.
        cc.aAxzy(1, 2)
           ~~~~~
BC31429: 'AAxzy' is ambiguous because multiple kinds of members with this name exist in class 'Container31'.
        cc.AAxzy(1)
           ~~~~~
BC31429: 'aAxzz' is ambiguous because multiple kinds of members with this name exist in class 'Container31'.
        cc.aAxzz(1, 2)
           ~~~~~
BC31429: 'AAxzz' is ambiguous because multiple kinds of members with this name exist in class 'Container31'.
        cc.AAxzz(1)
           ~~~~~
BC31429: 'AAyyy' is ambiguous because multiple kinds of members with this name exist in class 'Container31'.
        cc.aAyyy(1)
           ~~~~~
BC31429: 'Aayyz' is ambiguous because multiple kinds of members with this name exist in class 'Container31'.
        cc.aAyyz(1)
           ~~~~~
BC31429: 'Aayzx' is ambiguous because multiple kinds of members with this name exist in class 'Container31'.
        cc.aAyzx(1)
           ~~~~~
BC31429: 'aAyzy' is ambiguous because multiple kinds of members with this name exist in class 'Container31'.
        cc.aAyzy(1)
           ~~~~~
BC31429: 'aAyzz' is ambiguous because multiple kinds of members with this name exist in class 'Container31'.
        cc.aAyzz(1)
           ~~~~~
BC31429: 'aAzxx' is ambiguous because multiple kinds of members with this name exist in class 'Container31'.
        cc.aAzxx(1)
           ~~~~~
BC31429: 'aAxzx' is ambiguous because multiple kinds of members with this name exist in class 'Container31'.
        cc.aAxzx()
           ~~~~~
BC31429: 'aAxzy' is ambiguous because multiple kinds of members with this name exist in class 'Container31'.
        cc.aAxzy()
           ~~~~~
BC31429: 'aAxzz' is ambiguous because multiple kinds of members with this name exist in class 'Container31'.
        cc.aAxzz()
           ~~~~~
</expected>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="ConsoleApplication1">
    <file name="a.vb">
Module Program
    Sub Main
        Dim cc As New Container31

        cc.Aaxxy(1)
        cc.aAxxz(1)
        cc.aAyxx(1)
        cc.Aayxy(1)
        cc.AAyxz(1)
        cc.aayyx(1)
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation2, expectedOutput:=
            <![CDATA[
Aaxxy
aAxxz
aAyxx
Aayxy
AAyxz
AAyyx
]]>)
        End Sub

        <Fact()>
        Public Sub SameKindOverloadingInSameContainer_2()
            Dim customIL = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) }
.assembly extern Microsoft.VisualBasic { .ver 10:0:0:0 .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly '<<GeneratedFileName>>'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 13 43 6F 6E 73 6F 6C 65 41 70 70 6C 69 63   // ...ConsoleApplic
                                                                                                                 61 74 69 6F 6E 31 00 00 )                         // ation1..
}
.module '<<GeneratedFileName>>.dll'

.class public auto ansi beforefieldinit Container32
       extends [mscorlib]System.Object
{
  .method family hidebysig instance void 
          aAxxx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "aAxxx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container32::aAxxx

  .method assembly hidebysig instance void 
          Aaxxx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Aaxxx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container32::Aaxxx

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Container32::.ctor

} // end of class Container32
]]>


            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="ConsoleApplication">
    <file name="a.vb">
Module Program
    Sub Main
        Dim cc As New Container32
        cc.aAxxx(1)
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC30390: 'Container32.Protected Overloads Sub aAxxx(x As Integer)' is not accessible in this context because it is 'Protected'.
        cc.aAxxx(1)
        ~~~~~~~~
</expected>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="ConsoleApplication1">
    <file name="a.vb">
Module Program
    Class Test
        Inherits Container32

        Sub Test()
            aAxxx(1)
        End Sub
    End Class

    Sub Main
        Dim tt As New Test()
        tt.Test()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation2, expectedOutput:=
            <![CDATA[
aAxxx
]]>)
        End Sub

        <Fact()>
        Public Sub SameKindOverloadingInSameContainer_3()
            Dim customIL = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly extern System.Core { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) }
.assembly extern Microsoft.VisualBasic { .ver 10:0:0:0 .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly '<<GeneratedFileName>>'
{
}
.module '<<GeneratedFileName>>.dll'

.class public auto ansi beforefieldinit Container32
       extends [mscorlib]System.Object
{
  .method family hidebysig instance void 
          aAxxx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "aAxxx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container32::aAxxx

  .method assembly hidebysig instance void 
          Aaxxx(int32 x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Aaxxx"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Container32::Aaxxx

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Container32::.ctor

} // end of class Container32
]]>


            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="ConsoleApplication">
    <file name="a.vb">
Module Program
    Sub Main
        Dim cc As New Container32
        cc.aAxxx(1)
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<expected>
BC30390: 'Container32.Protected Overloads Sub aAxxx(x As Integer)' is not accessible in this context because it is 'Protected'.
        cc.aAxxx(1)
        ~~~~~~~~
</expected>)

            Dim compilation2 = CompilationUtils.CreateCompilationWithCustomILSource(
<compilation name="ConsoleApplication1">
    <file name="a.vb">
Module Program
    Class Test
        Inherits Container32

        Sub Test()
            aAxxx(1)
        End Sub
    End Class

    Sub Main
        Dim tt As New Test()
        tt.Test()
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, includeSystemCore:=True, appendDefaultHeader:=False, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation2, expectedOutput:=
            <![CDATA[
aAxxx
]]>)
        End Sub

        <Fact, WorkItem(4704, "https://github.com/dotnet/roslyn/issues/4704")>
        Public Sub UnsupportedOverloadingOfExtensionMethods()

            Dim ilSource =
            <![CDATA[
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}

.assembly '<<GeneratedFileName>>'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
}

// MVID: {866DFDCB-9BF6-47CA-A90A-4C1FCEA128C0}
.imagebase 0x10000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00BA0000


// =============== CLASS MEMBERS DECLARATION ===================

.class public abstract auto ansi sealed beforefieldinit Matrix
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 

.method public hidebysig static void 
        ToArray(int32 a,
                string[] b) cil managed
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  .param [2]
  .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldstr      "1"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000a:  ret
} // end of method Matrix::ToArray

.method public hidebysig static void 
        ToArray(int32 a,
                [out] string[]& b) cil managed
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 ) 
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldstr      "2"
  IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
  IL_000a:  ret
} // end of method Matrix::ToArray

} // end of class Matrix

// =============================================================
]]>

            Dim compDef1 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
        Dim a As Integer = 5

        Matrix.ToArray(a, "Field1", "Field2", "Field3")
        Matrix.ToArray(a, {"Field1", "Field2", "Field3"})
        a.ToArray({"Field1", "Field2", "Field3"})

        Dim b As System.Action(Of Integer, String, String, String) = AddressOf Matrix.ToArray
        Dim c As System.Action(Of Integer, String()) = AddressOf Matrix.ToArray

    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation1 = CompilationUtils.CreateCompilationWithCustomILSource(compDef1, ilSource.Value, TestOptions.ReleaseExe, appendDefaultHeader:=False)

            AssertTheseDiagnostics(compilation1,
<expected>
BC31429: 'ToArray' is ambiguous because multiple kinds of members with this name exist in class 'Matrix'.
        Matrix.ToArray(a, "Field1", "Field2", "Field3")
               ~~~~~~~
BC31429: 'ToArray' is ambiguous because multiple kinds of members with this name exist in class 'Matrix'.
        Matrix.ToArray(a, {"Field1", "Field2", "Field3"})
               ~~~~~~~
BC30521: Overload resolution failed because no accessible 'ToArray' is most specific for these arguments:
    Extension method 'Public Sub ToArray(ParamArray b As String())' defined in 'Matrix': Not most specific.
    Extension method 'Public Sub ToArray(ByRef b As String())' defined in 'Matrix': Not most specific.
        a.ToArray({"Field1", "Field2", "Field3"})
          ~~~~~~~
BC31429: 'ToArray' is ambiguous because multiple kinds of members with this name exist in class 'Matrix'.
        Dim b As System.Action(Of Integer, String, String, String) = AddressOf Matrix.ToArray
                                                                               ~~~~~~~~~~~~~~
BC31429: 'ToArray' is ambiguous because multiple kinds of members with this name exist in class 'Matrix'.
        Dim c As System.Action(Of Integer, String()) = AddressOf Matrix.ToArray
                                                                 ~~~~~~~~~~~~~~
</expected>)


            Dim compDef2 =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class Module1
    Shared Sub Main()
        Dim a As Integer = 5

        a.ToArray("Field1", "Field2", "Field3")
        Dim b As System.Action(Of String, String, String) = AddressOf a.ToArray
   	    b("Field1", "Field2", "Field3")
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim compilation2 = CompilationUtils.CreateCompilationWithCustomILSource(compDef2, ilSource.Value, TestOptions.ReleaseExe, appendDefaultHeader:=False)


            CompileAndVerify(compilation2, expectedOutput:="1
1")
        End Sub

    End Class

End Namespace
