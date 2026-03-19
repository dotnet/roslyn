' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification
    <Trait(Traits.Feature, Traits.Features.Simplification)>
    Public Class InitializerSimplificationTests
        Inherits AbstractSimplificationTests

#Region "VB tests"

        <Fact>
        Public Async Function TestVisualBasic_DoNotRemovePropertyNameForObjectCreationInitializer() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Text

Friend Class InitializerTestClass
    Public Function CopyLength(sb As StringBuilder) As Object
    Return New StringBuilder With {
            {|SimplifyExtension:.Length = sb.Length|}
        }
    End Function
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Text

Friend Class InitializerTestClass
    Public Function CopyLength(sb As StringBuilder) As Object
    Return New StringBuilder With {
            .Length = sb.Length
        }
    End Function
End Class

</code>

            Await AssertCompilesAndEqual(input, expected).ConfigureAwait(False)
        End Function

        <Fact>
        Public Async Function TestVisualBasic_RemoveInferrablePropertyNameForAnonymousObjectCreationInitializer() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System.Text

Friend Class InitializerTestClass
    Public Function CopyLength(sb As StringBuilder) As Object
    Return New With {
            {|SimplifyExtension:.Length = sb.Length|}
        }
    End Function
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Imports System.Text

Friend Class InitializerTestClass
    Public Function CopyLength(sb As StringBuilder) As Object
    Return New With {
            sb.Length
        }
    End Function
End Class
</code>
            Await AssertCompilesAndEqual(input, expected).ConfigureAwait(False)
        End Function
#End Region

        Private Shared Async Function AssertCompilesAndEqual(input As XElement, expected As XElement) As Task
            Using workspace = Await CreateTestWorkspaceAsync(input)
                Dim simplifiedDocument = Await SimplifyAsync(workspace).ConfigureAwait(False)

                Dim semanticModel = Await simplifiedDocument.GetSemanticModelAsync()
                Dim diagnosticsFromSimplifiedDocument = semanticModel.Compilation.GetDiagnostics()
                Assert.Empty(diagnosticsFromSimplifiedDocument)

                Await AssertCodeEqual(expected, simplifiedDocument)
            End Using
        End Function
    End Class

End Namespace
