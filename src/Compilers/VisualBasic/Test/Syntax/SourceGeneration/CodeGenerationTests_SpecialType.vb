' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.SourceGeneration
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.SourceGeneration
    Partial Public Class CodeGenerationTests
        <Fact>
        Public Sub TestSpecialTypeInt32()
            AssertEx.AreEqual(
"Integer",
Int32.GenerateTypeString())
        End Sub

        <Fact>
        Public Sub TestSpecialNameInt32()
            AssertEx.AreEqual(
"Global.System.Int32",
Int32.GenerateNameString())
        End Sub
    End Class
End Namespace
