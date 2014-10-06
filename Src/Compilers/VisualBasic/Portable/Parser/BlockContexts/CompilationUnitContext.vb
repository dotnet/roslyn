' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
'-----------------------------------------------------------------------------
' Contains the definition of the DeclarationContext
'-----------------------------------------------------------------------------

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend NotInheritable Class CompilationUnitContext
        Inherits NamespaceBlockContext

        Private _optionStmts As SyntaxList(Of OptionStatementSyntax)
        Private _importsStmts As SyntaxList(Of ImportsStatementSyntax)
        Private _attributeStmts As SyntaxList(Of AttributesStatementSyntax)
        Private _state As SyntaxKind

        Friend Sub New(parser As Parser)
            MyBase.New(SyntaxKind.CompilationUnit, Nothing, Nothing)

            Me.Parser = parser
            _statements = _parser._pool.Allocate(Of StatementSyntax)()
            _state = SyntaxKind.OptionStatement
        End Sub

        Friend Overrides ReadOnly Property IsWithinAsyncMethodOrLambda As Boolean
            Get
                Return Parser.IsScript
            End Get
        End Property

        Friend Overrides Function ProcessSyntax(node As VBSyntaxNode) As BlockContext
            Do
                Select Case _state
                    Case SyntaxKind.OptionStatement
                        If node.Kind = SyntaxKind.OptionStatement Then
                            Add(node)
                            Return Me
                        End If
                        _optionStmts = New SyntaxList(Of OptionStatementSyntax)(Body.Node)
                        _state = SyntaxKind.ImportsStatement

                    Case SyntaxKind.ImportsStatement
                        If node.Kind = SyntaxKind.ImportsStatement Then
                            Add(node)
                            Return Me
                        End If
                        _importsStmts = New SyntaxList(Of ImportsStatementSyntax)(Body.Node)
                        _state = SyntaxKind.AttributesStatement

                    Case SyntaxKind.AttributesStatement
                        If node.Kind = SyntaxKind.AttributesStatement Then
                            Add(node)
                            Return Me
                        End If
                        _attributeStmts = New SyntaxList(Of AttributesStatementSyntax)(Body.Node)
                        _state = SyntaxKind.None

                    Case Else
                        ' only allow executable statements in top-level script code
                        If _parser.IsScript Then
                            Dim newContext = TryProcessExecutableStatement(node)
                            If newContext IsNot Nothing Then
                                Return newContext
                            End If
                        End If

                        Return MyBase.ProcessSyntax(node)
                End Select
            Loop
        End Function

        Friend Overrides Function TryLinkSyntax(node As VBSyntaxNode, ByRef newContext As BlockContext) As LinkResult
            If _parser.IsScript Then
                Return TryLinkStatement(node, newContext)
            Else
                Return MyBase.TryLinkSyntax(node, newContext)
            End If
        End Function

        Friend Overrides Function CreateBlockSyntax(endStmt As StatementSyntax) As VBSyntaxNode
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Function CreateCompilationUnit(optionalTerminator As PunctuationSyntax,
                                              notClosedIfDirectives As ArrayBuilder(Of IfDirectiveTriviaSyntax),
                                              notClosedRegionDirectives As ArrayBuilder(Of RegionDirectiveTriviaSyntax),
                                              notClosedExternalSourceDirective As ExternalSourceDirectiveTriviaSyntax) As CompilationUnitSyntax

            Debug.Assert(optionalTerminator Is Nothing OrElse optionalTerminator.Kind = SyntaxKind.EndOfFileToken)

            If _state <> SyntaxKind.None Then
                Select Case _state
                    Case SyntaxKind.OptionStatement
                        _optionStmts = New SyntaxList(Of OptionStatementSyntax)(Body.Node)

                    Case SyntaxKind.ImportsStatement
                        _importsStmts = New SyntaxList(Of ImportsStatementSyntax)(Body.Node)

                    Case SyntaxKind.AttributesStatement
                        _attributeStmts = New SyntaxList(Of AttributesStatementSyntax)(Body.Node)
                End Select
                _state = SyntaxKind.None
            End If

            Dim declarations = Body()

            Dim result = SyntaxFactory.CompilationUnit(_optionStmts,
                                                       _importsStmts,
                                                       _attributeStmts,
                                                       declarations,
                                                       optionalTerminator)

            If notClosedIfDirectives IsNot Nothing OrElse notClosedRegionDirectives IsNot Nothing OrElse notClosedExternalSourceDirective IsNot Nothing Then
                result = DiagnosticRewriter.Rewrite(result, notClosedIfDirectives, notClosedRegionDirectives, notClosedExternalSourceDirective)

                If notClosedIfDirectives IsNot Nothing Then
                    notClosedIfDirectives.Free()
                End If
                If notClosedRegionDirectives IsNot Nothing Then
                    notClosedRegionDirectives.Free()
                End If
            End If

            FreeStatements()
            Return result
        End Function

        Private Class DiagnosticRewriter
            Inherits VBSyntaxRewriter

            Private _notClosedIfDirectives As HashSet(Of IfDirectiveTriviaSyntax) = Nothing
            Private _notClosedRegionDirectives As HashSet(Of RegionDirectiveTriviaSyntax) = Nothing
            Private _notClosedExternalSourceDirective As ExternalSourceDirectiveTriviaSyntax = Nothing

            Private Sub New()
                MyBase.New()
            End Sub

            Public Shared Function Rewrite(compilationUnit As CompilationUnitSyntax,
                                           notClosedIfDirectives As ArrayBuilder(Of IfDirectiveTriviaSyntax),
                                           notClosedRegionDirectives As ArrayBuilder(Of RegionDirectiveTriviaSyntax),
                                           notClosedExternalSourceDirective As ExternalSourceDirectiveTriviaSyntax) As CompilationUnitSyntax

                Dim rewriter As New DiagnosticRewriter()

                If notClosedIfDirectives IsNot Nothing Then
                    rewriter._notClosedIfDirectives =
                        New HashSet(Of IfDirectiveTriviaSyntax)(ReferenceEqualityComparer.Instance)

                    For Each node In notClosedIfDirectives
                        rewriter._notClosedIfDirectives.Add(node)
                    Next
                End If

                If notClosedRegionDirectives IsNot Nothing Then
                    rewriter._notClosedRegionDirectives =
                        New HashSet(Of RegionDirectiveTriviaSyntax)(ReferenceEqualityComparer.Instance)

                    For Each node In notClosedRegionDirectives
                        rewriter._notClosedRegionDirectives.Add(node)
                    Next
                End If

                rewriter._notClosedExternalSourceDirective = notClosedExternalSourceDirective

                Return DirectCast(rewriter.Visit(compilationUnit), CompilationUnitSyntax)
            End Function

#If DEBUG Then
            ' NOTE: the logic is heavily relying on the fact that green nodes in 
            ' NOTE: one single tree are not reused, the following code assert this
            Private ReadOnly _processedNodesWithoutDuplication As HashSet(Of VBSyntaxNode) = New HashSet(Of VBSyntaxNode)(ReferenceEqualityComparer.Instance)
#End If

            Public Overrides Function VisitIfDirectiveTrivia(node As IfDirectiveTriviaSyntax) As VBSyntaxNode
#If DEBUG Then
                Debug.Assert(_processedNodesWithoutDuplication.Add(node))
#End If
                Dim rewritten = MyBase.VisitIfDirectiveTrivia(node)
                If Me._notClosedIfDirectives IsNot Nothing AndAlso Me._notClosedIfDirectives.Contains(node) Then
                    rewritten = Parser.ReportSyntaxError(rewritten, ERRID.ERR_LbExpectedEndIf)
                End If
                Return rewritten
            End Function

            Public Overrides Function VisitRegionDirectiveTrivia(node As RegionDirectiveTriviaSyntax) As VBSyntaxNode
#If DEBUG Then
                Debug.Assert(_processedNodesWithoutDuplication.Add(node))
#End If
                Dim rewritten = MyBase.VisitRegionDirectiveTrivia(node)
                If Me._notClosedRegionDirectives IsNot Nothing AndAlso Me._notClosedRegionDirectives.Contains(node) Then
                    rewritten = Parser.ReportSyntaxError(rewritten, ERRID.ERR_ExpectedEndRegion)
                End If
                Return rewritten
            End Function

            Public Overrides Function VisitExternalSourceDirectiveTrivia(node As ExternalSourceDirectiveTriviaSyntax) As VBSyntaxNode
#If DEBUG Then
                Debug.Assert(_processedNodesWithoutDuplication.Add(node))
#End If
                Dim rewritten = MyBase.VisitExternalSourceDirectiveTrivia(node)
                If Me._notClosedExternalSourceDirective Is node Then
                    rewritten = Parser.ReportSyntaxError(rewritten, ERRID.ERR_ExpectedEndExternalSource)
                End If
                Return rewritten
            End Function

            Public Overrides Function Visit(node As VBSyntaxNode) As VBSyntaxNode
                If node Is Nothing OrElse Not node.ContainsDirectives Then
                    Return node
                End If

                Return node.Accept(Me)
            End Function

            Public Overrides Function VisitSyntaxToken(token As SyntaxToken) As SyntaxToken
                If token Is Nothing OrElse Not token.ContainsDirectives Then
                    Return token
                End If

                Dim leadingTrivia = token.GetLeadingTrivia()
                Dim trailingTrivia = token.GetTrailingTrivia()

                If leadingTrivia IsNot Nothing Then
                    Dim rewritten = VisitList(New SyntaxList(Of VBSyntaxNode)(leadingTrivia)).Node
                    If leadingTrivia IsNot rewritten Then
                        token = DirectCast(token.WithLeadingTrivia(rewritten), SyntaxToken)
                    End If
                End If

                If trailingTrivia IsNot Nothing Then
                    Dim rewritten = VisitList(New SyntaxList(Of VBSyntaxNode)(trailingTrivia)).Node
                    If trailingTrivia IsNot rewritten Then
                        token = DirectCast(token.WithTrailingTrivia(rewritten), SyntaxToken)
                    End If
                End If

                Return token
            End Function

        End Class
    End Class

End Namespace