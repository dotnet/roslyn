' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        Friend Overrides Function ProcessSyntax(node As VisualBasicSyntaxNode) As BlockContext
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

        Friend Overrides Function TryLinkSyntax(node As VisualBasicSyntaxNode, ByRef newContext As BlockContext) As LinkResult
            If _parser.IsScript Then
                Return TryLinkStatement(node, newContext)
            Else
                Return MyBase.TryLinkSyntax(node, newContext)
            End If
        End Function

        Friend Overrides Function CreateBlockSyntax(endStmt As StatementSyntax) As VisualBasicSyntaxNode
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Function CreateCompilationUnit(optionalTerminator As PunctuationSyntax,
                                              notClosedIfDirectives As ArrayBuilder(Of IfDirectiveTriviaSyntax),
                                              notClosedRegionDirectives As ArrayBuilder(Of RegionDirectiveTriviaSyntax),
                                              haveRegionDirectives As Boolean,
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

            Dim regionsAreAllowedEverywhere = Not haveRegionDirectives OrElse Parser.CheckFeatureAvailability(Feature.RegionsEverywhere)

            If notClosedIfDirectives IsNot Nothing OrElse notClosedRegionDirectives IsNot Nothing OrElse notClosedExternalSourceDirective IsNot Nothing OrElse
               Not regionsAreAllowedEverywhere Then
                result = DiagnosticRewriter.Rewrite(result, notClosedIfDirectives, notClosedRegionDirectives, regionsAreAllowedEverywhere, notClosedExternalSourceDirective, Parser)

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
            Inherits VisualBasicSyntaxRewriter

            Private _notClosedIfDirectives As HashSet(Of IfDirectiveTriviaSyntax) = Nothing
            Private _notClosedRegionDirectives As HashSet(Of RegionDirectiveTriviaSyntax) = Nothing
            Private _notClosedExternalSourceDirective As ExternalSourceDirectiveTriviaSyntax = Nothing
            Private _regionsAreAllowedEverywhere As Boolean

            Private _parser As Parser
            Private _declarationBlocksBeingVisited As ArrayBuilder(Of VisualBasicSyntaxNode) ' CompilationUnitSyntax is treated as a declaration block for our purposes
            Private _parentsOfRegionDirectivesAwaitingClosure As ArrayBuilder(Of VisualBasicSyntaxNode) ' Nodes are coming from _declrationBlocksBeingVisited
            Private _tokenWithDirectivesBeingVisited As SyntaxToken

            Private Sub New()
                MyBase.New()
            End Sub

            Public Shared Function Rewrite(compilationUnit As CompilationUnitSyntax,
                                           notClosedIfDirectives As ArrayBuilder(Of IfDirectiveTriviaSyntax),
                                           notClosedRegionDirectives As ArrayBuilder(Of RegionDirectiveTriviaSyntax),
                                           regionsAreAllowedEverywhere As Boolean,
                                           notClosedExternalSourceDirective As ExternalSourceDirectiveTriviaSyntax,
                                           parser As Parser) As CompilationUnitSyntax

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

                rewriter._parser = parser
                rewriter._regionsAreAllowedEverywhere = regionsAreAllowedEverywhere

                If Not regionsAreAllowedEverywhere Then
                    rewriter._declarationBlocksBeingVisited = ArrayBuilder(Of VisualBasicSyntaxNode).GetInstance()
                    rewriter._parentsOfRegionDirectivesAwaitingClosure = ArrayBuilder(Of VisualBasicSyntaxNode).GetInstance()
                End If

                rewriter._notClosedExternalSourceDirective = notClosedExternalSourceDirective

                Dim result = DirectCast(rewriter.Visit(compilationUnit), CompilationUnitSyntax)

                If Not regionsAreAllowedEverywhere Then
                    Debug.Assert(rewriter._declarationBlocksBeingVisited.Count = 0)
                    Debug.Assert(rewriter._parentsOfRegionDirectivesAwaitingClosure.Count = 0) ' We never add parents of not closed #Region directives into this stack.
                    rewriter._declarationBlocksBeingVisited.Free()
                    rewriter._parentsOfRegionDirectivesAwaitingClosure.Free()
                End If

                Return result
            End Function

#If DEBUG Then
            ' NOTE: the logic is heavily relying on the fact that green nodes in 
            ' NOTE: one single tree are not reused, the following code assert this
            Private ReadOnly _processedNodesWithoutDuplication As HashSet(Of VisualBasicSyntaxNode) = New HashSet(Of VisualBasicSyntaxNode)(ReferenceEqualityComparer.Instance)
#End If

            Public Overrides Function VisitCompilationUnit(node As CompilationUnitSyntax) As VisualBasicSyntaxNode
                If _declarationBlocksBeingVisited IsNot Nothing Then
                    _declarationBlocksBeingVisited.Push(node)
                End If

                Dim result = MyBase.VisitCompilationUnit(node)

                If _declarationBlocksBeingVisited IsNot Nothing Then
                    Dim n = _declarationBlocksBeingVisited.Pop()
                    Debug.Assert(n Is node)
                End If

                Return result
            End Function

            Public Overrides Function VisitMethodBlock(node As MethodBlockSyntax) As VisualBasicSyntaxNode
                If _declarationBlocksBeingVisited IsNot Nothing Then
                    _declarationBlocksBeingVisited.Push(node)
                End If

                Dim result = MyBase.VisitMethodBlock(node)

                If _declarationBlocksBeingVisited IsNot Nothing Then
                    Dim n = _declarationBlocksBeingVisited.Pop()
                    Debug.Assert(n Is node)
                End If

                Return result
            End Function

            Public Overrides Function VisitConstructorBlock(node As ConstructorBlockSyntax) As VisualBasicSyntaxNode
                If _declarationBlocksBeingVisited IsNot Nothing Then
                    _declarationBlocksBeingVisited.Push(node)
                End If

                Dim result = MyBase.VisitConstructorBlock(node)

                If _declarationBlocksBeingVisited IsNot Nothing Then
                    Dim n = _declarationBlocksBeingVisited.Pop()
                    Debug.Assert(n Is node)
                End If

                Return result
            End Function

            Public Overrides Function VisitOperatorBlock(node As OperatorBlockSyntax) As VisualBasicSyntaxNode
                If _declarationBlocksBeingVisited IsNot Nothing Then
                    _declarationBlocksBeingVisited.Push(node)
                End If

                Dim result = MyBase.VisitOperatorBlock(node)

                If _declarationBlocksBeingVisited IsNot Nothing Then
                    Dim n = _declarationBlocksBeingVisited.Pop()
                    Debug.Assert(n Is node)
                End If

                Return result
            End Function

            Public Overrides Function VisitAccessorBlock(node As AccessorBlockSyntax) As VisualBasicSyntaxNode
                If _declarationBlocksBeingVisited IsNot Nothing Then
                    _declarationBlocksBeingVisited.Push(node)
                End If

                Dim result = MyBase.VisitAccessorBlock(node)

                If _declarationBlocksBeingVisited IsNot Nothing Then
                    Dim n = _declarationBlocksBeingVisited.Pop()
                    Debug.Assert(n Is node)
                End If

                Return result
            End Function

            Public Overrides Function VisitNamespaceBlock(node As NamespaceBlockSyntax) As VisualBasicSyntaxNode
                If _declarationBlocksBeingVisited IsNot Nothing Then
                    _declarationBlocksBeingVisited.Push(node)
                End If

                Dim result = MyBase.VisitNamespaceBlock(node)

                If _declarationBlocksBeingVisited IsNot Nothing Then
                    Dim n = _declarationBlocksBeingVisited.Pop()
                    Debug.Assert(n Is node)
                End If

                Return result
            End Function

            Public Overrides Function VisitClassBlock(node As ClassBlockSyntax) As VisualBasicSyntaxNode
                If _declarationBlocksBeingVisited IsNot Nothing Then
                    _declarationBlocksBeingVisited.Push(node)
                End If

                Dim result = MyBase.VisitClassBlock(node)

                If _declarationBlocksBeingVisited IsNot Nothing Then
                    Dim n = _declarationBlocksBeingVisited.Pop()
                    Debug.Assert(n Is node)
                End If

                Return result
            End Function

            Public Overrides Function VisitStructureBlock(node As StructureBlockSyntax) As VisualBasicSyntaxNode
                If _declarationBlocksBeingVisited IsNot Nothing Then
                    _declarationBlocksBeingVisited.Push(node)
                End If

                Dim result = MyBase.VisitStructureBlock(node)

                If _declarationBlocksBeingVisited IsNot Nothing Then
                    Dim n = _declarationBlocksBeingVisited.Pop()
                    Debug.Assert(n Is node)
                End If

                Return result
            End Function

            Public Overrides Function VisitModuleBlock(node As ModuleBlockSyntax) As VisualBasicSyntaxNode
                If _declarationBlocksBeingVisited IsNot Nothing Then
                    _declarationBlocksBeingVisited.Push(node)
                End If

                Dim result = MyBase.VisitModuleBlock(node)

                If _declarationBlocksBeingVisited IsNot Nothing Then
                    Dim n = _declarationBlocksBeingVisited.Pop()
                    Debug.Assert(n Is node)
                End If

                Return result
            End Function

            Public Overrides Function VisitInterfaceBlock(node As InterfaceBlockSyntax) As VisualBasicSyntaxNode
                If _declarationBlocksBeingVisited IsNot Nothing Then
                    _declarationBlocksBeingVisited.Push(node)
                End If

                Dim result = MyBase.VisitInterfaceBlock(node)

                If _declarationBlocksBeingVisited IsNot Nothing Then
                    Dim n = _declarationBlocksBeingVisited.Pop()
                    Debug.Assert(n Is node)
                End If

                Return result
            End Function

            Public Overrides Function VisitEnumBlock(node As EnumBlockSyntax) As VisualBasicSyntaxNode
                If _declarationBlocksBeingVisited IsNot Nothing Then
                    _declarationBlocksBeingVisited.Push(node)
                End If

                Dim result = MyBase.VisitEnumBlock(node)

                If _declarationBlocksBeingVisited IsNot Nothing Then
                    Dim n = _declarationBlocksBeingVisited.Pop()
                    Debug.Assert(n Is node)
                End If

                Return result
            End Function

            Public Overrides Function VisitPropertyBlock(node As PropertyBlockSyntax) As VisualBasicSyntaxNode
                If _declarationBlocksBeingVisited IsNot Nothing Then
                    _declarationBlocksBeingVisited.Push(node)
                End If

                Dim result = MyBase.VisitPropertyBlock(node)

                If _declarationBlocksBeingVisited IsNot Nothing Then
                    Dim n = _declarationBlocksBeingVisited.Pop()
                    Debug.Assert(n Is node)
                End If

                Return result
            End Function

            Public Overrides Function VisitEventBlock(node As EventBlockSyntax) As VisualBasicSyntaxNode
                If _declarationBlocksBeingVisited IsNot Nothing Then
                    _declarationBlocksBeingVisited.Push(node)
                End If

                Dim result = MyBase.VisitEventBlock(node)

                If _declarationBlocksBeingVisited IsNot Nothing Then
                    Dim n = _declarationBlocksBeingVisited.Pop()
                    Debug.Assert(n Is node)
                End If

                Return result
            End Function

            Public Overrides Function VisitIfDirectiveTrivia(node As IfDirectiveTriviaSyntax) As VisualBasicSyntaxNode
#If DEBUG Then
                Debug.Assert(_processedNodesWithoutDuplication.Add(node))
#End If
                Dim rewritten = MyBase.VisitIfDirectiveTrivia(node)
                If Me._notClosedIfDirectives IsNot Nothing AndAlso Me._notClosedIfDirectives.Contains(node) Then
                    rewritten = Parser.ReportSyntaxError(rewritten, ERRID.ERR_LbExpectedEndIf)
                End If

                Return rewritten
            End Function

            Public Overrides Function VisitRegionDirectiveTrivia(node As RegionDirectiveTriviaSyntax) As VisualBasicSyntaxNode
#If DEBUG Then
                Debug.Assert(_processedNodesWithoutDuplication.Add(node))
#End If
                Dim rewritten = MyBase.VisitRegionDirectiveTrivia(node)
                If Me._notClosedRegionDirectives IsNot Nothing AndAlso Me._notClosedRegionDirectives.Contains(node) Then
                    rewritten = Parser.ReportSyntaxError(rewritten, ERRID.ERR_ExpectedEndRegion)

                ElseIf Not _regionsAreAllowedEverywhere
                    rewritten = VerifyRegionPlacement(node, rewritten)
                End If

                Return rewritten
            End Function


            Private Function VerifyRegionPlacement(original As VisualBasicSyntaxNode, rewritten As VisualBasicSyntaxNode) As VisualBasicSyntaxNode
                Dim containingBlock = _declarationBlocksBeingVisited.Peek()

                ' Ensure that the directive is inside the block, rather than is attached to it as a leading/trailing trivia 
                Debug.Assert(_declarationBlocksBeingVisited.Count > 1 OrElse containingBlock.Kind = SyntaxKind.CompilationUnit)

                If _declarationBlocksBeingVisited.Count > 1 Then

                    If _tokenWithDirectivesBeingVisited Is containingBlock.GetFirstToken() Then
                        Dim leadingTrivia = _tokenWithDirectivesBeingVisited.GetLeadingTrivia()

                        If leadingTrivia IsNot Nothing AndAlso New SyntaxList(Of VisualBasicSyntaxNode)(leadingTrivia).Nodes.Contains(original) Then
                            containingBlock = _declarationBlocksBeingVisited(_declarationBlocksBeingVisited.Count - 2)
                        End If

                    ElseIf _tokenWithDirectivesBeingVisited Is containingBlock.GetLastToken() Then
                        Dim trailingTrivia = _tokenWithDirectivesBeingVisited.GetTrailingTrivia()

                        If trailingTrivia IsNot Nothing AndAlso New SyntaxList(Of VisualBasicSyntaxNode)(trailingTrivia).Nodes.Contains(original) Then
                            containingBlock = _declarationBlocksBeingVisited(_declarationBlocksBeingVisited.Count - 2)
                        End If
                    End If
                End If

                Dim reportAnError = Not IsValidContainingBlockForRegionInVB12(containingBlock)

                If original.Kind = SyntaxKind.RegionDirectiveTrivia Then
                    _parentsOfRegionDirectivesAwaitingClosure.Push(containingBlock)
                Else
                    Debug.Assert(original.Kind = SyntaxKind.EndRegionDirectiveTrivia)

                    If _parentsOfRegionDirectivesAwaitingClosure.Count > 0 Then
                        Dim regionBeginContainingBlock = _parentsOfRegionDirectivesAwaitingClosure.Pop()

                        If regionBeginContainingBlock IsNot containingBlock AndAlso IsValidContainingBlockForRegionInVB12(regionBeginContainingBlock) Then
                            reportAnError = True
                        End If
                    End If
                End If

                If reportAnError Then
                    rewritten = _parser.ReportFeatureUnavailable(Feature.RegionsEverywhere, rewritten)
                End If

                Return rewritten
            End Function

            Private Shared Function IsValidContainingBlockForRegionInVB12(containingBlock As VisualBasicSyntaxNode) As Boolean
                Select Case containingBlock.Kind
                    Case SyntaxKind.FunctionBlock,
                         SyntaxKind.SubBlock,
                         SyntaxKind.ConstructorBlock,
                         SyntaxKind.OperatorBlock,
                         SyntaxKind.SetAccessorBlock,
                         SyntaxKind.GetAccessorBlock,
                         SyntaxKind.AddHandlerAccessorBlock,
                         SyntaxKind.RemoveHandlerAccessorBlock,
                         SyntaxKind.RaiseEventAccessorBlock

                        Return False
                End Select

                Return True
            End Function

            Public Overrides Function VisitEndRegionDirectiveTrivia(node As EndRegionDirectiveTriviaSyntax) As VisualBasicSyntaxNode
#If DEBUG Then
                Debug.Assert(_processedNodesWithoutDuplication.Add(node))
#End If
                Dim rewritten = MyBase.VisitEndRegionDirectiveTrivia(node)

                If Not _regionsAreAllowedEverywhere Then
                    rewritten = VerifyRegionPlacement(node, rewritten)
                End If

                Return rewritten
            End Function

            Public Overrides Function VisitExternalSourceDirectiveTrivia(node As ExternalSourceDirectiveTriviaSyntax) As VisualBasicSyntaxNode
#If DEBUG Then
                Debug.Assert(_processedNodesWithoutDuplication.Add(node))
#End If
                Dim rewritten = MyBase.VisitExternalSourceDirectiveTrivia(node)
                If Me._notClosedExternalSourceDirective Is node Then
                    rewritten = Parser.ReportSyntaxError(rewritten, ERRID.ERR_ExpectedEndExternalSource)
                End If
                Return rewritten
            End Function

            Public Overrides Function Visit(node As VisualBasicSyntaxNode) As VisualBasicSyntaxNode
                If node Is Nothing OrElse Not node.ContainsDirectives Then
                    Return node
                End If

                Return node.Accept(Me)
            End Function

            Public Overrides Function VisitSyntaxToken(token As SyntaxToken) As SyntaxToken
                If token Is Nothing OrElse Not token.ContainsDirectives Then
                    Return token
                End If

                Debug.Assert(_tokenWithDirectivesBeingVisited Is Nothing)
                _tokenWithDirectivesBeingVisited = token

                Dim leadingTrivia = token.GetLeadingTrivia()
                Dim trailingTrivia = token.GetTrailingTrivia()

                If leadingTrivia IsNot Nothing Then
                    Dim rewritten = VisitList(New SyntaxList(Of VisualBasicSyntaxNode)(leadingTrivia)).Node
                    If leadingTrivia IsNot rewritten Then
                        token = DirectCast(token.WithLeadingTrivia(rewritten), SyntaxToken)
                    End If
                End If

                If trailingTrivia IsNot Nothing Then
                    Dim rewritten = VisitList(New SyntaxList(Of VisualBasicSyntaxNode)(trailingTrivia)).Node
                    If trailingTrivia IsNot rewritten Then
                        token = DirectCast(token.WithTrailingTrivia(rewritten), SyntaxToken)
                    End If
                End If

                _tokenWithDirectivesBeingVisited = Nothing
                Return token
            End Function

        End Class
    End Class

End Namespace
