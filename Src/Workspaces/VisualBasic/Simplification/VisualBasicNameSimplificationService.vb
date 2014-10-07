Imports System.Threading
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Services.Shared.Collections
Imports Roslyn.Services.VisualBasic.SemanticTransformation
Imports Roslyn.Services.Simplification

Namespace Roslyn.Services.VisualBasic.Simplification
    Partial Friend Class VisualBasicNameSimplificationService
        Inherits AbstractNameSimplificationService

        Public Overrides Function SimplifyNames(document As Document,
                                                spans As IEnumerable(Of TextSpan),
                                                cancellationToken As CancellationToken) As SimplificationResult
            Dim semanticModel = document.GetVisualBasicSemanticModel(cancellationToken)
            Dim rewriter = New Rewriter(semanticModel, SimpleIntervalTree.Create(TextSpanIntervalIntrospector.Instance, spans), cancellationToken)
            Dim newRoot = rewriter.Visit(semanticModel.SyntaxTree.GetRoot(cancellationToken))

            Return New SimplificationResult(document.WithSyntaxRoot(newRoot), rewriter.SimplifiedNodes)
        End Function

        Private Overloads Shared Function SimplifyNode(semanticModel As ISemanticModel, node As SyntaxNode) As SyntaxNode
            Dim result = SimplifyNodeWorker(semanticModel, node)
            If result IsNot node Then
                result = node.CopyAnnotationsTo(result)
            End If

            Return result
        End Function

        Private Overloads Shared Function SimplifyNodeWorker(semanticModel As ISemanticModel, node As SyntaxNode) As SyntaxNode
            If TypeOf node Is NameSyntax Then
                Dim name = DirectCast(node, NameSyntax)
                If Not name.IsLeftSideOfQualifiedName() AndAlso Not name.IsRightSideOfDot() Then
                    Return SimplifyExpression(semanticModel, name)
                End If

                If name.IsLeftSideOfDot() AndAlso
                   name.IsParentKind(SyntaxKind.QualifiedName) AndAlso
                   name.Parent.IsParentKind(SyntaxKind.ImplementsClause) Then
                    Return SimplifyExpression(semanticModel, name)
                End If
            ElseIf TypeOf node Is MemberAccessExpressionSyntax Then
                Dim memberAccessExpression = DirectCast(node, MemberAccessExpressionSyntax)
                Dim result = SimplifyMemberAccess(semanticModel, memberAccessExpression)

                Return result
            End If

            Return node
        End Function

        Private Overloads Shared Function SimplifyMemberAccess(semanticModel As ISemanticModel, memberAccessExpression As MemberAccessExpressionSyntax) As ExpressionSyntax
            If Not IsQualifiedName(memberAccessExpression) Then
                Return memberAccessExpression
            End If

            Dim result = SimplifyExpression(semanticModel, DirectCast(memberAccessExpression, ExpressionSyntax))

            ' Oddest special case.  If we have (System.String).Equals("", "") then we can't simplify
            ' "System.String" to "String" as (String).Equals("", "") is illegal.  Weirdness.
            Dim invalid1 = IsParenthesizedPredefinedType(memberAccessExpression, result)

            If invalid1 Then
                Return memberAccessExpression
            End If

            If result IsNot memberAccessExpression Then
                Return result
            End If

            If Not IsMeOrNamedType(memberAccessExpression.Expression, semanticModel) Then
                Return memberAccessExpression
            End If

            ' See if we can simplify a member access expression of the form E.M or E.M() to M or M()

            Dim memberAccessAndOptionalInvocation As ExpressionSyntax = memberAccessExpression
            Dim nameAndOptionalInvocation As ExpressionSyntax = memberAccessExpression.Name

            ' is this an array access?
            ' in that case we can't get a symbol from the invocation expression and need to fall back to the member access expression
            Dim typeInfo = semanticModel.GetTypeInfo(memberAccessExpression)
            If typeInfo.Type IsNot Nothing AndAlso
                typeInfo.Type.IsArrayType Then

                memberAccessAndOptionalInvocation = memberAccessExpression
                nameAndOptionalInvocation = memberAccessAndOptionalInvocation.ReplaceNode(
                        memberAccessExpression, nameAndOptionalInvocation)
            Else
                If memberAccessExpression.IsParentKind(SyntaxKind.InvocationExpression) Then
                    memberAccessAndOptionalInvocation = DirectCast(memberAccessExpression.Parent, InvocationExpressionSyntax)
                    nameAndOptionalInvocation = memberAccessAndOptionalInvocation.ReplaceNode(
                        memberAccessExpression, nameAndOptionalInvocation)
                End If
            End If

            Dim originalSymbolInfo = semanticModel.GetSymbolInfo(memberAccessAndOptionalInvocation)
            Dim symbolInfo = semanticModel.GetSpeculativeSymbolInfo(memberAccessExpression.Span.Start, nameAndOptionalInvocation, SpeculativeBindingOption.BindAsExpression)

            If Not IsValidSymbolInfo(originalSymbolInfo.Symbol) OrElse
               Not IsValidSymbolInfo(symbolInfo.Symbol) OrElse
               Not Object.Equals(originalSymbolInfo.Symbol, symbolInfo.Symbol) Then
                Return memberAccessExpression
            End If

            Dim name = memberAccessExpression.Name
            Dim escapedIdentifierToken = VisualBasicSemanticTransformationService.TryEscapeIdentifier(name.Identifier)

            If name.Kind = SyntaxKind.IdentifierName Then
                Return DirectCast(memberAccessExpression.Name, IdentifierNameSyntax).
                    WithIdentifier(escapedIdentifierToken).
                        WithLeadingTrivia(memberAccessExpression.GetLeadingTrivia())
            Else
                Debug.Assert(name.Kind = SyntaxKind.GenericName)

                Return DirectCast(memberAccessExpression.Name, GenericNameSyntax).
                    WithIdentifier(escapedIdentifierToken).
                        WithLeadingTrivia(memberAccessExpression.GetLeadingTrivia())
            End If

        End Function

        Private Shared Function IsParenthesizedPredefinedType(memberAccessExpression As MemberAccessExpressionSyntax,
                                                              result As ExpressionSyntax) As Boolean
            Return result.IsKind(SyntaxKind.PredefinedType) AndAlso memberAccessExpression.IsParentKind(SyntaxKind.ParenthesizedExpression)
        End Function

        Private Shared Function IsMeOrNamedType(expression As ExpressionSyntax, semanticModel As ISemanticModel) As Boolean
            If expression.Kind = SyntaxKind.MeExpression Then
                Return True
            End If

            Dim expressionInfo = semanticModel.GetSymbolInfo(expression)
            If IsValidSymbolInfo(expressionInfo.Symbol) Then
                If TypeOf expressionInfo.Symbol Is INamedTypeSymbol Then
                    Return True
                End If
            End If

            Return False
        End Function

        Private Overloads Shared Function SimplifyExpression(semanticModel As ISemanticModel, expression As ExpressionSyntax) As ExpressionSyntax
            Dim simplifiedNode = SimplifyExpressionWorker(semanticModel, expression)
            ' Special case.  if this new minimal name parses out to a predefined type, then we
            ' have to make sure that we're not in a using alias.   That's the one place where the
            ' language doesn't allow predefined types.  You have to use the fully qualified name
            ' instead.
            Dim invalidTransformation1 = IsNonNameSyntaxInImportsDirective(expression, simplifiedNode)
            Dim invalidTransformation2 = IsReservedNameInAttribute(expression, simplifiedNode)

            Return If(invalidTransformation1 OrElse invalidTransformation2, expression, simplifiedNode)
        End Function

        Private Shared Function IsReservedNameInAttribute(expression As ExpressionSyntax, simplifiedNode As ExpressionSyntax) As Boolean
            If expression.IsParentKind(SyntaxKind.Attribute) AndAlso simplifiedNode.IsKind(SyntaxKind.IdentifierName) Then
                Dim attribute = DirectCast(expression.Parent, AttributeSyntax)
                Dim identifier = DirectCast(simplifiedNode, IdentifierNameSyntax)

                If attribute.Target Is Nothing Then
                    Dim identifierValue = identifier.Identifier.ValueText
                    If "Assembly".Equals(identifierValue, StringComparison.OrdinalIgnoreCase) OrElse
                       "Module".Equals(identifierValue, StringComparison.OrdinalIgnoreCase) Then
                        Return True
                    End If
                End If
            End If

            Return False
        End Function

        Private Shared Function IsNonNameSyntaxInImportsDirective(expression As ExpressionSyntax, simplifiedNode As ExpressionSyntax) As Boolean
            Return TypeOf expression.Parent Is ImportsClauseSyntax AndAlso
                Not TypeOf simplifiedNode Is NameSyntax
        End Function

        Private Shared Function SimplifyExpressionWorker(semanticModel As ISemanticModel, expression As ExpressionSyntax) As ExpressionSyntax
            ' 1. see whether binding the name binds to a symbol/type. if not, it is ambiguous and
            '    nothing we can do here.
            Dim symbol = GetOriginalSymbolInfo(semanticModel, expression)
            If IsValidSymbolInfo(symbol) Then
                Dim method = TryCast(symbol, MethodSymbol)
                If method IsNot Nothing AndAlso method.MethodKind = MethodKind.Constructor Then
                    symbol = method.ContainingType
                End If

                Dim namedType = TryCast(symbol, NamedTypeSymbol)
                If namedType IsNot Nothing Then
                    Return SimplifyNamedType(semanticModel, expression, namedType)
                End If
            End If

            Return expression
        End Function

        Private Shared Function SimplifyNamedType(semanticModel As ISemanticModel, expression As ExpressionSyntax, namedType As NamedTypeSymbol) As ExpressionSyntax
            Dim format = If(SyntaxFacts.IsAttributeName(expression),
                             TypeNameWithoutAttributeSuffixFormat,
                             TypeNameFormat)

            Dim simplifiedNameParts = namedType.ToMinimalDisplayParts(
                expression.GetLocation(),
                semanticModel, format)

            Dim expressionTokens = expression.DescendantTokens()
            If GetNonWhitespaceParts(simplifiedNameParts).Count >= expressionTokens.Count Then
                ' No point simplifying if it didn't decrease the token count.
                Return expression
            End If

            Dim typeNode = Syntax.ParseTypeName(simplifiedNameParts.ToDisplayString())
            If IsMemberAccessOffOfNullable(expression, typeNode) Then
                Return expression
            End If

            Dim node = If(TypeOf expression Is MemberAccessExpressionSyntax,
                          Syntax.ParseExpression(simplifiedNameParts.ToDisplayString()),
                          typeNode)
            Return node.WithLeadingTrivia(expression.GetLeadingTrivia()).WithTrailingTrivia(expression.GetTrailingTrivia())
        End Function

        Private Shared Function IsMemberAccessOffOfNullable(expression As ExpressionSyntax, simplifiedNode As TypeSyntax) As Boolean
            Return expression.IsParentKind(SyntaxKind.MemberAccessExpression) AndAlso simplifiedNode.IsKind(SyntaxKind.NullableType)
        End Function

        Private Shared Function IsQualifiedName(memberAccessExpression As MemberAccessExpressionSyntax) As Boolean
            If memberAccessExpression.Kind = SyntaxKind.MemberAccessExpression Then
                If TypeOf memberAccessExpression.Expression Is MemberAccessExpressionSyntax Then
                    Return IsQualifiedName(DirectCast(memberAccessExpression.Expression, MemberAccessExpressionSyntax))
                ElseIf TypeOf memberAccessExpression.Expression Is NameSyntax Then
                    Return True
                End If
            End If

            Return False
        End Function
    End Class
End Namespace