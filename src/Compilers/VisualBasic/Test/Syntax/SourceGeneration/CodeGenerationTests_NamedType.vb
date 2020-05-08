' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.SourceGeneration
Imports Roslyn.Test.Utilities
Imports Xunit
Imports Microsoft.CodeAnalysis.SourceGeneration.CodeGenerator

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.SourceGeneration
    Partial Public Class CodeGenerationTests
        <Fact>
        Public Sub TestClassType1()
            AssertEx.AreEqual(
"x",
[Class]("x").GenerateTypeString())
        End Sub

        <Fact>
        Public Sub TestClassTypeWithTypeArguments1()
            AssertEx.AreEqual(
"X(Of Integer)",
[Class](
    "X",
    typeArguments:=ImmutableArray.Create(Int32)).GenerateTypeString())
        End Sub

        <Fact>
        Public Sub TestClassTypeWithTypeArguments2()
            AssertEx.AreEqual(
"X(Of Integer, Boolean)",
[Class](
    "X",
    typeArguments:=ImmutableArray.Create(Int32, [Boolean])).GenerateTypeString())
        End Sub
    End Class
End Namespace
