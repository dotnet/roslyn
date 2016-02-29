' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.IO
Imports System.Linq
Imports System.Reflection.Metadata
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
    End Class
End Namespace