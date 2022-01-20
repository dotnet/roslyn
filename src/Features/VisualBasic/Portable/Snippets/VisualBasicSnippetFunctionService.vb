' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    <ExportLanguageService(GetType(SnippetFunctionService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSnippetFunctionService
        Inherits SnippetFunctionService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides Async Function GetContainingClassNameAsync(document As Document, position As Integer, cancellationToken As CancellationToken) As Task(Of String)
            Dim syntaxTree = Await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            Dim typeBlock = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken).GetAncestor(Of TypeBlockSyntax)

            Return typeBlock.GetNameToken().ValueText
        End Function

        Protected Overrides Async Function GetEnumSymbolAsync(document As Document, switchExpressionSpan As TextSpan, cancellationToken As CancellationToken) As Task(Of ITypeSymbol)
            Dim syntaxTree = Await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            Dim token = syntaxTree.FindTokenOnRightOfPosition(switchExpressionSpan.Start, cancellationToken)
            Dim expressionNode = token.FirstAncestorOrSelf(Function(n) n.Span = switchExpressionSpan)

            If expressionNode Is Nothing Then
                Return Nothing
            End If

            Dim model As SemanticModel = Await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim typeSymbol = model.GetTypeInfo(expressionNode, cancellationToken).Type

            Return typeSymbol
        End Function

        Protected Overrides Async Function GetDocumentWithEnumCaseAsync(document As Document, fullyQualifiedTypeName As String, firstEnumMemberName As String, caseGenerationLocation As TextSpan, cancellationToken As CancellationToken) As Task(Of (Document, TextSpan))
            Dim str = "Case " + fullyQualifiedTypeName + "." + firstEnumMemberName + ":" + vbCrLf
            Dim textChange = New TextChange(caseGenerationLocation, str)
            Dim typeSpan = New TextSpan(caseGenerationLocation.Start + "Case ".Length, fullyQualifiedTypeName.Length)

            Dim text = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)
            Dim documentWithCaseAdded = document.WithText(text.WithChanges(textChange))

            Return (documentWithCaseAdded, typeSpan)
        End Function

        Public Overrides ReadOnly Property SwitchCaseFormat As String
            Get
                Return "Case {0}.{1}" & vbCrLf & vbCrLf
            End Get
        End Property

        Public Overrides ReadOnly Property SwitchDefaultCaseForm As String
            Get
                Return "Case Else" & vbCrLf
            End Get
        End Property
    End Class
End Namespace
