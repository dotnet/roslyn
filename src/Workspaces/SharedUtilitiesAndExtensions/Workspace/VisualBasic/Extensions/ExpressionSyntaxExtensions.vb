' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module ExpressionSyntaxExtensions

        Public ReadOnly typeNameFormatWithGenerics As New SymbolDisplayFormat(
                globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Included,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions:=SymbolDisplayMemberOptions.IncludeContainingType,
                localOptions:=SymbolDisplayLocalOptions.IncludeType,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers,
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces)

        Public ReadOnly typeNameFormatWithoutGenerics As New SymbolDisplayFormat(
                globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Included,
                genericsOptions:=SymbolDisplayGenericsOptions.None,
                memberOptions:=SymbolDisplayMemberOptions.IncludeContainingType,
                localOptions:=SymbolDisplayLocalOptions.IncludeType,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers,
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces)

        <Extension()>
        Public Function Parenthesize(expression As ExpressionSyntax, Optional addSimplifierAnnotation As Boolean = True) As ParenthesizedExpressionSyntax
            Dim result = SyntaxFactory.ParenthesizedExpression(expression.WithoutTrivia()) _
                                      .WithTriviaFrom(expression)
            Return If(addSimplifierAnnotation,
                      result.WithAdditionalAnnotations(Simplifier.Annotation),
                      result)
        End Function

        <Extension()>
        Public Function TryGetNameParts(expression As ExpressionSyntax, ByRef parts As IList(Of String)) As Boolean
            Dim partsList = New List(Of String)
            If Not expression.TryGetNameParts(partsList) Then
                parts = Nothing
                Return False
            End If

            parts = partsList
            Return True
        End Function

        <Extension()>
        Public Function TryGetNameParts(expression As ExpressionSyntax, parts As List(Of String)) As Boolean
            If expression.IsKind(SyntaxKind.SimpleMemberAccessExpression) Then
                Dim memberAccess = DirectCast(expression, MemberAccessExpressionSyntax)
                If Not memberAccess.Name.TryGetNameParts(parts) Then
                    Return False
                End If

                Return AddSimpleName(memberAccess.Name, parts)
            ElseIf expression.IsKind(SyntaxKind.QualifiedName) Then
                Dim qualifiedName = DirectCast(expression, QualifiedNameSyntax)
                If Not qualifiedName.Left.TryGetNameParts(parts) Then
                    Return False
                End If

                Return AddSimpleName(qualifiedName.Right, parts)
            ElseIf TypeOf expression Is SimpleNameSyntax Then
                Return AddSimpleName(DirectCast(expression, SimpleNameSyntax), parts)
            Else
                Return False
            End If
        End Function

        Private Function AddSimpleName(simpleName As SimpleNameSyntax, parts As List(Of String)) As Boolean
            If Not simpleName.IsKind(SyntaxKind.IdentifierName) Then
                Return False
            End If

            parts.Add(simpleName.Identifier.ValueText)
            Return True
        End Function

        <Extension>
        Public Function Cast(
            expression As ExpressionSyntax,
            targetType As ITypeSymbol,
            <Out> ByRef isResultPredefinedCast As Boolean) As ExpressionSyntax

            ' Parenthesize the expression, except for collection initializers and interpolated strings,
            ' where parenthesizing changes semantics.
            Dim newExpression = expression

            If Not expression.IsKind(SyntaxKind.CollectionInitializer, SyntaxKind.InterpolatedStringExpression) Then
                newExpression = expression.Parenthesize()
            End If

            Dim leadingTrivia = newExpression.GetLeadingTrivia()
            Dim trailingTrivia = newExpression.GetTrailingTrivia()

            Dim stripped = newExpression.WithoutLeadingTrivia().WithoutTrailingTrivia()

            Dim castKeyword = targetType.SpecialType.GetPredefinedCastKeyword()
            If castKeyword = SyntaxKind.None Then
                isResultPredefinedCast = False
                Return SyntaxFactory.CTypeExpression(
                    expression:=stripped,
                    type:=targetType.GenerateTypeSyntax()) _
                .WithLeadingTrivia(leadingTrivia) _
                .WithTrailingTrivia(trailingTrivia) _
                .WithAdditionalAnnotations(Simplifier.Annotation)
            Else
                isResultPredefinedCast = True
                Return SyntaxFactory.PredefinedCastExpression(
                    keyword:=SyntaxFactory.Token(castKeyword),
                    expression:=stripped) _
                .WithLeadingTrivia(leadingTrivia) _
                .WithTrailingTrivia(trailingTrivia) _
                .WithAdditionalAnnotations(Simplifier.Annotation)
            End If
        End Function

        <Extension()>
        Public Function CastIfPossible(
            expression As ExpressionSyntax,
            targetType As ITypeSymbol,
            position As Integer,
            semanticModel As SemanticModel,
            <Out> ByRef wasCastAdded As Boolean,
            cancellationToken As CancellationToken) As ExpressionSyntax

            wasCastAdded = False

            If targetType.ContainsAnonymousType() OrElse expression.IsParentKind(SyntaxKind.AsNewClause) Then
                Return expression
            End If

            Dim typeSyntax = targetType.GenerateTypeSyntax()
            Dim type = semanticModel.GetSpeculativeTypeInfo(
                position,
                typeSyntax,
                SpeculativeBindingOption.BindAsTypeOrNamespace).Type

            If Not targetType.Equals(type) Then
                Return expression
            End If

            Dim isResultPredefinedCast As Boolean = False
            Dim castExpression = expression.Cast(targetType, isResultPredefinedCast)

            ' Ensure that inserting the cast doesn't change the semantics.
            Dim specAnalyzer = New SpeculationAnalyzer(expression, castExpression, semanticModel, cancellationToken)
            Dim speculativeSemanticModel = specAnalyzer.SpeculativeSemanticModel
            If speculativeSemanticModel Is Nothing Then
                Return expression
            End If

            Dim speculatedCastExpression = specAnalyzer.ReplacedExpression
            Dim speculatedCastInnerExpression = If(isResultPredefinedCast,
                                                   DirectCast(speculatedCastExpression, PredefinedCastExpressionSyntax).Expression,
                                                   DirectCast(speculatedCastExpression, CastExpressionSyntax).Expression)
            If Not CastAnalyzer.IsUnnecessary(speculatedCastExpression, speculatedCastInnerExpression, speculativeSemanticModel, True, cancellationToken) Then
                Return expression
            End If

            wasCastAdded = True
            Return castExpression
        End Function

        <Extension()>
        Public Function IsObjectCreationWithoutArgumentList(expression As ExpressionSyntax) As Boolean
            Return _
                TypeOf expression Is ObjectCreationExpressionSyntax AndAlso
                DirectCast(expression, ObjectCreationExpressionSyntax).ArgumentList Is Nothing
        End Function

        <Extension()>
        Public Function IsMeMyBaseOrMyClass(expression As ExpressionSyntax) As Boolean
            If expression Is Nothing Then
                Return False
            End If

            Return expression.Kind = SyntaxKind.MeExpression OrElse
                   expression.Kind = SyntaxKind.MyBaseExpression OrElse
                   expression.Kind = SyntaxKind.MyClassExpression
        End Function

        <Extension()>
        Public Function IsFirstStatementInCtor(expression As ExpressionSyntax) As Boolean
            Dim statement = expression.FirstAncestorOrSelf(Of StatementSyntax)()
            If statement Is Nothing Then
                Return False
            End If

            If Not statement.IsParentKind(SyntaxKind.ConstructorBlock) Then
                Return False
            End If

            Return DirectCast(statement.Parent, ConstructorBlockSyntax).Statements(0) Is statement
        End Function

        <Extension()>
        Public Function IsNamedArgumentIdentifier(expression As ExpressionSyntax) As Boolean
            Dim simpleArgument = TryCast(expression.Parent, SimpleArgumentSyntax)
            Return simpleArgument IsNot Nothing AndAlso simpleArgument.NameColonEquals.Name Is expression
        End Function

        <Extension>
        Public Function ContainsImplicitMemberAccess(expression As ExpressionSyntax) As Boolean
            Return ContainsImplicitMemberAccessWorker(expression)
        End Function

        <Extension>
        Public Function ContainsImplicitMemberAccess(statement As StatementSyntax) As Boolean
            Return ContainsImplicitMemberAccessWorker(statement)
        End Function

        <Extension>
        Public Function GetImplicitMemberAccessExpressions(expression As SyntaxNode, span As TextSpan) As IEnumerable(Of ExpressionSyntax)
            ' We don't want to allow a variable to be introduced if the expression contains an
            ' implicit member access.  i.e. ".Blah.ToString()" as that .Blah refers to the containing
            ' object creation or anonymous type and we can't make a local for it.  So we get all the
            ' descendants and we suppress ourselves. 

            ' Note: if we hit a with block or an anonymous type, then we do not look deeper.  Any
            ' implicit member accesses will refer to that thing and we *can* introduce a variable
            Dim descendentExpressions = expression.DescendantNodesAndSelf().OfType(Of ExpressionSyntax).Where(Function(e) span.Contains(e.Span)).ToSet()

            Return descendentExpressions.OfType(Of MemberAccessExpressionSyntax).
                                         Select(Function(m) m.GetExpressionOfMemberAccessExpression(allowImplicitTarget:=True)).
                                         Where(Function(e) Not descendentExpressions.Contains(e))
        End Function

        <Extension>
        Public Function GetImplicitMemberAccessExpressions(expression As SyntaxNode) As IEnumerable(Of ExpressionSyntax)
            Return GetImplicitMemberAccessExpressions(expression, expression.FullSpan)
        End Function

        Private Function ContainsImplicitMemberAccessWorker(expression As SyntaxNode) As Boolean
            Return GetImplicitMemberAccessExpressions(expression).Any()
        End Function

        <Extension>
        Public Function InsideCrefReference(expression As ExpressionSyntax) As Boolean
            Dim crefAttribute = expression.FirstAncestorOrSelf(Of XmlCrefAttributeSyntax)()
            Return crefAttribute IsNot Nothing
        End Function

        <Extension>
        Public Function IsDirectChildOfMemberAccessExpression(expression As ExpressionSyntax) As Boolean
            Return TypeOf expression?.Parent Is MemberAccessExpressionSyntax
        End Function

        <Extension>
        Public Function GetRightmostName(node As ExpressionSyntax) As SimpleNameSyntax
            Dim memberAccess = TryCast(node, MemberAccessExpressionSyntax)
            If memberAccess IsNot Nothing AndAlso memberAccess.Name IsNot Nothing Then
                Return memberAccess.Name
            End If

            Dim qualified = TryCast(node, QualifiedNameSyntax)
            If qualified IsNot Nothing AndAlso qualified.Right IsNot Nothing Then
                Return qualified.Right
            End If

            Return TryCast(node, SimpleNameSyntax)
        End Function

        <Extension>
        Public Function IsNameOfArgumentExpression(expression As ExpressionSyntax) As Boolean
            Return expression.IsParentKind(SyntaxKind.NameOfExpression)
        End Function

        Public Function IsReservedNameInAttribute(originalName As NameSyntax, simplifiedNode As ExpressionSyntax) As Boolean
            Dim attribute = originalName.GetAncestorOrThis(Of AttributeSyntax)()
            If attribute Is Nothing Then
                Return False
            End If

            Dim identifier As SimpleNameSyntax
            If simplifiedNode.Kind = SyntaxKind.IdentifierName Then
                identifier = DirectCast(simplifiedNode, SimpleNameSyntax)
            ElseIf simplifiedNode.Kind = SyntaxKind.QualifiedName Then
                identifier = DirectCast(DirectCast(simplifiedNode, QualifiedNameSyntax).Left, SimpleNameSyntax)
            Else
                Return False
            End If

            If identifier.Identifier.IsBracketed Then
                Return False
            End If

            If attribute.Target Is Nothing Then
                Dim identifierValue = SyntaxFacts.MakeHalfWidthIdentifier(identifier.Identifier.ValueText)

                If CaseInsensitiveComparison.Equals(identifierValue, "Assembly") OrElse
                   CaseInsensitiveComparison.Equals(identifierValue, "Module") Then
                    Return True
                End If
            End If

            Return False
        End Function
    End Module
End Namespace
