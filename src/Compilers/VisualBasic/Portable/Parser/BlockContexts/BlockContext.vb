' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'-----------------------------------------------------------------------------
' Contains the definition of the BlockContext
'-----------------------------------------------------------------------------
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports InternalSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend MustInherit Class BlockContext
        Implements ISyntaxFactoryContext

        Private _beginStatement As StatementSyntax

        Protected _parser As Parser
        Protected _statements As SyntaxListBuilder(Of StatementSyntax)

        Private ReadOnly _kind As SyntaxKind
        Private ReadOnly _endKind As SyntaxKind
        Private ReadOnly _prev As BlockContext
        Private ReadOnly _isWithinMultiLineLambda As Boolean
        Private ReadOnly _isWithinSingleLineLambda As Boolean
        Private ReadOnly _isWithinAsyncMethodOrLambda As Boolean
        Private ReadOnly _isWithinIteratorMethodOrLambdaOrProperty As Boolean
        Private ReadOnly _level As Integer
        Private ReadOnly _syntaxFactory As ContextAwareSyntaxFactory

        Protected Sub New(kind As SyntaxKind, statement As StatementSyntax, prev As BlockContext)
            _beginStatement = statement
            _kind = kind
            _prev = prev
            _syntaxFactory = New ContextAwareSyntaxFactory(Me)

            If prev IsNot Nothing Then
                _isWithinSingleLineLambda = prev._isWithinSingleLineLambda
                _isWithinMultiLineLambda = prev._isWithinMultiLineLambda
            End If

            If Not _isWithinSingleLineLambda Then
                _isWithinSingleLineLambda = SyntaxFacts.IsSingleLineLambdaExpression(_kind)
            End If

            If Not _isWithinMultiLineLambda Then
                _isWithinMultiLineLambda = SyntaxFacts.IsMultiLineLambdaExpression(_kind)
            End If

            Select Case _kind

                Case SyntaxKind.PropertyBlock

                    _isWithinIteratorMethodOrLambdaOrProperty = DirectCast(statement, PropertyStatementSyntax).Modifiers.Any(SyntaxKind.IteratorKeyword)

                Case SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock

                    Debug.Assert(_prev IsNot Nothing)
                    _isWithinIteratorMethodOrLambdaOrProperty = _prev.IsWithinIteratorMethodOrLambdaOrProperty

                Case SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock

                    _isWithinAsyncMethodOrLambda = DirectCast(statement, MethodStatementSyntax).Modifiers.Any(SyntaxKind.AsyncKeyword)
                    _isWithinIteratorMethodOrLambdaOrProperty = DirectCast(statement, MethodStatementSyntax).Modifiers.Any(SyntaxKind.IteratorKeyword)

                Case SyntaxKind.SingleLineSubLambdaExpression,
                        SyntaxKind.MultiLineSubLambdaExpression,
                        SyntaxKind.SingleLineFunctionLambdaExpression,
                        SyntaxKind.MultiLineFunctionLambdaExpression

                    _isWithinAsyncMethodOrLambda = DirectCast(statement, LambdaHeaderSyntax).Modifiers.Any(SyntaxKind.AsyncKeyword)
                    _isWithinIteratorMethodOrLambdaOrProperty = DirectCast(statement, LambdaHeaderSyntax).Modifiers.Any(SyntaxKind.IteratorKeyword)

                Case Else

                    If _prev IsNot Nothing Then
                        _isWithinAsyncMethodOrLambda = _prev.IsWithinAsyncMethodOrLambda
                        _isWithinIteratorMethodOrLambdaOrProperty = _prev.IsWithinIteratorMethodOrLambdaOrProperty
                    End If
            End Select


            _endKind = GetEndKind(kind)
            _level = If(prev IsNot Nothing, prev.Level + 1, 0)

            If prev IsNot Nothing Then
                _parser = prev.Parser
                _statements = _parser._pool.Allocate(Of StatementSyntax)()
            End If
        End Sub

        Friend ReadOnly Property BeginStatement As StatementSyntax
            Get
                Return _beginStatement
            End Get
        End Property

        Friend Sub GetBeginEndStatements(Of T1 As StatementSyntax, T2 As StatementSyntax)(ByRef beginStmt As T1, ByRef endStmt As T2)
            Debug.Assert(BeginStatement IsNot Nothing)

            beginStmt = DirectCast(BeginStatement, T1)

            If endStmt Is Nothing Then
                Dim errorId As ERRID
                endStmt = DirectCast(CreateMissingEnd(errorId), T2)

                If errorId <> Nothing Then
                    beginStmt = Parser.ReportSyntaxError(beginStmt, errorId)
                End If
            End If
        End Sub

        Friend Overridable Function KindEndsBlock(kind As SyntaxKind) As Boolean
            Return _endKind = kind
        End Function

        Friend ReadOnly Property IsLineIf As Boolean
            Get
                Return _kind = SyntaxKind.SingleLineIfStatement OrElse _kind = SyntaxKind.SingleLineElseClause
            End Get
        End Property

        Friend ReadOnly Property IsWithinLambda As Boolean
            Get
                Return _isWithinMultiLineLambda Or _isWithinSingleLineLambda
            End Get
        End Property

        Friend ReadOnly Property IsWithinSingleLineLambda As Boolean
            Get
                Return _isWithinSingleLineLambda
            End Get
        End Property

        Friend Overridable ReadOnly Property IsWithinAsyncMethodOrLambda As Boolean Implements ISyntaxFactoryContext.IsWithinAsyncMethodOrLambda
            Get
                Return _isWithinAsyncMethodOrLambda
            End Get
        End Property

        Friend Overridable ReadOnly Property IsWithinIteratorContext As Boolean Implements ISyntaxFactoryContext.IsWithinIteratorContext
            Get
                Return _isWithinIteratorMethodOrLambdaOrProperty
            End Get
        End Property

        Friend ReadOnly Property IsWithinIteratorMethodOrLambdaOrProperty As Boolean
            Get
                Return _isWithinIteratorMethodOrLambdaOrProperty
            End Get
        End Property

        'TODO - Remove dependency on Parser
        '  For errors call error function directly
        '  for parsing, just pass a delegate to the context
        Friend Property Parser As Parser
            Get
                Return _parser
            End Get
            Set(value As Parser)
                Debug.Assert(BlockKind = SyntaxKind.CompilationUnit)
                _parser = value
            End Set
        End Property

        Friend ReadOnly Property SyntaxFactory As ContextAwareSyntaxFactory
            Get
                Return _syntaxFactory
            End Get
        End Property

        Friend ReadOnly Property BlockKind As SyntaxKind
            Get
                Return _kind
            End Get
        End Property

        Friend ReadOnly Property PrevBlock As BlockContext
            Get
                Return _prev
            End Get
        End Property

        Friend ReadOnly Property Level As Integer
            Get
                Return _level
            End Get
        End Property

        Friend Sub Add(node As VisualBasicSyntaxNode)
            Debug.Assert(node IsNot Nothing)

            _statements.Add(DirectCast(node, StatementSyntax))
        End Sub

        Friend ReadOnly Property Statements As SyntaxListBuilder(Of StatementSyntax)
            Get
                Return _statements
            End Get
        End Property

        Friend Sub FreeStatements()
            _parser._pool.Free(_statements)
        End Sub

        Friend Function Body() As SyntaxList(Of StatementSyntax)
            Dim result = _statements.ToList()

            _statements.Clear()

            Return result
        End Function

        ''' <summary>
        ''' Returns the statement if there is exactly one in the body,
        ''' otherwise returns Nothing.
        ''' </summary>
        Friend Function SingleStatementOrDefault() As StatementSyntax
            Return If(_statements.Count = 1, _statements(0), Nothing)
        End Function

        ''' <summary>
        ''' Return an empty body if the body is a single, zero-width EmptyStatement,
        ''' otherwise returns the entire body.
        ''' </summary>
        Friend Function OptionalBody() As SyntaxList(Of StatementSyntax)
            Dim statement = SingleStatementOrDefault()

            If statement IsNot Nothing AndAlso
                statement.Kind = SyntaxKind.EmptyStatement AndAlso
                statement.FullWidth = 0 Then
                Return Nothing
            End If

            Return Body()
        End Function

        Friend Function Body(Of T As StatementSyntax)() As SyntaxList(Of T)
            Dim result = _statements.ToList(Of T)()

            _statements.Clear()

            Return result
        End Function

        ' Same as Body(), but use a SyntaxListWithManyChildren if the
        ' body is large enough, so we get red node with weak children.
        Friend Function BodyWithWeakChildren() As SyntaxList(Of StatementSyntax)
            If IsLargeEnoughNonEmptyStatementList(_statements) Then
                Dim result = New SyntaxList(Of StatementSyntax)(SyntaxList.List(CType(_statements, SyntaxListBuilder).ToArray))

                _statements.Clear()

                Return result
            Else
                Return Body()
            End If
        End Function

        ' Is this statement list non-empty, and large enough to make using weak children beneficial?
        Private Shared Function IsLargeEnoughNonEmptyStatementList(statements As SyntaxListBuilder(Of StatementSyntax)) As Boolean
            If statements.Count = 0 Then
                Return False
            ElseIf statements.Count <= 2 Then
                ' If we have a single statement (Count include separators), it might be small, like "return null", or large,
                ' like a loop or if or switch with many statements inside. Use the width as a proxy for
                ' how big it is. If it's small, its better to forgo a many children list anyway, since the
                ' weak reference would consume as much memory as is saved.
                Return statements(0).Width > 60
            Else
                ' For 2 or more statements, go ahead and create a many-children lists.
                Return True
            End If
        End Function

        Friend Function BaseDeclarations(Of T As InheritsOrImplementsStatementSyntax)() As SyntaxList(Of T)

            Dim result = _statements.ToList(Of T)()

            _statements.Clear()
            Return result
        End Function

        Friend MustOverride Function Parse() As StatementSyntax

        Friend MustOverride Function ProcessSyntax(syntax As VisualBasicSyntaxNode) As BlockContext

        Friend MustOverride Function CreateBlockSyntax(statement As StatementSyntax) As VisualBasicSyntaxNode

        Friend MustOverride Function EndBlock(statement As StatementSyntax) As BlockContext

        Friend MustOverride Function RecoverFromMismatchedEnd(statement As StatementSyntax) As BlockContext

        Friend Overridable Function ResyncAndProcessStatementTerminator(statement As StatementSyntax, lambdaContext As BlockContext) As BlockContext
            Dim unexpected = Parser.ResyncAt()
            HandleAnyUnexpectedTokens(statement, unexpected)
            Return ProcessStatementTerminator(lambdaContext)
        End Function

        Friend MustOverride Function ProcessStatementTerminator(lambdaContext As BlockContext) As BlockContext

        Friend Overridable Function ProcessElseAsStatementTerminator() As BlockContext
            ' Nothing to do. The Else should be processed as a
            ' statement associated with this context.
            Return Me
        End Function

        Friend Overridable Function ProcessOtherAsStatementTerminator() As BlockContext
            ' Nothing to do.
            Return Me
        End Function

        Friend MustOverride ReadOnly Property IsSingleLine As Boolean

        Friend Overridable ReadOnly Property IsLambda As Boolean
            Get
                Return False
            End Get
        End Property

        Private Sub HandleAnyUnexpectedTokens(currentStmt As StatementSyntax, unexpected As SyntaxList(Of SyntaxToken))
            If unexpected.Node Is Nothing Then
                Return
            End If

            Dim index As Integer
            Dim stmt As StatementSyntax

            If _statements.Count = 0 Then
                index = -1
                stmt = _beginStatement
            Else
                index = _statements.Count - 1
                stmt = _statements(index)
            End If

            Debug.Assert(stmt IsNot Nothing)

            If Not currentStmt.ContainsDiagnostics AndAlso Not unexpected.ContainsDiagnostics Then
                stmt = stmt.AddTrailingSyntax(unexpected, ERRID.ERR_ExpectedEOS)
            Else
                ' Don't report ERRID_ExpectedEOS when the statement is known to be bad
                stmt = stmt.AddTrailingSyntax(unexpected)
            End If

            If index = -1 Then
                _beginStatement = stmt
            Else
                _statements(index) = stmt
            End If
        End Sub

        <Flags()>
        Friend Enum LinkResult
            NotUsed = 0             ' The syntax cannot be used.  Force a reparse.
            Used = 1                ' Reuse the syntax.
            SkipTerminator = 2      ' Syntax is not followed by a statement terminator.
            MissingTerminator = 4   ' Statement terminator is missing.
            TerminatorFlags = 6     ' Combination of the above 2 flags.
            Crumble = 8             ' Crumble the syntax and try to reuse the parts.
        End Enum

        Friend MustOverride Function TryLinkSyntax(node As VisualBasicSyntaxNode, ByRef newContext As BlockContext) As LinkResult

        Friend Function LinkSyntax(node As VisualBasicSyntaxNode) As BlockContext
            Debug.Assert(node IsNot Nothing)

            Dim kind As SyntaxKind = node.Kind
            Dim context = Me

            While context IsNot Nothing
                If context.KindEndsBlock(kind) Then
                    ' Note, end statements in single line lambdas and single line if's can never close an outer context.
                    Dim scope = FindNearestLambdaOrSingleLineIf(context)
                    If scope IsNot Nothing Then
                        If scope.IsLambda Then
                            ' Don't allow end statements from outer blocks to terminate single line statement lambdas.
                            ' Single line if's have a special error for this case but single line lambdas don't.
                            Exit While
                        Else
                            ' Don't allow end statements from outer blocks to terminate single line ifs.
                            node = Parser.ReportSyntaxError(node, ERRID.ERR_BogusWithinLineIf)
                            Return ProcessSyntax(node)
                            Debug.Assert(scope.IsLineIf)
                        End If
                    Else
                        If context IsNot Me Then
                            'This statement ends a block higher up.
                            'End all blocks from Me up to this one with a missing ends.
                            RecoverFromMissingEnd(context)
                        End If
                        'Add the block to the context above
                        Return context.EndBlock(DirectCast(node, StatementSyntax))
                    End If
                ElseIf SyntaxFacts.IsEndBlockLoopOrNextStatement(kind) Then
                    ' See if this kind closes an enclosing statement context
                    context = context.PrevBlock
                Else
                    Return ProcessSyntax(node)
                End If
            End While

            'No match was found for the end block statement
            'Add it to the current context and leave the context unchanged
            Return RecoverFromMismatchedEnd(DirectCast(node, StatementSyntax))
        End Function

        Friend Function UseSyntax(node As VisualBasicSyntaxNode, ByRef newContext As BlockContext, Optional AddMissingTerminator As Boolean = False) As LinkResult
            ' get off the current node as we are definitely using it and LinkStatement may need to look at next token
            Parser.GetNextSyntaxNode()

            ' TODO: this will add an error to the statement. Perhaps duplicating it
            ' context-sensitive errors should be filtered out before re-using nodes.
            ' or better we should put contextual errors on the actual block not on the offending node (if possible).
            newContext = LinkSyntax(node)

            If AddMissingTerminator Then
                Return LinkResult.Used Or LinkResult.MissingTerminator
            End If

            Return LinkResult.Used
        End Function

        Friend Function TryUseStatement(node As VisualBasicSyntaxNode, ByRef newContext As BlockContext) As LinkResult
            Dim statement = TryCast(node, StatementSyntax)
            If statement IsNot Nothing Then
                ' get off the current node as we are definitely using it and LinkStatement may need to look at next token
                Return UseSyntax(statement, newContext)
            Else
                Return LinkResult.NotUsed
            End If
        End Function

        ' Returns Nothing if the statement isn't processed
        Friend Function TryProcessExecutableStatement(node As VisualBasicSyntaxNode) As BlockContext
            ' top-level statements
            Select Case node.Kind
                Case SyntaxKind.SingleLineIfStatement
                    Add(node)

                Case SyntaxKind.IfStatement
                    ' A single line if has a "then" on the line and is not followed by a ":", EOL or EOF.
                    ' It is OK for else to follow a single line if. i.e
                    '       "if true then if true then else else
                    Dim ifStmt = DirectCast(node, IfStatementSyntax)
                    If ifStmt.ThenKeyword IsNot Nothing AndAlso Not SyntaxFacts.IsTerminator(Parser.CurrentToken.Kind) Then
                        Return New SingleLineIfBlockContext(ifStmt, Me)
                    Else
                        Return New IfBlockContext(ifStmt, Me)
                    End If

                Case SyntaxKind.ElseStatement
                    ' davidsch
                    ' This error used to be reported in ParseStatementInMethodBody.  Move to context.
                    ' It used to be this error with a note that Semantics doesn't like an ELSEIF without an IF.
                    ' Fully parse for now.
                    ' ReportUnrecognizedStatementError(ERRID_ElseIfNoMatchingIf, ErrorInConstruct)

                    Add(Parser.ReportSyntaxError(node, ERRID.ERR_ElseNoMatchingIf))

                Case SyntaxKind.ElseIfStatement
                    ' davidsch
                    ' This error used to be reported in ParseStatementInMethodBody.  Move to context.
                    ' It used to be this error with a note that Semantics doesn't like an ELSEIF without an IF.
                    ' Fully parse for now.
                    ' ReportUnrecognizedStatementError(ERRID_ElseIfNoMatchingIf, ErrorInConstruct)

                    Add(Parser.ReportSyntaxError(node, ERRID.ERR_ElseIfNoMatchingIf))

                Case SyntaxKind.SimpleDoStatement,
                     SyntaxKind.DoWhileStatement,
                     SyntaxKind.DoUntilStatement
                    Return New DoLoopBlockContext(DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.ForStatement, SyntaxKind.ForEachStatement
                    Return New ForBlockContext(DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.SelectStatement
                    Return New SelectBlockContext(DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.CaseStatement
                    'TODO - davidsch
                    ' In dev10 the error is reported on the CASE not the statement.  If needed this error can be
                    ' moved to ParseCaseStatement.
                    Add(Parser.ReportSyntaxError(node, ERRID.ERR_CaseNoSelect))

                Case SyntaxKind.CaseElseStatement
                    'TODO - davidsch
                    ' In dev10 the error is reported on the CASE not the statement.  If needed this error can be
                    ' moved to ParseCaseStatement.
                    Add(Parser.ReportSyntaxError(node, ERRID.ERR_CaseElseNoSelect))

                Case SyntaxKind.WhileStatement
                    Return New StatementBlockContext(SyntaxKind.WhileBlock, DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.WithStatement
                    Return New StatementBlockContext(SyntaxKind.WithBlock, DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.SyncLockStatement
                    Return New StatementBlockContext(SyntaxKind.SyncLockBlock, DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.UsingStatement
                    Return New StatementBlockContext(SyntaxKind.UsingBlock, DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.TryStatement
                    Return New TryBlockContext(DirectCast(node, StatementSyntax), Me)

                Case SyntaxKind.CatchStatement, SyntaxKind.FinallyStatement
                    Dim context = FindNearestInSameMethodScope(SyntaxKind.TryBlock, SyntaxKind.CatchBlock, SyntaxKind.FinallyBlock)
                    If context IsNot Nothing Then
                        RecoverFromMissingEnd(context)
                        Return context.ProcessSyntax(DirectCast(node, StatementSyntax))
                    End If

                    ' In dev10 the error is reported on the CATCH not the statement.
                    ' If needed this error can be moved to ParseCatchStatement.
                    Add(Parser.ReportSyntaxError(node, If(node.Kind = SyntaxKind.CatchStatement, ERRID.ERR_CatchNoMatchingTry, ERRID.ERR_FinallyNoMatchingTry)))

                Case SyntaxKind.SelectBlock,
                     SyntaxKind.WhileBlock,
                     SyntaxKind.WithBlock,
                     SyntaxKind.SyncLockBlock,
                     SyntaxKind.UsingBlock,
                     SyntaxKind.TryBlock,
                     SyntaxKind.SimpleDoLoopBlock,
                     SyntaxKind.DoWhileLoopBlock,
                     SyntaxKind.DoUntilLoopBlock,
                     SyntaxKind.DoLoopWhileBlock,
                     SyntaxKind.DoLoopUntilBlock,
                     SyntaxKind.ForBlock,
                     SyntaxKind.ForEachBlock,
                     SyntaxKind.SingleLineIfStatement,
                     SyntaxKind.MultiLineIfBlock
                    ' Handle any block that can be created by this context
                    Add(node)

                Case Else
                    If Not TypeOf node Is ExecutableStatementSyntax Then
                        Return Nothing
                    End If

                    Add(node)
            End Select
            Return Me
        End Function

        Friend Function TryLinkStatement(node As VisualBasicSyntaxNode, ByRef newContext As BlockContext) As LinkResult
            newContext = Nothing
            Select Case node.Kind
                Case SyntaxKind.SelectBlock
                    Return UseSyntax(node, newContext, DirectCast(node, SelectBlockSyntax).EndSelectStatement.IsMissing)

                Case SyntaxKind.WhileBlock
                    Return UseSyntax(node, newContext, DirectCast(node, WhileBlockSyntax).EndWhileStatement.IsMissing)

                Case SyntaxKind.WithBlock
                    Return UseSyntax(node, newContext, DirectCast(node, WithBlockSyntax).EndWithStatement.IsMissing)

                Case SyntaxKind.SyncLockBlock
                    Return UseSyntax(node, newContext, DirectCast(node, SyncLockBlockSyntax).EndSyncLockStatement.IsMissing)

                Case SyntaxKind.UsingBlock
                    Return UseSyntax(node, newContext, DirectCast(node, UsingBlockSyntax).EndUsingStatement.IsMissing)

                Case SyntaxKind.TryBlock
                    Return UseSyntax(node, newContext, DirectCast(node, TryBlockSyntax).EndTryStatement.IsMissing)

                Case SyntaxKind.SimpleDoLoopBlock,
                     SyntaxKind.DoWhileLoopBlock,
                     SyntaxKind.DoUntilLoopBlock,
                     SyntaxKind.DoLoopWhileBlock,
                     SyntaxKind.DoLoopUntilBlock

                    Return UseSyntax(node, newContext, DirectCast(node, DoLoopBlockSyntax).LoopStatement.IsMissing)

                Case SyntaxKind.ForBlock,
                     SyntaxKind.ForEachBlock

                    ' The EndOpt syntax can influence the next context in case it contains
                    ' several control variables. If they are still valid needs to be checked in the ForBlockContext.
                    ' This is the reason why we can't simply reuse the syntax here. 
                    newContext = Me
                    Return LinkResult.Crumble

                Case SyntaxKind.SingleLineIfStatement
                    Return UseSyntax(node, newContext)

                Case SyntaxKind.MultiLineIfBlock
                    Return UseSyntax(node, newContext, DirectCast(node, MultiLineIfBlockSyntax).EndIfStatement.IsMissing)

                Case SyntaxKind.NextStatement
                    ' Don't reuse a next statement. The parser matches the variable list with the for context blocks.
                    ' In order to reuse the next statement that error checking needs to be moved from the parser to the
                    ' contexts.  For now, crumble and reparse.  The next statement is small and fast to parse.
                    newContext = Me
                    Return LinkResult.NotUsed

                Case Else
                    Return TryUseStatement(node, newContext)

            End Select
        End Function

        Private Function CreateMissingEnd(ByRef errorId As ERRID) As StatementSyntax
            Return CreateMissingEnd(BlockKind, errorId)
        End Function

        Private Function CreateMissingEnd(kind As SyntaxKind, ByRef errorId As ERRID) As StatementSyntax
            Dim endStmt As StatementSyntax
            Dim missingEndKeyword = InternalSyntaxFactory.MissingKeyword(SyntaxKind.EndKeyword)

            Select Case kind
                Case SyntaxKind.NamespaceBlock
                    endStmt = SyntaxFactory.EndNamespaceStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.NamespaceKeyword))
                    errorId = ERRID.ERR_ExpectedEndNamespace

                Case SyntaxKind.ModuleBlock
                    endStmt = SyntaxFactory.EndModuleStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.ModuleKeyword))
                    errorId = ERRID.ERR_ExpectedEndModule

                Case SyntaxKind.ClassBlock
                    endStmt = SyntaxFactory.EndClassStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.ClassKeyword))
                    errorId = ERRID.ERR_ExpectedEndClass

                Case SyntaxKind.StructureBlock
                    endStmt = SyntaxFactory.EndStructureStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.StructureKeyword))
                    errorId = ERRID.ERR_ExpectedEndStructure

                Case SyntaxKind.InterfaceBlock
                    endStmt = SyntaxFactory.EndInterfaceStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.InterfaceKeyword))
                    errorId = ERRID.ERR_MissingEndInterface

                Case SyntaxKind.EnumBlock
                    endStmt = SyntaxFactory.EndEnumStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.EnumKeyword))
                    errorId = ERRID.ERR_MissingEndEnum

                Case SyntaxKind.SubBlock,
                    SyntaxKind.ConstructorBlock
                    endStmt = SyntaxFactory.EndSubStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.SubKeyword))
                    'TODO - davidsch make these expected error message names consistent. Some are EndXXExpected and others are ExpectedEndXX
                    errorId = ERRID.ERR_EndSubExpected

                Case SyntaxKind.MultiLineSubLambdaExpression
                    endStmt = SyntaxFactory.EndSubStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.SubKeyword))
                    errorId = ERRID.ERR_MultilineLambdaMissingSub

                Case SyntaxKind.FunctionBlock
                    endStmt = SyntaxFactory.EndFunctionStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.FunctionKeyword))
                    errorId = ERRID.ERR_EndFunctionExpected

                Case SyntaxKind.MultiLineFunctionLambdaExpression
                    endStmt = SyntaxFactory.EndFunctionStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.FunctionKeyword))
                    errorId = ERRID.ERR_MultilineLambdaMissingFunction

                Case SyntaxKind.OperatorBlock
                    endStmt = SyntaxFactory.EndOperatorStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.OperatorKeyword))
                    errorId = ERRID.ERR_EndOperatorExpected

                Case SyntaxKind.PropertyBlock
                    endStmt = SyntaxFactory.EndPropertyStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.PropertyKeyword))
                    'TODO rename this enum for consistency ERRID_MissingEndProperty
                    errorId = ERRID.ERR_EndProp

                Case SyntaxKind.GetAccessorBlock
                    endStmt = SyntaxFactory.EndGetStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.GetKeyword))
                    errorId = ERRID.ERR_MissingEndGet

                Case SyntaxKind.SetAccessorBlock
                    endStmt = SyntaxFactory.EndSetStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.SetKeyword))
                    errorId = ERRID.ERR_MissingEndSet

                Case SyntaxKind.EventBlock
                    endStmt = SyntaxFactory.EndEventStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.EventKeyword))
                    'TODO rename this enum for consistency ERRID_MissingEndProperty
                    errorId = ERRID.ERR_MissingEndEvent

                Case SyntaxKind.AddHandlerAccessorBlock
                    endStmt = SyntaxFactory.EndAddHandlerStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.AddHandlerKeyword))
                    errorId = ERRID.ERR_MissingEndAddHandler

                Case SyntaxKind.RemoveHandlerAccessorBlock
                    endStmt = SyntaxFactory.EndRemoveHandlerStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.RemoveHandlerKeyword))
                    errorId = ERRID.ERR_MissingEndRemoveHandler

                Case SyntaxKind.RaiseEventAccessorBlock
                    endStmt = SyntaxFactory.EndRaiseEventStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.RaiseEventKeyword))
                    errorId = ERRID.ERR_MissingEndRaiseEvent

                Case SyntaxKind.MultiLineIfBlock, SyntaxKind.ElseIfBlock, SyntaxKind.ElseBlock
                    endStmt = SyntaxFactory.EndIfStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.IfKeyword))
                    errorId = ERRID.ERR_ExpectedEndIf

                Case SyntaxKind.SimpleDoLoopBlock, SyntaxKind.DoWhileLoopBlock
                    endStmt = SyntaxFactory.SimpleLoopStatement(InternalSyntaxFactory.MissingKeyword(SyntaxKind.LoopKeyword), Nothing)
                    errorId = ERRID.ERR_ExpectedLoop

                Case SyntaxKind.WhileBlock
                    endStmt = SyntaxFactory.EndWhileStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.WhileKeyword))
                    errorId = ERRID.ERR_ExpectedEndWhile

                Case SyntaxKind.WithBlock
                    endStmt = SyntaxFactory.EndWithStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.WithKeyword))
                    errorId = ERRID.ERR_ExpectedEndWith

                Case SyntaxKind.ForBlock, SyntaxKind.ForEachBlock
                    endStmt = SyntaxFactory.NextStatement(InternalSyntaxFactory.MissingKeyword(SyntaxKind.NextKeyword), Nothing)
                    errorId = ERRID.ERR_ExpectedNext

                Case SyntaxKind.SyncLockBlock
                    endStmt = SyntaxFactory.EndSyncLockStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.SyncLockKeyword))
                    errorId = ERRID.ERR_ExpectedEndSyncLock

                Case SyntaxKind.SelectBlock
                    endStmt = SyntaxFactory.EndSelectStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.SelectKeyword))
                    errorId = ERRID.ERR_ExpectedEndSelect

                Case SyntaxKind.TryBlock
                    endStmt = SyntaxFactory.EndTryStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.TryKeyword))
                    errorId = ERRID.ERR_ExpectedEndTry

                Case SyntaxKind.UsingBlock
                    endStmt = SyntaxFactory.EndUsingStatement(missingEndKeyword, InternalSyntaxFactory.MissingKeyword(SyntaxKind.UsingKeyword))
                    errorId = ERRID.ERR_ExpectedEndUsing

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(kind)
            End Select

            Return endStmt
        End Function

        Private Shared Function GetEndKind(kind As SyntaxKind) As SyntaxKind
            Select Case kind
                Case SyntaxKind.CompilationUnit,
                    SyntaxKind.SingleLineFunctionLambdaExpression,
                    SyntaxKind.SingleLineSubLambdaExpression
                    Return SyntaxKind.None

                Case SyntaxKind.NamespaceBlock
                    Return SyntaxKind.EndNamespaceStatement

                Case SyntaxKind.ModuleBlock
                    Return SyntaxKind.EndModuleStatement

                Case SyntaxKind.ClassBlock
                    Return SyntaxKind.EndClassStatement

                Case SyntaxKind.StructureBlock
                    Return SyntaxKind.EndStructureStatement

                Case SyntaxKind.InterfaceBlock
                    Return SyntaxKind.EndInterfaceStatement

                Case SyntaxKind.EnumBlock
                    Return SyntaxKind.EndEnumStatement

                Case SyntaxKind.SubBlock,
                    SyntaxKind.ConstructorBlock,
                    SyntaxKind.MultiLineSubLambdaExpression
                    Return SyntaxKind.EndSubStatement

                Case SyntaxKind.FunctionBlock,
                    SyntaxKind.MultiLineFunctionLambdaExpression
                    Return SyntaxKind.EndFunctionStatement

                Case SyntaxKind.OperatorBlock
                    Return SyntaxKind.EndOperatorStatement

                Case SyntaxKind.PropertyBlock
                    Return SyntaxKind.EndPropertyStatement

                Case SyntaxKind.GetAccessorBlock
                    Return SyntaxKind.EndGetStatement

                Case SyntaxKind.SetAccessorBlock
                    Return SyntaxKind.EndSetStatement

                Case SyntaxKind.EventBlock
                    Return SyntaxKind.EndEventStatement

                Case SyntaxKind.AddHandlerAccessorBlock
                    Return SyntaxKind.EndAddHandlerStatement

                Case SyntaxKind.RemoveHandlerAccessorBlock
                    Return SyntaxKind.EndRemoveHandlerStatement

                Case SyntaxKind.RaiseEventAccessorBlock
                    Return SyntaxKind.EndRaiseEventStatement

                Case SyntaxKind.MultiLineIfBlock, SyntaxKind.ElseIfBlock, SyntaxKind.ElseBlock
                    Return SyntaxKind.EndIfStatement

                Case SyntaxKind.SingleLineIfStatement, SyntaxKind.SingleLineElseClause
                    Return SyntaxKind.None

                Case SyntaxKind.SimpleDoLoopBlock,
                     SyntaxKind.DoWhileLoopBlock
                    Return SyntaxKind.SimpleLoopStatement

                Case SyntaxKind.WhileBlock
                    Return SyntaxKind.EndWhileStatement

                Case SyntaxKind.ForBlock, SyntaxKind.ForEachBlock
                    Return SyntaxKind.NextStatement

                Case SyntaxKind.WithBlock
                    Return SyntaxKind.EndWithStatement

                Case SyntaxKind.SyncLockBlock
                    Return SyntaxKind.EndSyncLockStatement

                Case SyntaxKind.SelectBlock, SyntaxKind.CaseBlock, SyntaxKind.CaseElseBlock
                    Return SyntaxKind.EndSelectStatement

                Case SyntaxKind.TryBlock, SyntaxKind.CatchBlock, SyntaxKind.FinallyBlock
                    Return SyntaxKind.EndTryStatement

                Case SyntaxKind.UsingBlock
                    Return SyntaxKind.EndUsingStatement

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(kind)
            End Select
        End Function

    End Class

End Namespace
