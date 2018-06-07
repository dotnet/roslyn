' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class GetUnusedImportDirectivesTests
        Inherits SemanticModelTestBase

        <Fact()>
        Public Sub TestLinq()
            Dim compilation = CreateCompilationWithMscorlib40AndReferences(
<compilation name="TestLinq">
    <file name="a.vb">
Imports System.Linq
Imports System.IO

Class Program
    Sub Main(args As String())
        Dim q = From s In {} Select s
    End Sub
End Class

    </file>
</compilation>, {SystemCoreRef})

            compilation.AssertTheseDiagnostics(<errors>
BC50001: Unused import statement.
Imports System.IO
~~~~~~~~~~~~~~~~~
                                               </errors>, suppressInfos:=False)
        End Sub


        <Fact()>
        Public Sub TestSpeculativeBindingDoesNotAffectUsedUsings()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation name="TestSpeculativeBindingDoesNotAffectUsedUsings">
    <file name="a.vb">
Imports System

Public Class Blah
    Sub Goo()
        Goo() ' Comment
    End Sub
End Class
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim position = tree.GetText().ToString().IndexOf("' Comment", StringComparison.Ordinal)
            model.GetSpeculativeSymbolInfo(position, SyntaxFactory.IdentifierName("Console"), SpeculativeBindingOption.BindAsTypeOrNamespace)
            compilation.AssertTheseDiagnostics(<errors>
BC50001: Unused import statement.
Imports System
~~~~~~~~~~~~~~
                                               </errors>, suppressInfos:=False)
        End Sub

        <Fact>
        Public Sub AllAssemblyLevelAttributesMustBeBound()
            Dim snkPath = Temp.CreateFile().WriteAllBytes(TestResources.General.snKey).Path

            Dim ivtCompilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="IVT">
    <file name="ivt.vb"><![CDATA[
Imports System.Runtime.CompilerServices

<Assembly: InternalsVisibleTo("Lib, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb")>

Namespace NamespaceContainingInternalsOnly
    Friend Module Extensions
        <Extension>
        Sub Goo(x As Integer)
        End Sub
    End Module
End Namespace
]]>
    </file>
    <file name="key.vb"><![CDATA[
Imports System.Reflection

<Assembly: AssemblyVersion("1.2.3.4")>
<Assembly: AssemblyKeyFile("]]><%= snkPath %><![CDATA[")>
]]>
    </file>
</compilation>, references:={SystemCoreRef}, options:=TestOptions.ReleaseDll.WithStrongNameProvider(New DesktopStrongNameProvider()))

            Dim libCompilation = CreateCompilationWithMscorlib40AndReferences(
<compilation name="Lib">
    <file name="a.vb">
Imports NamespaceContainingInternalsOnly

Public Class C
    Shared Sub F(x As Integer)
        x.Goo()
    End Sub
End Class
    </file>
    <file name="key.vb"><![CDATA[
Imports System.Reflection

<Assembly: AssemblyVersion("1.2.3.4")>
<Assembly: AssemblyKeyFile("]]><%= snkPath %><![CDATA[")>
]]>
    </file>
</compilation>, references:={ivtCompilation.ToMetadataReference()}, options:=TestOptions.ReleaseDll.WithStrongNameProvider(New DesktopStrongNameProvider()))

            Dim tree = libCompilation.SyntaxTrees(0)
            Dim model = libCompilation.GetSemanticModel(tree)
            libCompilation.VerifyDiagnostics()
        End Sub

        <WorkItem(546110, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546110")>
        <Fact()>
        Public Sub TestAssemblyImport1()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation name="TestAssemblyImport">
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices
<Assembly: InternalsVisibleTo("FriendAssembliesB")>
Friend Class Class1
End Class
    ]]></file>
</compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            compilation.AssertTheseDiagnostics(<errors></errors>, suppressInfos:=False)
        End Sub

        <WorkItem(546110, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546110")>
        <Fact()>
        Public Sub TestAssemblyImport2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation name="TestAssemblyImport">
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("FriendAssembliesB")>
Friend Class Class1
End Class
    ]]></file>
</compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            compilation.AssertTheseDiagnostics(<errors>
BC50001: Unused import statement.
Imports System.Runtime.CompilerServices
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                               </errors>, suppressInfos:=False)

            'Assert.Equal(1, unusedImports.Count)
        End Sub

        <WorkItem(747219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/747219")>
        <Fact()>
        Public Sub SemanticModelCallDoesNotCountsAsUse()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation name="TestAssemblyImport">
    <file name="a.vb"><![CDATA[
Imports System.Collections
Imports System.Collections.Generic

Class C
    Sub M()
        Return
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)

            ' Looks in the usings, but does not count as "use".
            Assert.Equal(2, model.LookupNamespacesAndTypes(tree.ToString().IndexOf("Return", StringComparison.Ordinal), name:="IEnumerable").Length)

            compilation.AssertTheseDiagnostics(<errors>
BC50001: Unused import statement.
Imports System.Collections
~~~~~~~~~~~~~~~~~~~~~~~~~~
BC50001: Unused import statement.
Imports System.Collections.Generic
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                               </errors>, suppressInfos:=False)
        End Sub

        <WorkItem(747219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/747219")>
        <Fact()>
        Public Sub INF_UnusedImportStatement_Single()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
    ]]></file>
</compilation>)

            compilation.AssertTheseDiagnostics(<errors>
BC50001: Unused import statement.
Imports System
~~~~~~~~~~~~~~
                                               </errors>, suppressInfos:=False)
        End Sub

        <WorkItem(747219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/747219")>
        <Fact()>
        Public Sub INF_UnusedImportStatement_Multiple()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System, System.Diagnostics
    ]]></file>
</compilation>)

            compilation.AssertTheseDiagnostics(<errors>
BC50001: Unused import statement.
Imports System, System.Diagnostics
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                               </errors>, suppressInfos:=False)
        End Sub

        <WorkItem(747219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/747219")>
        <Fact()>
        Public Sub INF_UnusedImportClause_Single()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System, System.Diagnostics

Class A : Inherits Attribute
End Class
    ]]></file>
</compilation>)

            compilation.AssertTheseDiagnostics(<errors>
BC50000: Unused import clause.
Imports System, System.Diagnostics
                ~~~~~~~~~~~~~~~~~~
                                               </errors>, suppressInfos:=False)
        End Sub

        <WorkItem(747219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/747219")>
        <Fact()>
        Public Sub INF_UnusedImportClause_Multiple()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System, System.Diagnostics, System.Collections

Class A
    Sub M()
        Debug.Assert(false)
    End Sub
End Class
    ]]></file>
</compilation>)

            compilation.AssertTheseDiagnostics(<errors>
BC50000: Unused import clause.
Imports System, System.Diagnostics, System.Collections
        ~~~~~~
BC50000: Unused import clause.
Imports System, System.Diagnostics, System.Collections
                                    ~~~~~~~~~~~~~~~~~~
                                               </errors>, suppressInfos:=False)
        End Sub

        <WorkItem(747219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/747219")>
        <Fact()>
        Public Sub CrefCountsAsUse()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

''' <see cref='Console'/>
Class A
End Class
]]></file>
</compilation>

            ' Without doc comments.
            CreateCompilationWithMscorlib40(source, parseOptions:=New VisualBasicParseOptions(documentationMode:=DocumentationMode.None)).AssertNoDiagnostics(suppressInfos:=False)

            ' With doc comments parsed.
            CreateCompilationWithMscorlib40(source, parseOptions:=New VisualBasicParseOptions(documentationMode:=DocumentationMode.Parse)).AssertNoDiagnostics(suppressInfos:=False)

            ' With doc comments diagnosed.
            CreateCompilationWithMscorlib40(source, parseOptions:=New VisualBasicParseOptions(documentationMode:=DocumentationMode.Diagnose)).AssertNoDiagnostics(suppressInfos:=False)
        End Sub

        <Fact>
        Public Sub UnusedImportInteractive()
            Dim tree = Parse("Imports System", options:=TestOptions.Script)
            Dim compilation = VisualBasicCompilation.CreateScriptCompilation("sub1", tree, {MscorlibRef_v4_0_30316_17626})
            compilation.AssertNoDiagnostics(suppressInfos:=False)
        End Sub

        <Fact()>
        Public Sub UnusedImportScript()
            Dim tree = Parse("Imports System", options:=TestOptions.Script)
            Dim compilation = CreateCompilationWithMscorlib45({tree})
            compilation.AssertTheseDiagnostics(
                <errors>
BC50001: Unused import statement.
Imports System
~~~~~~~~~~~~~~
                </errors>, suppressInfos:=False)
        End Sub

        <Fact, WorkItem(18348, "https://github.com/dotnet/roslyn/issues/18348")>
        Public Sub IncorrectUnusedUsingWhenAttributeOnParameter_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
Partial Class Program
    Private Sub M(<Out> <InAttribute> ByRef x As Integer)
    End Sub
End Class
]]></file>
    <file name="b.vb"><![CDATA[
Partial Class Program
    Partial Private Sub M(ByRef x As Integer)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            Dim tree = comp.SyntaxTrees(0)
            Dim model = comp.GetSemanticModel(tree)
            Dim diagnostics = model.GetDiagnostics()
            AssertTheseDiagnostics(diagnostics, <errors></errors>)
        End Sub

        <Fact, WorkItem(18348, "https://github.com/dotnet/roslyn/issues/18348")>
        Public Sub IncorrectUnusedUsingWhenAttributeOnParameter_02()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
Partial Class Program
    Partial Private Sub M(<Out> <InAttribute> ByRef x As Integer)
    End Sub
End Class
]]></file>
    <file name="b.vb"><![CDATA[
Partial Class Program
    Private Sub M(ByRef x As Integer)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            Dim tree = comp.SyntaxTrees(0)
            Dim model = comp.GetSemanticModel(tree)
            Dim diagnostics = model.GetDiagnostics()
            AssertTheseDiagnostics(diagnostics, <errors></errors>)
        End Sub

        <Fact, WorkItem(2773, "https://github.com/dotnet/roslyn/issues/2773")>
        Public Sub UsageInDocComment()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports X

''' <summary/>
Public Class Program

    ''' <summary>
    ''' <see cref="Q"/>
    ''' </summary>
    Public Sub Main()
    End Sub

End Class

Namespace Global.X
    ''' <summary/>
    Public Class Q
    End Class
End Namespace
]]></file>
</compilation>
            For Each documentationMode As DocumentationMode In [Enum].GetValues(GetType(DocumentationMode))
                Dim compilation = CreateCompilationWithMscorlib40(source, parseOptions:=TestOptions.Regular.WithDocumentationMode(documentationMode))
                compilation.AssertNoDiagnostics(suppressInfos:=False)
            Next
        End Sub
    End Class
End Namespace
