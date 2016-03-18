' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities

Public Class MetadataFileReferenceCompilationTests
    Inherits BasicTestBase

    <WorkItem(539480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539480")>
    <WorkItem(1037628, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems?_a=edit&id=1037628")>
    <Fact>
    Public Sub BC31011ERR_BadRefLib1()
        Dim ref = MetadataReference.CreateFromImage({}, filePath:="Foo.dll")
        Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="BadRefLib1">
    <file name="a.vb">
Class C1
End Class
    </file>
</compilation>)
        compilation1 = compilation1.AddReferences(ref)
        Dim expectedErrors1 = <errors>
BC31519: 'Foo.dll' cannot be referenced because it is not a valid assembly.
                 </errors>
        CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
    End Sub

    <WorkItem(1037628, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems?_a=edit&id=1037628")>
    <Fact>
    Public Sub BC31007ERR_BadModuleFile1()
        Dim ref = ModuleMetadata.CreateFromImage({}).GetReference(filePath:="Foo.dll")
        Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="BadRefLib1">
    <file name="a.vb">
Class C1
End Class
    </file>
</compilation>)
        compilation1 = compilation1.AddReferences(ref)
        Dim expectedErrors1 = <errors>
BC31007: Unable to load module file 'Foo.dll': PE image doesn't contain managed metadata.
                 </errors>
        CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
    End Sub

    <WorkItem(538349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538349")>
    <WorkItem(545062, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545062")>
    <Fact>
    Public Sub DuplicateReferences()
        Dim mscorlibMetadata = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.mscorlib)

        Dim mscorlib1 = mscorlibMetadata.GetReference(filePath:="lib1.dll")
        Dim mscorlib2 = mscorlibMetadata.GetReference(filePath:="lib1.dll")

        Dim comp = VisualBasicCompilation.Create("test", references:={mscorlib1, mscorlib2})
        Assert.Equal(2, comp.ExternalReferences.Length)
        Assert.Null(comp.GetReferencedAssemblySymbol(mscorlib1))             ' ignored
        Assert.NotNull(comp.GetReferencedAssemblySymbol(mscorlib2))

        Dim mscorlibNoEmbed = mscorlibMetadata.GetReference(filePath:="lib1.dll")
        Dim mscorlibEmbed = mscorlibMetadata.GetReference(filePath:="lib1.dll", embedInteropTypes:=True)

        comp = VisualBasicCompilation.Create("test", references:={mscorlibNoEmbed, mscorlibEmbed})
        Assert.Equal(2, comp.ExternalReferences.Length)
        Assert.Null(comp.GetReferencedAssemblySymbol(mscorlibNoEmbed))       ' ignored
        Assert.NotNull(comp.GetReferencedAssemblySymbol(mscorlibEmbed))

        comp = VisualBasicCompilation.Create("test", references:={mscorlibEmbed, mscorlibNoEmbed})
        Assert.Equal(2, comp.ExternalReferences.Length)
        Assert.Null(comp.GetReferencedAssemblySymbol(mscorlibEmbed))         ' ignored
        Assert.NotNull(comp.GetReferencedAssemblySymbol(mscorlibNoEmbed))
    End Sub

    <Fact>
    Public Sub ReferencesVersioning()
        Dim metadata1 = AssemblyMetadata.CreateFromImage(TestResources.General.C1)
        Dim metadata2 = AssemblyMetadata.CreateFromImage(TestResources.General.C2)

        Dim b = CompilationUtils.CreateCompilationWithMscorlibAndReferences(
<compilation name="b">
    <file name="b.vb">
Public Class B
    Public Shared Function Main() As Integer
        Return C.Main()
    End Function
End Class
    </file>
</compilation>,
        references:={MetadataReference.CreateFromImage(TestResources.General.C2)},
        options:=TestOptions.ReleaseDll)

        Dim metadata3 = AssemblyMetadata.CreateFromImage(b.EmitToArray())

        Dim a = CompilationUtils.CreateCompilationWithMscorlibAndReferences(
<compilation name="a">
    <file name="a.vb">
Class A
        Public Shared Sub Main()
            B.Main()
        End Sub
End Class
    </file>
</compilation>,
        references:={metadata1.GetReference(filePath:="file1.dll"), metadata2.GetReference(filePath:="file2.dll"), metadata3.GetReference(filePath:="file1.dll")},
        options:=TestOptions.ReleaseDll)

        Using stream = New MemoryStream()
            a.Emit(stream)
        End Using
    End Sub
End Class

