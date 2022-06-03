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

    <ExportSignatureHelpProvider("GenericNameSignatureHelpProvider", LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class GenericNameSignatureHelpProvider
        Inherits AbstractVisualBasicSignatureHelpProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides Function IsTriggerCharacter(ch As Char) As Boolean
            Return ch = " "c OrElse ch = ","c
        End Function

        Public Overrides Function IsRetriggerCharacter(ch As Char) As Boolean
            Return ch = ")"c
        End Function

        Private Shared Function GetCurrentArgumentState(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, currentSpan As TextSpan, cancellationToken As CancellationToken) As SignatureHelpState
            Dim expression As GenericNameSyntax = Nothing
            If TryGetGenericName(root, position, syntaxFacts, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken, expression) AndAlso
                currentSpan.Start = SignatureHelpUtilities.GetSignatureHelpSpan(expression.TypeArgumentList).Start Then

                Return SignatureHelpUtilities.GetSignatureHelpState(expression.TypeArgumentList, position)
            End If

            Return Nothing
        End Function

        Private Shared Function TryGetGenericName(root As SyntaxNode, position As Integer, syntaxFacts As ISyntaxFactsService, triggerReason As SignatureHelpTriggerReason, cancellationToken As CancellationToken, ByRef genericName As GenericNameSyntax) As Boolean
            If Not CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, AddressOf IsTriggerToken, AddressOf IsArgumentListToken, cancellationToken, genericName) Then
                Return False
            End If

            Return genericName.TypeArgumentList IsNot Nothing
        End Function

        Private Shared Function IsTriggerToken(token As SyntaxToken) As Boolean
            Return (token.Kind = SyntaxKind.OfKeyword OrElse token.Kind = SyntaxKind.CommaToken) AndAlso
                    TypeOf token.Parent Is TypeArgumentListSyntax AndAlso
                    TypeOf token.Parent.Parent Is GenericNameSyntax
        End Function

        Private Shared Function IsArgumentListToken(node As GenericNameSyntax, token As SyntaxToken) As Boolean
            Return node.TypeArgumentList.Span.Contains(token.SpanStart) AndAlso
                token <> node.TypeArgumentList.CloseParenToken
        End Function

        Protected Overrides Async Function GetItemsWorkerAsync(document As Document, position As Integer, triggerInfo As SignatureHelpTriggerInfo, options As SignatureHelpOptions, cancellationToken As CancellationToken) As Task(Of SignatureHelpItems)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            Dim genericName As GenericNameSyntax = Nothing
            If Not TryGetGenericName(root, position, document.GetLanguageService(Of ISyntaxFactsService), triggerInfo.TriggerReason, cancellationToken, genericName) Then
                Return Nothing
            End If

            Dim beforeDotExpression = If(genericName.IsRightSideOfDotOrBang(), genericName.GetLeftSideOfDot(), Nothing)
            Dim semanticModel = Await document.ReuseExistingSpeculativeModelAsync(If(beforeDotExpression, genericName), cancellationToken).ConfigureAwait(False)

            Dim leftSymbol = If(beforeDotExpression Is Nothing, Nothing,
                                TryCast(semanticModel.GetSymbolInfo(beforeDotExpression, cancellationToken).GetAnySymbol(), INamespaceOrTypeSymbol))
            Dim leftType = If(beforeDotExpression Is Nothing, Nothing,
                              TryCast(semanticModel.GetTypeInfo(beforeDotExpression, cancellationToken).Type, INamespaceOrTypeSymbol))
            Dim leftContainer = If(leftSymbol, leftType)

            Dim isBaseAccess = TypeOf beforeDotExpression Is MyBaseExpressionSyntax
            Dim namespacesOrTypesOnly = SyntaxFacts.IsInNamespaceOrTypeContext(genericName)
            Dim includeExtensions = leftSymbol Is Nothing AndAlso leftType IsNot Nothing

            Dim name As String = genericName.Identifier.ValueText
            Dim symbols = If(
                isBaseAccess,
                semanticModel.LookupBaseMembers(position, name),
                If(
                    namespacesOrTypesOnly,
                    semanticModel.LookupNamespacesAndTypes(position, leftContainer, name),
                    semanticModel.LookupSymbols(position, leftContainer, name, includeExtensions)))

            Dim within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken)
            If within Is Nothing Then
                Return Nothing
            End If

            Dim accessibleSymbols = symbols.WhereAsArray(Function(s) s.GetArity() > 0).
                                            WhereAsArray(Function(s) TypeOf s Is INamedTypeSymbol OrElse TypeOf s Is IMethodSymbol).
                                            FilterToVisibleAndBrowsableSymbolsAndNotUnsafeSymbols(options.HideAdvancedMembers, semanticModel.Compilation).
                                            Sort(semanticModel, genericName.SpanStart)

            If accessibleSymbols.Length = 0 Then
                Return Nothing
            End If

            Dim structuralTypeDisplayService = document.GetLanguageService(Of IStructuralTypeDisplayService)()
            Dim documentationCommentFormattingService = document.GetLanguageService(Of IDocumentationCommentFormattingService)()
            Dim textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(genericName.TypeArgumentList)
            Dim _syntaxFacts = document.GetLanguageService(Of ISyntaxFactsService)

            Return CreateSignatureHelpItems(
                accessibleSymbols.Select(Function(s) Convert(s, genericName, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService)).ToList(),
                textSpan, GetCurrentArgumentState(root, position, _syntaxFacts, textSpan, cancellationToken), selectedItem:=Nothing)
        End Function

        Private Overloads Shared Function Convert(symbol As ISymbol, genericName As GenericNameSyntax, semanticModel As SemanticModel, structuralTypeDisplayService As IStructuralTypeDisplayService, documentationCommentFormattingService As IDocumentationCommentFormattingService) As SignatureHelpItem
            Dim position = genericName.SpanStart
            Dim item As SignatureHelpItem
            If TypeOf symbol Is INamedTypeSymbol Then
                Dim namedType = DirectCast(symbol, INamedTypeSymbol)
                item = CreateItem(
                    symbol, semanticModel, position,
                    structuralTypeDisplayService,
                    False,
                    symbol.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                    GetPreambleParts(namedType, semanticModel, position),
                    GetSeparatorParts(),
                    GetPostambleParts(),
                    namedType.TypeParameters.Select(Function(p) Convert(p, semanticModel, position, documentationCommentFormattingService)).ToList())
            Else
                Dim method = DirectCast(symbol, IMethodSymbol)
                item = CreateItem(
                    symbol, semanticModel, position,
                    structuralTypeDisplayService,
                    False,
                    symbol.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                    GetPreambleParts(method, semanticModel, position),
                    GetSeparatorParts(),
                    GetPostambleParts(method, semanticModel, position),
                    method.TypeParameters.Select(Function(p) Convert(p, semanticModel, position, documentationCommentFormattingService)).ToList())
            End If

            Return item
        End Function

        Private Shared ReadOnly s_minimallyQualifiedFormat As SymbolDisplayFormat = SymbolDisplayFormat.MinimallyQualifiedFormat.WithGenericsOptions(SymbolDisplayFormat.MinimallyQualifiedFormat.GenericsOptions Or SymbolDisplayGenericsOptions.IncludeVariance)

        Private Overloads Shared Function Convert(parameter As ITypeParameterSymbol, semanticModel As SemanticModel, position As Integer, documentationCommentFormattingService As IDocumentationCommentFormattingService) As SignatureHelpSymbolParameter
            Dim parts = New List(Of SymbolDisplayPart)
            parts.AddRange(parameter.ToMinimalDisplayParts(semanticModel, position, s_minimallyQualifiedFormat))
            AddConstraints(parameter, parts, semanticModel, position)

            Return New SignatureHelpSymbolParameter(
                parameter.Name,
                isOptional:=False,
                documentationFactory:=parameter.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                displayParts:=parts)
        End Function

        Private Shared Function AddConstraints(typeParam As ITypeParameterSymbol,
                                        parts As List(Of SymbolDisplayPart),
                                        semanticModel As SemanticModel,
                                        position As Integer) As IList(Of SymbolDisplayPart)
            Dim constraintTypes = typeParam.ConstraintTypes
            Dim constraintCount = TypeParameterSpecialConstraintCount(typeParam) + constraintTypes.Length

            If constraintCount <> 0 Then
                parts.Add(Space())
                parts.Add(Keyword(SyntaxKind.AsKeyword))
                parts.Add(Space())

                If constraintCount > 1 Then
                    parts.Add(Punctuation(SyntaxKind.OpenBraceToken))
                End If

                Dim needComma As Boolean = False
                If typeParam.HasReferenceTypeConstraint Then
                    parts.Add(Keyword(SyntaxKind.ClassKeyword))
                    needComma = True
                ElseIf typeParam.HasValueTypeConstraint Then
                    parts.Add(Keyword(SyntaxKind.StructureKeyword))
                    needComma = True
                End If

                For Each baseType In constraintTypes
                    If needComma Then
                        parts.Add(Punctuation(SyntaxKind.CommaToken))
                        parts.Add(Space())
                    End If

                    parts.AddRange(baseType.ToMinimalDisplayParts(semanticModel, position))
                    needComma = True
                Next

                If typeParam.HasConstructorConstraint Then
                    If needComma Then
                        parts.Add(Punctuation(SyntaxKind.CommaToken))
                        parts.Add(Space())
                    End If

                    parts.Add(Keyword(SyntaxKind.NewKeyword))
                End If

                If constraintCount > 1 Then
                    parts.Add(Punctuation(SyntaxKind.CloseBraceToken))
                End If
            End If

            Return parts
        End Function

        Private Shared Function TypeParameterSpecialConstraintCount(typeParam As ITypeParameterSymbol) As Integer
            Return If(typeParam.HasReferenceTypeConstraint, 1, 0) +
                If(typeParam.HasValueTypeConstraint, 1, 0) +
                If(typeParam.HasConstructorConstraint, 1, 0)
        End Function
    End Class
End Namespace

