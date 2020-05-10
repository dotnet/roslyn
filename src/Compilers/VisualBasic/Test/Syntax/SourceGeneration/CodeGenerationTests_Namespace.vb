' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.SourceGeneration.CodeGenerator
Imports Microsoft.CodeAnalysis.VisualBasic.SourceGeneration
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.SourceGeneration
    Partial Public Class CodeGenerationTests
        <Fact>
        Public Sub TestNamespace()
            AssertEx.AreEqual(
"Namespace N
End Namespace",
[Namespace]("N").GenerateString())
        End Sub

        <Fact>
        Public Sub TestNamespaceWithDottedName()
            AssertEx.AreEqual(
"Namespace N1.N2
End Namespace",
[Namespace]("N1.N2").GenerateString())
        End Sub

        <Fact>
        Public Sub TestNamespaceWithNestedNamespace()
            AssertEx.AreEqual(
"Namespace N1
    Namespace N2
    End Namespace
End Namespace",
[Namespace]("N1").WithMembers([Namespace]("N2")).GenerateString())
        End Sub
    End Class
End Namespace
