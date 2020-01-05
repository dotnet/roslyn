' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PDBNamespaceScopes
        Inherits BasicTestBase

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub ProjectLevelXmlImportsWithoutRootNamespace()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

imports &lt;xmlns:file1="http://stuff/fromFile"&gt;
imports &lt;xmlns="http://stuff/fromFile1"&gt;

Imports System
Imports System.Collections.Generic

Imports file1=System.Collections

Imports typefile1 = System.String

Imports ignoredAliasFile1 = System.Collections.Generic.Dictionary(Of String, String) ' ignored 
Imports System.Collections.Generic.List(Of String) ' ignored

Imports NS1.NS2.C1.C2
Imports C3ALIAS=NS1.NS2.C1.C2.C3

Namespace Boo

Partial Class C1
    Public Field1 as Object = new Object()  

    Public Shared Sub Main()
        Console.WriteLine("Hello World!")
    End Sub

    Public Shared Sub DoStuff()
        Console.WriteLine("Hello World again!")
    End Sub
End Class

End Namespace
    </file>

    <file name="b.vb">
Option Strict On

imports &lt;xmlns:file2="http://stuff/fromFile"&gt;
imports &lt;xmlns="http://stuff/fromFile2"&gt;

Imports System.Diagnostics
Imports file2=System.Collections

Imports typefile2 = System.Int32

Imports System.Collections.ArrayList ' import type without alias

Namespace Boo

Partial Class C1
    Public Field2 as Object = new Object()
End Class

End Namespace

' C2 has empty current namespace
Class C2
    Public Shared Sub DoStuff2()
        System.Console.WriteLine("Hello World again and again!")
    End Sub
End Class

Namespace NS1.NS2

    Public Class C1
        Public Class C2    
            Public Class C3
            End Class
        End Class
    End Class

End Namespace
    </file>
</compilation>

            Dim globalImports = GlobalImport.Parse(
                "<xmlns:prjlevel1=""http://NewNamespace"">",
                "<xmlns=""http://NewNamespace/prjlevel"">",
                "prjlevel=System.Collections.Generic",
                "System.Threading",
                "typeproj1=System.Int64",
                "prjlevelIgnored=System.Collections.Generic.List(Of String)",
                "System.Collections.Generic.List(Of String)",
                "System.Collections.ArrayList")

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                source,
                TestOptions.DebugExe.WithGlobalImports(globalImports).WithRootNamespace(""))

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="a.vb" language="VB" checksumAlgorithm="SHA1" checksum="40-26-2D-BC-C1-9A-0B-B7-68-F0-ED-8E-CA-70-22-73-78-33-EA-C0"/>
        <file id="2" name="b.vb" language="VB" checksumAlgorithm="SHA1" checksum="94-7A-FB-0B-3B-B0-EF-63-B9-ED-E8-A9-D0-58-BA-D0-21-07-C2-CE"/>
    </files>
    <entryPoint declaringType="Boo.C1" methodName="Main"/>
    <methods>
        <method containingType="Boo.C1" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x7" startLine="22" startColumn="12" endLine="22" endColumn="43" document="1"/>
                <entry offset="0x17" startLine="16" startColumn="12" endLine="16" endColumn="43" document="2"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x28">
                <xmlnamespace prefix="file1" name="http://stuff/fromFile" importlevel="file"/>
                <xmlnamespace prefix="" name="http://stuff/fromFile1" importlevel="file"/>
                <alias name="file1" target="System.Collections" kind="namespace" importlevel="file"/>
                <alias name="typefile1" target="System.String" kind="namespace" importlevel="file"/>
                <alias name="C3ALIAS" target="NS1.NS2.C1.C2.C3" kind="namespace" importlevel="file"/>
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <type name="NS1.NS2.C1.C2" importlevel="file"/>
                <xmlnamespace prefix="prjlevel1" name="http://NewNamespace" importlevel="project"/>
                <xmlnamespace prefix="" name="http://NewNamespace/prjlevel" importlevel="project"/>
                <alias name="prjlevel" target="System.Collections.Generic" kind="namespace" importlevel="project"/>
                <alias name="typeproj1" target="System.Int64" kind="namespace" importlevel="project"/>
                <namespace name="System.Threading" importlevel="project"/>
                <type name="System.Collections.ArrayList" importlevel="project"/>
                <currentnamespace name="Boo"/>
            </scope>
        </method>
        <method containingType="Boo.C1" name="Main">
            <sequencePoints>
                <entry offset="0x0" startLine="24" startColumn="5" endLine="24" endColumn="29" document="1"/>
                <entry offset="0x1" startLine="25" startColumn="9" endLine="25" endColumn="42" document="1"/>
                <entry offset="0xc" startLine="26" startColumn="5" endLine="26" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xd">
                <importsforward declaringType="Boo.C1" methodName=".ctor"/>
            </scope>
        </method>
        <method containingType="Boo.C1" name="DoStuff">
            <sequencePoints>
                <entry offset="0x0" startLine="28" startColumn="5" endLine="28" endColumn="32" document="1"/>
                <entry offset="0x1" startLine="29" startColumn="9" endLine="29" endColumn="48" document="1"/>
                <entry offset="0xc" startLine="30" startColumn="5" endLine="30" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xd">
                <importsforward declaringType="Boo.C1" methodName=".ctor"/>
            </scope>
        </method>
        <method containingType="C2" name="DoStuff2">
            <sequencePoints>
                <entry offset="0x0" startLine="23" startColumn="5" endLine="23" endColumn="33" document="2"/>
                <entry offset="0x1" startLine="24" startColumn="9" endLine="24" endColumn="65" document="2"/>
                <entry offset="0xc" startLine="25" startColumn="5" endLine="25" endColumn="12" document="2"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xd">
                <xmlnamespace prefix="file2" name="http://stuff/fromFile" importlevel="file"/>
                <xmlnamespace prefix="" name="http://stuff/fromFile2" importlevel="file"/>
                <alias name="file2" target="System.Collections" kind="namespace" importlevel="file"/>
                <alias name="typefile2" target="System.Int32" kind="namespace" importlevel="file"/>
                <namespace name="System.Diagnostics" importlevel="file"/>
                <type name="System.Collections.ArrayList" importlevel="file"/>
                <xmlnamespace prefix="prjlevel1" name="http://NewNamespace" importlevel="project"/>
                <xmlnamespace prefix="" name="http://NewNamespace/prjlevel" importlevel="project"/>
                <alias name="prjlevel" target="System.Collections.Generic" kind="namespace" importlevel="project"/>
                <alias name="typeproj1" target="System.Int64" kind="namespace" importlevel="project"/>
                <namespace name="System.Threading" importlevel="project"/>
                <type name="System.Collections.ArrayList" importlevel="project"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub ProjectLevelXmlImportsWithRootNamespace()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On

imports &lt;xmlns:file1="http://stuff/fromFile"&gt;
imports &lt;xmlns="http://stuff/fromFile1"&gt;

Imports System
Imports System.Collections.Generic

Imports file1=System.Collections

Imports typefile1 = System.String

Imports ignoredAliasFile1 = System.Collections.Generic.Dictionary(Of String, String) ' ignored 
Imports System.Collections.Generic.List(Of String) ' ignored

Imports DefaultNamespace.NS1.NS2.C1.C2
Imports C3ALIAS=DefaultNamespace.NS1.NS2.C1.C2.C3

Namespace Boo

Partial Class C1
    Public Field1 as Object = new Object()  

    Public Shared Sub Main()
        Console.WriteLine("Hello World!")
    End Sub

    Public Shared Sub DoStuff()
        Console.WriteLine("Hello World again!")
    End Sub
End Class

End Namespace
    </file>

    <file name="b.vb">
Option Strict On

imports &lt;xmlns:file2="http://stuff/fromFile"&gt;
imports &lt;xmlns="http://stuff/fromFile2"&gt;

Imports System.Diagnostics
Imports file2=System.Collections

Imports typefile2 = System.Int32

Imports System.Collections.ArrayList ' import type without alias

Namespace Boo

Partial Class C1
    Public Field2 as Object = new Object()
End Class

End Namespace

' C2 has empty current namespace
Class C2
    Public Shared Sub DoStuff2()
        System.Console.WriteLine("Hello World again and again!")
    End Sub
End Class

Namespace NS1.NS2

    Public Class C1
        Public Class C2    
            Public Class C3
            End Class
        End Class
    End Class

End Namespace
    </file>
</compilation>

            Dim globalImports = GlobalImport.Parse(
                "<xmlns:prjlevel1=""http://NewNamespace"">",
                "<xmlns=""http://NewNamespace/prjlevel"">",
                "prjlevel=System.Collections.Generic",
                "System.Threading",
                "typeproj1=System.Int64",
                "prjlevelIgnored=System.Collections.Generic.List(Of String)",
                "System.Collections.Generic.List(Of String)",
                "System.Collections.ArrayList")

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                source,
                TestOptions.DebugExe.WithGlobalImports(globalImports).WithRootNamespace("DefaultNamespace"))

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="a.vb" language="VB" checksumAlgorithm="SHA1" checksum="93-20-A5-3E-2C-50-B2-0E-7C-D6-29-3F-E9-9E-33-72-A6-21-FD-3F"/>
        <file id="2" name="b.vb" language="VB" checksumAlgorithm="SHA1" checksum="94-7A-FB-0B-3B-B0-EF-63-B9-ED-E8-A9-D0-58-BA-D0-21-07-C2-CE"/>
    </files>
    <entryPoint declaringType="DefaultNamespace.Boo.C1" methodName="Main"/>
    <methods>
        <method containingType="DefaultNamespace.Boo.C1" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x7" startLine="22" startColumn="12" endLine="22" endColumn="43" document="1"/>
                <entry offset="0x17" startLine="16" startColumn="12" endLine="16" endColumn="43" document="2"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x28">
                <xmlnamespace prefix="file1" name="http://stuff/fromFile" importlevel="file"/>
                <xmlnamespace prefix="" name="http://stuff/fromFile1" importlevel="file"/>
                <alias name="file1" target="System.Collections" kind="namespace" importlevel="file"/>
                <alias name="typefile1" target="System.String" kind="namespace" importlevel="file"/>
                <alias name="C3ALIAS" target="DefaultNamespace.NS1.NS2.C1.C2.C3" kind="namespace" importlevel="file"/>
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <type name="DefaultNamespace.NS1.NS2.C1.C2" importlevel="file"/>
                <defaultnamespace name="DefaultNamespace"/>
                <xmlnamespace prefix="prjlevel1" name="http://NewNamespace" importlevel="project"/>
                <xmlnamespace prefix="" name="http://NewNamespace/prjlevel" importlevel="project"/>
                <alias name="prjlevel" target="System.Collections.Generic" kind="namespace" importlevel="project"/>
                <alias name="typeproj1" target="System.Int64" kind="namespace" importlevel="project"/>
                <namespace name="System.Threading" importlevel="project"/>
                <type name="System.Collections.ArrayList" importlevel="project"/>
                <currentnamespace name="DefaultNamespace.Boo"/>
            </scope>
        </method>
        <method containingType="DefaultNamespace.Boo.C1" name="Main">
            <sequencePoints>
                <entry offset="0x0" startLine="24" startColumn="5" endLine="24" endColumn="29" document="1"/>
                <entry offset="0x1" startLine="25" startColumn="9" endLine="25" endColumn="42" document="1"/>
                <entry offset="0xc" startLine="26" startColumn="5" endLine="26" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xd">
                <importsforward declaringType="DefaultNamespace.Boo.C1" methodName=".ctor"/>
            </scope>
        </method>
        <method containingType="DefaultNamespace.Boo.C1" name="DoStuff">
            <sequencePoints>
                <entry offset="0x0" startLine="28" startColumn="5" endLine="28" endColumn="32" document="1"/>
                <entry offset="0x1" startLine="29" startColumn="9" endLine="29" endColumn="48" document="1"/>
                <entry offset="0xc" startLine="30" startColumn="5" endLine="30" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xd">
                <importsforward declaringType="DefaultNamespace.Boo.C1" methodName=".ctor"/>
            </scope>
        </method>
        <method containingType="DefaultNamespace.C2" name="DoStuff2">
            <sequencePoints>
                <entry offset="0x0" startLine="23" startColumn="5" endLine="23" endColumn="33" document="2"/>
                <entry offset="0x1" startLine="24" startColumn="9" endLine="24" endColumn="65" document="2"/>
                <entry offset="0xc" startLine="25" startColumn="5" endLine="25" endColumn="12" document="2"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xd">
                <xmlnamespace prefix="file2" name="http://stuff/fromFile" importlevel="file"/>
                <xmlnamespace prefix="" name="http://stuff/fromFile2" importlevel="file"/>
                <alias name="file2" target="System.Collections" kind="namespace" importlevel="file"/>
                <alias name="typefile2" target="System.Int32" kind="namespace" importlevel="file"/>
                <namespace name="System.Diagnostics" importlevel="file"/>
                <type name="System.Collections.ArrayList" importlevel="file"/>
                <defaultnamespace name="DefaultNamespace"/>
                <xmlnamespace prefix="prjlevel1" name="http://NewNamespace" importlevel="project"/>
                <xmlnamespace prefix="" name="http://NewNamespace/prjlevel" importlevel="project"/>
                <alias name="prjlevel" target="System.Collections.Generic" kind="namespace" importlevel="project"/>
                <alias name="typeproj1" target="System.Int64" kind="namespace" importlevel="project"/>
                <namespace name="System.Threading" importlevel="project"/>
                <type name="System.Collections.ArrayList" importlevel="project"/>
                <currentnamespace name="DefaultNamespace"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub EmittingPdbVsNot()
            Dim source =
<compilation name="EmittingPdbVsNot">
    <file>
Imports System
Imports X = System.IO.FileStream

Class C
    Dim x As Integer = 1
    Shared y As Integer = 1

    Sub New()
        Console.WriteLine()
    End Sub
End Class
    </file>
</compilation>

            Dim c = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll)

            Dim peStream1 = New MemoryStream()
            Dim peStream2 = New MemoryStream()
            Dim pdbStream = New MemoryStream()

            Dim emitResult1 = c.Emit(peStream:=peStream1, pdbStream:=pdbStream)
            Dim emitResult2 = c.Emit(peStream:=peStream2)

            MetadataValidation.VerifyMetadataEqualModuloMvid(peStream1, peStream2)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NoPiaNeedsDesktop)>
        Public Sub ImportedNoPiaTypes()
            Dim sourceLib =
<compilation name="ImportedNoPiaTypesAssemblyName">
    <file><![CDATA[
Imports System
Imports System.Reflection
Imports System.Runtime.InteropServices

<Assembly:Guid("11111111-1111-1111-1111-111111111111")>
<Assembly:ImportedFromTypeLib("Goo")>
<Assembly:TypeLibVersion(1, 0)>

Namespace N
    Public Enum E
        Value1 = 1
    End Enum

    Public Structure S1
        Public A1 As Integer
        Public A2 As Integer
    End Structure

    Public Structure S2
        Public Const Value2 As Integer = 2
    End Structure

    Public Structure SBad
        Public A3 As Integer
        Public Const Value3 As Integer = 3
    End Structure

    <ComImport, Guid("22222222-2222-2222-2222-222222222222")>
    Public Interface I
        Sub F()
    End Interface

    Public Interface IBad
        Sub F()
    End Interface
End Namespace
]]>
    </file>
</compilation>

            Dim source =
<compilation>
    <file>
Imports System
Imports N.E
Imports N.SBad
Imports Z1 = N.S1
Imports Z2 = N.S2
Imports ZBad = N.SBad
Imports NI = N.I
Imports NIBad = N.IBad

Class C
    Dim i As NI 

    Sub M
        Console.WriteLine(Value1)
        Console.WriteLine(Z2.Value2)
        Console.WriteLine(New Z1())
    End Sub
End Class
    </file>
</compilation>

            Dim globalImports = GlobalImport.Parse(
                "GlobalNIBad = N.IBad",
                "GlobalZ1 = N.S1",
                "GlobalZ2 = N.S2",
                "GlobalZBad = N.SBad",
                "GlobalNI = N.I")

            Dim libRef = CreateCompilationWithMscorlib40(sourceLib).EmitToImageReference(embedInteropTypes:=True)
            Dim compilation = CreateCompilationWithMscorlib40AndReferences(source, {libRef}, options:=TestOptions.DebugDll.WithGlobalImports(globalImports))
            Dim v = CompileAndVerify(compilation)

            v.Diagnostics.Verify(
                Diagnostic(ERRID.HDN_UnusedImportStatement, "Imports N.SBad"),
                Diagnostic(ERRID.HDN_UnusedImportStatement, "Imports ZBad = N.SBad"),
                Diagnostic(ERRID.HDN_UnusedImportStatement, "Imports NIBad = N.IBad"))

            ' Imports of embedded types are currently omitted:
            v.VerifyPdb("C.M",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C" name="M">
            <sequencePoints>
                <entry offset="0x0" startLine="13" startColumn="5" endLine="13" endColumn="10" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="14" endColumn="34" document="1"/>
                <entry offset="0x8" startLine="15" startColumn="9" endLine="15" endColumn="37" document="1"/>
                <entry offset="0xf" startLine="16" startColumn="9" endLine="16" endColumn="36" document="1"/>
                <entry offset="0x23" startLine="17" startColumn="5" endLine="17" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x24">
                <namespace name="System" importlevel="file"/>
                <defunct name="&amp;ImportedNoPiaTypesAssemblyName"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub ImportedTypeWithUnknownBase()
            Dim sourceLib1 =
<compilation>
    <file>
Namespace N
    Public Class A
    End Class
End Namespace
    </file>
</compilation>

            Dim sourceLib2 =
<compilation name="LibRef2">
    <file>
Namespace N
    Public Class B
        Inherits A
    End Class
End Namespace
    </file>
</compilation>

            Dim source =
<compilation>
    <file>
Imports System
Imports X = N.B

Class C
    Sub M()
        Console.WriteLine()
    End Sub
End Class
    </file>
</compilation>

            Dim libRef1 = CreateCompilationWithMscorlib40(sourceLib1).EmitToImageReference()
            Dim libRef2 = CreateCompilationWithMscorlib40AndReferences(sourceLib2, {libRef1}).EmitToImageReference()
            Dim compilation = CreateCompilationWithMscorlib40AndReferences(source, {libRef2})

            Dim v = CompileAndVerify(compilation)

            v.Diagnostics.Verify(
                Diagnostic(ERRID.HDN_UnusedImportStatement, "Imports X = N.B"))

            v.VerifyPdb("C.M",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C" name="M">
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="9" endLine="6" endColumn="28" document="1"/>
                <entry offset="0x5" startLine="7" startColumn="5" endLine="7" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6">
                <alias name="X" target="N.B" kind="namespace" importlevel="file"/>
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub
    End Class
End Namespace


