' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp.Providers
    Friend Class FunctionAggregationSignatureHelpProvider
        Inherits AbstractVisualBasicSignatureHelpProvider

        Public Overrides Function IsTriggerCharacter(ch As Char) As Boolean
            Return ch = "("c
        End Function

        Public Overrides Function IsRetriggerCharacter(ch As Char) As Boolean
            Return ch = ")"c
        End Function

        Protected Overrides Function GetCurrentArgumentState(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, currentSpan As TextSpan, cancellationToken As CancellationToken) As SignatureHelpState
            Dim functionAggregation As FunctionAggregationSyntax = Nothing
            If TryGetFunctionAggregation(root, position, syntaxFacts, SignatureHelpTriggerKind.Other, cancellationToken, functionAggregation) AndAlso
                functionAggregation.SpanStart = currentSpan.Start Then
                Return New SignatureHelpState(0, 0, Nothing, Nothing)
            End If

            Return Nothing
        End Function

        Private Function TryGetFunctionAggregation(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, triggerReason As SignatureHelpTriggerKind,
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

        Protected Overrides Async Function ProvideSignaturesWorkerAsync(context As SignatureContext) As Task
            Dim document = context.Document
            Dim position = context.Position
            Dim trigger = context.Trigger
            Dim cancellationToken = context.CancellationToken

            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            Dim functionAggregation As FunctionAggregationSyntax = Nothing
            If Not TryGetFunctionAggregation(root, position, document.GetLanguageService(Of ISyntaxFactsService), trigger.Kind, cancellationToken, functionAggregation) Then
                Return
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
                Return
            End If

            Dim symbolDisplayService = document.Project.LanguageServices.GetService(Of ISymbolDisplayService)()
            Dim accessibleMethods = methods.WhereAsArray(Function(m) m.IsAccessibleWithin(within)).
                                            FilterToVisibleAndBrowsableSymbolsAndNotUnsafeSymbols(document.ShouldHideAdvancedMembers(), semanticModel.Compilation).
                                            Sort(symbolDisplayService, semanticModel, functionAggregation.SpanStart)

            If Not accessibleMethods.Any() Then
                Return
            End If

            Dim anonymousTypeDisplayService = document.Project.LanguageServices.GetService(Of IAnonymousTypeDisplayService)()
            Dim textSpan = CommonSignatureHelpUtilities.GetSignatureHelpSpan(functionAggregation, functionAggregation.SpanStart, Function(n) n.CloseParenToken)
            Dim syntaxFacts = document.GetLanguageService(Of ISyntaxFactsService)

            context.AddItems(accessibleMethods.Select(Function(m) Convert(m, functionAggregation, semanticModel, symbolDisplayService, anonymousTypeDisplayService, cancellationToken)))
            context.SetSpan(textSpan)
            context.SetState(GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken))
        End Function

        Private Overloads Function Convert(method As IMethodSymbol,
                                           functionAggregation As FunctionAggregationSyntax,
                                           semanticModel As SemanticModel,
                                           symbolDisplayService As ISymbolDisplayService,
                                           anonymousTypeDisplayService As IAnonymousTypeDisplayService,
                                           cancellationToken As CancellationToken) As SignatureHelpItem
            Dim position = functionAggregation.SpanStart
            Dim item = CreateItem(
                method, semanticModel, position,
                symbolDisplayService, anonymousTypeDisplayService,
                False,
                GetPreambleParts(method, semanticModel, position),
                GetSeparatorParts(),
                GetPostambleParts(method, semanticModel, position),
                GetParameterParts(method, semanticModel, position, cancellationToken))
            Return item
        End Function

        Private Function GetPreambleParts(method As IMethodSymbol, semanticModel As SemanticModel, position As Integer) As IList(Of SymbolDisplayPart)
            Dim result = New List(Of SymbolDisplayPart)()
            AddExtensionPreamble(method, result)
            result.AddMethodName(method.Name)
            result.Add(Punctuation(SyntaxKind.OpenParenToken))
            Return result
        End Function

        Private Function GetPostambleParts(method As IMethodSymbol,
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

        Private Function GetParameterParts(method As IMethodSymbol, semanticModel As SemanticModel, position As Integer, cancellationToken As CancellationToken) As IList(Of CommonParameterData)
            ' Function <name>() As <type>
            If method.Parameters.Length <> 1 Then
                Return SpecializedCollections.EmptyList(Of CommonParameterData)()
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

                    Dim sigHelpParameter = New CommonParameterData(
                        VBWorkspaceResources.expression,
                        parameter.IsOptional,
                        parameter,
                        position,
                        parts.ToImmutableArrayOrEmpty())

                    Return {sigHelpParameter}
                End If
            End If

            Return SpecializedCollections.EmptyList(Of CommonParameterData)()
        End Function
    End Class
End Namespace