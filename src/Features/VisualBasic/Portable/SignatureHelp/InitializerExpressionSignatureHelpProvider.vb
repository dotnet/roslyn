' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections
Imports System.Collections.Immutable
Imports System.Composition
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.SignatureHelp
    <ExportSignatureHelpProvider(NameOf(CollectionInitializerSignatureHelpProvider), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class CollectionInitializerSignatureHelpProvider
        Inherits AbstractOrdinaryMethodSignatureHelpProvider

        Public Overrides Function IsTriggerCharacter(ch As Char) As Boolean
            Return ch = "{"c OrElse ch = ","c
        End Function

        Public Overrides Function IsRetriggerCharacter(ch As Char) As Boolean
            Return ch = "}"c
        End Function

        Private Function TryGetInitializerExpression(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, triggerReason As SignatureHelpTriggerReason, cancellationToken As CancellationToken, ByRef expression As CollectionInitializerSyntax) As Boolean
            Return CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, AddressOf IsTriggerToken, AddressOf IsInitializerExpressionToken, cancellationToken, expression) AndAlso
                   expression IsNot Nothing
        End Function

        Private Function IsTriggerToken(token As SyntaxToken) As Boolean
            Return Not token.IsKind(SyntaxKind.None) AndAlso
               token.ValueText.Length = 1 AndAlso
               IsTriggerCharacter(token.ValueText(0)) AndAlso
               TypeOf token.Parent Is CollectionInitializerSyntax
        End Function

        Private Shared Function IsInitializerExpressionToken(expression As CollectionInitializerSyntax, token As SyntaxToken) As Boolean
            Return expression.Span.Contains(token.SpanStart) AndAlso token <> expression.CloseBraceToken
        End Function

        Protected Overrides Async Function GetItemsWorkerAsync(document As Document, position As Integer, triggerInfo As SignatureHelpTriggerInfo, cancellationToken As CancellationToken) As Task(Of SignatureHelpItems)
            Dim root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim collectionInitializer As CollectionInitializerSyntax
            If Not TryGetInitializerExpression(root, position, document.GetLanguageService(Of ISyntaxFactsService)(), triggerInfo.TriggerReason, cancellationToken, collectionInitializer) Then
                Return Nothing
            End If

            Dim semanticModel = Await document.GetSemanticModelForNodeAsync(collectionInitializer, cancellationToken).ConfigureAwait(False)
            Dim compilation = semanticModel.Compilation
            Dim ienumerableType = compilation.GetTypeByMetadataName(GetType(IEnumerable).FullName)
            If ienumerableType Is Nothing Then
                Return Nothing
            End If

            If TypeOf collectionInitializer.Parent IsNot CollectionInitializerSyntax OrElse
               TypeOf collectionInitializer.Parent.Parent IsNot ObjectCollectionInitializerSyntax Then
                Return Nothing
            End If

            ' get the regular signature help items
            Dim parentOperation = TryCast(semanticModel.GetOperation(collectionInitializer.Parent.Parent, cancellationToken), IObjectOrCollectionInitializerOperation)
            Dim parentType = parentOperation?.Type
            If parentType Is Nothing Then
                Return Nothing
            End If

            If Not parentType.AllInterfaces.Contains(ienumerableType) Then
                Return Nothing
            End If

            Dim addSymbols = semanticModel.LookupSymbols(
                position, parentType, WellKnownMemberNames.CollectionInitializerAddMethodName, includeReducedExtensionMethods:=True)

            ' We want all the accessible '.Add' methods that take at least two arguments. For
            ' example, say there is:
            '
            '      new JObject From { { $$ } }
            '
            ' Technically, the user could be calling the `.Add(object)` overload in this case.
            ' However, normally in that case, they would just supply the value directly like so:
            '
            '      new JObject From { new JProperty(...), new JProperty(...) }
            '
            ' So, it's a strong signal when they're inside another `{ $$ }` that they want to
            ' call the .Add methods that take multiple args, like so:
            '
            '      new JObject From { { propName, propValue }, { propName, propValue } }

            Dim symbolDisplayService = document.GetLanguageService(Of ISymbolDisplayService)()
            Dim addMethods = addSymbols.OfType(Of IMethodSymbol)().
                Where(Function(m) m.Parameters.Length >= 2).
                ToImmutableArray().
                FilterToVisibleAndBrowsableSymbols(document.ShouldHideAdvancedMembers(), semanticModel.Compilation).
                Sort(symbolDisplayService, semanticModel, collectionInitializer.SpanStart)

            If addMethods.IsEmpty Then
                Return Nothing
            End If

            Dim textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(collectionInitializer)
            Dim syntaxFacts = document.GetLanguageService(Of ISyntaxFactsService)()

            Return CreateSignatureHelpItems(
                addMethods.Select(Function(s) ConvertMemberGroupMember(document, s, collectionInitializer.OpenBraceToken.SpanStart, semanticModel, cancellationToken)).ToList(),
                textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken), selectedItem:=Nothing)
        End Function

        Public Overrides Function GetCurrentArgumentState(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, currentSpan As TextSpan, cancellationToken As CancellationToken) As SignatureHelpState
            Dim expression As CollectionInitializerSyntax
            If TryGetInitializerExpression(
                        root,
                        position,
                        syntaxFacts,
                        SignatureHelpTriggerReason.InvokeSignatureHelpCommand,
                        cancellationToken,
                        expression) AndAlso
                    currentSpan.Start = SignatureHelpUtilities.GetSignatureHelpSpan(expression).Start Then
                Return SignatureHelpUtilities.GetSignatureHelpState(expression, position)
            End If

            Return Nothing
        End Function
    End Class

End Namespace
