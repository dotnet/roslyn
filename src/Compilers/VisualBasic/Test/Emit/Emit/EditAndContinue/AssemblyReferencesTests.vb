' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.MetadataUtilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class AssemblyReferencesTests
        Inherits EditAndContinueTestBase

        <Fact>
        Public Sub DependencyVersionWildcards()
            Dim srcLib = "
<Assembly: System.Reflection.AssemblyVersion(""1.0.*"")>

Public Class D
End Class
"

            Dim src1 = "
Imports System
Class C 
    Public Shared Function F(a As D) As Integer
        Return 1
    End Function
End Class
"
            Dim src2 = "
Imports System
Class C 
    Public Shared Function F(a As D) As Integer
        Return 2
    End Function
End Class
"
            Dim lib1 = CreateCompilationWithMscorlib({srcLib}, assemblyName:="Lib", options:=TestOptions.DebugDll)
            lib1.VerifyDiagnostics()

            Dim lib2 As Compilation

            Do
                Thread.Sleep(1000)
                lib2 = CreateCompilationWithMscorlib({srcLib}, assemblyName:="Lib", options:=TestOptions.DebugDll)
            Loop While lib1.Assembly.Identity.Version = lib2.Assembly.Identity.Version

            lib2.VerifyDiagnostics()

            Dim compilation0 = CreateCompilationWithMscorlib({src1}, {lib1.ToMetadataReference()}, assemblyName:="C", options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(src2).WithReferences({MscorlibRef, lib2.ToMetadataReference()})

            Dim v0 = CompileAndVerify(compilation0)
            Dim v1 = CompileAndVerify(compilation1)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider)

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1)))

            Dim md1 = diff1.GetMetadata()

            Dim aggReader = New AggregatedMetadataReader(md0.MetadataReader, md1.Reader)

            VerifyAssemblyReferences(aggReader,
            {
                "mscorlib, 4.0.0.0",
                "Lib, " & lib1.Assembly.Identity.Version.ToString(),
                "mscorlib, 4.0.0.0",
                "Lib, " & lib1.Assembly.Identity.Version.ToString()
            })
        End Sub

        Public Sub VerifyAssemblyReferences(reader As AggregatedMetadataReader, expected As String())
            AssertEx.Equal(expected, reader.GetAssemblyReferences().Select(Function(aref) $"{reader.GetString(aref.Name)}, {aref.Version}"))
        End Sub
    End Class
End Namespace
