' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
    Partial Friend Class VisualBasicMethodExtractor
        Inherits MethodExtractor

        Public Sub New(result As VisualBasicSelectionResult, options As ExtractMethodGenerationOptions)
            MyBase.New(result, options, localFunction:=False)
        End Sub

        Protected Overrides Function AnalyzeAsync(selectionResult As SelectionResult, localFunction As Boolean, cancellationToken As CancellationToken) As Task(Of AnalyzerResult)
            Return VisualBasicAnalyzer.AnalyzeResultAsync(selectionResult, cancellationToken)
        End Function

        Protected Overrides Async Function GetInsertionPointAsync(document As SemanticDocument, cancellationToken As CancellationToken) As Task(Of InsertionPoint)
            Dim originalSpanStart = OriginalSelectionResult.OriginalSpan.Start
            Contract.ThrowIfFalse(originalSpanStart >= 0)

            Dim root = Await document.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
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
            Return Await InsertionPoint.CreateAsync(document, enclosingTopLevelNode, cancellationToken).ConfigureAwait(False)
        End Function

        Protected Overrides Async Function PreserveTriviaAsync(selectionResult As SelectionResult, cancellationToken As CancellationToken) As Task(Of TriviaResult)
            Return Await VisualBasicTriviaResult.ProcessAsync(selectionResult, cancellationToken).ConfigureAwait(False)
        End Function

        Protected Overrides Async Function ExpandAsync(selection As SelectionResult, cancellationToken As CancellationToken) As Task(Of SemanticDocument)
            Dim lastExpression = selection.GetFirstTokenInSelection().GetCommonRoot(selection.GetLastTokenInSelection()).GetAncestors(Of ExpressionSyntax)().LastOrDefault()
            If lastExpression Is Nothing Then
                Return selection.SemanticDocument
            End If

            Dim newStatement = Await Simplifier.ExpandAsync(lastExpression, selection.SemanticDocument.Document, Function(n) n IsNot selection.GetContainingScope(), expandParameter:=False, cancellationToken:=cancellationToken).ConfigureAwait(False)
            Return Await selection.SemanticDocument.WithSyntaxRootAsync(selection.SemanticDocument.Root.ReplaceNode(lastExpression, newStatement), cancellationToken).ConfigureAwait(False)
        End Function

        Protected Overrides Function GenerateCodeAsync(insertionPoint As InsertionPoint, selectionResult As SelectionResult, analyzeResult As AnalyzerResult, options As CodeGenerationOptions, namingPreferences As NamingStylePreferencesProvider, cancellationToken As CancellationToken) As Task(Of GeneratedCode)
            Return VisualBasicCodeGenerator.GenerateResultAsync(insertionPoint, selectionResult, analyzeResult, cancellationToken)
        End Function

        Protected Overrides Function GetCustomFormattingRules(document As Document) As ImmutableArray(Of AbstractFormattingRule)
            Return ImmutableArray.Create(Of AbstractFormattingRule)(New FormattingRule())
        End Function

        Protected Overrides Function GetMethodNameAtInvocation(methodNames As IEnumerable(Of SyntaxNodeOrToken)) As SyntaxToken
            Return CType(methodNames.FirstOrDefault(Function(t) t.Parent.Kind <> SyntaxKind.SubStatement AndAlso t.Parent.Kind <> SyntaxKind.FunctionStatement), SyntaxToken)
        End Function

        Protected Overrides Async Function CheckTypeAsync(document As Document,
                                               contextNode As SyntaxNode,
                                               location As Location,
                                               type As ITypeSymbol,
                                               cancellationToken As CancellationToken) As Task(Of OperationStatus)
            Contract.ThrowIfNull(type)

            If type.SpecialType = SpecialType.System_Void Then
                ' this can happen if there is no return value
                Return OperationStatus.Succeeded
            End If

            If type.TypeKind = TypeKind.Error OrElse type.TypeKind = TypeKind.Unknown Then
                Return OperationStatus.ErrorOrUnknownType
            End If

            ' if it is type parameter, make sure we are getting same type parameter
            Dim binding = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

            For Each typeParameter In TypeParameterCollector.Collect(type)
                Dim typeName = SyntaxFactory.ParseTypeName(typeParameter.Name)
                Dim symbolInfo = binding.GetSpeculativeSymbolInfo(contextNode.SpanStart, typeName, SpeculativeBindingOption.BindAsTypeOrNamespace)
                Dim currentType = TryCast(symbolInfo.Symbol, ITypeSymbol)

                If Not SymbolEqualityComparer.Default.Equals(currentType, typeParameter) Then
                    Return New OperationStatus(OperationStatusFlag.BestEffort,
                        String.Format(FeaturesResources.Type_parameter_0_is_hidden_by_another_type_parameter_1,
                            typeParameter.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            If(currentType Is Nothing, String.Empty, currentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))))
                End If
            Next typeParameter

            Return OperationStatus.Succeeded
        End Function

        Private Class FormattingRule
            Inherits CompatAbstractFormattingRule

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

        Protected Overrides Function InsertNewLineBeforeLocalFunctionIfNecessaryAsync(document As Document,
                                                                                      methodName As SyntaxToken,
                                                                                      methodDefinition As SyntaxNode,
                                                                                      cancellationToken As CancellationToken) As Task(Of (document As Document, methodName As SyntaxToken, methodDefinition As SyntaxNode))
            ' VB doesn't need to do any correction, so we just return the values untouched
            Return Task.FromResult((document, methodName, methodDefinition))
        End Function
    End Class
End Namespace
