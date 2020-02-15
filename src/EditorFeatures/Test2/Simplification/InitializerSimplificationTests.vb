' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification
    Public Class InitializerSimplificationTests
        Inherits AbstractSimplificationTests

#Region "VB tests"

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestVisualBasic_DontRemoveParensAroundConditionalAccessExpressionIfParentIsMemberAccessExpression() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Drawing

Friend Class CommentTestClass
    Public Function WidthOnly(r As Rectangle) As Rectangle
        Return New Rectangle With {
            {|SimplifyExtension:.Y = r.Y|}
        }
    End Function
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Drawing

Friend Class CommentTestClass
    Public Function WidthOnly(r As Rectangle) As Rectangle
        Return New Rectangle With {
            .Y = r.Y
        }
    End Function
End Class
</code>
            Using workspace = CreateTestWorkspace(input)
                Dim simplifiedDocument = Await SimplifyAsync(workspace).ConfigureAwait(False)

                Dim semanticModel = Await simplifiedDocument.GetSemanticModelAsync()
                Dim diagnosticsFromSimplifiedDocument = semanticModel.Compilation.GetDiagnostics()
                Assert.Empty(diagnosticsFromSimplifiedDocument)

                Await AssertCodeEqual(expected, simplifiedDocument)
            End Using
        End Function
#End Region
    End Class


End Namespace
