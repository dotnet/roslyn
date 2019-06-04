' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Partial Friend Module SyntaxTokenExtensions
        <Extension()>
        Public Function IsKindOrHasMatchingText(token As SyntaxToken, kind As SyntaxKind) As Boolean
            Return token.Kind = kind OrElse
                   token.HasMatchingText(kind)
        End Function

        <Extension()>
        Public Function HasMatchingText(token As SyntaxToken, kind As SyntaxKind) As Boolean
            Return String.Equals(token.ToString(), SyntaxFacts.GetText(kind), StringComparison.OrdinalIgnoreCase)
        End Function

        <Extension()>
        Public Function IsCharacterLiteral(token As SyntaxToken) As Boolean
            Return token.Kind = SyntaxKind.CharacterLiteralToken
        End Function

        <Extension()>
        Public Function IsNumericLiteral(token As SyntaxToken) As Boolean
            Return _
                token.Kind = SyntaxKind.DateLiteralToken OrElse
                token.Kind = SyntaxKind.DecimalLiteralToken OrElse
                token.Kind = SyntaxKind.FloatingLiteralToken OrElse
                token.Kind = SyntaxKind.IntegerLiteralToken
        End Function

        <Extension()>
        Public Function IsNewOnRightSideOfDotOrBang(token As SyntaxToken) As Boolean
            Dim expression = TryCast(token.Parent, ExpressionSyntax)
            Return If(expression IsNot Nothing,
                      expression.IsNewOnRightSideOfDotOrBang(),
                      False)
        End Function

        <Extension()>
        Public Function IsSkipped(token As SyntaxToken) As Boolean
            Return TypeOf token.Parent Is SkippedTokensTriviaSyntax
        End Function

        <Extension()>
        Public Function FirstAncestorOrSelf(token As SyntaxToken, predicate As Func(Of SyntaxNode, Boolean)) As SyntaxNode
            Return token.Parent.FirstAncestorOrSelf(predicate)
        End Function

        <Extension()>
        Public Function HasAncestor(Of T As SyntaxNode)(token As SyntaxToken) As Boolean
            Return token.GetAncestor(Of T)() IsNot Nothing
        End Function

        ''' <summary>
        ''' Returns true if is a given token is a child token of a certain type of parent node.
        ''' </summary>
        ''' <typeparam name="TParent">The type of the parent node.</typeparam>
        ''' <param name="token">The token that we are testing.</param>
        ''' <param name="childGetter">A function that, when given the parent node, returns the child token we are interested in.</param>
        <Extension()>
        Public Function IsChildToken(Of TParent As SyntaxNode)(token As SyntaxToken, childGetter As Func(Of TParent, SyntaxToken)) As Boolean
            Dim ancestor = token.GetAncestor(Of TParent)()

            If ancestor Is Nothing Then
                Return False
            End If

            Dim ancestorToken = childGetter(ancestor)

            Return token = ancestorToken
        End Function

        ''' <summary>
        ''' Returns true if is a given token is a separator token in a given parent list.
        ''' </summary>
        ''' <typeparam name="TParent">The type of the parent node containing the separated list.</typeparam>
        ''' <param name="token">The token that we are testing.</param>
        ''' <param name="childGetter">A function that, when given the parent node, returns the separated list.</param>
        <Extension()>
        Public Function IsChildSeparatorToken(Of TParent As SyntaxNode, TChild As SyntaxNode)(token As SyntaxToken, childGetter As Func(Of TParent, SeparatedSyntaxList(Of TChild))) As Boolean
            Dim ancestor = token.GetAncestor(Of TParent)()

            If ancestor Is Nothing Then
                Return False
            End If

            Dim separatedList = childGetter(ancestor)
            For i = 0 To separatedList.SeparatorCount - 1
                If separatedList.GetSeparator(i) = token Then
                    Return True
                End If
            Next

            Return False
        End Function

        <Extension>
        Public Function IsDescendantOf(token As SyntaxToken, node As SyntaxNode) As Boolean
            Return token.Parent IsNot Nothing AndAlso
                   token.Parent.AncestorsAndSelf().Any(Function(n) n Is node)
        End Function

        <Extension()>
        Friend Function GetInnermostDeclarationContext(node As SyntaxToken) As SyntaxNode
            Dim ancestors = node.GetAncestors(Of SyntaxNode)

            ' In error cases where the declaration is not complete, the parser attaches the incomplete token to the
            ' trailing trivia of preceding block. In such cases, skip through the siblings and search upwards to find a candidate ancestor.
            If TypeOf ancestors.FirstOrDefault() Is EndBlockStatementSyntax Then

                ' If the first ancestor is an EndBlock, the second is the matching OpenBlock, if one exists
                Dim openBlock = ancestors.ElementAtOrDefault(1)
                Dim closeTypeBlock = DirectCast(ancestors.First(), EndBlockStatementSyntax)

                If openBlock Is Nothing Then
                    ' case: No matching open block
                    '      End Class
                    '    C|
                    ancestors = ancestors.Skip(1)
                ElseIf TypeOf openBlock Is TypeBlockSyntax Then
                    ancestors = FilterAncestors(ancestors, DirectCast(openBlock, TypeBlockSyntax).EndBlockStatement, closeTypeBlock)
                ElseIf TypeOf openBlock Is NamespaceBlockSyntax Then
                    ancestors = FilterAncestors(ancestors, DirectCast(openBlock, NamespaceBlockSyntax).EndNamespaceStatement, closeTypeBlock)
                ElseIf TypeOf openBlock Is EnumBlockSyntax Then
                    ancestors = FilterAncestors(ancestors, DirectCast(openBlock, EnumBlockSyntax).EndEnumStatement, closeTypeBlock)
                End If
            End If

            Return ancestors.FirstOrDefault(
                Function(ancestor) ancestor.IsKind(SyntaxKind.ClassBlock,
                                                        SyntaxKind.StructureBlock,
                                                        SyntaxKind.EnumBlock,
                                                        SyntaxKind.InterfaceBlock,
                                                        SyntaxKind.NamespaceBlock,
                                                        SyntaxKind.ModuleBlock,
                                                        SyntaxKind.CompilationUnit))
        End Function

        Private Function FilterAncestors(ancestors As IEnumerable(Of SyntaxNode),
                                         parentEndBlock As EndBlockStatementSyntax,
                                         precedingEndBlock As EndBlockStatementSyntax) As IEnumerable(Of SyntaxNode)
            If parentEndBlock.Equals(precedingEndBlock) Then
                ' case: the preceding end block has a matching open block and the declaration context for 'C' is 'N1'
                '    Namespace N1
                '      Class C1
                '
                '      End Class
                '    C|
                '    End Namespace
                Return ancestors.Skip(2)
            Else
                ' case: mismatched end block and the declaration context for 'C' is 'N1'
                '    Namespace N1
                '      End Class
                '    C|
                '    End Namespace
                Return ancestors.Skip(1)
            End If
        End Function

        <Extension()>
        Public Function GetContainingMember(token As SyntaxToken) As DeclarationStatementSyntax
            Return token.GetAncestors(Of DeclarationStatementSyntax) _
                .FirstOrDefault(Function(a)
                                    Return a.IsMemberDeclaration() OrElse
                                          (a.IsMemberBlock() AndAlso a.GetMemberBlockBegin().IsMemberDeclaration())
                                End Function)
        End Function

        <Extension()>
        Public Function GetContainingMemberBlockBegin(token As SyntaxToken) As StatementSyntax
            Return token.GetContainingMember().GetMemberBlockBegin()
        End Function

        ''' <summary>
        ''' Determines whether the given SyntaxToken is the first token on a line
        ''' </summary>
        <Extension()>
        Public Function IsFirstTokenOnLine(token As SyntaxToken, cancellationToken As CancellationToken) As Boolean
            Dim previousToken = token.GetPreviousToken(includeSkipped:=True, includeDirectives:=True, includeDocumentationComments:=True)
            If previousToken.Kind = SyntaxKind.None Then
                Return True
            End If

            Dim text = token.SyntaxTree.GetText()
            Dim tokenLine = text.Lines.IndexOf(token.SpanStart)
            Dim previousTokenLine = text.Lines.IndexOf(previousToken.SpanStart)
            Return tokenLine > previousTokenLine
        End Function

        <Extension()>
        Public Function SpansPreprocessorDirective(tokens As IEnumerable(Of SyntaxToken)) As Boolean
            Return VisualBasicSyntaxFactsService.Instance.SpansPreprocessorDirective(tokens)
        End Function

        <Extension()>
        Public Function GetPreviousTokenIfTouchingWord(token As SyntaxToken, position As Integer) As SyntaxToken
            Return If(token.IntersectsWith(position) AndAlso IsWord(token),
                      token.GetPreviousToken(includeSkipped:=True),
                      token)
        End Function

        <Extension>
        Public Function IsWord(token As SyntaxToken) As Boolean
            Return VisualBasicSyntaxFactsService.Instance.IsWord(token)
        End Function

        <Extension()>
        Public Function IntersectsWith(token As SyntaxToken, position As Integer) As Boolean
            Return token.Span.IntersectsWith(position)
        End Function

        <Extension()>
        Public Function GetNextNonZeroWidthTokenOrEndOfFile(token As SyntaxToken) As SyntaxToken
            Dim nextToken = token.GetNextToken()
            Return If(nextToken.Kind = SyntaxKind.None, token.GetAncestor(Of CompilationUnitSyntax)().EndOfFileToken, nextToken)
        End Function

        <Extension>
        Public Function IsValidAttributeTarget(token As SyntaxToken) As Boolean
            Return token.Kind() = SyntaxKind.AssemblyKeyword OrElse
                   token.Kind() = SyntaxKind.ModuleKeyword
        End Function
    End Module
End Namespace
