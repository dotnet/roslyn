' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification.Simplifiers
    Friend Class ExpressionSimplifier
        Inherits AbstractVisualBasicSimplifier(Of ExpressionSyntax, ExpressionSyntax)

        Public Shared ReadOnly Instance As New ExpressionSimplifier()

        Private Sub New()
        End Sub

        Public Overrides Function TrySimplify(expression As ExpressionSyntax,
                                              semanticModel As SemanticModel,
                                              options As VisualBasicSimplifierOptions,
                                              ByRef replacementNode As ExpressionSyntax,
                                              ByRef issueSpan As TextSpan,
                                              cancellationToken As CancellationToken) As Boolean

            Dim memberAccessExpression = TryCast(expression, MemberAccessExpressionSyntax)
            If memberAccessExpression?.Expression?.Kind() = SyntaxKind.MeExpression Then
                If Not MemberAccessExpressionSimplifier.Instance.ShouldSimplifyThisMemberAccessExpression(
                    memberAccessExpression, semanticModel, options, thisExpression:=Nothing, severity:=Nothing, cancellationToken) Then
                    Return False
                End If

                replacementNode = memberAccessExpression.GetNameWithTriviaMoved()
                issueSpan = memberAccessExpression.Expression.Span
                Return True
            End If

            If TryReduceExplicitName(expression, semanticModel, replacementNode, issueSpan, options, cancellationToken) Then
                Return True
            End If

            Return TrySimplify(expression, semanticModel, replacementNode, issueSpan)
        End Function

        Private Shared Function TryReduceExplicitName(
            expression As ExpressionSyntax,
            semanticModel As SemanticModel,
            <Out> ByRef replacementNode As ExpressionSyntax,
            <Out> ByRef issueSpan As TextSpan,
            options As VisualBasicSimplifierOptions,
            cancellationToken As CancellationToken
        ) As Boolean
            replacementNode = Nothing
            issueSpan = Nothing

            If expression.Kind = SyntaxKind.SimpleMemberAccessExpression Then
                Dim memberAccess = DirectCast(expression, MemberAccessExpressionSyntax)
                Return TryReduce(
                    memberAccess, semanticModel, replacementNode, issueSpan, options, cancellationToken)
            ElseIf TypeOf expression Is NameSyntax Then
                Dim name = DirectCast(expression, NameSyntax)
                Return NameSimplifier.Instance.TrySimplify(
                    name, semanticModel, options, replacementNode, issueSpan, cancellationToken)
            End If

            Return False
        End Function

        Private Shared Function TryReduce(
            memberAccess As MemberAccessExpressionSyntax,
            semanticModel As SemanticModel,
            <Out> ByRef replacementNode As ExpressionSyntax,
            <Out> ByRef issueSpan As TextSpan,
            options As VisualBasicSimplifierOptions,
            cancellationToken As CancellationToken
        ) As Boolean
            If memberAccess.Expression Is Nothing OrElse memberAccess.Name Is Nothing Then
                Return False
            End If

            If memberAccess.HasAnnotations(SpecialTypeAnnotation.Kind) Then
                replacementNode = SyntaxFactory.PredefinedType(
                    SyntaxFactory.Token(
                        GetPredefinedKeywordKind(SpecialTypeAnnotation.GetSpecialType(memberAccess.GetAnnotations(SpecialTypeAnnotation.Kind).First())))) _
                            .WithLeadingTrivia(memberAccess.GetLeadingTrivia())

                issueSpan = memberAccess.Span
                Return True
            End If

            ' See https//github.com/dotnet/roslyn/issues/40974
            '
            ' To be very safe, we only support simplifying code that bound to a symbol without any
            ' sort of problems.  We could potentially relax this in the future.  However, we would
            ' need to be very careful about the implications of us offering to fixup 'broken' code 
            ' in a manner that might end up making things worse Or confusing the user.
            Dim symbol = SimplificationHelpers.GetOriginalSymbolInfo(semanticModel, memberAccess)
            If symbol Is Nothing Then
                Return False
            End If

            If Not memberAccess.IsRightSideOfDot() Then
                Dim aliasReplacement As IAliasSymbol = Nothing

                If TryReplaceWithAlias(memberAccess, semanticModel, aliasReplacement) Then
                    Dim identifierToken = SyntaxFactory.Identifier(
                                memberAccess.GetLeadingTrivia(),
                                aliasReplacement.Name,
                                memberAccess.GetTrailingTrivia())

                    identifierToken = TryEscapeIdentifierToken(identifierToken)
                    replacementNode = SyntaxFactory.IdentifierName(identifierToken)

                    issueSpan = memberAccess.Span

                    ' In case the alias name is the same as the last name of the alias target, we only include 
                    ' the left part of the name in the unnecessary span to Not confuse uses.
                    If memberAccess.Name.Identifier.ValueText = identifierToken.ValueText Then
                        issueSpan = memberAccess.Expression.Span
                    End If

                    Return True
                End If

                If PreferPredefinedTypeKeywordInMemberAccess(memberAccess, options) Then
                    If (symbol IsNot Nothing AndAlso symbol.IsKind(SymbolKind.NamedType)) Then
                        Dim keywordKind = GetPredefinedKeywordKind(DirectCast(symbol, INamedTypeSymbol).SpecialType)
                        If keywordKind <> SyntaxKind.None Then
                            replacementNode = SyntaxFactory.PredefinedType(
                                                SyntaxFactory.Token(
                                                    memberAccess.GetLeadingTrivia(),
                                                    keywordKind,
                                                    memberAccess.GetTrailingTrivia()))

                            replacementNode = replacementNode.WithAdditionalAnnotations(
                                    New SyntaxAnnotation(NameOf(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess)))

                            issueSpan = memberAccess.Span
                            Return True
                        End If
                    End If
                End If
            End If

            ' a module name was inserted by the name expansion, so removing this should be tried first.
            If memberAccess.HasAnnotation(SimplificationHelpers.SimplifyModuleNameAnnotation) Then
                If TryOmitModuleName(memberAccess, semanticModel, symbol, replacementNode, issueSpan, cancellationToken) Then
                    Return True
                End If
            End If

            replacementNode = memberAccess.GetNameWithTriviaMoved()
            issueSpan = memberAccess.Expression.Span

            If CanReplaceWithReducedName(memberAccess, replacementNode, semanticModel, symbol, cancellationToken) Then
                Return True
            End If

            If TryOmitModuleName(memberAccess, semanticModel, symbol, replacementNode, issueSpan, cancellationToken) Then
                Return True
            End If

            Return False
        End Function

        Private Overloads Shared Function TrySimplify(
            expression As ExpressionSyntax,
            semanticModel As SemanticModel,
            <Out> ByRef replacementNode As ExpressionSyntax,
            <Out> ByRef issueSpan As TextSpan
        ) As Boolean
            replacementNode = Nothing
            issueSpan = Nothing

            Select Case expression.Kind
                Case SyntaxKind.SimpleMemberAccessExpression
                    If True Then
                        Dim memberAccess = DirectCast(expression, MemberAccessExpressionSyntax)
                        Dim newLeft As ExpressionSyntax = Nothing
                        If TrySimplifyMemberAccessOrQualifiedName(memberAccess.Expression, memberAccess.Name, semanticModel, newLeft, issueSpan) Then
                            ' replacement node might not be in it's simplest form, so add simplify annotation to it.
                            replacementNode = memberAccess.Update(memberAccess.Kind, newLeft, memberAccess.OperatorToken, memberAccess.Name).WithAdditionalAnnotations(Simplifier.Annotation)

                            ' Ensure that replacement doesn't change semantics.
                            Return Not ReplacementChangesSemantics(memberAccess, replacementNode, semanticModel)
                        End If

                        Return False
                    End If

                Case SyntaxKind.QualifiedName
                    If True Then
                        Dim qualifiedName = DirectCast(expression, QualifiedNameSyntax)
                        Dim newLeft As ExpressionSyntax = Nothing
                        If TrySimplifyMemberAccessOrQualifiedName(qualifiedName.Left, qualifiedName.Right, semanticModel, newLeft, issueSpan) Then
                            If Not TypeOf newLeft Is NameSyntax Then
                                Contract.Fail("QualifiedName Left = " + qualifiedName.Left.ToString() + " and QualifiedName Right = " + qualifiedName.Right.ToString() + " . Left is tried to be replaced with the PredefinedType " + replacementNode.ToString())
                            End If

                            ' replacement node might not be in it's simplest form, so add simplify annotation to it.
                            replacementNode = qualifiedName.Update(DirectCast(newLeft, NameSyntax), qualifiedName.DotToken, qualifiedName.Right).WithAdditionalAnnotations(Simplifier.Annotation)

                            ' Ensure that replacement doesn't change semantics.
                            Return Not ReplacementChangesSemantics(qualifiedName, replacementNode, semanticModel)
                        End If

                        Return False
                    End If
            End Select

            Return False
        End Function

        Private Shared Function ReplacementChangesSemantics(originalExpression As ExpressionSyntax, replacedExpression As ExpressionSyntax, semanticModel As SemanticModel) As Boolean
            Dim speculationAnalyzer = New SpeculationAnalyzer(originalExpression, replacedExpression, semanticModel, CancellationToken.None)
            Return speculationAnalyzer.ReplacementChangesSemantics()
        End Function

        ' Note: The caller needs to verify that replacement doesn't change semantics of the original expression.
        Private Shared Function TrySimplifyMemberAccessOrQualifiedName(
            left As ExpressionSyntax,
            right As ExpressionSyntax,
            semanticModel As SemanticModel,
            <Out()> ByRef replacementNode As ExpressionSyntax,
            <Out()> ByRef issueSpan As TextSpan
        ) As Boolean
            replacementNode = Nothing
            issueSpan = Nothing

            If left IsNot Nothing AndAlso right IsNot Nothing Then
                Dim leftSymbol = SimplificationHelpers.GetOriginalSymbolInfo(semanticModel, left)
                If leftSymbol IsNot Nothing AndAlso leftSymbol.Kind = SymbolKind.NamedType Then
                    Dim rightSymbol = SimplificationHelpers.GetOriginalSymbolInfo(semanticModel, right)
                    If rightSymbol IsNot Nothing AndAlso (rightSymbol.IsStatic OrElse rightSymbol.Kind = SymbolKind.NamedType) Then
                        ' Static member access or nested type member access.
                        Dim containingType As INamedTypeSymbol = rightSymbol.ContainingType
                        Dim isInCref = left.Ancestors(ascendOutOfTrivia:=True).OfType(Of CrefReferenceSyntax)().Any()

                        ' Crefs in VB , irrespective of the expression are parsed as QualifiedName (no MemberAccessExpression)
                        ' Hence the Left can never be a PredefinedType (or anything other than NameSyntax)
                        If isInCref AndAlso TypeOf rightSymbol Is IMethodSymbol AndAlso Not containingType.SpecialType = SpecialType.None Then
                            Return False
                        End If

                        If containingType IsNot Nothing AndAlso Not containingType.Equals(leftSymbol) Then

                            Dim namedType = TryCast(leftSymbol, INamedTypeSymbol)
                            If namedType IsNot Nothing AndAlso
                               containingType.TypeArguments.Length <> 0 Then
                                Return False
                            End If

                            ' We have a static member access or a nested type member access using a more derived type.
                            ' Simplify syntax so as to use accessed member's most immediate containing type instead of the derived type.
                            replacementNode = containingType.GenerateTypeSyntax().WithLeadingTrivia(left.GetLeadingTrivia()).WithTrailingTrivia(left.GetTrailingTrivia()).WithAdditionalAnnotations(Simplifier.Annotation)
                            issueSpan = left.Span
                            Return True
                        End If
                    End If
                End If
            End If

            Return False
        End Function

        Private Shared Function TryOmitModuleName(memberAccess As MemberAccessExpressionSyntax,
                                           semanticModel As SemanticModel,
                                           symbol As ISymbol,
                                           <Out> ByRef replacementNode As ExpressionSyntax,
                                           <Out> ByRef issueSpan As TextSpan,
                                           cancellationToken As CancellationToken) As Boolean
            If memberAccess.IsParentKind(SyntaxKind.SimpleMemberAccessExpression) Then
                Dim symbolForMemberAccess = semanticModel.GetSymbolInfo(DirectCast(memberAccess.Parent, MemberAccessExpressionSyntax), cancellationToken).Symbol
                If symbolForMemberAccess.IsModuleMember Then
                    replacementNode = memberAccess.Expression.WithLeadingTrivia(memberAccess.GetLeadingTrivia())
                    issueSpan = memberAccess.Name.Span

                    Dim parent = DirectCast(memberAccess.Parent, MemberAccessExpressionSyntax)
                    Dim parentReplacement = parent.ReplaceNode(parent.Expression, replacementNode)

                    If CanReplaceWithReducedName(parent, parentReplacement, semanticModel, symbol, cancellationToken) Then
                        Return True
                    End If
                End If
            End If

            Return False
        End Function

        Private Shared Function CanReplaceWithReducedName(
                memberAccess As MemberAccessExpressionSyntax,
                reducedNode As ExpressionSyntax,
                semanticModel As SemanticModel,
                symbol As ISymbol,
                cancellationToken As CancellationToken) As Boolean
            If Not SimplificationHelpers.IsNamespaceOrTypeOrThisParameter(memberAccess.Expression, semanticModel) Then
                Return False
            End If

            ' See if we can simplify a member access expression of the form E.M or E.M() to M or M()
            Dim speculationAnalyzer = New SpeculationAnalyzer(memberAccess, reducedNode, semanticModel, cancellationToken)
            If Not speculationAnalyzer.SymbolsForOriginalAndReplacedNodesAreCompatible() OrElse
                speculationAnalyzer.ReplacementChangesSemantics() Then
                Return False
            End If

            If memberAccess.Expression.IsKind(SyntaxKind.MyBaseExpression) Then
                Dim enclosingNamedType = semanticModel.GetEnclosingNamedType(memberAccess.SpanStart, cancellationToken)
                If enclosingNamedType IsNot Nothing AndAlso
                    Not enclosingNamedType.IsSealed AndAlso
                    symbol IsNot Nothing AndAlso
                    symbol.IsOverridable() Then
                    Return False
                End If
            End If

            Return True
        End Function
    End Class
End Namespace
