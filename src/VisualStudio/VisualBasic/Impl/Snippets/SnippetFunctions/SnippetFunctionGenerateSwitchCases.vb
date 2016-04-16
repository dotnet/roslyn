' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Shared.Extensions
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Snippets.SnippetFunctions
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports TextSpan = Microsoft.CodeAnalysis.Text.TextSpan
Imports VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Snippets.SnippetFunctions
    Friend NotInheritable Class SnippetFunctionGenerateSwitchCases
        Inherits AbstractSnippetFunctionGenerateSwitchCases

        Public Sub New(snippetExpansionClient As SnippetExpansionClient, textView As ITextView, subjectBuffer As ITextBuffer, caseGenerationLocationField As String, switchExpressionField As String)
            MyBase.New(snippetExpansionClient, textView, subjectBuffer, caseGenerationLocationField, switchExpressionField)
        End Sub

        Protected Overrides ReadOnly Property CaseFormat As String
            Get
                Return "Case {0}.{1}" & vbCrLf & vbCrLf
            End Get
        End Property

        Protected Overrides ReadOnly Property DefaultCase As String
            Get
                Return "Case Else" & vbCrLf
            End Get
        End Property

        Protected Overrides Function TryGetEnumTypeSymbol(cancellationToken As CancellationToken, ByRef typeSymbol As ITypeSymbol) As Boolean
            typeSymbol = Nothing

            Dim document As Document = Nothing
            If Not TryGetDocument(document) Then
                Return False
            End If

            Dim surfaceBufferFieldSpan(1) As VsTextSpan
            If SnippetExpansionClient.ExpansionSession.GetFieldSpan(SwitchExpressionField, surfaceBufferFieldSpan) <> VSConstants.S_OK Then
                Return False
            End If

            Dim subjectBufferFieldSpan As SnapshotSpan = Nothing
            If Not SnippetExpansionClient.TryGetSubjectBufferSpan(surfaceBufferFieldSpan(0), subjectBufferFieldSpan) Then
                Return False
            End If

            Dim syntaxTree = document.GetSyntaxTreeAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None)
            Dim token = syntaxTree.FindTokenOnRightOfPosition(subjectBufferFieldSpan.Start.Position, cancellationToken)
            Dim expressionNode = token.FirstAncestorOrSelf(Function(n) n.Span = subjectBufferFieldSpan.Span.ToTextSpan())

            If expressionNode Is Nothing Then
                Return False
            End If

            Dim model As SemanticModel = document.GetSemanticModelAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None)
            typeSymbol = model.GetTypeInfo(expressionNode).Type

            Return typeSymbol IsNot Nothing
        End Function

        Protected Overrides Function TryGetSimplifiedTypeNameInCaseContext(document As Document, fullyQualifiedTypeName As String, firstEnumMemberName As String, startPosition As Integer, endPosition As Integer, cancellationToken As CancellationToken, ByRef simplifiedTypeName As String) As Boolean
            simplifiedTypeName = String.Empty
            Dim typeAnnotation = New SyntaxAnnotation()

            Dim str = "Case " + fullyQualifiedTypeName + "." + firstEnumMemberName + ":" + vbCrLf
            Dim textChange = New TextChange(New TextSpan(startPosition, endPosition - startPosition), str)
            Dim typeSpanToAnnotate = New TextSpan(startPosition + "Case ".Length, fullyQualifiedTypeName.Length)

            Dim textWithCaseAdded = document.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken).WithChanges(textChange)
            Dim documentWithCaseAdded = document.WithText(textWithCaseAdded)

            Dim syntaxRoot = documentWithCaseAdded.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken)
            Dim nodeToReplace = syntaxRoot.DescendantNodes().FirstOrDefault(Function(n) n.Span = typeSpanToAnnotate)

            If nodeToReplace Is Nothing Then
                Return False
            End If

            Dim updatedRoot = syntaxRoot.ReplaceNode(nodeToReplace, nodeToReplace.WithAdditionalAnnotations(typeAnnotation, Simplifier.Annotation))
            Dim documentWithAnnotations = documentWithCaseAdded.WithSyntaxRoot(updatedRoot)

            Dim simplifiedDocument = Simplifier.ReduceAsync(documentWithAnnotations, cancellationToken:=cancellationToken).WaitAndGetResult(cancellationToken)
            simplifiedTypeName = simplifiedDocument.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken).GetAnnotatedNodesAndTokens(typeAnnotation).Single().ToString()
            Return True
        End Function

    End Class
End Namespace
