' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Editor.Extensibility.NavigationBar
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Utilities
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.NavigationBar
    Friend MustInherit Class AbstractGenerateCodeItem
        Inherits RoslynNavigationBarItem

        Friend Shared ReadOnly GeneratedSymbolAnnotation As SyntaxAnnotation = New SyntaxAnnotation()

        Protected ReadOnly _destinationTypeSymbolKey As SymbolKey

        Public Sub New(kind As RoslynNavigationBarItemKind, text As String, glyph As Glyph, destinationTypeSymbolKey As SymbolKey)
            MyBase.New(kind, text, glyph, SpecializedCollections.EmptyList(Of TextSpan))
            _destinationTypeSymbolKey = destinationTypeSymbolKey
        End Sub

        Protected Overridable ReadOnly Property ApplyLineAdjustmentFormattingRule As Boolean
            Get
                Return True
            End Get
        End Property

        Protected MustOverride Function GetGeneratedDocumentCoreAsync(document As Document, codeGenerationOptions As CodeGenerationOptions, cancellationToken As CancellationToken) As Task(Of Document)

        Public Async Function GetGeneratedDocumentAsync(document As Document, cancellationToken As CancellationToken) As Task(Of Document)
            Dim syntaxTree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            Dim contextLocation = syntaxTree.GetLocation(New TextSpan(0, 0))
            Dim codeGenerationOptions As New CodeGenerationOptions(contextLocation, generateMethodBodies:=True)

            Dim newDocument = Await GetGeneratedDocumentCoreAsync(document, codeGenerationOptions, cancellationToken).ConfigureAwait(False)
            If newDocument Is Nothing Then
                Return document
            End If

            newDocument = Simplifier.ReduceAsync(newDocument, Simplifier.Annotation, Nothing, cancellationToken).WaitAndGetResult(cancellationToken)

            Dim formatterRules = Formatter.GetDefaultFormattingRules(newDocument)
            If ApplyLineAdjustmentFormattingRule Then
                formatterRules = LineAdjustmentFormattingRule.Instance.Concat(formatterRules)
            End If

            Dim documentOptions = Await newDocument.GetOptionsAsync(cancellationToken).ConfigureAwait(False)
            Return Formatter.FormatAsync(newDocument,
                                         Formatter.Annotation,
                                         options:=documentOptions,
                                         cancellationToken:=cancellationToken,
                                         rules:=formatterRules).WaitAndGetResult(cancellationToken)
        End Function
    End Class
End Namespace
