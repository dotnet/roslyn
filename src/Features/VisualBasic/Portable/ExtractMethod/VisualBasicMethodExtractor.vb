' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Simplification

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
    Partial Friend Class VisualBasicMethodExtractor
        Inherits MethodExtractor

        Public Sub New(result As VisualBasicSelectionResult)
            MyBase.New(result, extractLocalFunction:=False)
        End Sub

        Protected Overrides Function AnalyzeAsync(selectionResult As SelectionResult, extractLocalFunction As Boolean, cancellationToken As CancellationToken) As Task(Of AnalyzerResult)
            Return VisualBasicAnalyzer.AnalyzeResultAsync(selectionResult, cancellationToken)
        End Function

        Protected Overrides Async Function GetInsertionPointAsync(document As SemanticDocument, position As Integer, cancellationToken As CancellationToken) As Task(Of InsertionPoint)
            Contract.ThrowIfFalse(position >= 0)

            Dim root = Await document.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim basePosition = root.FindToken(position)

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

        Protected Overrides Function GenerateCodeAsync(insertionPoint As InsertionPoint, selectionResult As SelectionResult, analyzeResult As AnalyzerResult, cancellationToken As CancellationToken) As Task(Of GeneratedCode)
            Return VisualBasicCodeGenerator.GenerateResultAsync(insertionPoint, selectionResult, analyzeResult, cancellationToken)
        End Function

        Protected Overrides Function GetFormattingRules(document As Document) As IEnumerable(Of AbstractFormattingRule)
            Return SpecializedCollections.SingletonEnumerable(Of AbstractFormattingRule)(New FormattingRule()).Concat(Formatter.GetDefaultFormattingRules(document))
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
                Dim vbType = DirectCast(typeParameter, ITypeSymbol)

                Dim typeName = SyntaxFactory.ParseTypeName(typeParameter.Name)
                Dim symbolInfo = binding.GetSpeculativeSymbolInfo(contextNode.SpanStart, typeName, SpeculativeBindingOption.BindAsTypeOrNamespace)
                Dim currentType = TryCast(symbolInfo.Symbol, ITypeSymbol)

                If Not AllNullabilityIgnoringSymbolComparer.Instance.Equals(currentType, typeParameter) Then
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

            Public Overrides Function GetAdjustNewLinesOperationSlow(previousToken As SyntaxToken, currentToken As SyntaxToken, optionSet As OptionSet, ByRef nextOperation As NextGetAdjustNewLinesOperation) As AdjustNewLinesOperation
                If Not previousToken.IsLastTokenOfStatement() Then
                    Return nextOperation.Invoke()
                End If

                ' between [generated code] and [existing code]
                If Not CommonFormattingHelpers.HasAnyWhitespaceElasticTrivia(previousToken, currentToken) Then
                    Return nextOperation.Invoke()
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

                Return nextOperation.Invoke()
            End Function

            Private Function IsLessThanInAttribute(token As SyntaxToken) As Boolean
                ' < in attribute
                If token.Kind = SyntaxKind.LessThanToken AndAlso
                   token.Parent.Kind = SyntaxKind.AttributeList AndAlso
                   DirectCast(token.Parent, AttributeListSyntax).LessThanToken.Equals(token) Then
                    Return True
                End If

                Return False
            End Function
        End Class
    End Class
End Namespace
