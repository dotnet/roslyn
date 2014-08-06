' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities
Imports ProprietaryTestResources = Microsoft.CodeAnalysis.Test.Resources.Proprietary

Public Class MetadataFileReferenceCompilationTests
    Inherits BasicTestBase

    <Fact>
    Public Sub AssemblyFileReferenceNotFound()
        Dim comp = VisualBasicCompilation.Create("Compilation", references:={New MetadataFileReference("c:\file_that_does_not_exist.bbb")})
        Assert.Equal(comp.GetDiagnostics().First().Code, ERRID.ERR_LibNotFound)
    End Sub

    <Fact>
    Public Sub ModuleFileReferenceNotFound()
        Dim comp = VisualBasicCompilation.Create("Compilation", references:={New MetadataFileReference("c:\file_that_does_not_exist.bbb", MetadataImageKind.Module)})
        Assert.Equal(comp.GetDiagnostics().First().Code, ERRID.ERR_LibNotFound)
    End Sub

    <WorkItem(539480, "DevDiv")>
    <Fact>
    Public Sub BC31011ERR_BadRefLib1()
        Using MetadataCache.LockAndClean()
            Dim refFile = Temp.CreateFile().Path

            Dim ref = New MetadataFileReference(refFile)
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="BadRefLib1">
    <file name="a.vb">
Class C1
End Class
    </file>
</compilation>)
            compilation1 = compilation1.AddReferences(ref)
            Dim expectedErrors1 = <errors>
BC31519: '<%= refFile %>' cannot be referenced because it is not a valid assembly.
                 </errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Using
    End Sub

    <Fact>
    Public Sub BC31007ERR_BadModuleFile1()
        Using MetadataCache.LockAndClean()
            Dim refFile = Temp.CreateFile().Path
            Dim ref = New MetadataFileReference(refFile, MetadataImageKind.Module)
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="BadRefLib1">
    <file name="a.vb">
Class C1
End Class
    </file>
</compilation>)
            compilation1 = compilation1.AddReferences(ref)
            Dim expectedErrors1 = <errors>
BC31007: Unable to load module file '<%= refFile %>': Image too small to contain DOS header.
                 </errors>
            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation1, expectedErrors1)
        End Using
    End Sub

    <WorkItem(538349, "DevDiv")>
    <WorkItem(545062, "DevDiv")>
    <Fact>
    Public Sub DuplicateReferences()
        Using MetadataCache.LockAndClean()
            Dim mscorlibPath = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib).Path

            Dim mscorlib1 = New MetadataFileReference(mscorlibPath)
            Dim mscorlib2 = New MetadataFileReference(mscorlibPath)

            Dim comp = VisualBasicCompilation.Create("test", references:={mscorlib1, mscorlib2})
            Assert.Equal(2, comp.ExternalReferences.Length)
            Assert.Null(comp.GetReferencedAssemblySymbol(mscorlib1))             ' ignored
            Assert.NotNull(comp.GetReferencedAssemblySymbol(mscorlib2))

            Dim mscorlibNoEmbed = New MetadataFileReference(mscorlibPath)
            Dim mscorlibEmbed = New MetadataFileReference(mscorlibPath, embedInteropTypes:=True)

            comp = VisualBasicCompilation.Create("test", references:={mscorlibNoEmbed, mscorlibEmbed})
            Assert.Equal(2, comp.ExternalReferences.Length)
            Assert.Null(comp.GetReferencedAssemblySymbol(mscorlibNoEmbed))       ' ignored
            Assert.NotNull(comp.GetReferencedAssemblySymbol(mscorlibEmbed))

            comp = VisualBasicCompilation.Create("test", references:={mscorlibEmbed, mscorlibNoEmbed})
            Assert.Equal(2, comp.ExternalReferences.Length)
            Assert.Null(comp.GetReferencedAssemblySymbol(mscorlibEmbed))         ' ignored
            Assert.NotNull(comp.GetReferencedAssemblySymbol(mscorlibNoEmbed))
        End Using
    End Sub

    <Fact>
    Public Sub ReferencesVersioning()
        Using MetadataCache.LockAndClean

            Dim dir1 = Temp.CreateDirectory()
            Dim dir2 = Temp.CreateDirectory()
            Dim dir3 = Temp.CreateDirectory()
            Dim file1 = dir1.CreateFile("C.dll")
            Dim file2 = dir2.CreateFile("C.dll")
            Dim file3 = dir3.CreateFile("main.dll")
            file1.WriteAllBytes(TestResources.SymbolsTests.General.C1)
            file2.WriteAllBytes(TestResources.SymbolsTests.General.C2)

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
            references:={New MetadataImageReference(TestResources.SymbolsTests.General.C2.AsImmutableOrNull())},
            options:=TestOptions.ReleaseDll)

            file3.WriteAllBytes(b.EmitToArray())

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
            references:={New MetadataFileReference(file1.Path), New MetadataFileReference(file2.Path), New MetadataFileReference(file3.Path)},
            options:=TestOptions.ReleaseDll)

            Using stream = New MemoryStream()
                a.Emit(stream)
            End Using
        End Using
    End Sub
End Class
