' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicEscapingReducer
        Inherits AbstractVisualBasicReducer

        Private Shared ReadOnly s_pool As ObjectPool(Of IReductionRewriter) =
            New ObjectPool(Of IReductionRewriter)(Function() New Rewriter(s_pool))

        Public Sub New()
            MyBase.New(s_pool)
        End Sub

        Public Overrides Function IsApplicable(options As VisualBasicSimplifierOptions) As Boolean
            Return True
        End Function

#Disable Warning IDE0060 ' Remove unused parameter - False positive, used as a delegate in a nested type.
        ' https://github.com/dotnet/roslyn/issues/44226
        Private Shared Function TryUnescapeToken(identifier As SyntaxToken, semanticModel As SemanticModel, options As VisualBasicSimplifierOptions, cancellationToken As CancellationToken) As SyntaxToken
#Enable Warning IDE0060 ' Remove unused parameter
            If Not identifier.IsBracketed Then
                Return identifier
            End If

            Dim unescapedIdentifier = identifier.ValueText

            ' 1. handle keywords

            ' REM should always be escaped
            ' e.g.
            ' Dim [Rem] = 23
            ' Call Goo.[Rem]()
            If SyntaxFacts.GetKeywordKind(unescapedIdentifier) = SyntaxKind.REMKeyword Then
                Return identifier
            End If

            Dim parent = identifier.Parent

            ' this identifier is a keyword
            If SyntaxFacts.GetKeywordKind(unescapedIdentifier) <> SyntaxKind.None Then

                ' Always escape keywords as identifier if they are not part of a qualified name or member access
                ' e.g. Class [Class]
                If Not TypeOf (parent) Is ExpressionSyntax Then
                    Return identifier
                Else
                    ' always escape keywords on the left side of a dot
                    If Not DirectCast(parent, ExpressionSyntax).IsRightSideOfDot() Then
                        Return identifier
                    End If
                End If
            End If

            ' 2. Handle contextual keywords

            ' Escape the Await Identifier if within the Single Line Lambda & Multi Line Context
            ' Dim y = Async Function() [Await]() but not Dim y = Async Function() Await()
            ' Same behavior for Multi Line Lambda
            If SyntaxFacts.GetContextualKeywordKind(unescapedIdentifier) = SyntaxKind.AwaitKeyword Then
                Dim enclosingSingleLineLambda = parent.GetAncestor(Of LambdaExpressionSyntax)()
                If enclosingSingleLineLambda IsNot Nothing AndAlso enclosingSingleLineLambda.SubOrFunctionHeader.Modifiers.Any(Function(modifier) modifier.Kind = SyntaxKind.AsyncKeyword) Then
                    Return identifier
                End If

                Dim enclosingMethodBlock = parent.GetAncestor(Of MethodBlockBaseSyntax)()
                If enclosingMethodBlock IsNot Nothing AndAlso enclosingMethodBlock.BlockStatement.Modifiers.Any(Function(modifier) modifier.Kind = SyntaxKind.AsyncKeyword) Then
                    Return identifier
                End If
            End If

            ' escape the identifier "preserve" if it's inside of a redim statement
            If TypeOf parent Is SimpleNameSyntax AndAlso IsPreserveInReDim(DirectCast(parent, SimpleNameSyntax)) Then
                Return identifier
            End If

            ' handle "Mid" identifier that is not part of an Mid assignment statement which must be escaped if the containing statement
            ' starts with the "Mid" identifier token.
            If SyntaxFacts.GetContextualKeywordKind(unescapedIdentifier) = SyntaxKind.MidKeyword Then
                Dim enclosingStatement = parent.GetAncestor(Of StatementSyntax)()

                If enclosingStatement.Kind <> SyntaxKind.MidAssignmentStatement Then
                    If enclosingStatement.GetFirstToken() = identifier Then
                        Return identifier
                    End If
                End If
            End If

            ' handle new identifier
            If SyntaxFacts.GetKeywordKind(unescapedIdentifier) = SyntaxKind.NewKeyword Then
                Dim typedParent = TryCast(parent, ExpressionSyntax)
                If typedParent IsNot Nothing Then
                    Dim symbol = semanticModel.GetSymbolInfo(typedParent, cancellationToken).Symbol

                    If symbol IsNot Nothing AndAlso symbol.Kind = SymbolKind.Method AndAlso Not DirectCast(symbol, IMethodSymbol).IsConstructor Then
                        If symbol.ContainingType IsNot Nothing Then
                            Dim type = symbol.ContainingType
                            If type.TypeKind <> TypeKind.Interface AndAlso type.TypeKind <> TypeKind.Enum Then
                                Return identifier
                            End If
                        End If
                    End If
                End If
            End If

            ' handle identifier Group in a function aggregation
            If SyntaxFacts.GetContextualKeywordKind(unescapedIdentifier) = SyntaxKind.GroupKeyword Then
                If parent.Kind = SyntaxKind.FunctionAggregation AndAlso parent.GetFirstToken() = identifier Then
                    Return identifier
                End If
            End If

            Dim lastTokenOfQuery As SyntaxToken = Nothing
            Dim firstTokenAfterQueryExpression As SyntaxToken = Nothing

            ' escape contextual query keywords if they are the first token after a query expression 
            ' and on the following line
            Dim previousToken = identifier.GetPreviousToken(False, False, True, True)
            Dim queryAncestorOfPrevious = previousToken.GetAncestors(Of QueryExpressionSyntax).FirstOrDefault()
            If queryAncestorOfPrevious IsNot Nothing AndAlso queryAncestorOfPrevious.GetLastToken() = previousToken Then
                lastTokenOfQuery = previousToken

                Dim checkQueryToken = False
                Select Case SyntaxFacts.GetContextualKeywordKind(unescapedIdentifier)
                    Case SyntaxKind.AggregateKeyword,
                        SyntaxKind.DistinctKeyword,
                        SyntaxKind.FromKeyword,
                        SyntaxKind.GroupKeyword,
                        SyntaxKind.IntoKeyword,
                        SyntaxKind.JoinKeyword,
                        SyntaxKind.OrderKeyword,
                        SyntaxKind.SkipKeyword,
                        SyntaxKind.TakeKeyword,
                        SyntaxKind.WhereKeyword

                        checkQueryToken = True

                    Case SyntaxKind.AscendingKeyword,
                        SyntaxKind.DescendingKeyword

                        checkQueryToken = lastTokenOfQuery.HasAncestor(Of OrderByClauseSyntax)()
                End Select

                If checkQueryToken Then
                    Dim text = parent.SyntaxTree.GetText(cancellationToken)

                    Dim endLineOfQuery = text.Lines.GetLineFromPosition(lastTokenOfQuery.Span.End).LineNumber
                    Dim startLineOfCurrentToken = text.Lines.GetLineFromPosition(identifier.SpanStart).LineNumber

                    ' Easy out: if the current token starts the line after the query, we can't escape.
                    If startLineOfCurrentToken = endLineOfQuery + 1 Then
                        Return identifier
                    End If

                    ' if this token is part of a XmlDocument, all trailing whitespace is part of the XmlDocument
                    ' so all line breaks actually will not help.
                    ' see VB spec #11.23.3
                    If previousToken.GetAncestors(Of XmlDocumentSyntax).FirstOrDefault() IsNot Nothing Then
                        Return identifier
                    End If

                    ' If there are more lines between the query and the next token, we check to see if any
                    ' of them are blank lines. If a blank line is encountered, we can assume that the
                    ' identifier can be unescaped. Otherwise, we'll end up incorrectly unescaping in
                    ' code like so.
                    '
                    '   Dim q = From x in ""
                    '   _
                    '   _
                    '   [Take]()

                    If startLineOfCurrentToken > endLineOfQuery + 1 Then
                        Dim unescape = False
                        For i = endLineOfQuery + 1 To startLineOfCurrentToken - 1
                            If text.Lines(i).IsEmptyOrWhitespace() Then
                                unescape = True
                                Exit For
                            End If
                        Next

                        If Not unescape Then
                            Return identifier
                        End If
                    End If
                End If
            End If

            ' build new unescaped identifier token
            Dim newIdentifier = CreateNewIdentifierTokenFromToken(identifier, False)

            Dim parentAsSimpleName = TryCast(parent, SimpleNameSyntax)
            If parentAsSimpleName IsNot Nothing Then
                ' try if unescaped identifier is valid in this context
                If ExpressionSyntaxExtensions.IsReservedNameInAttribute(parentAsSimpleName, parentAsSimpleName.WithIdentifier(newIdentifier)) Then
                    Return identifier
                End If
            End If

            ' safe to return an unescaped identifier
            Return newIdentifier
        End Function

        Private Shared Function CreateNewIdentifierTokenFromToken(originalToken As SyntaxToken, escape As Boolean) As SyntaxToken
            Return If(escape,
                      originalToken.CopyAnnotationsTo(SyntaxFactory.BracketedIdentifier(originalToken.LeadingTrivia, originalToken.ValueText, originalToken.TrailingTrivia)),
                      originalToken.CopyAnnotationsTo(SyntaxFactory.Identifier(originalToken.LeadingTrivia, originalToken.ValueText, originalToken.TrailingTrivia)))
        End Function

        Private Shared Function IsPreserveInReDim(node As SimpleNameSyntax) As Boolean
            Dim redimStatement = node.GetAncestor(Of ReDimStatementSyntax)()

            If redimStatement IsNot Nothing AndAlso
               SyntaxFacts.GetContextualKeywordKind(node.Identifier.GetIdentifierText()) = SyntaxKind.PreserveKeyword AndAlso
               redimStatement.Clauses.Count > 0 AndAlso
               redimStatement.Clauses.First().GetFirstToken() = node.GetFirstToken() Then
                Return True
            End If

            Return False
        End Function
    End Class
End Namespace
