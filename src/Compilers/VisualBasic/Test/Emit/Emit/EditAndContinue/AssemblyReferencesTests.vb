' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.IO
Imports System.Linq
Imports System.Reflection.Metadata
Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.MetadataUtilities
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests
    Public Class AssemblyReferencesTests
        Inherits EditAndContinueTestBase

        Private Shared ReadOnly s_signedDll As VisualBasicCompilationOptions =
            TestOptions.ReleaseDll.WithCryptoPublicKey(TestResources.TestKeys.PublicKey_ce65828c82a341f2)

        ''' <summary>
        ''' Symbol matcher considers two source types that only differ in the declaring compilations different.
        ''' </summary>
        <Fact>
        Public Sub ChangingCompilationDependencies()
            Dim srcLib = "
Public Class D
End Class
"
            Dim src0 = "
Public Class C 
    Public Shared Function F(a as D) As Integer
        Return 1
    End Function
End Class
"
            Dim src1 = "
Public Class C 
    Public Shared Function F(a as D) As Integer
        Return 2
    End Function
End Class
"
            Dim src2 As String = "
Public Class C 
    Public Shared Function F(a as D) As Integer
        Return 3
    End Function
End Class
"

            Dim lib0 = CreateCompilationWithMscorlib({srcLib}, assemblyName:="Lib", options:=TestOptions.DebugDll)
            lib0.VerifyDiagnostics()
            Dim lib1 = CreateCompilationWithMscorlib({srcLib}, assemblyName:="Lib", options:=TestOptions.DebugDll)
            lib1.VerifyDiagnostics()
            Dim lib2 = CreateCompilationWithMscorlib({srcLib}, assemblyName:="Lib", options:=TestOptions.DebugDll)
            lib2.VerifyDiagnostics()

            Dim compilation0 = CreateCompilationWithMscorlib({src0}, {lib0.ToMetadataReference()}, assemblyName:="C", options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(src1).WithReferences({MscorlibRef, lib1.ToMetadataReference()})
            Dim compilation2 = compilation1.WithSource(src2).WithReferences({MscorlibRef, lib2.ToMetadataReference()})

            Dim v0 = CompileAndVerify(compilation0)
            Dim v1 = CompileAndVerify(compilation1)
            Dim v2 = CompileAndVerify(compilation2)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")
            Dim f2 = compilation2.GetMember(Of MethodSymbol)("C.F")

            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider)

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1)))

            diff1.EmitResult.Diagnostics.Verify()

            Dim diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f1, f2)))

            diff2.EmitResult.Diagnostics.Verify()
        End Sub

        <Fact>
        Public Sub DependencyVersionWildcards_Compilation1()
            TestDependencyVersionWildcards(
                "1.0.0.*",
                New Version(1, 0, 2000, 1001),
                New Version(1, 0, 2000, 1001),
                New Version(1, 0, 2000, 1002))

            TestDependencyVersionWildcards(
                "1.0.0.*",
                New Version(1, 0, 2000, 1001),
                New Version(1, 0, 2000, 1002),
                New Version(1, 0, 2000, 1002))

            TestDependencyVersionWildcards(
                "1.0.0.*",
                New Version(1, 0, 2000, 1003),
                New Version(1, 0, 2000, 1002),
                New Version(1, 0, 2000, 1001))

            TestDependencyVersionWildcards(
                "1.0.*",
                New Version(1, 0, 2000, 1001),
                New Version(1, 0, 2000, 1002),
                New Version(1, 0, 2000, 1003))

            TestDependencyVersionWildcards(
                "1.0.*",
                New Version(1, 0, 2000, 1001),
                New Version(1, 0, 2000, 1005),
                New Version(1, 0, 2000, 1002))
        End Sub

        Private Sub TestDependencyVersionWildcards(sourceVersion As String, version0 As Version, version1 As Version, version2 As Version)
            Dim srcLib = $"
<Assembly: System.Reflection.AssemblyVersion(""{sourceVersion}"")>

Public Class D
End Class
"
            Dim src0 As String = "
Class C 
    Public Shared Function F(a As D) As Integer 
        Return 1
    End Function
End Class
"
            Dim src1 As String = "
Class C 
    Public Shared Function F(a As D) As Integer 
        Return 2
    End Function
End Class
"
            Dim src2 As String = "
Class C 
    Public Shared Function F(a As D) As Integer 
        Return 3
    End Function

    Public Shared Function G(a As D) As Integer
        Return 4
    End Function
End Class
"
            Dim lib0 = CreateCompilationWithMscorlib({srcLib}, assemblyName:="Lib", options:=TestOptions.DebugDll)
            DirectCast(lib0.Assembly, SourceAssemblySymbol).m_lazyIdentity = New AssemblyIdentity("Lib", version0)
            lib0.VerifyDiagnostics()

            Dim lib1 = CreateCompilationWithMscorlib({srcLib}, assemblyName:="Lib", options:=TestOptions.DebugDll)
            DirectCast(lib1.Assembly, SourceAssemblySymbol).m_lazyIdentity = New AssemblyIdentity("Lib", version1)
            lib1.VerifyDiagnostics()

            Dim lib2 = CreateCompilationWithMscorlib({srcLib}, assemblyName:="Lib", options:=TestOptions.DebugDll)
            DirectCast(lib2.Assembly, SourceAssemblySymbol).m_lazyIdentity = New AssemblyIdentity("Lib", version2)
            lib2.VerifyDiagnostics()

            Dim compilation0 = CreateCompilationWithMscorlib({src0}, {lib0.ToMetadataReference()}, assemblyName:="C", options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(src1).WithReferences({MscorlibRef, lib1.ToMetadataReference()})
            Dim compilation2 = compilation1.WithSource(src2).WithReferences({MscorlibRef, lib2.ToMetadataReference()})

            Dim v0 = CompileAndVerify(compilation0)
            Dim v1 = CompileAndVerify(compilation1)
            Dim v2 = CompileAndVerify(compilation2)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")
            Dim f2 = compilation2.GetMember(Of MethodSymbol)("C.F")
            Dim g2 = compilation2.GetMember(Of MethodSymbol)("C.G")

            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider)

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1)))

            Dim diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f1, f2),
                                      New SemanticEdit(SemanticEditKind.Insert, Nothing, g2)))

            Dim md1 = diff1.GetMetadata()
            Dim md2 = diff2.GetMetadata()

            Dim aggReader = New AggregatedMetadataReader(md0.MetadataReader, md1.Reader, md2.Reader)
            VerifyAssemblyReferences(aggReader,
            {
                "mscorlib, 4.0.0.0",
                "Lib, " & lib0.Assembly.Identity.Version.ToString(),
                "mscorlib, 4.0.0.0",
                "Lib, " & lib0.Assembly.Identity.Version.ToString(),
                "mscorlib, 4.0.0.0",
                "Lib, " & lib0.Assembly.Identity.Version.ToString()
            })
        End Sub

        <Fact>
        Public Sub DependencyVersionWildcards_Metadata()
            Dim srcLib = $"
<Assembly: System.Reflection.AssemblyVersion(""1.0.*"")>

Public Class D
End Class
"
            Dim src0 As String = "
Class C 
    Public Shared Function F(a As D) As Integer 
        Return 1
    End Function
End Class
"
            Dim src1 As String = "
Class C 
    Public Shared Function F(a As D) As Integer 
        Return 2
    End Function
End Class
"
            Dim src2 As String = "
Class C 
    Public Shared Function F(a As D) As Integer 
        Return 3
    End Function

    Public Shared Function G(a As D) As Integer
        Return 4
    End Function
End Class
"
            Dim lib0 = CreateCompilationWithMscorlib({srcLib}, assemblyName:="Lib", options:=TestOptions.DebugDll)
            DirectCast(lib0.Assembly, SourceAssemblySymbol).m_lazyIdentity = New AssemblyIdentity("Lib", New Version(1, 0, 2000, 1001))
            lib0.VerifyDiagnostics()

            Dim lib1 = CreateCompilationWithMscorlib({srcLib}, assemblyName:="Lib", options:=TestOptions.DebugDll)
            DirectCast(lib1.Assembly, SourceAssemblySymbol).m_lazyIdentity = New AssemblyIdentity("Lib", New Version(1, 0, 2000, 1002))
            lib1.VerifyDiagnostics()

            Dim lib2 = CreateCompilationWithMscorlib({srcLib}, assemblyName:="Lib", options:=TestOptions.DebugDll)
            DirectCast(lib2.Assembly, SourceAssemblySymbol).m_lazyIdentity = New AssemblyIdentity("Lib", New Version(1, 0, 2000, 1003))
            lib2.VerifyDiagnostics()

            Dim compilation0 = CreateCompilationWithMscorlib({src0}, {lib0.EmitToImageReference()}, assemblyName:="C", options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(src1).WithReferences({MscorlibRef, lib1.EmitToImageReference()})
            Dim compilation2 = compilation1.WithSource(src2).WithReferences({MscorlibRef, lib2.EmitToImageReference()})

            Dim v0 = CompileAndVerify(compilation0)
            Dim v1 = CompileAndVerify(compilation1)
            Dim v2 = CompileAndVerify(compilation2)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")
            Dim f2 = compilation2.GetMember(Of MethodSymbol)("C.F")
            Dim g2 = compilation2.GetMember(Of MethodSymbol)("C.G")

            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)
            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider)

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1)))

            diff1.EmitResult.Diagnostics.Verify(
                Diagnostic(ERRID.ERR_ModuleEmitFailure).WithArguments("C"))
        End Sub

        <Fact, WorkItem(9004, "https://github.com/dotnet/roslyn/issues/9004")>
        Public Sub DependencyVersionWildcardsCollisions()
            Dim srcLib01 = "
<Assembly: System.Reflection.AssemblyVersion(""1.0.0.1"")>

Public Class D0
End Class
"
            Dim srcLib02 = "
<Assembly: System.Reflection.AssemblyVersion(""1.0.0.2"")>

Public Class D0
End Class
"
            Dim srcLib11 = "
<Assembly: System.Reflection.AssemblyVersion(""1.0.1.1"")>

Public Class D1
End Class
"
            Dim srcLib12 = "
<Assembly: System.Reflection.AssemblyVersion(""1.0.1.2"")>

Public Class D1
End Class 
"
            Dim src0 = "
Class C 
    Public Shared Function F(a As D0, b As D1) As Integer
        Return 1
    End Function
End Class
"
            Dim src1 = "
Class C 
    Public Shared Function F(a As D0, b As D1) As Integer
        Return 2
    End Function
End Class
"
            Dim lib01 = CreateCompilationWithMscorlib({srcLib01}, assemblyName:="Lib", options:=s_signedDll).VerifyDiagnostics()
            Dim ref01 = lib01.ToMetadataReference()
            Dim lib02 = CreateCompilationWithMscorlib({srcLib02}, assemblyName:="Lib", options:=s_signedDll).VerifyDiagnostics()
            Dim ref02 = lib02.ToMetadataReference()
            Dim lib11 = CreateCompilationWithMscorlib({srcLib11}, assemblyName:="Lib", options:=s_signedDll).VerifyDiagnostics()
            Dim ref11 = lib11.ToMetadataReference()
            Dim lib12 = CreateCompilationWithMscorlib({srcLib12}, assemblyName:="Lib", options:=s_signedDll).VerifyDiagnostics()
            Dim ref12 = lib12.ToMetadataReference()

            Dim compilation0 = CreateCompilationWithMscorlib({src0}, {ref01, ref11}, assemblyName:="C", options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(src1).WithReferences({MscorlibRef, ref02, ref12})

            Dim v0 = CompileAndVerify(compilation0)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider)

            Dim diff1 = compilation1.EmitDifference(generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1)))

            diff1.EmitResult.Diagnostics.Verify(
                Diagnostic(ERRID.ERR_ModuleEmitFailure).WithArguments("C"))
        End Sub

        Public Sub VerifyAssemblyReferences(reader As AggregatedMetadataReader, expected As String())
            AssertEx.Equal(expected, reader.GetAssemblyReferences().Select(Function(aref) $"{reader.GetString(aref.Name)}, {aref.Version}"))
        End Sub
    End Class
End Namespace
