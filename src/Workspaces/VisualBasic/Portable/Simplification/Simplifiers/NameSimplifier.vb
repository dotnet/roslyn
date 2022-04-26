' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Rename.ConflictEngine
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification.Simplifiers
    Friend Class NameSimplifier
        Inherits AbstractVisualBasicSimplifier(Of NameSyntax, ExpressionSyntax)

        Public Shared ReadOnly Instance As New NameSimplifier()

        Private Sub New()
        End Sub

        Public Overrides Function TrySimplify(
                name As NameSyntax,
                semanticModel As SemanticModel,
                options As VisualBasicSimplifierOptions,
                <Out> ByRef replacementNode As ExpressionSyntax,
                <Out> ByRef issueSpan As TextSpan,
                cancellationToken As CancellationToken) As Boolean

            ' do not simplify names of a namespace declaration
            If IsPartOfNamespaceDeclarationName(name) Then
                Return False
            End If

            ' see whether binding the name binds to a symbol/type. if not, it is ambiguous and
            ' nothing we can do here.
            Dim symbol = SimplificationHelpers.GetOriginalSymbolInfo(semanticModel, name)
            If SimplificationHelpers.IsValidSymbolInfo(symbol) Then
                If symbol.Kind = SymbolKind.Method AndAlso symbol.IsConstructor() Then
                    symbol = symbol.ContainingType
                End If

                If symbol.Kind = SymbolKind.Method AndAlso name.Kind = SyntaxKind.GenericName Then
                    Dim genericName = DirectCast(name, GenericNameSyntax)
                    replacementNode = SyntaxFactory.IdentifierName(genericName.Identifier).WithLeadingTrivia(genericName.GetLeadingTrivia()).WithTrailingTrivia(genericName.GetTrailingTrivia())

                    issueSpan = genericName.TypeArgumentList.Span
                    Return CanReplaceWithReducedName(name, replacementNode, semanticModel, cancellationToken)
                End If

                If Not TypeOf symbol Is INamespaceOrTypeSymbol Then
                    Return False
                End If
            Else
                Return False
            End If

            If name.HasAnnotations(SpecialTypeAnnotation.Kind) Then
                replacementNode = SyntaxFactory.PredefinedType(
                    SyntaxFactory.Token(name.GetLeadingTrivia(),
                                 GetPredefinedKeywordKind(SpecialTypeAnnotation.GetSpecialType(name.GetAnnotations(SpecialTypeAnnotation.Kind).First())),
                                 name.GetTrailingTrivia()))

                issueSpan = name.Span

                Return CanReplaceWithReducedNameInContext(name, replacementNode)
            Else

                If Not name.IsRightSideOfDot() Then

                    Dim aliasReplacement As IAliasSymbol = Nothing
                    If TryReplaceWithAlias(name, semanticModel, aliasReplacement) Then
                        Dim identifierToken = SyntaxFactory.Identifier(
                                name.GetLeadingTrivia(),
                                aliasReplacement.Name,
                                name.GetTrailingTrivia())

                        identifierToken = TryEscapeIdentifierToken(identifierToken)

                        replacementNode = SyntaxFactory.IdentifierName(identifierToken)

                        Dim annotatedNodesOrTokens = name.GetAnnotatedNodesAndTokens(RenameAnnotation.Kind)
                        For Each annotatedNodeOrToken In annotatedNodesOrTokens
                            If annotatedNodeOrToken.IsToken Then
                                identifierToken = annotatedNodeOrToken.AsToken().CopyAnnotationsTo(identifierToken)
                            Else
                                replacementNode = annotatedNodeOrToken.AsNode().CopyAnnotationsTo(replacementNode)
                            End If
                        Next

                        annotatedNodesOrTokens = name.GetAnnotatedNodesAndTokens(AliasAnnotation.Kind)
                        For Each annotatedNodeOrToken In annotatedNodesOrTokens
                            If annotatedNodeOrToken.IsToken Then
                                identifierToken = annotatedNodeOrToken.AsToken().CopyAnnotationsTo(identifierToken)
                            Else
                                replacementNode = annotatedNodeOrToken.AsNode().CopyAnnotationsTo(replacementNode)
                            End If
                        Next

                        replacementNode = DirectCast(replacementNode, SimpleNameSyntax).WithIdentifier(identifierToken)
                        issueSpan = name.Span

                        ' In case the alias name is the same as the last name of the alias target, we only include 
                        ' the left part of the name in the unnecessary span to Not confuse uses.
                        If name.Kind = SyntaxKind.QualifiedName Then
                            Dim qualifiedName As QualifiedNameSyntax = DirectCast(name, QualifiedNameSyntax)

                            If qualifiedName.Right.Identifier.ValueText = identifierToken.ValueText Then
                                issueSpan = qualifiedName.Left.Span
                            End If
                        End If

                        If CanReplaceWithReducedNameInContext(name, replacementNode) Then

                            ' check if the alias name ends with an Attribute suffix that can be omitted.
                            Dim replacementNodeWithoutAttributeSuffix As ExpressionSyntax = Nothing
                            Dim issueSpanWithoutAttributeSuffix As TextSpan = Nothing
                            If TryReduceAttributeSuffix(name, identifierToken, semanticModel, aliasReplacement IsNot Nothing, replacementNodeWithoutAttributeSuffix, issueSpanWithoutAttributeSuffix, cancellationToken) Then
                                If CanReplaceWithReducedName(name, replacementNodeWithoutAttributeSuffix, semanticModel, cancellationToken) Then
                                    replacementNode = replacementNode.CopyAnnotationsTo(replacementNodeWithoutAttributeSuffix)
                                    issueSpan = issueSpanWithoutAttributeSuffix
                                End If
                            End If

                            Return True
                        End If

                        Return False
                    End If

                    Dim nameHasNoAlias = False

                    If TypeOf name Is SimpleNameSyntax Then
                        Dim simpleName = DirectCast(name, SimpleNameSyntax)
                        If Not simpleName.Identifier.HasAnnotations(AliasAnnotation.Kind) Then
                            nameHasNoAlias = True
                        End If
                    End If

                    If TypeOf name Is QualifiedNameSyntax Then
                        Dim qualifiedNameSyntax = DirectCast(name, QualifiedNameSyntax)
                        If Not qualifiedNameSyntax.Right.Identifier.HasAnnotations(AliasAnnotation.Kind) Then
                            nameHasNoAlias = True
                        End If
                    End If

                    Dim aliasInfo = semanticModel.GetAliasInfo(name, cancellationToken)

                    ' Don't simplify to predefined type if name is part of a QualifiedName.
                    ' QualifiedNames can't contain PredefinedTypeNames (although MemberAccessExpressions can).
                    ' In other words, the left side of a QualifiedName can't be a PredefinedTypeName.
                    If nameHasNoAlias AndAlso aliasInfo Is Nothing AndAlso Not name.Parent.IsKind(SyntaxKind.QualifiedName) Then
                        Dim type = semanticModel.GetTypeInfo(name, cancellationToken).Type
                        If type IsNot Nothing Then
                            Dim keywordKind = GetPredefinedKeywordKind(type.SpecialType)
                            If keywordKind <> SyntaxKind.None Then
                                ' But do simplify to predefined type if not simplifying results in just the addition of escaping
                                '   brackets.  E.g., even if specified otherwise, prefer `String` to `[String]`.
                                Dim token = SyntaxFactory.Token(
                                    name.GetLeadingTrivia(),
                                    keywordKind,
                                    name.GetTrailingTrivia())
                                Dim valueText = TryCast(name, IdentifierNameSyntax)?.Identifier.ValueText
                                Dim inDeclarationContext = PreferPredefinedTypeKeywordInDeclarations(name, options)
                                Dim inMemberAccessContext = PreferPredefinedTypeKeywordInMemberAccess(name, options)
                                If token.Text = valueText OrElse (inDeclarationContext OrElse inMemberAccessContext) Then

                                    Dim codeStyleOptionName As String = Nothing
                                    If inDeclarationContext Then
                                        codeStyleOptionName = NameOf(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration)
                                    ElseIf inMemberAccessContext Then
                                        codeStyleOptionName = NameOf(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess)
                                    End If

                                    replacementNode = SyntaxFactory.PredefinedType(token)
                                    issueSpan = name.Span

                                    Dim canReplace = CanReplaceWithReducedNameInContext(name, replacementNode)
                                    If canReplace Then
                                        replacementNode = replacementNode.WithAdditionalAnnotations(New SyntaxAnnotation(codeStyleOptionName))
                                    End If

                                    Return canReplace
                                End If
                            End If
                        End If
                    End If

                    ' Nullable rewrite: Nullable(Of Integer) -> Integer?
                    ' Don't rewrite in the case where Nullable(Of Integer) is part of some qualified name like Nullable(Of Integer).Something
                    If (symbol.Kind = SymbolKind.NamedType) AndAlso (Not name.IsLeftSideOfQualifiedName) Then
                        Dim type = DirectCast(symbol, INamedTypeSymbol)
                        If aliasInfo Is Nothing AndAlso CanSimplifyNullable(type, name) Then
                            Dim genericName As GenericNameSyntax
                            If name.Kind = SyntaxKind.QualifiedName Then
                                genericName = DirectCast(DirectCast(name, QualifiedNameSyntax).Right, GenericNameSyntax)
                            Else
                                genericName = DirectCast(name, GenericNameSyntax)
                            End If

                            Dim oldType = genericName.TypeArgumentList.Arguments.First()
                            replacementNode = SyntaxFactory.NullableType(oldType).WithLeadingTrivia(name.GetLeadingTrivia())

                            issueSpan = name.Span

                            If CanReplaceWithReducedNameInContext(name, replacementNode) Then
                                Return True
                            End If
                        End If
                    End If
                End If

                Select Case name.Kind
                    Case SyntaxKind.QualifiedName
                        ' a module name was inserted by the name expansion, so removing this should be tried first.
                        Dim qualifiedName = DirectCast(name, QualifiedNameSyntax)
                        If qualifiedName.HasAnnotation(SimplificationHelpers.SimplifyModuleNameAnnotation) Then
                            If TryOmitModuleName(qualifiedName, semanticModel, replacementNode, issueSpan, cancellationToken) Then
                                Return True
                            End If
                        End If

                        replacementNode = qualifiedName.Right.WithLeadingTrivia(name.GetLeadingTrivia())
                        replacementNode = DirectCast(replacementNode, SimpleNameSyntax) _
                            .WithIdentifier(TryEscapeIdentifierToken(DirectCast(replacementNode, SimpleNameSyntax).Identifier))
                        issueSpan = qualifiedName.Left.Span

                        If CanReplaceWithReducedName(name, replacementNode, semanticModel, cancellationToken) Then
                            Return True
                        End If

                        If TryOmitModuleName(qualifiedName, semanticModel, replacementNode, issueSpan, cancellationToken) Then
                            Return True
                        End If

                    Case SyntaxKind.IdentifierName
                        Dim identifier = DirectCast(name, IdentifierNameSyntax).Identifier
                        TryReduceAttributeSuffix(name, identifier, semanticModel, False, replacementNode, issueSpan, cancellationToken)
                End Select
            End If

            If replacementNode Is Nothing Then
                Return False
            End If

            Return CanReplaceWithReducedName(name, replacementNode, semanticModel, cancellationToken)
        End Function

        ''' <summary>
        ''' Checks if the SyntaxNode is a name of a namespace declaration. To be a namespace name, the syntax
        ''' must be parented by an namespace declaration and the node itself must be equal to the declaration's Name
        ''' property.
        ''' </summary>
        Private Shared Function IsPartOfNamespaceDeclarationName(node As SyntaxNode) As Boolean

            Dim nextNode As SyntaxNode = node

            Do While nextNode IsNot Nothing

                Select Case nextNode.Kind

                    Case SyntaxKind.IdentifierName, SyntaxKind.QualifiedName
                        node = nextNode
                        nextNode = nextNode.Parent

                    Case SyntaxKind.NamespaceStatement
                        Dim namespaceStatement = DirectCast(nextNode, NamespaceStatementSyntax)
                        Return namespaceStatement.Name Is node

                    Case Else
                        Return False

                End Select

            Loop

            Return False
        End Function

        Private Shared Function CanReplaceWithReducedName(
            name As NameSyntax,
            replacementNode As ExpressionSyntax,
            semanticModel As SemanticModel,
            cancellationToken As CancellationToken
        ) As Boolean
            Dim speculationAnalyzer = New SpeculationAnalyzer(name, replacementNode, semanticModel, cancellationToken)
            If speculationAnalyzer.ReplacementChangesSemantics() Then
                Return False
            End If

            Return CanReplaceWithReducedNameInContext(name, replacementNode)
        End Function

        Private Shared Function CanReplaceWithReducedNameInContext(name As NameSyntax, replacementNode As ExpressionSyntax) As Boolean

            ' Special case.  if this new minimal name parses out to a predefined type, then we
            ' have to make sure that we're not in a using alias.   That's the one place where the
            ' language doesn't allow predefined types.  You have to use the fully qualified name
            ' instead.
            Dim invalidTransformation1 = IsNonNameSyntaxInImportsDirective(name, replacementNode)
            Dim invalidTransformation2 = IsReservedNameInAttribute(name, replacementNode)

            Dim invalidTransformation3 = IsNullableTypeSyntaxLeftOfDotInMemberAccess(name, replacementNode)

            Return Not (invalidTransformation1 OrElse invalidTransformation2 OrElse invalidTransformation3)
        End Function

        Private Shared Function IsNonNameSyntaxInImportsDirective(expression As ExpressionSyntax, simplifiedNode As ExpressionSyntax) As Boolean
            Return TypeOf expression.Parent Is ImportsClauseSyntax AndAlso
                Not TypeOf simplifiedNode Is NameSyntax
        End Function

        Private Shared Function IsNullableTypeSyntaxLeftOfDotInMemberAccess(expression As ExpressionSyntax, simplifiedNode As ExpressionSyntax) As Boolean
            Return expression.IsParentKind(SyntaxKind.SimpleMemberAccessExpression) AndAlso
                simplifiedNode.Kind = SyntaxKind.NullableType
        End Function

        Private Shared Function TryOmitModuleName(name As QualifiedNameSyntax, semanticModel As SemanticModel, <Out()> ByRef replacementNode As ExpressionSyntax, <Out()> ByRef issueSpan As TextSpan, cancellationToken As CancellationToken) As Boolean
            If name.IsParentKind(SyntaxKind.QualifiedName) Then
                Dim symbolForName = semanticModel.GetSymbolInfo(DirectCast(name.Parent, QualifiedNameSyntax), cancellationToken).Symbol

                ' in case this QN is used in a "New NSName.ModuleName.MemberName()" expression
                ' the returned symbol is a constructor. Then we need to get the containing type.
                If symbolForName.IsConstructor Then
                    symbolForName = symbolForName.ContainingType
                End If

                If symbolForName.IsModuleMember Then

                    replacementNode = name.Left.WithLeadingTrivia(name.GetLeadingTrivia())
                    issueSpan = name.Right.Span

                    Dim parent = DirectCast(name.Parent, QualifiedNameSyntax)
                    Dim parentReplacement = parent.ReplaceNode(parent.Left, replacementNode)

                    If CanReplaceWithReducedName(parent, parentReplacement, semanticModel, cancellationToken) Then
                        Return True
                    End If
                End If
            End If

            Return False
        End Function

        Private Shared Function TryReduceAttributeSuffix(
            name As NameSyntax,
            identifierToken As SyntaxToken,
            semanticModel As SemanticModel,
            isIdentifierNameFromAlias As Boolean,
            <Out> ByRef replacementNode As ExpressionSyntax,
            <Out> ByRef issueSpan As TextSpan,
            cancellationToken As CancellationToken
        ) As Boolean
            If SyntaxFacts.IsAttributeName(name) AndAlso Not isIdentifierNameFromAlias Then

                ' When the replacement is an Alias we don't want the "Attribute" Suffix to be removed because this will result in symbol change
                Dim aliasSymbol = semanticModel.GetAliasInfo(name, cancellationToken)
                If aliasSymbol IsNot Nothing AndAlso
                   String.Compare(aliasSymbol.Name, identifierToken.ValueText, StringComparison.OrdinalIgnoreCase) = 0 Then
                    Return False
                End If

                If name.Parent.Kind = SyntaxKind.Attribute OrElse name.IsRightSideOfDot() Then
                    Dim newIdentifierText = String.Empty

                    ' an attribute that should keep it (unnecessary "Attribute" suffix should be annotated with a DontSimplifyAnnotation
                    If identifierToken.HasAnnotation(SimplificationHelpers.DontSimplifyAnnotation) Then
                        newIdentifierText = identifierToken.ValueText + "Attribute"
                    ElseIf identifierToken.ValueText.TryReduceAttributeSuffix(newIdentifierText) Then
                        issueSpan = New TextSpan(name.Span.End - 9, 9)
                    Else
                        Return False
                    End If

                    ' escape it (VB allows escaping even for abbreviated identifiers, C# does not!)
                    Dim newIdentifierToken = identifierToken.CopyAnnotationsTo(
                                             SyntaxFactory.Identifier(
                                                 identifierToken.LeadingTrivia,
                                                 newIdentifierText,
                                                 identifierToken.TrailingTrivia))
                    newIdentifierToken = TryEscapeIdentifierToken(newIdentifierToken)
                    replacementNode = SyntaxFactory.IdentifierName(newIdentifierToken).WithLeadingTrivia(name.GetLeadingTrivia())
                    Return True
                End If
            End If

            Return False
        End Function

        Private Shared Function PreferPredefinedTypeKeywordInDeclarations(name As NameSyntax, options As VisualBasicSimplifierOptions) As Boolean
            Return (Not IsDirectChildOfMemberAccessExpression(name)) AndAlso
                   (Not InsideCrefReference(name)) AndAlso
                   (Not InsideNameOfExpression(name)) AndAlso
                   options.PreferPredefinedTypeKeywordInDeclaration.Value
        End Function

        Private Shared Function CanSimplifyNullable(type As INamedTypeSymbol, name As NameSyntax) As Boolean
            If Not type.IsNullable Then
                Return False
            End If

            If type.IsUnboundGenericType Then
                ' Don't simplify unbound generic type "Nullable(Of )".
                Return False
            End If

            If InsideNameOfExpression(name) Then
                ' Nullable(Of T) can't be simplified to T? in nameof expressions.
                Return False
            End If

            If Not InsideCrefReference(name) Then
                ' Nullable(Of T) can always be simplified to T? outside crefs.
                Return True
            End If

            ' Inside crefs, if the T in this Nullable(Of T) is being declared right here
            ' then this Nullable(Of T) is not a constructed generic type and we should
            ' not offer to simplify this to T?.
            '
            ' For example, we should not offer the simplification in the following cases where
            ' T does not bind to an existing type / type parameter in the user's code.
            ' - <see cref="Nullable(Of T)"/>
            ' - <see cref="System.Nullable(Of T).Value"/>
            '
            ' And we should offer the simplification in the following cases where SomeType and
            ' SomeMethod bind to a type and method declared elsewhere in the users code.
            ' - <see cref="SomeType.SomeMethod(Nullable(Of SomeType))"/>

            If name.IsKind(SyntaxKind.GenericName) Then
                If (name.IsParentKind(SyntaxKind.CrefReference)) OrElse ' cref="Nullable(Of T)"
                   (name.IsParentKind(SyntaxKind.QualifiedName) AndAlso name.Parent?.IsParentKind(SyntaxKind.CrefReference)) OrElse ' cref="System.Nullable(Of T)"
                   (name.IsParentKind(SyntaxKind.QualifiedName) AndAlso If(name.Parent?.IsParentKind(SyntaxKind.QualifiedName), False) AndAlso name.Parent.Parent?.IsParentKind(SyntaxKind.CrefReference)) Then  ' cref="System.Nullable(Of T).Value"
                    ' Unfortunately, unlike in corresponding C# case, we need syntax based checking to detect these cases because of bugs in the VB SemanticModel.
                    ' See https://github.com/dotnet/roslyn/issues/2196, https://github.com/dotnet/roslyn/issues/2197
                    Return False
                End If
            End If

            Dim argument = type.TypeArguments.SingleOrDefault()
            If argument Is Nothing OrElse argument.IsErrorType() Then
                Return False
            End If

            Dim argumentDecl = argument.DeclaringSyntaxReferences.FirstOrDefault()
            If argumentDecl Is Nothing Then
                ' The type argument is a type from metadata - so this is a constructed generic nullable type that can be simplified (e.g. Nullable(Of Integer)).
                Return True
            End If

            Return Not name.Span.Contains(argumentDecl.Span)
        End Function

    End Class
End Namespace
