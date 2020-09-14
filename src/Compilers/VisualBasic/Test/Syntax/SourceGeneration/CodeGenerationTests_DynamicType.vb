' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.SourceGeneration.CodeGenerator
Imports Microsoft.CodeAnalysis.VisualBasic.SourceGeneration
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.SourceGeneration
    Partial Public Class CodeGenerationTests
        <Fact>
        Public Sub TestDynamic()
            AssertEx.AreEqual(
"Object",
DynamicType().GenerateTypeString())
        End Sub

        <Fact>
        Public Sub TestDynamicWithNullable()
            AssertEx.AreEqual(
"Object",
DynamicType(nullableAnnotation:=NullableAnnotation.Annotated).GenerateTypeString())
        End Sub
    End Class
End Namespace
