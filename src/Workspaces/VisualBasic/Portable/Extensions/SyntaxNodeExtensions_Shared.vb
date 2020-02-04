' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Partial Friend Module SyntaxNodeExtensions
        <Extension()>
        Public Function IsParentKind(node As SyntaxNode, kind1 As SyntaxKind, kind2 As SyntaxKind) As Boolean
            Return node IsNot Nothing AndAlso
                   IsKind(node.Parent, kind1, kind2)
        End Function

        <Extension()>
        Public Function IsParentKind(node As SyntaxNode, kind1 As SyntaxKind, kind2 As SyntaxKind, kind3 As SyntaxKind) As Boolean
            Return node IsNot Nothing AndAlso
                   IsKind(node.Parent, kind1, kind2, kind3)
        End Function

        <Extension()>
        Public Function IsKind(node As SyntaxNode, kind1 As SyntaxKind, kind2 As SyntaxKind) As Boolean
            If node Is Nothing Then
                Return False
            End If

            Return node.Kind = kind1 OrElse
                   node.Kind = kind2
        End Function

        <Extension()>
        Public Function IsKind(node As SyntaxNode, kind1 As SyntaxKind, kind2 As SyntaxKind, kind3 As SyntaxKind) As Boolean
            If node Is Nothing Then
                Return False
            End If

            Return node.Kind = kind1 OrElse
                   node.Kind = kind2 OrElse
                   node.Kind = kind3
        End Function

        <Extension()>
        Public Function IsKind(node As SyntaxNode, ParamArray kinds As SyntaxKind()) As Boolean
            If node Is Nothing Then
                Return False
            End If

            Return kinds.Contains(node.Kind())
        End Function

        <Extension()>
        Public Function IsParentKind(node As SyntaxNode, kind As SyntaxKind) As Boolean
            Return node IsNot Nothing AndAlso
                   node.Parent.IsKind(kind)
        End Function

        <Extension()>
        Public Function GetBraces(node As SyntaxNode) As (openBrace As SyntaxToken, closeBrace As SyntaxToken)
            Return node.TypeSwitch(
                Function(n As TypeParameterMultipleConstraintClauseSyntax) (n.OpenBraceToken, n.CloseBraceToken),
                Function(n As ObjectMemberInitializerSyntax) (n.OpenBraceToken, n.CloseBraceToken),
                Function(n As CollectionInitializerSyntax) (n.OpenBraceToken, n.CloseBraceToken),
                Function(n As SyntaxNode) CType(Nothing, (SyntaxToken, SyntaxToken)))
        End Function

        <Extension()>
        Public Function IsInStaticContext(node As SyntaxNode) As Boolean
            Dim containingType = node.GetAncestorOrThis(Of TypeBlockSyntax)()
            If containingType.IsKind(SyntaxKind.ModuleBlock) Then
                Return True
            End If

            Return node.GetAncestorsOrThis(Of StatementSyntax)().
                        SelectMany(Function(s) s.GetModifiers()).
                        Any(Function(t) t.Kind = SyntaxKind.SharedKeyword)
        End Function
        <Extension()>
        Public Function IsInConstantContext(expression As SyntaxNode) As Boolean
            If expression.GetAncestor(Of ParameterSyntax)() IsNot Nothing Then
                Return True
            End If

            ' TODO(cyrusn): Add more cases
            Return False
        End Function

        <Extension()>
        Public Function GetMembers(node As SyntaxNode) As SyntaxList(Of StatementSyntax)
            Dim compilation = TryCast(node, CompilationUnitSyntax)
            If compilation IsNot Nothing Then
                Return compilation.Members
            End If

            Dim [namespace] = TryCast(node, NamespaceBlockSyntax)
            If [namespace] IsNot Nothing Then
                Return [namespace].Members
            End If

            Dim type = TryCast(node, TypeBlockSyntax)
            If type IsNot Nothing Then
                Return type.Members
            End If

            Dim [enum] = TryCast(node, EnumBlockSyntax)
            If [enum] IsNot Nothing Then
                Return [enum].Members
            End If

            Return Nothing
        End Function

        <Extension>
        Public Function IsLeftSideOfSimpleAssignmentStatement(node As SyntaxNode) As Boolean
            Return node.IsParentKind(SyntaxKind.SimpleAssignmentStatement) AndAlso
                DirectCast(node.Parent, AssignmentStatementSyntax).Left Is node
        End Function

        <Extension>
        Public Function IsLeftSideOfAnyAssignmentStatement(node As SyntaxNode) As Boolean
            Return node IsNot Nothing AndAlso
                node.Parent.IsAnyAssignmentStatement() AndAlso
                DirectCast(node.Parent, AssignmentStatementSyntax).Left Is node
        End Function

        <Extension>
        Public Function IsAnyAssignmentStatement(node As SyntaxNode) As Boolean
            Return node IsNot Nothing AndAlso
                SyntaxFacts.IsAssignmentStatement(node.Kind)
        End Function

        <Extension>
        Public Function IsLeftSideOfCompoundAssignmentStatement(node As SyntaxNode) As Boolean
            Return node IsNot Nothing AndAlso
                node.Parent.IsCompoundAssignmentStatement() AndAlso
                DirectCast(node.Parent, AssignmentStatementSyntax).Left Is node
        End Function

        <Extension>
        Public Function IsCompoundAssignmentStatement(node As SyntaxNode) As Boolean
            If node IsNot Nothing Then
                Select Case node.Kind
                    Case SyntaxKind.AddAssignmentStatement,
                         SyntaxKind.SubtractAssignmentStatement,
                         SyntaxKind.MultiplyAssignmentStatement,
                         SyntaxKind.DivideAssignmentStatement,
                         SyntaxKind.IntegerDivideAssignmentStatement,
                         SyntaxKind.ExponentiateAssignmentStatement,
                         SyntaxKind.LeftShiftAssignmentStatement,
                         SyntaxKind.RightShiftAssignmentStatement,
                         SyntaxKind.ConcatenateAssignmentStatement
                        Return True
                End Select
            End If

            Return False
        End Function

        <Extension()>
        Public Function ConvertToSingleLine(Of TNode As SyntaxNode)(node As TNode, Optional useElasticTrivia As Boolean = False) As TNode
            If node Is Nothing Then
                Return node
            End If

            Dim rewriter = New SingleLineRewriter(useElasticTrivia)
            Return DirectCast(rewriter.Visit(node), TNode)
        End Function

        ''' <summary>
        ''' Returns true if the passed in node contains an interleaved pp 
        ''' directive.
        ''' 
        ''' i.e. The following returns false:
        ''' 
        '''   void Goo() {
        ''' #if true
        ''' #endif
        '''   }
        ''' 
        ''' #if true
        '''   void Goo() {
        '''   }
        ''' #endif
        ''' 
        ''' but these return true:
        ''' 
        ''' #if true
        '''   void Goo() {
        ''' #endif
        '''   }
        ''' 
        '''   void Goo() {
        ''' #if true
        '''   }
        ''' #endif
        ''' 
        ''' #if true
        '''   void Goo() {
        ''' #else
        '''   }
        ''' #endif
        ''' 
        ''' i.e. the method returns true if it contains a PP directive that 
        ''' belongs to a grouping constructs (like #if/#endif or 
        ''' #region/#endregion), but the grouping construct isn't entirely c
        ''' contained within the span of the node.
        ''' </summary>
        <Extension()>
        Public Function ContainsInterleavedDirective(node As SyntaxNode, cancellationToken As CancellationToken) As Boolean
            Return VisualBasicSyntaxFactsService.Instance.ContainsInterleavedDirective(node, cancellationToken)
        End Function

        <Extension>
        Public Function ContainsInterleavedDirective(
            token As SyntaxToken,
            textSpan As TextSpan,
            cancellationToken As CancellationToken) As Boolean

            Return ContainsInterleavedDirective(textSpan, token.LeadingTrivia, cancellationToken) OrElse
                ContainsInterleavedDirective(textSpan, token.TrailingTrivia, cancellationToken)
        End Function

        Private Function ContainsInterleavedDirective(
            textSpan As TextSpan,
            list As SyntaxTriviaList,
            cancellationToken As CancellationToken) As Boolean

            For Each trivia In list
                If textSpan.Contains(trivia.Span) Then
                    If ContainsInterleavedDirective(textSpan, trivia, cancellationToken) Then
                        Return True
                    End If
                End If
            Next trivia

            Return False
        End Function

        Private Function ContainsInterleavedDirective(
            textSpan As TextSpan,
            trivia As SyntaxTrivia,
            cancellationToken As CancellationToken) As Boolean

            If trivia.HasStructure AndAlso TypeOf trivia.GetStructure() Is DirectiveTriviaSyntax Then
                Dim parentSpan = trivia.GetStructure().Span
                Dim directiveSyntax = DirectCast(trivia.GetStructure(), DirectiveTriviaSyntax)
                If directiveSyntax.IsKind(SyntaxKind.RegionDirectiveTrivia, SyntaxKind.EndRegionDirectiveTrivia, SyntaxKind.IfDirectiveTrivia, SyntaxKind.EndIfDirectiveTrivia) Then
                    Dim match = directiveSyntax.GetMatchingStartOrEndDirective(cancellationToken)
                    If match IsNot Nothing Then
                        Dim matchSpan = match.Span
                        If Not textSpan.Contains(matchSpan.Start) Then
                            ' The match for this pp directive is outside
                            ' this node.
                            Return True
                        End If
                    End If
                ElseIf directiveSyntax.IsKind(SyntaxKind.ElseDirectiveTrivia, SyntaxKind.ElseIfDirectiveTrivia) Then
                    Dim directives = directiveSyntax.GetMatchingConditionalDirectives(cancellationToken)
                    If directives IsNot Nothing AndAlso directives.Count > 0 Then
                        If Not textSpan.Contains(directives(0).SpanStart) OrElse
                           Not textSpan.Contains(directives(directives.Count - 1).SpanStart) Then
                            ' This else/elif belongs to a pp span that isn't 
                            ' entirely within this node.
                            Return True
                        End If
                    End If
                End If
            End If

            Return False
        End Function

        ''' <summary>
        ''' Returns true if this is a block that can contain multiple executable statements.  i.e.
        ''' this node is the VB equivalent of a BlockSyntax in C#.
        ''' </summary>
        <Extension()>
        Public Function IsExecutableBlock(node As SyntaxNode) As Boolean
            If node IsNot Nothing Then
                If TypeOf node Is MethodBlockBaseSyntax OrElse
                   TypeOf node Is DoLoopBlockSyntax OrElse
                   TypeOf node Is ForOrForEachBlockSyntax OrElse
                   TypeOf node Is MultiLineLambdaExpressionSyntax Then
                    Return True
                End If

                Select Case node.Kind
                    Case SyntaxKind.WhileBlock,
                         SyntaxKind.UsingBlock,
                         SyntaxKind.SyncLockBlock,
                         SyntaxKind.WithBlock,
                         SyntaxKind.SingleLineIfStatement,
                         SyntaxKind.SingleLineElseClause,
                         SyntaxKind.SingleLineSubLambdaExpression,
                         SyntaxKind.MultiLineIfBlock,
                         SyntaxKind.ElseIfBlock,
                         SyntaxKind.ElseBlock,
                         SyntaxKind.TryBlock,
                         SyntaxKind.CatchBlock,
                         SyntaxKind.FinallyBlock,
                         SyntaxKind.CaseBlock,
                         SyntaxKind.CaseElseBlock
                        Return True
                End Select
            End If

            Return False
        End Function

        <Extension()>
        Public Function FindInnermostCommonExecutableBlock(nodes As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Dim blocks As IEnumerable(Of SyntaxNode) = Nothing
            For Each node In nodes
                blocks = If(blocks Is Nothing,
                             node.GetContainingExecutableBlocks(),
                             blocks.Intersect(node.GetContainingExecutableBlocks()))
            Next

            Return If(blocks Is Nothing, Nothing, blocks.FirstOrDefault())
        End Function

        <Extension()>
        Public Function GetExecutableBlockStatements(node As SyntaxNode) As SyntaxList(Of StatementSyntax)
            If node IsNot Nothing Then
                If TypeOf node Is MethodBlockBaseSyntax Then
                    Return DirectCast(node, MethodBlockBaseSyntax).Statements
                ElseIf TypeOf node Is DoLoopBlockSyntax Then
                    Return DirectCast(node, DoLoopBlockSyntax).Statements
                ElseIf TypeOf node Is ForOrForEachBlockSyntax Then
                    Return DirectCast(node, ForOrForEachBlockSyntax).Statements
                ElseIf TypeOf node Is MultiLineLambdaExpressionSyntax Then
                    Return DirectCast(node, MultiLineLambdaExpressionSyntax).Statements
                End If

                Select Case node.Kind
                    Case SyntaxKind.WhileBlock
                        Return DirectCast(node, WhileBlockSyntax).Statements
                    Case SyntaxKind.UsingBlock
                        Return DirectCast(node, UsingBlockSyntax).Statements
                    Case SyntaxKind.SyncLockBlock
                        Return DirectCast(node, SyncLockBlockSyntax).Statements
                    Case SyntaxKind.WithBlock
                        Return DirectCast(node, WithBlockSyntax).Statements
                    Case SyntaxKind.SingleLineIfStatement
                        Return DirectCast(node, SingleLineIfStatementSyntax).Statements
                    Case SyntaxKind.SingleLineElseClause
                        Return DirectCast(node, SingleLineElseClauseSyntax).Statements
                    Case SyntaxKind.SingleLineSubLambdaExpression
                        Return SyntaxFactory.SingletonList(DirectCast(DirectCast(node, SingleLineLambdaExpressionSyntax).Body, StatementSyntax))
                    Case SyntaxKind.MultiLineIfBlock
                        Return DirectCast(node, MultiLineIfBlockSyntax).Statements
                    Case SyntaxKind.ElseIfBlock
                        Return DirectCast(node, ElseIfBlockSyntax).Statements
                    Case SyntaxKind.ElseBlock
                        Return DirectCast(node, ElseBlockSyntax).Statements
                    Case SyntaxKind.TryBlock
                        Return DirectCast(node, TryBlockSyntax).Statements
                    Case SyntaxKind.CatchBlock
                        Return DirectCast(node, CatchBlockSyntax).Statements
                    Case SyntaxKind.FinallyBlock
                        Return DirectCast(node, FinallyBlockSyntax).Statements
                    Case SyntaxKind.CaseBlock, SyntaxKind.CaseElseBlock
                        Return DirectCast(node, CaseBlockSyntax).Statements
                End Select
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Given an expression within a tree of <see cref="ConditionalAccessExpressionSyntax"/>s, 
        ''' finds the <see cref="ConditionalAccessExpressionSyntax"/> that it is part of.
        ''' </summary>
        ''' <param name="node"></param>
        ''' <returns></returns>
        <Extension>
        Friend Function GetCorrespondingConditionalAccessExpression(node As ExpressionSyntax) As ConditionalAccessExpressionSyntax
            Dim access As SyntaxNode = node
            Dim parent As SyntaxNode = access.Parent

            While parent IsNot Nothing
                Select Case parent.Kind
                    Case SyntaxKind.DictionaryAccessExpression,
                         SyntaxKind.SimpleMemberAccessExpression

                        If DirectCast(parent, MemberAccessExpressionSyntax).Expression IsNot access Then
                            Return Nothing
                        End If

                    Case SyntaxKind.XmlElementAccessExpression,
                         SyntaxKind.XmlDescendantAccessExpression,
                         SyntaxKind.XmlAttributeAccessExpression

                        If DirectCast(parent, XmlMemberAccessExpressionSyntax).Base IsNot access Then
                            Return Nothing
                        End If

                    Case SyntaxKind.InvocationExpression

                        If DirectCast(parent, InvocationExpressionSyntax).Expression IsNot access Then
                            Return Nothing
                        End If

                    Case SyntaxKind.ConditionalAccessExpression

                        Dim conditional = DirectCast(parent, ConditionalAccessExpressionSyntax)
                        If conditional.WhenNotNull Is access Then
                            Return conditional
                        ElseIf conditional.Expression IsNot access Then
                            Return Nothing
                        End If

                    Case Else
                        Return Nothing
                End Select

                access = parent
                parent = access.Parent
            End While

            Return Nothing
        End Function

        <Extension()>
        Public Function GetContainingExecutableBlocks(node As SyntaxNode) As IEnumerable(Of SyntaxNode)
            Return node.GetAncestorsOrThis(Of StatementSyntax).
                        Where(Function(s) s.Parent.IsExecutableBlock() AndAlso s.Parent.GetExecutableBlockStatements().Contains(s)).
                        Select(Function(s) s.Parent)
        End Function

    End Module
End Namespace
