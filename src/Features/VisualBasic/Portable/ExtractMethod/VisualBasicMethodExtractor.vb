' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
    Partial Friend Class VisualBasicMethodExtractor
        Inherits MethodExtractor(Of VisualBasicSelectionResult, ExecutableStatementSyntax, ExpressionSyntax)

        Public Sub New(result As VisualBasicSelectionResult, options As ExtractMethodGenerationOptions)
            MyBase.New(result, options, localFunction:=False)
        End Sub

        Protected Overrides Function CreateCodeGenerator(analyzerResult As AnalyzerResult) As CodeGenerator
            Return VisualBasicCodeGenerator.Create(Me.OriginalSelectionResult, analyzerResult, DirectCast(Me.Options.CodeGenerationOptions, VisualBasicCodeGenerationOptions))
        End Function

        Protected Overrides Function Analyze(selectionResult As VisualBasicSelectionResult, localFunction As Boolean, cancellationToken As CancellationToken) As AnalyzerResult
            Return VisualBasicAnalyzer.AnalyzeResult(selectionResult, cancellationToken)
        End Function

        Protected Overrides Function GetInsertionPointNode(
                analyzerResult As AnalyzerResult, cancellationToken As CancellationToken) As SyntaxNode
            Dim document = Me.OriginalSelectionResult.SemanticDocument
            Dim originalSpanStart = OriginalSelectionResult.OriginalSpan.Start
            Contract.ThrowIfFalse(originalSpanStart >= 0)

            Dim root = document.Root
            Dim basePosition = root.FindToken(originalSpanStart)

            Dim enclosingTopLevelNode As SyntaxNode = basePosition.GetAncestor(Of PropertyBlockSyntax)()
            If enclosingTopLevelNode Is Nothing Then
                enclosingTopLevelNode = basePosition.GetAncestor(Of EventBlockSyntax)()
            End If

            If enclosingTopLevelNode Is Nothing Then
                enclosingTopLevelNode = basePosition.GetAncestor(Of MethodBlockBaseSyntax)()
            End If

            If enclosingTopLevelNode Is Nothing Then
                enclosingTopLevelNode = basePosition.GetAncestor(Of FieldDeclarationSyntax)()
            End If

            If enclosingTopLevelNode Is Nothing Then
                enclosingTopLevelNode = basePosition.GetAncestor(Of PropertyStatementSyntax)()
            End If

            Contract.ThrowIfNull(enclosingTopLevelNode)
            Return enclosingTopLevelNode
        End Function

        Protected Overrides Async Function PreserveTriviaAsync(selectionResult As VisualBasicSelectionResult, cancellationToken As CancellationToken) As Task(Of TriviaResult)
            Return Await VisualBasicTriviaResult.ProcessAsync(selectionResult, cancellationToken).ConfigureAwait(False)
        End Function

        Protected Overrides Function GenerateCodeAsync(insertionPoint As InsertionPoint, selectionResult As VisualBasicSelectionResult, analyzeResult As AnalyzerResult, options As CodeGenerationOptions, cancellationToken As CancellationToken) As Task(Of GeneratedCode)
            Return VisualBasicCodeGenerator.GenerateResultAsync(insertionPoint, selectionResult, analyzeResult, DirectCast(options, VisualBasicCodeGenerationOptions), cancellationToken)
        End Function

        Protected Overrides Function GetCustomFormattingRule(document As Document) As AbstractFormattingRule
            Return FormattingRule.Instance
        End Function

        Protected Overrides Function GetInvocationNameToken(methodNames As IEnumerable(Of SyntaxToken)) As SyntaxToken?
            Return methodNames.FirstOrNull(Function(t) t.Parent.Kind <> SyntaxKind.SubStatement AndAlso t.Parent.Kind <> SyntaxKind.FunctionStatement)
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
                invocationNameToken? As SyntaxToken,
                methodDefinition As SyntaxNode,
                cancellationToken As CancellationToken) As Task(Of (document As Document, invocationNameToken As SyntaxToken?))
            ' VB doesn't need to do any correction, so we just return the values untouched
            Return Task.FromResult((document, invocationNameToken))
        End Function
    End Class
End Namespace
