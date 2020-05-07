' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.SourceGeneration
Imports Roslyn.Test.Utilities
Imports Xunit
Imports Microsoft.CodeAnalysis.SourceGeneration.CodeGenerator
Imports Microsoft.CodeAnalysis.SourceGeneration

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.SourceGeneration
    Partial Public Class CodeGenerationTests
        <Fact>
        Public Sub TestArrayOfInt32()
            AssertEx.AreEqual(
"Integer()",
ArrayType(CodeGenerator.SpecialType(SpecialType.System_Int32)).GenerateTypeString())
        End Sub

        <Fact>
        Public Sub TestArrayOfInt32WithRank2()
            AssertEx.AreEqual(
"Integer(,)",
ArrayType(CodeGenerator.SpecialType(SpecialType.System_Int32), rank:=2).GenerateTypeString())
        End Sub

        <Fact>
        Public Sub TestNullableArrayOfInt32()
            AssertEx.AreEqual(
"Integer()",
ArrayType(CodeGenerator.SpecialType(SpecialType.System_Int32), nullableAnnotation:=NullableAnnotation.Annotated).GenerateTypeString())
        End Sub

        <Fact>
        Public Sub TestNullableArrayOfInt32WithRank2()
            AssertEx.AreEqual(
"Integer(,)",
ArrayType(CodeGenerator.SpecialType(SpecialType.System_Int32), rank:=2, nullableAnnotation:=NullableAnnotation.Annotated).GenerateTypeString())
        End Sub
    End Class
End Namespace
