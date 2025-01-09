' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
    Partial Friend NotInheritable Class VisualBasicExtractMethodService
        Partial Friend Class VisualBasicMethodExtractor
            Inherits MethodExtractor

            Public Sub New(result As SelectionResult, options As ExtractMethodGenerationOptions)
                MyBase.New(result, options, localFunction:=False)
            End Sub

            Protected Overrides Function CreateCodeGenerator(selectionResult As SelectionResult, analyzerResult As AnalyzerResult) As CodeGenerator
                Return VisualBasicCodeGenerator.Create(selectionResult, analyzerResult, Me.Options)
            End Function

            Protected Overrides Function Analyze(cancellationToken As CancellationToken) As AnalyzerResult
                Dim analyzer = New VisualBasicAnalyzer(Me.OriginalSelectionResult, cancellationToken)
                Return analyzer.Analyze()
            End Function

            Protected Overrides Function GetInsertionPointNode(
                    analyzerResult As AnalyzerResult, cancellationToken As CancellationToken) As SyntaxNode
                Dim document = Me.OriginalSelectionResult.SemanticDocument
                Dim spanStart = OriginalSelectionResult.FinalSpan.Start
                Contract.ThrowIfFalse(spanStart >= 0)

                Dim root = document.Root
                Dim basePosition = root.FindToken(spanStart)

                Dim enclosingTopLevelNode As SyntaxNode = basePosition.GetAncestor(Of PropertyBlockSyntax)()

                enclosingTopLevelNode = If(enclosingTopLevelNode, basePosition.GetAncestor(Of EventBlockSyntax))
                enclosingTopLevelNode = If(enclosingTopLevelNode, basePosition.GetAncestor(Of MethodBlockBaseSyntax))
                enclosingTopLevelNode = If(enclosingTopLevelNode, basePosition.GetAncestor(Of FieldDeclarationSyntax))
                enclosingTopLevelNode = If(enclosingTopLevelNode, basePosition.GetAncestor(Of PropertyStatementSyntax))

                Contract.ThrowIfNull(enclosingTopLevelNode)
                Return enclosingTopLevelNode
            End Function

            Protected Overrides Async Function PreserveTriviaAsync(root As SyntaxNode, cancellationToken As CancellationToken) As Task(Of TriviaResult)
                Dim semanticDocument = Me.OriginalSelectionResult.SemanticDocument
                Dim preservationService = semanticDocument.Document.Project.Services.GetService(Of ISyntaxTriviaService)()

                Dim result = preservationService.SaveTriviaAroundSelection(root, Me.OriginalSelectionResult.FinalSpan)

                Return New VisualBasicTriviaResult(
                        Await semanticDocument.WithSyntaxRootAsync(result.Root, cancellationToken).ConfigureAwait(False),
                        result)
            End Function

            Protected Overrides Function GetCustomFormattingRule(document As Document) As AbstractFormattingRule
                Return FormattingRule.Instance
            End Function

            Protected Overrides Function ParseTypeName(name As String) As SyntaxNode
                Return SyntaxFactory.ParseTypeName(name)
            End Function

            Private NotInheritable Class FormattingRule
                Inherits CompatAbstractFormattingRule

                Public Shared ReadOnly Instance As New FormattingRule()

                Private Sub New()
                End Sub

                Public Overrides Function GetAdjustNewLinesOperationSlow(ByRef previousToken As SyntaxToken, ByRef currentToken As SyntaxToken, ByRef nextOperation As NextGetAdjustNewLinesOperation) As AdjustNewLinesOperation
                    If Not previousToken.IsLastTokenOfStatement() Then
                        Return nextOperation.Invoke(previousToken, currentToken)
                    End If

                    ' between [generated code] and [existing code]
                    If Not CommonFormattingHelpers.HasAnyWhitespaceElasticTrivia(previousToken, currentToken) Then
                        Return nextOperation.Invoke(previousToken, currentToken)
                    End If

                    ' make sure attribute and previous statement has at least 1 blank lines between them
                    If IsLessThanInAttribute(currentToken) Then
                        Return FormattingOperations.CreateAdjustNewLinesOperation(2, AdjustNewLinesOption.ForceLines)
                    End If

                    ' make sure previous statement and next type has at least 1 blank lines between them
                    If TypeOf currentToken.Parent Is TypeStatementSyntax AndAlso
                       currentToken.Parent.GetFirstToken(includeZeroWidth:=True) = currentToken Then
                        Return FormattingOperations.CreateAdjustNewLinesOperation(2, AdjustNewLinesOption.ForceLines)
                    End If

                    Return nextOperation.Invoke(previousToken, currentToken)
                End Function

                Private Shared Function IsLessThanInAttribute(token As SyntaxToken) As Boolean
                    ' < in attribute
                    If token.Kind = SyntaxKind.LessThanToken AndAlso
                       token.Parent.Kind = SyntaxKind.AttributeList AndAlso
                       DirectCast(token.Parent, AttributeListSyntax).LessThanToken.Equals(token) Then
                        Return True
                    End If

                    Return False
                End Function
            End Class

            Protected Overrides Function InsertNewLineBeforeLocalFunctionIfNecessaryAsync(
                    document As Document,
                    invocationNameToken As SyntaxToken,
                    methodDefinition As SyntaxNode,
                    cancellationToken As CancellationToken) As Task(Of (document As Document, invocationNameToken As SyntaxToken))
                ' VB doesn't need to do any correction, so we just return the values untouched
                Return Task.FromResult((document, invocationNameToken))
            End Function
        End Class
    End Class
End Namespace
