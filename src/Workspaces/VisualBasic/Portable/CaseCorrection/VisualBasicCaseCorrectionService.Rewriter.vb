' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CaseCorrection
    Partial Friend Class VisualBasicCaseCorrectionService
        Private Class Rewriter
            Inherits VisualBasicSyntaxRewriter

            Private ReadOnly _createAliasSet As Func(Of ImmutableHashSet(Of String)) =
                Function()
                    Dim model = Me._semanticModel.GetOriginalSemanticModel()

                    ' root should be already available
                    If Not model.SyntaxTree.HasCompilationUnitRoot Then
                        Return ImmutableHashSet.Create(Of String)()
                    End If

                    Dim root = model.SyntaxTree.GetCompilationUnitRoot()

                    Dim [set] = ImmutableHashSet.CreateBuilder(Of String)(StringComparer.OrdinalIgnoreCase)
                    For Each importsClause In root.GetAliasImportsClauses()
                        If Not String.IsNullOrWhiteSpace(importsClause.Alias.Identifier.ValueText) Then
                            [set].Add(importsClause.Alias.Identifier.ValueText)
                        End If
                    Next

                    For Each import In model.Compilation.AliasImports
                        [set].Add(import.Name)
                    Next

                    Return [set].ToImmutable()
                End Function

            Private ReadOnly _syntaxFactsService As ISyntaxFactsService
            Private ReadOnly _semanticModel As SemanticModel
            Private ReadOnly _aliasSet As Lazy(Of ImmutableHashSet(Of String))
            Private ReadOnly _cancellationToken As CancellationToken

            Public Sub New(syntaxFactsService As ISyntaxFactsService, semanticModel As SemanticModel, cancellationToken As CancellationToken)
                MyBase.New(visitIntoStructuredTrivia:=True)
                Me._syntaxFactsService = syntaxFactsService
                Me._semanticModel = semanticModel
                Me._aliasSet = New Lazy(Of ImmutableHashSet(Of String))(_createAliasSet)
                Me._cancellationToken = cancellationToken
            End Sub

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                Dim newToken = MyBase.VisitToken(token)

                If _syntaxFactsService.IsIdentifier(newToken) Then
                    Return VisitIdentifier(token, newToken)
                ElseIf _syntaxFactsService.IsReservedOrContextualKeyword(newToken) Then
                    Return VisitKeyword(newToken)
                ElseIf token.IsNumericLiteral() Then
                    Return VisitNumericLiteral(newToken)
                ElseIf token.IsCharacterLiteral() Then
                    Return VisitCharacterLiteral(newToken)
                End If

                Return newToken
            End Function

            Private Function VisitIdentifier(token As SyntaxToken, newToken As SyntaxToken) As SyntaxToken
                If newToken.IsMissing OrElse TypeOf newToken.Parent Is ArgumentSyntax OrElse _semanticModel Is Nothing Then
                    Return newToken
                End If

                If token.Parent.IsPartOfStructuredTrivia() Then
                    Dim identifierSyntax = TryCast(token.Parent, IdentifierNameSyntax)
                    If identifierSyntax IsNot Nothing Then
                        Dim preprocessingSymbolInfo = _semanticModel.GetPreprocessingSymbolInfo(identifierSyntax)
                        If preprocessingSymbolInfo.Symbol IsNot Nothing Then
                            Dim name = preprocessingSymbolInfo.Symbol.Name
                            If Not String.IsNullOrEmpty(name) AndAlso name <> token.ValueText Then
                                ' Name should differ only in case
                                Debug.Assert(name.Equals(token.ValueText, StringComparison.OrdinalIgnoreCase))

                                Return GetIdentifierWithCorrectedName(name, newToken)
                            End If
                        End If
                    End If

                    Return newToken
                Else
                    Dim methodDeclaration = TryCast(token.Parent, MethodStatementSyntax)
                    If methodDeclaration IsNot Nothing Then
                        ' If this is a partial method implementation part, then case correct the method name to match the partial method definition part.
                        Dim definitionPart As IMethodSymbol = Nothing
                        Dim otherPartOfPartial = GetOtherPartOfPartialMethod(methodDeclaration, definitionPart)
                        If otherPartOfPartial IsNot Nothing And Equals(otherPartOfPartial, definitionPart) Then
                            Return CaseCorrectIdentifierIfNamesDiffer(token, newToken, otherPartOfPartial)
                        End If
                    Else
                        Dim parameterSyntax = token.GetAncestor(Of ParameterSyntax)()
                        If parameterSyntax IsNot Nothing Then
                            ' If this is a parameter declaration for a partial method implementation part,
                            ' then case correct the parameter name to match the corresponding parameter in the partial method definition part.
                            methodDeclaration = parameterSyntax.GetAncestor(Of MethodStatementSyntax)()
                            If methodDeclaration IsNot Nothing Then
                                Dim definitionPart As IMethodSymbol = Nothing
                                Dim otherPartOfPartial = GetOtherPartOfPartialMethod(methodDeclaration, definitionPart)
                                If otherPartOfPartial IsNot Nothing And Equals(otherPartOfPartial, definitionPart) Then
                                    Dim ordinal As Integer = 0
                                    For Each param As SyntaxNode In methodDeclaration.ParameterList.Parameters
                                        If param Is parameterSyntax Then
                                            Exit For
                                        End If

                                        ordinal = ordinal + 1
                                    Next

                                    Debug.Assert(otherPartOfPartial.Parameters.Length > ordinal)
                                    Dim otherPartParam = otherPartOfPartial.Parameters(ordinal)

                                    ' We don't want to rename the parameter if names are not equal ignoring case.
                                    ' Compiler will anyways generate an error for this case.
                                    Return CaseCorrectIdentifierIfNamesDiffer(token, newToken, otherPartParam, namesMustBeEqualIgnoringCase:=True)
                                End If
                            End If
                        Else
                            ' Named tuple expression
                            Dim nameColonEquals = TryCast(token.Parent?.Parent, NameColonEqualsSyntax)
                            If nameColonEquals IsNot Nothing AndAlso TypeOf nameColonEquals.Parent?.Parent Is TupleExpressionSyntax Then
                                Return newToken
                            End If
                        End If
                    End If
                End If

                Dim symbol = GetAliasOrAnySymbol(_semanticModel, token.Parent, _cancellationToken)
                If symbol Is Nothing Then
                    Return newToken
                End If

                Dim expression = TryCast(token.Parent, ExpressionSyntax)
                If expression IsNot Nothing AndAlso SyntaxFacts.IsInNamespaceOrTypeContext(expression) AndAlso Not IsNamespaceOrTypeRelatedSymbol(symbol) Then
                    Return newToken
                End If

                If TypeOf symbol Is ITypeSymbol AndAlso DirectCast(symbol, ITypeSymbol).TypeKind = TypeKind.Error Then
                    Return newToken
                End If

                ' If it's a constructor we bind to, then we want to compare the name on the token to the
                ' name of the type.  The name of the bound symbol will be something useless like '.ctor'.
                ' However, if it's an explicit New on the right side of a member access or qualified name, we want to use "New".
                If symbol.IsConstructor Then
                    If token.IsNewOnRightSideOfDotOrBang() Then
                        Return SyntaxFactory.Identifier(newToken.LeadingTrivia, "New", newToken.TrailingTrivia)
                    End If

                    symbol = symbol.ContainingType
                End If

                Return CaseCorrectIdentifierIfNamesDiffer(token, newToken, symbol)
            End Function

            Private Shared Function IsNamespaceOrTypeRelatedSymbol(symbol As ISymbol) As Boolean
                Return TypeOf symbol Is INamespaceOrTypeSymbol OrElse
                    (TypeOf symbol Is IAliasSymbol AndAlso TypeOf DirectCast(symbol, IAliasSymbol).Target Is INamespaceOrTypeSymbol) OrElse
                    (symbol.IsKind(SymbolKind.Method) AndAlso DirectCast(symbol, IMethodSymbol).MethodKind = MethodKind.Constructor)
            End Function

            Private Function GetAliasOrAnySymbol(model As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken) As ISymbol
                Dim identifier = TryCast(node, IdentifierNameSyntax)
                If identifier IsNot Nothing AndAlso Me._aliasSet.Value.Contains(identifier.Identifier.ValueText) Then
                    Dim [alias] = model.GetAliasInfo(identifier, cancellationToken)
                    If [alias] IsNot Nothing Then
                        Return [alias]
                    End If
                End If

                Return model.GetSymbolInfo(node, cancellationToken).GetAnySymbol()
            End Function

            Private Shared Function CaseCorrectIdentifierIfNamesDiffer(
                token As SyntaxToken,
                newToken As SyntaxToken,
                symbol As ISymbol,
                Optional namesMustBeEqualIgnoringCase As Boolean = False
            ) As SyntaxToken
                If NamesDiffer(symbol, token) Then
                    If namesMustBeEqualIgnoringCase AndAlso Not String.Equals(symbol.Name, token.ValueText, StringComparison.OrdinalIgnoreCase) Then
                        Return newToken
                    End If

                    Dim correctedName = GetCorrectedName(token, symbol)
                    Return GetIdentifierWithCorrectedName(correctedName, newToken)
                End If

                Return newToken
            End Function

            Private Function GetOtherPartOfPartialMethod(methodDeclaration As MethodStatementSyntax, <Out> ByRef definitionPart As IMethodSymbol) As IMethodSymbol
                Contract.ThrowIfNull(methodDeclaration)
                Contract.ThrowIfNull(_semanticModel)

                Dim methodSymbol = _semanticModel.GetDeclaredSymbol(methodDeclaration, _cancellationToken)
                If methodSymbol IsNot Nothing Then
                    definitionPart = If(methodSymbol.PartialDefinitionPart, methodSymbol)
                    Return If(methodSymbol.PartialDefinitionPart, methodSymbol.PartialImplementationPart)
                End If

                Return Nothing
            End Function

            Private Shared Function GetCorrectedName(token As SyntaxToken, symbol As ISymbol) As String
                If symbol.IsAttribute Then
                    If String.Equals(token.ValueText & s_attributeSuffix, symbol.Name, StringComparison.OrdinalIgnoreCase) Then
                        Return symbol.Name.Substring(0, symbol.Name.Length - s_attributeSuffix.Length)
                    End If
                End If

                Return symbol.Name
            End Function

            Private Shared Function GetIdentifierWithCorrectedName(correctedName As String, token As SyntaxToken) As SyntaxToken
                If token.IsBracketed Then
                    Return SyntaxFactory.BracketedIdentifier(token.LeadingTrivia, correctedName, token.TrailingTrivia)
                Else
                    Return SyntaxFactory.Identifier(token.LeadingTrivia, correctedName, token.TrailingTrivia)
                End If
            End Function

            Private Shared Function NamesDiffer(symbol As ISymbol,
                                         token As SyntaxToken) As Boolean
                If String.IsNullOrEmpty(symbol.Name) Then
                    Return False
                End If

                If symbol.Name = token.ValueText Then
                    Return False
                End If

                If symbol.IsAttribute() Then
                    If symbol.Name = token.ValueText & s_attributeSuffix Then
                        Return False
                    End If
                End If

                Return True
            End Function

            Private Function VisitKeyword(token As SyntaxToken) As SyntaxToken
                If Not token.IsMissing Then
                    Dim actualText = token.ToString()
                    Dim expectedText = _syntaxFactsService.GetText(token.Kind)

                    If Not String.IsNullOrWhiteSpace(expectedText) AndAlso actualText <> expectedText Then
                        Return SyntaxFactory.Token(token.LeadingTrivia, token.Kind, token.TrailingTrivia, expectedText)
                    End If
                End If

                Return token
            End Function

            Private Shared Function VisitNumericLiteral(token As SyntaxToken) As SyntaxToken
                If Not token.IsMissing Then

                    ' For any numeric literal, we simply case correct any letters to uppercase.
                    ' The only letters allowed in a numeric literal are:
                    '   * Type characters: S, US, I, UI, L, UL, D, F, R
                    '   * Hex/Octal literals: H, O and A, B, C, D, E, F
                    '   * Exponent: E
                    '   * Time literals: AM, PM 

                    Dim actualText = token.ToString()
                    Dim expectedText = actualText.ToUpperInvariant()

                    If actualText <> expectedText Then
                        Return SyntaxFactory.ParseToken(expectedText).WithLeadingTrivia(token.LeadingTrivia).WithTrailingTrivia(token.TrailingTrivia)
                    End If
                End If

                Return token
            End Function

            Private Shared Function VisitCharacterLiteral(token As SyntaxToken) As SyntaxToken
                If Not token.IsMissing Then

                    ' For character literals, we case correct the type character to "c".
                    Dim actualText = token.ToString()

                    If actualText.EndsWith("C", StringComparison.Ordinal) Then
                        Dim expectedText = actualText.Substring(0, actualText.Length - 1) & "c"
                        Return SyntaxFactory.ParseToken(expectedText).WithLeadingTrivia(token.LeadingTrivia).WithTrailingTrivia(token.TrailingTrivia)
                    End If
                End If

                Return token
            End Function

            Public Overrides Function VisitTrivia(trivia As SyntaxTrivia) As SyntaxTrivia
                trivia = MyBase.VisitTrivia(trivia)

                If trivia.Kind = SyntaxKind.CommentTrivia AndAlso trivia.Width >= 3 Then
                    Dim remText = trivia.ToString().Substring(0, 3)
                    Dim remKeywordText As String = _syntaxFactsService.GetText(SyntaxKind.REMKeyword)
                    If remText <> remKeywordText AndAlso SyntaxFacts.GetKeywordKind(remText) = SyntaxKind.REMKeyword Then
                        Dim expectedText = remKeywordText & trivia.ToString().Substring(3)
                        Return SyntaxFactory.CommentTrivia(expectedText)
                    End If
                End If

                Return trivia
            End Function

        End Class
    End Class
End Namespace
