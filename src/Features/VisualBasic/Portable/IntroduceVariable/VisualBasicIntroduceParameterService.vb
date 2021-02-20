' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.IntroduceVariable
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.IntroduceVariable
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicIntroduceParameterService
        Inherits AbstractIntroduceParameterService(Of VisualBasicIntroduceParameterService, ExpressionSyntax, MethodBlockSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Async Function IntroduceParameterAsync(document As SemanticDocument, expression As ExpressionSyntax, allOccurrences As Boolean, trampoline As Boolean, cancellationToken As Threading.CancellationToken) As Task(Of Solution)
            Dim parameterName = GetNewParameterName(document, expression, cancellationToken)
            Dim annotatedExpression = New SyntaxAnnotation(ExpressionAnnotationKind)
            Dim annotatedSemanticDocument = Await GetAnnotatedSemanticDocumentAsync(document, annotatedExpression, expression, cancellationToken).ConfigureAwait(False)
            Dim annotatedExpressionWithinDocument = DirectCast(annotatedSemanticDocument.Root.GetAnnotatedNodesAndTokens(annotatedExpression).Single().AsNode(), ExpressionSyntax)
            Dim methodSymbolInfo = GetMethodSymbolFromExpression(annotatedSemanticDocument, annotatedExpressionWithinDocument, cancellationToken)
            Dim methodCallSites = Await FindCallSitesAsync(annotatedSemanticDocument, methodSymbolInfo, cancellationToken).ConfigureAwait(False)
            Dim updatedCallSitesSolution = Await RewriteCallSitesAsync(annotatedExpressionWithinDocument, methodCallSites, methodSymbolInfo, cancellationToken).ConfigureAwait(False)

            If updatedCallSitesSolution Is Nothing Then
                updatedCallSitesSolution = annotatedSemanticDocument.Document.Project.Solution
            End If

            Dim updatedCallSitesDocument = Await SemanticDocument.CreateAsync(updatedCallSitesSolution.GetDocument(document.Document.Id), cancellationToken).ConfigureAwait(False)

            annotatedExpressionWithinDocument = DirectCast(updatedCallSitesDocument.Root.GetAnnotatedNodesAndTokens(annotatedExpression).Single().AsNode(), ExpressionSyntax)
            Dim updatedSolutionWithParameter = Await AddParameterToMethodHeaderAsync(updatedCallSitesDocument, annotatedExpressionWithinDocument, parameterName, cancellationToken).ConfigureAwait(False)
            Dim updatedSemanticDocument = Await SemanticDocument.CreateAsync(updatedSolutionWithParameter.GetDocument(document.Document.Id), cancellationToken).ConfigureAwait(False)

            Dim newExpression = DirectCast(updatedSemanticDocument.Root.GetAnnotatedNodesAndTokens(annotatedExpression).Single().AsNode(), ExpressionSyntax)
            Dim documentWithUpdatedMethodBody = ConvertExpressionWithNewParameter(updatedSemanticDocument, newExpression, parameterName, allOccurrences, cancellationToken)
            Return documentWithUpdatedMethodBody.Project.Solution
        End Function

        Private Shared Async Function RewriteCallSitesAsync(expression As ExpressionSyntax, callSites As ImmutableDictionary(Of Document, List(Of InvocationExpressionSyntax)),
                                                            methodSymbol As IMethodSymbol, cancellationToken As CancellationToken) As Task(Of Solution)
            Dim mappingDictionary = TieExpressionToParameters(expression, methodSymbol)
            expression = expression.TrackNodes(mappingDictionary.Values)
            Dim identifiers = expression.DescendantNodes().OfType(Of IdentifierNameSyntax)

            If Not callSites.Keys.Any() Then
                Return Nothing
            End If

            Dim firstCallSite = callSites.Keys.First()
            Dim currentSolution = firstCallSite.Project.Solution

            For Each keyValuePair In callSites
                Dim document = currentSolution.GetDocument(keyValuePair.Key.Id)
                Dim invocationExpressionList = keyValuePair.Value
                Dim generator = SyntaxGenerator.GetGenerator(document)
                Dim semanticFacts = document.GetRequiredLanguageService(Of ISemanticFactsService)
                Dim invocationSemanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
                Dim editor = New SyntaxEditor(Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False), generator)

                For Each invocationExpression In invocationExpressionList
                    Dim newArgumentExpression = expression
                    Dim invocationArguments = invocationExpression.ArgumentList.Arguments

                    For Each argument In invocationArguments
                        Dim simpleArgument = TryCast(argument, SimpleArgumentSyntax)
                        Dim associatedParameter = semanticFacts.FindParameterForArgument(invocationSemanticModel, argument, cancellationToken)
                        Dim parenthesizedArgumentExpression = generator.AddParentheses(simpleArgument.Expression, False)
                        Dim value As IdentifierNameSyntax = Nothing
                        If Not mappingDictionary.TryGetValue(associatedParameter, value) Then
                            Continue For
                        End If

                        newArgumentExpression = newArgumentExpression.ReplaceNode(newArgumentExpression.GetCurrentNode(value), parenthesizedArgumentExpression)
                    Next

                    Dim allArguments = invocationExpression.ArgumentList.Arguments.Add(SyntaxFactory.SimpleArgument(newArgumentExpression.WithoutAnnotations(ExpressionAnnotationKind).WithAdditionalAnnotations(Simplifier.Annotation)))
                    editor.ReplaceNode(invocationExpression, editor.Generator.InvocationExpression(invocationExpression.Expression, allArguments))
                Next

                Dim newRoot = editor.GetChangedRoot()
                document = document.WithSyntaxRoot(newRoot)
                currentSolution = document.Project.Solution
            Next

            Return currentSolution
        End Function

        Private Shared Async Function FindCallSitesAsync(document As SemanticDocument, methodSymbol As IMethodSymbol,
            cancellationToken As CancellationToken) As Task(Of ImmutableDictionary(Of Document, List(Of InvocationExpressionSyntax)))

            Dim methodCallSites = New Dictionary(Of Document, List(Of InvocationExpressionSyntax))
            Dim progress = New StreamingProgressCollector()

            Await SymbolFinder.FindReferencesAsync(methodSymbol, document.Document.Project.Solution, progress, Nothing, FindReferencesSearchOptions.Default, cancellationToken).ConfigureAwait(False)
            Dim referencedSymbols = progress.GetReferencedSymbols()
            Dim referencedLocations = referencedSymbols.SelectMany(Function(referencedSymbol) referencedSymbol.Locations).Distinct().ToImmutableArray()

            Dim list = New List(Of InvocationExpressionSyntax)
            For Each refLocation In referencedLocations
                If Not methodCallSites.TryGetValue(refLocation.Document, list) Then
                    list = New List(Of InvocationExpressionSyntax)
                    methodCallSites.Add(refLocation.Document, list)
                End If
                list.Add(DirectCast(refLocation.Location.FindNode(cancellationToken).Parent, InvocationExpressionSyntax))
            Next

            Return methodCallSites.ToImmutableDictionary()
        End Function

        Private Shared Function TieExpressionToParameters(expression As ExpressionSyntax, methodSymbol As IMethodSymbol) As Dictionary(Of IParameterSymbol, IdentifierNameSyntax)

            Dim nameToParameterDict = New Dictionary(Of IParameterSymbol, IdentifierNameSyntax)
            Dim variablesInExpression = expression.DescendantNodes().OfType(Of IdentifierNameSyntax)

            For Each variable In variablesInExpression
                For Each parameter In methodSymbol.Parameters
                    If variable.Identifier.ValueText = parameter.Name Then
                        nameToParameterDict.Add(parameter, variable)
                        Exit For
                    End If
                Next
            Next

            Return nameToParameterDict
        End Function

        Protected Overrides Function ExpressionWithinParameterizedMethod(expression As ExpressionSyntax) As Boolean
            Dim methodExpression = expression.FirstAncestorOrSelf(Of MethodBlockSyntax)()
            Dim variablesInExpression = expression.DescendantNodes().OfType(Of IdentifierNameSyntax)
            Dim variableCount = 0
            Dim parameterCount = 0

            For Each variable In variablesInExpression
                variableCount += 1
                For Each parameter In methodExpression.BlockStatement.ParameterList.Parameters
                    If variable.Identifier.Value Is parameter.Identifier.Identifier.Value Then
                        parameterCount += 1
                        Exit For
                    End If
                Next
            Next

            Return variableCount = parameterCount
        End Function

        Protected Overrides Function RewriteCore(Of TNode As SyntaxNode)(node As TNode, replacementNode As SyntaxNode, matches As ISet(Of ExpressionSyntax)) As TNode
            Return DirectCast(Rewriter.Visit(node, replacementNode, matches), TNode)
        End Function
    End Class
End Namespace
