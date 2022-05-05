' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

    <ExportSignatureHelpProvider("FunctionAggregationSignatureHelpProvider", LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class FunctionAggregationSignatureHelpProvider
        Inherits AbstractVisualBasicSignatureHelpProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides Function IsTriggerCharacter(ch As Char) As Boolean
            Return ch = "("c
        End Function

        Public Overrides Function IsRetriggerCharacter(ch As Char) As Boolean
            Return ch = ")"c
        End Function

        Private Shared Function GetCurrentArgumentState(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, currentSpan As TextSpan, cancellationToken As CancellationToken) As SignatureHelpState
            Dim functionAggregation As FunctionAggregationSyntax = Nothing
            If TryGetFunctionAggregation(root, position, syntaxFacts, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken, functionAggregation) AndAlso
                functionAggregation.SpanStart = currentSpan.Start Then
                Return New SignatureHelpState(0, 0, Nothing, Nothing)
            End If

            Return Nothing
        End Function

        Private Shared Function TryGetFunctionAggregation(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, triggerReason As SignatureHelpTriggerReason,
                                                   cancellationToken As CancellationToken, ByRef functionAggregation As FunctionAggregationSyntax) As Boolean
            Return CommonSignatureHelpUtilities.TryGetSyntax(
                root,
                position,
                syntaxFacts,
                triggerReason,
                Function(t) TypeOf t.Parent Is FunctionAggregationSyntax,
                Function(n, t) n.CloseParenToken <> t AndAlso n.Span.Contains(t.SpanStart) AndAlso n.OpenParenToken.SpanStart <= t.SpanStart,
                cancellationToken,
                functionAggregation)
        End Function

        Protected Overrides Async Function GetItemsWorkerAsync(document As Document, position As Integer, triggerInfo As SignatureHelpTriggerInfo, options As SignatureHelpOptions, cancellationToken As CancellationToken) As Task(Of SignatureHelpItems)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            Dim functionAggregation As FunctionAggregationSyntax = Nothing
            If Not TryGetFunctionAggregation(root, position, document.GetLanguageService(Of ISyntaxFactsService), triggerInfo.TriggerReason, cancellationToken, functionAggregation) Then
                Return Nothing
            End If

            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim methods = semanticModel.LookupSymbols(
                functionAggregation.SpanStart,
                name:=functionAggregation.FunctionName.ValueText,
                includeReducedExtensionMethods:=True).OfType(Of IMethodSymbol).
                                                      Where(Function(m) m.IsAggregateFunction()).
                                                      ToImmutableArrayOrEmpty()

            Dim within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken)
            If within Is Nothing Then
                Return Nothing
            End If

            Dim accessibleMethods = methods.WhereAsArray(Function(m) m.IsAccessibleWithin(within)).
                                            FilterToVisibleAndBrowsableSymbolsAndNotUnsafeSymbols(options.HideAdvancedMembers, semanticModel.Compilation).
                                            Sort(semanticModel, functionAggregation.SpanStart)

            If Not accessibleMethods.Any() Then
                Return Nothing
            End If

            Dim structuralTypeDisplayService = document.GetLanguageService(Of IStructuralTypeDisplayService)()
            Dim documentationCommentFormattingService = document.GetLanguageService(Of IDocumentationCommentFormattingService)()
            Dim textSpan = CommonSignatureHelpUtilities.GetSignatureHelpSpan(functionAggregation, functionAggregation.SpanStart, Function(n) n.CloseParenToken)
            Dim syntaxFacts = document.GetLanguageService(Of ISyntaxFactsService)

            Return CreateSignatureHelpItems(
                accessibleMethods.Select(Function(m) Convert(m, functionAggregation, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService)).ToList(),
                textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken), selectedItem:=Nothing)
        End Function

        Private Overloads Shared Function Convert(method As IMethodSymbol,
                                           functionAggregation As FunctionAggregationSyntax,
                                           semanticModel As SemanticModel,
                                           structuralTypeDisplayService As IStructuralTypeDisplayService,
                                           documentationCommentFormattingService As IDocumentationCommentFormattingService) As SignatureHelpItem
            Dim position = functionAggregation.SpanStart
            Dim item = CreateItem(
                method, semanticModel, position,
                structuralTypeDisplayService,
                False,
                method.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                GetPreambleParts(method),
                GetSeparatorParts(),
                GetPostambleParts(method, semanticModel, position),
                GetParameterParts(method, semanticModel, position, documentationCommentFormattingService))
            Return item
        End Function

        Private Shared Function GetPreambleParts(method As IMethodSymbol) As IList(Of SymbolDisplayPart)
            Dim result = New List(Of SymbolDisplayPart)()
            AddExtensionPreamble(method, result)
            result.AddMethodName(method.Name)
            result.Add(Punctuation(SyntaxKind.OpenParenToken))
            Return result
        End Function

        Private Shared Function GetPostambleParts(method As IMethodSymbol,
                                           semanticModel As SemanticModel,
                                           position As Integer) As IList(Of SymbolDisplayPart)
            Dim parts = New List(Of SymbolDisplayPart)
            parts.Add(Punctuation(SyntaxKind.CloseParenToken))

            If Not method.ReturnsVoid Then
                parts.Add(Space())
                parts.Add(Keyword(SyntaxKind.AsKeyword))
                parts.Add(Space())
                parts.AddRange(method.ReturnType.ToMinimalDisplayParts(semanticModel, position))
            End If

            Return parts
        End Function

        Private Shared Function GetParameterParts(method As IMethodSymbol, semanticModel As SemanticModel, position As Integer,
                                           documentationCommentFormattingService As IDocumentationCommentFormattingService) As IList(Of SignatureHelpSymbolParameter)
            ' Function <name>() As <type>
            If method.Parameters.Length <> 1 Then
                Return SpecializedCollections.EmptyList(Of SignatureHelpSymbolParameter)()
            End If

            ' Function <name>(selector as Func(Of T, R)) As R
            Dim parameter = method.Parameters(0)
            If parameter.Type.TypeKind = TypeKind.Delegate Then
                Dim delegateInvokeMethod = DirectCast(parameter.Type, INamedTypeSymbol).DelegateInvokeMethod

                If delegateInvokeMethod IsNot Nothing AndAlso
                   delegateInvokeMethod.Parameters.Length = 1 AndAlso
                   Not delegateInvokeMethod.ReturnsVoid Then

                    Dim parts = New List(Of SymbolDisplayPart)
                    parts.Add(Text(VBWorkspaceResources.expression))
                    parts.Add(Space())
                    parts.Add(Keyword(SyntaxKind.AsKeyword))
                    parts.Add(Space())
                    parts.AddRange(delegateInvokeMethod.ReturnType.ToMinimalDisplayParts(semanticModel, position))

                    Dim sigHelpParameter = New SignatureHelpSymbolParameter(
                        VBWorkspaceResources.expression,
                        parameter.IsOptional,
                        parameter.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                        parts)

                    Return {sigHelpParameter}
                End If
            End If

            Return SpecializedCollections.EmptyList(Of SignatureHelpSymbolParameter)()
        End Function
    End Class
End Namespace
