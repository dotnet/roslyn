' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Public Class VisualBasicCompilation
        Partial Friend Class DocumentationCommentCompiler
            Inherits VisualBasicSymbolVisitor

            Private Class MislocatedDocumentationCommentFinder
                Inherits VisualBasicSyntaxWalker

                Private ReadOnly _diagnostics As DiagnosticBag
                Private ReadOnly _filterSpanWithinTree As TextSpan?
                Private ReadOnly _cancellationToken As CancellationToken

                Private _isInsideMethodOrLambda As Boolean

                Private Sub New(diagnostics As DiagnosticBag,
                                filterSpanWithinTree As TextSpan?,
                                cancellationToken As CancellationToken)

                    MyBase.New(SyntaxWalkerDepth.Trivia)

                    Debug.Assert(diagnostics IsNot Nothing)
                    Me._diagnostics = diagnostics
                    Me._filterSpanWithinTree = filterSpanWithinTree
                    Me._cancellationToken = cancellationToken

                    Me._isInsideMethodOrLambda = False
                End Sub

                Public Shared Sub ReportUnprocessed(tree As SyntaxTree, filterSpanWithinTree As TextSpan?, diagnostics As DiagnosticBag, cancellationToken As CancellationToken)
                    If tree.ReportDocumentationCommentDiagnostics() Then
                        Dim finder As New MislocatedDocumentationCommentFinder(diagnostics, filterSpanWithinTree, cancellationToken)
                        finder.Visit(tree.GetRoot(cancellationToken))
                    End If
                End Sub

                Private Function IsSyntacticallyFilteredOut(fullSpan As TextSpan) As Boolean
                    Return Me._filterSpanWithinTree.HasValue AndAlso Not Me._filterSpanWithinTree.Value.Contains(fullSpan)
                End Function

                Public Overrides Sub VisitMethodBlock(node As MethodBlockSyntax)
                    VisitMethodBlockBase(node)
                End Sub

                Public Overrides Sub VisitConstructorBlock(node As ConstructorBlockSyntax)
                    VisitMethodBlockBase(node)
                End Sub

                Public Overrides Sub VisitOperatorBlock(node As OperatorBlockSyntax)
                    VisitMethodBlockBase(node)
                End Sub

                Public Overrides Sub VisitAccessorBlock(node As AccessorBlockSyntax)
                    VisitMethodBlockBase(node)
                End Sub

                Private Sub VisitMethodBlockBase(node As Syntax.MethodBlockBaseSyntax)
                    Me._cancellationToken.ThrowIfCancellationRequested()

                    If IsSyntacticallyFilteredOut(node.FullSpan) Then
                        Return
                    End If

                    Dim stored = Me._isInsideMethodOrLambda

                    ' Visit block start statement
                    Me._isInsideMethodOrLambda = False
                    Me.Visit(node.BlockStatement)

                    ' Visit the rest 
                    Me._isInsideMethodOrLambda = True
                    Me.DefaultVisitChildrenStartingWith(node, 1)

                    Me._isInsideMethodOrLambda = stored
                End Sub

                Public Overrides Sub VisitMultiLineLambdaExpression(node As Syntax.MultiLineLambdaExpressionSyntax)
                    Me._cancellationToken.ThrowIfCancellationRequested()

                    If IsSyntacticallyFilteredOut(node.FullSpan) Then
                        Return
                    End If

                    Dim stored = Me._isInsideMethodOrLambda
                    Me._isInsideMethodOrLambda = True  ' Any doc comment inside this block is an error

                    MyBase.VisitMultiLineLambdaExpression(node)

                    Me._isInsideMethodOrLambda = stored
                End Sub

                Public Overrides Sub DefaultVisit(node As SyntaxNode)
                    ' Short-circuit traversal if we know there are no documentation comments below.
                    If node.HasStructuredTrivia AndAlso Not IsSyntacticallyFilteredOut(node.FullSpan) Then
                        MyBase.DefaultVisit(node)
                    End If
                End Sub

                Private Sub DefaultVisitChildrenStartingWith(node As SyntaxNode, start As Integer)
                    Dim list = node.ChildNodesAndTokens()
                    Dim childCnt = list.Count

                    Dim i As Integer = start
                    While i < childCnt
                        Dim child = list(i)
                        i = i + 1

                        Dim asNode = child.AsNode()
                        If asNode IsNot Nothing Then
                            Me.Visit(asNode)
                        Else
                            Me.VisitToken(child.AsToken())
                        End If
                    End While
                End Sub

                Public Overrides Sub VisitTrivia(trivia As SyntaxTrivia)
                    If IsSyntacticallyFilteredOut(trivia.FullSpan) Then
                        Return
                    End If

                    If trivia.Kind = SyntaxKind.DocumentationCommentTrivia Then
                        If Me._isInsideMethodOrLambda Then
                            Me._diagnostics.Add(ERRID.WRN_XMLDocInsideMethod, trivia.GetLocation())

                        Else
                            Dim parent As VisualBasicSyntaxNode = DirectCast(trivia.Token.Parent, VisualBasicSyntaxNode)
lAgain:
                            Debug.Assert(parent IsNot Nothing)
                            Select Case parent.Kind
                                Case SyntaxKind.ClassStatement,
                                     SyntaxKind.EnumStatement,
                                     SyntaxKind.InterfaceStatement,
                                     SyntaxKind.StructureStatement,
                                     SyntaxKind.ModuleStatement,
                                     SyntaxKind.SubStatement,
                                     SyntaxKind.SubNewStatement,
                                     SyntaxKind.FunctionStatement,
                                     SyntaxKind.DelegateSubStatement,
                                     SyntaxKind.DelegateFunctionStatement,
                                     SyntaxKind.DeclareSubStatement,
                                     SyntaxKind.DeclareFunctionStatement,
                                     SyntaxKind.OperatorStatement,
                                     SyntaxKind.PropertyStatement,
                                     SyntaxKind.EventStatement,
                                     SyntaxKind.FieldDeclaration,
                                     SyntaxKind.EnumMemberDeclaration
                                    ' Do nothing, the comment is properly located

                                Case SyntaxKind.AttributeList
                                    parent = parent.Parent
                                    GoTo lAgain

                                Case Else
                                    Me._diagnostics.Add(ERRID.WRN_XMLDocWithoutLanguageElement, trivia.GetLocation())

                            End Select
                        End If
                    End If

                    MyBase.VisitTrivia(trivia)
                End Sub

            End Class

        End Class
    End Class
End Namespace
