' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PDBNamespaceScopes
        Inherits BasicTestBase

        <Fact>
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

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
                source,
                TestOptions.DebugExe.WithGlobalImports(globalImports).WithRootNamespace(""))

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="40, 26, 2D, BC, C1, 9A,  B, B7, 68, F0, ED, 8E, CA, 70, 22, 73, 78, 33, EA, C0, "/>
        <file id="2" name="b.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="94, 7A, FB,  B, 3B, B0, EF, 63, B9, ED, E8, A9, D0, 58, BA, D0, 21,  7, C2, CE, "/>
    </files>
    <entryPoint declaringType="Boo.C1" methodName="Main"/>
    <methods>
        <method containingType="Boo.C1" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x6" startLine="22" startColumn="12" endLine="22" endColumn="43" document="1"/>
                <entry offset="0x16" startLine="16" startColumn="12" endLine="16" endColumn="43" document="2"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x27">
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

        <Fact>
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

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
                source,
                TestOptions.DebugExe.WithGlobalImports(globalImports).WithRootNamespace("DefaultNamespace"))

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="93, 20, A5, 3E, 2C, 50, B2,  E, 7C, D6, 29, 3F, E9, 9E, 33, 72, A6, 21, FD, 3F, "/>
        <file id="2" name="b.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="94, 7A, FB,  B, 3B, B0, EF, 63, B9, ED, E8, A9, D0, 58, BA, D0, 21,  7, C2, CE, "/>
    </files>
    <entryPoint declaringType="DefaultNamespace.Boo.C1" methodName="Main"/>
    <methods>
        <method containingType="DefaultNamespace.Boo.C1" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x6" startLine="22" startColumn="12" endLine="22" endColumn="43" document="1"/>
                <entry offset="0x16" startLine="16" startColumn="12" endLine="16" endColumn="43" document="2"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x27">
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

    End Class
End Namespace


