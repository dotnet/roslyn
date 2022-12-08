' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------
' Contains the definition of the Scanner, which produces tokens from text 
'-----------------------------------------------------------------------------

Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax
Imports Microsoft.CodeAnalysis.Text
Imports CoreInternalSyntax = Microsoft.CodeAnalysis.Syntax.InternalSyntax
Imports InternalSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend Class Parser
        Implements ISyntaxFactoryContext, IDisposable

        Private Enum PossibleFirstStatementKind
            No
            Yes
            IfPrecededByLineBreak
        End Enum

        Private _allowLeadingMultilineTrivia As Boolean = True
        Private _hadImplicitLineContinuation As Boolean = False
        Private _hadLineContinuationComment As Boolean = False
        Private _possibleFirstStatementOnLine As PossibleFirstStatementKind = PossibleFirstStatementKind.Yes
        Private _recursionDepth As Integer
        Private _evaluatingConditionCompilationExpression As Boolean
        Private ReadOnly _scanner As Scanner
        Private ReadOnly _cancellationToken As CancellationToken
        Friend ReadOnly _pool As New SyntaxListPool
        Private ReadOnly _syntaxFactory As ContextAwareSyntaxFactory

        ' When parser owns the scanner, it is responsible for disposing it
        Private ReadOnly _disposeScanner As Boolean

        ' Parser looks at Context for
        ' 1. the blockKind
        ' 2. the nearest block for error recovery in continue and exit statements
        ' 3. matching variables in next with blocks in for statement
        ' 4. end statements to terminate lambda parsing
        Private _context As BlockContext = Nothing
        Private _isInMethodDeclarationHeader As Boolean
        Private _isInAsyncMethodDeclarationHeader As Boolean
        Private _isInIteratorMethodDeclarationHeader As Boolean

        Friend Sub New(text As SourceText, options As VisualBasicParseOptions, Optional cancellationToken As CancellationToken = Nothing)
            MyClass.New(New Scanner(text, options))
            Debug.Assert(text IsNot Nothing)
            Debug.Assert(options IsNot Nothing)
            Me._disposeScanner = True
            Me._cancellationToken = cancellationToken
        End Sub

        Friend Sub New(scanner As Scanner)
            Debug.Assert(scanner IsNot Nothing)
            _scanner = scanner
            _context = New CompilationUnitContext(Me)
            _syntaxFactory = New ContextAwareSyntaxFactory(Me)
        End Sub

        Friend Sub Dispose() Implements IDisposable.Dispose
            If _disposeScanner Then
                Me._scanner.Dispose()
            End If
        End Sub

        Friend ReadOnly Property IsScript As Boolean
            Get
                Return _scanner.Options.Kind = SourceCodeKind.Script
            End Get
        End Property

        Private Function ParseSimpleName(
                                     allowGenericArguments As Boolean,
                                     allowGenericsWithoutOf As Boolean,
                                     disallowGenericArgumentsOnLastQualifiedName As Boolean,
                                     nonArrayName As Boolean,
                                     allowKeyword As Boolean,
                                     ByRef allowEmptyGenericArguments As Boolean,
                                     ByRef allowNonEmptyGenericArguments As Boolean
                                 ) As SimpleNameSyntax

            Dim id As IdentifierTokenSyntax = If(allowKeyword,
                                                 ParseIdentifierAllowingKeyword(),
                                                 ParseIdentifier())

            Dim typeArguments As TypeArgumentListSyntax = Nothing

            If allowGenericArguments Then

                ' Test for a generic type name.
                If BeginsGeneric(nonArrayName:=nonArrayName, allowGenericsWithoutOf:=allowGenericsWithoutOf) Then

                    Debug.Assert(CurrentToken.Kind = SyntaxKind.OpenParenToken, "Generic parameter parsing lost!!!")

                    typeArguments = ParseGenericArguments(
                                        allowEmptyGenericArguments,
                                        allowNonEmptyGenericArguments)
                End If
            End If

            If typeArguments Is Nothing Then

                Return SyntaxFactory.IdentifierName(id)

            ElseIf disallowGenericArgumentsOnLastQualifiedName AndAlso
                CurrentToken.Kind <> SyntaxKind.DotToken AndAlso
                Not typeArguments.ContainsDiagnostics() Then

                id = id.AddTrailingSyntax(typeArguments, ERRID.ERR_TypeArgsUnexpected)
                Return SyntaxFactory.IdentifierName(id)

            Else

                Return SyntaxFactory.GenericName(id, typeArguments)
            End If
        End Function

        Public ReadOnly Property IsWithinAsyncMethodOrLambda As Boolean Implements ISyntaxFactoryContext.IsWithinAsyncMethodOrLambda
            Get
                Return If(Not _isInMethodDeclarationHeader, Context.IsWithinAsyncMethodOrLambda, _isInAsyncMethodDeclarationHeader)
            End Get
        End Property

        Public ReadOnly Property IsWithinIteratorContext As Boolean Implements ISyntaxFactoryContext.IsWithinIteratorContext
            Get
                Return If(Not _isInMethodDeclarationHeader, Context.IsWithinIteratorMethodOrLambdaOrProperty, _isInIteratorMethodDeclarationHeader)
            End Get
        End Property

        '
        '============ Methods for parsing declaration constructs ============
        '

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseName
        ' *
        ' * Purpose: Will parse a dot qualified or unqualified name
        ' *
        ' *          Ex: class1.proc
        ' *
        ' **********************************************************************/

        ' File: Parser.cpp
        ' Lines: 7742 - 7742
        ' Name* .Parser::ParseName( [ bool RequireQualification ] [ _Inout_ bool& ErrorInConstruct ] [ bool AllowGlobalNameSpace ] [ bool AllowGenericArguments ] [ bool DisallowGenericArgumentsOnLastQualifiedName ] [ bool AllowEmptyGenericArguments ] [ _Out_opt_ bool* AllowedEmptyGenericArguments ] )

        Friend Function ParseName(
                            requireQualification As Boolean,
                            allowGlobalNameSpace As Boolean,
                            allowGenericArguments As Boolean,
                            allowGenericsWithoutOf As Boolean,
                            Optional nonArrayName As Boolean = False,
                            Optional disallowGenericArgumentsOnLastQualifiedName As Boolean = False,
                            Optional allowEmptyGenericArguments As Boolean = False,
                            Optional ByRef allowedEmptyGenericArguments As Boolean = False,
                            Optional isNameInNamespaceDeclaration As Boolean = False
                        ) As NameSyntax

            Debug.Assert(allowGenericArguments OrElse Not allowEmptyGenericArguments, "Inconsistency in generic arguments parsing requirements!!!")

            Dim allowNonEmptyGenericArguments As Boolean = True

            Dim result As NameSyntax = Nothing

            ' Parse head: Either a GlobalName or a SimpleName.
            If CurrentToken.Kind = SyntaxKind.GlobalKeyword Then

                result = SyntaxFactory.GlobalName(DirectCast(CurrentToken, KeywordSyntax))

                If isNameInNamespaceDeclaration Then
                    result = CheckFeatureAvailability(Feature.GlobalNamespace, result)
                End If

                GetNextToken()

                If Not allowGlobalNameSpace Then

                    ' Report the error and turn into a bad simple name in order to let compilation continue.
                    result = ReportSyntaxError(result, ERRID.ERR_NoGlobalExpectedIdentifier)
                End If
            Else

                result = ParseSimpleName(
                    allowGenericArguments:=allowGenericArguments,
                    allowGenericsWithoutOf:=allowGenericsWithoutOf,
                    disallowGenericArgumentsOnLastQualifiedName:=disallowGenericArgumentsOnLastQualifiedName,
                    allowKeyword:=False,
                    nonArrayName:=nonArrayName,
                    allowEmptyGenericArguments:=allowEmptyGenericArguments,
                    allowNonEmptyGenericArguments:=allowNonEmptyGenericArguments)
            End If

            ' Parse tail: A sequence of zero or more [dot SimpleName].
            Dim dotToken As PunctuationSyntax = Nothing

            Do While TryGetTokenAndEatNewLine(SyntaxKind.DotToken, dotToken)
                Debug.Assert(dotToken IsNot Nothing)

                result = SyntaxFactory.QualifiedName(
                    result,
                    dotToken,
                    ParseSimpleName(
                        allowGenericArguments:=allowGenericArguments,
                        allowGenericsWithoutOf:=allowGenericsWithoutOf,
                        disallowGenericArgumentsOnLastQualifiedName:=disallowGenericArgumentsOnLastQualifiedName,
                        allowKeyword:=True,
                        nonArrayName:=nonArrayName,
                        allowEmptyGenericArguments:=allowEmptyGenericArguments,
                        allowNonEmptyGenericArguments:=allowNonEmptyGenericArguments))
            Loop

            If requireQualification AndAlso dotToken Is Nothing Then

                result = SyntaxFactory.QualifiedName(result, InternalSyntaxFactory.MissingPunctuation(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(InternalSyntaxFactory.MissingIdentifier()))
                result = ReportSyntaxError(result, ERRID.ERR_ExpectedDot)
            End If

            Debug.Assert(Not allowGenericArguments OrElse allowEmptyGenericArguments OrElse allowNonEmptyGenericArguments,
                         "Generic argument parsing inconsistency!!!")

            allowedEmptyGenericArguments = (allowNonEmptyGenericArguments = False)

            Return result
        End Function

        ''' <summary>
        ''' gets the last token that has nonzero FullWidth. 
        ''' NOTE: this helper will not descend into structured trivia.
        ''' </summary>
        Private Shared Function GetLastNZWToken(node As Microsoft.CodeAnalysis.SyntaxNode) As Microsoft.CodeAnalysis.SyntaxToken
            Do
                Debug.Assert(node.FullWidth <> 0)
                For Each child In node.ChildNodesAndTokens.Reverse
                    If child.FullWidth <> 0 Then
                        node = child.AsNode
                        If node Is Nothing Then
                            Return child.AsToken
                        Else
                            Continue Do
                        End If
                    End If
                Next

                Throw ExceptionUtilities.Unreachable
            Loop
        End Function

        ''' <summary>
        ''' gets the last token regardless if it has zero FullWidth or not 
        ''' NOTE: this helper will not descend into structured trivia.
        ''' </summary>
        Private Shared Function GetLastToken(node As Microsoft.CodeAnalysis.SyntaxNode) As Microsoft.CodeAnalysis.SyntaxToken
            Do
                Dim child = node.ChildNodesAndTokens.Last

                node = child.AsNode
                If node Is Nothing Then
                    Return child.AsToken
                End If
            Loop
        End Function

        ''' <summary>
        ''' Adjust the trivia on a node so that missing tokens are always before newline and colon trivia.
        ''' Because new lines and colons are eagerly attached as trivia, missing tokens can end up incorrectly after the new line.
        ''' This method moves the trailing non-whitespace trivia from the last token to the last zero with token.
        ''' </summary>
        Private Shared Function AdjustTriviaForMissingTokens(Of T As VisualBasicSyntaxNode)(node As T) As T
            If Not node.ContainsDiagnostics Then
                ' no errors means no skipped tokens.
                Return node
            End If

            If node.GetLastTerminal().FullWidth <> 0 Then
                ' last token is not empty, cannot move anything past it
                Return node
            End If

            Return AdjustTriviaForMissingTokensCore(node)
        End Function

        ''' <summary>
        ''' Slow part of AdjustTriviaForMissingTokensCore where we actually do the work when we need to.
        ''' </summary>
        Private Shared Function AdjustTriviaForMissingTokensCore(Of T As VisualBasicSyntaxNode)(node As T) As T
            Dim redNode = node.CreateRed(Nothing, 0)

            ' here we have last token with some actual content. 
            ' Since we are dealing with statements here, it is extremely 
            ' likely that the token contains a statement terminator in its trailing trivia
            ' NOTE: all tokens after this one do not have any content
            Dim lastNonZeroWidthToken = GetLastNZWToken(redNode)

            ' get the absolutely last token. It must be zero-width or we would not get here
            Dim lastZeroWidthToken = GetLastToken(redNode)
            Debug.Assert(lastZeroWidthToken.FullWidth = 0)

            ' if the nonzeroWidthToken contains trailing trivia, move that to the last token.
            Dim triviaToMove = lastNonZeroWidthToken.TrailingTrivia
            Dim triviaToMoveCnt = triviaToMove.Count

            For Each trivia In triviaToMove
                If trivia.Kind = SyntaxKind.WhitespaceTrivia Then
                    triviaToMoveCnt -= 1
                Else
                    Exit For
                End If
            Next

            If triviaToMoveCnt = 0 Then
                ' this is very unlikely, but we have nothing to move
                Return node
            End If

            ' leave whitespace trivia on NZW token up until a non-whitespace trivia is found (that and the rest we move)
            Dim newNonZeroWidthTokenTrivia(triviaToMove.Count - triviaToMoveCnt - 1) As Microsoft.CodeAnalysis.SyntaxTrivia
            triviaToMove.CopyTo(0, newNonZeroWidthTokenTrivia, 0, newNonZeroWidthTokenTrivia.Length)

            Dim nonZwTokenReplacement = lastNonZeroWidthToken.WithTrailingTrivia(newNonZeroWidthTokenTrivia)

            ' move non-whitespace trivia and following to the beginning of the trailing trivia on last token
            Dim originalTrailingTrivia = lastZeroWidthToken.TrailingTrivia
            Dim newTrailingTrivia(triviaToMoveCnt + originalTrailingTrivia.Count - 1) As Microsoft.CodeAnalysis.SyntaxTrivia

            triviaToMove.CopyTo(triviaToMove.Count - triviaToMoveCnt, newTrailingTrivia, 0, triviaToMoveCnt)
            originalTrailingTrivia.CopyTo(0, newTrailingTrivia, triviaToMoveCnt, originalTrailingTrivia.Count)

            Dim lastTokenReplacement = lastZeroWidthToken.WithTrailingTrivia(newTrailingTrivia)

            redNode = redNode.ReplaceTokens({lastNonZeroWidthToken, lastZeroWidthToken},
                Function(oldToken, newToken)
                    If oldToken = lastNonZeroWidthToken Then
                        Return nonZwTokenReplacement

                    ElseIf oldToken = lastZeroWidthToken Then
                        Return lastTokenReplacement

                    Else
                        Return newToken

                    End If
                End Function)

            node = DirectCast(redNode.Green, T)

            Return node
        End Function
        Private Shared Function MergeTokenText(firstToken As SyntaxToken, secondToken As SyntaxToken) As String

            ' grab the part that doesn't contain the preceding and trailing trivia.

            Dim builder = PooledStringBuilder.GetInstance()
            Dim writer As New IO.StringWriter(builder)

            firstToken.WriteTo(writer)
            secondToken.WriteTo(writer)

            Dim leadingWidth = firstToken.GetLeadingTriviaWidth()
            Dim trailingWidth = secondToken.GetTrailingTriviaWidth()
            Dim fullWidth = firstToken.FullWidth + secondToken.FullWidth

            Debug.Assert(builder.Length = fullWidth)
            Debug.Assert(builder.Length >= leadingWidth + trailingWidth)

            Return builder.ToStringAndFree(leadingWidth, fullWidth - leadingWidth - trailingWidth)

        End Function

        Private Function GetCurrentSyntaxNodeIfApplicable(<Out()> ByRef curSyntaxNode As VisualBasicSyntaxNode) As BlockContext
            Dim result As BlockContext.LinkResult
            Dim incrementalContext = _context

            Do
                curSyntaxNode = _scanner.GetCurrentSyntaxNode()

                ' Try linking whole node
                If curSyntaxNode Is Nothing Then
                    ' nothing to use
                    result = BlockContext.LinkResult.NotUsed

                ElseIf TypeOf curSyntaxNode Is DirectiveTriviaSyntax OrElse
                    curSyntaxNode.Kind = SyntaxKind.DocumentationCommentTrivia Then
                    ' this can be used only by preprocessor
                    result = BlockContext.LinkResult.NotUsed

                Else
                    result = incrementalContext.TryLinkSyntax(curSyntaxNode, incrementalContext)
                End If

                ' did context request a crumble?
                If result <> BlockContext.LinkResult.Crumble OrElse
                    Not _scanner.TryCrumbleOnce() Then

                    Exit Do
                End If
            Loop

            If (result And BlockContext.LinkResult.Used) = BlockContext.LinkResult.Used Then
                Return incrementalContext
            End If

            Return Nothing
        End Function

        ' Create trees for the module-level declarations (everything except method bodies)
        ' in a source module.
        ' File:Parser.cpp
        ' Lines: 1125 - 1125
        ' HRESULT .Parser::ParseDecls( [ _In_ Scanner* InputStream ] [ ErrorTable* Errors ] [ SourceFile* InputFile ] [  ParseTree::FileBlockStatement** Result ] [ _Inout_ NorlsAllocator* ConditionalCompilationSymbolsStorage ] [ BCSYM_Container* ProjectLevelCondCompScope ] [ _Out_opt_ BCSYM_Container** ConditionalCompilationConstants ] [ _In_ LineMarkerTable* LineMarkerTableForConditionals ] )
        Friend Function ParseCompilationUnit() As CompilationUnitSyntax
            Return ParseWithStackGuard(Of CompilationUnitSyntax)(
                AddressOf Me.ParseCompilationUnitCore,
                Function() SyntaxFactory.CompilationUnit(
                    New CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(),
                    New CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(),
                    New CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(),
                    New CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(),
                    Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.SyntaxFactory.EndOfFileToken()))
        End Function

        Friend Function ParseCompilationUnitCore() As CompilationUnitSyntax
            Debug.Assert(_context IsNot Nothing)
            Dim programContext As CompilationUnitContext = DirectCast(_context, CompilationUnitContext)

            GetNextToken()

            While True
                Dim curSyntaxNode As VisualBasicSyntaxNode = Nothing
                Dim incrementalContext = GetCurrentSyntaxNodeIfApplicable(curSyntaxNode)

                If incrementalContext IsNot Nothing Then
                    _context = incrementalContext
                    Dim lastTrivia = curSyntaxNode.LastTriviaIfAny()
                    If lastTrivia IsNot Nothing Then
                        If lastTrivia.Kind = SyntaxKind.EndOfLineTrivia Then
                            ConsumedStatementTerminator(allowLeadingMultilineTrivia:=True)
                            ResetCurrentToken(ScannerState.VBAllowLeadingMultilineTrivia)
                        ElseIf lastTrivia.Kind = SyntaxKind.ColonTrivia Then
                            Debug.Assert(Not _context.IsSingleLine) ' Not handling single-line statements.
                            ConsumedStatementTerminator(
                            allowLeadingMultilineTrivia:=True,
                            possibleFirstStatementOnLine:=PossibleFirstStatementKind.IfPrecededByLineBreak)
                            ResetCurrentToken(If(_allowLeadingMultilineTrivia, ScannerState.VBAllowLeadingMultilineTrivia, ScannerState.VB))
                        End If
                    Else
                        ' If we reuse a label statement, note that it may end with a colon.
                        Dim curNodeLabel As LabelStatementSyntax = TryCast(curSyntaxNode, LabelStatementSyntax)
                        If curNodeLabel IsNot Nothing AndAlso curNodeLabel.ColonToken.Kind = SyntaxKind.ColonToken Then
                            ConsumedStatementTerminator(
                            allowLeadingMultilineTrivia:=True,
                            possibleFirstStatementOnLine:=PossibleFirstStatementKind.IfPrecededByLineBreak)
                        End If
                    End If
                Else
                    ResetCurrentToken(If(_allowLeadingMultilineTrivia, ScannerState.VBAllowLeadingMultilineTrivia, ScannerState.VB))

                    If CurrentToken.IsEndOfParse Then
                        _context.RecoverFromMissingEnd(programContext)
                        Exit While
                    End If

                    Dim statement = _context.Parse()
                    Dim adjustedStatement = AdjustTriviaForMissingTokens(statement)
                    _context = _context.LinkSyntax(adjustedStatement)
                    _context = _context.ResyncAndProcessStatementTerminator(adjustedStatement, lambdaContext:=Nothing)

                End If
            End While

            ' Create program
            Dim terminator = DirectCast(CurrentToken, PunctuationSyntax)
            Debug.Assert(terminator.Kind = SyntaxKind.EndOfFileToken)

            Dim notClosedIfDirectives As ArrayBuilder(Of IfDirectiveTriviaSyntax) = Nothing
            Dim notClosedRegionDirectives As ArrayBuilder(Of RegionDirectiveTriviaSyntax) = Nothing
            Dim haveRegionDirectives As Boolean = False
            Dim notClosedExternalSourceDirective As ExternalSourceDirectiveTriviaSyntax = Nothing
            terminator = _scanner.RecoverFromMissingConditionalEnds(terminator, notClosedIfDirectives, notClosedRegionDirectives, haveRegionDirectives, notClosedExternalSourceDirective)
            Return programContext.CreateCompilationUnit(terminator, notClosedIfDirectives, notClosedRegionDirectives, haveRegionDirectives, notClosedExternalSourceDirective)
        End Function

        Private Function ParseWithStackGuard(Of TNode As VisualBasicSyntaxNode)(parseFunc As Func(Of TNode), defaultFunc As Func(Of TNode)) As TNode
            Debug.Assert(_recursionDepth = 0)
            Dim restorePoint = _scanner.CreateRestorePoint()
            Try
                Return parseFunc()

            Catch ex As InsufficientExecutionStackException
                Return CreateForInsufficientStack(restorePoint, defaultFunc())
            End Try
        End Function

        Private Function CreateForInsufficientStack(Of TNode As VisualBasicSyntaxNode)(ByRef restorePoint As Scanner.RestorePoint, result As TNode) As TNode
            restorePoint.Restore()
            GetNextToken()

            Dim builder = New SyntaxListBuilder(4)
            While CurrentToken.Kind <> SyntaxKind.EndOfFileToken
                builder.Add(CurrentToken)
                GetNextToken()
            End While

            Return result.AddLeadingSyntax(builder.ToList(Of SyntaxToken)(), ERRID.ERR_TooLongOrComplexExpression)
        End Function

        Friend Function ParseExecutableStatement() As StatementSyntax
            Return ParseWithStackGuard(Of StatementSyntax)(
                AddressOf Me.ParseExecutableStatementCore,
                Function() InternalSyntaxFactory.EmptyStatement())
        End Function

        Private Function ParseExecutableStatementCore() As StatementSyntax
            Dim outerContext As New CompilationUnitContext(Me)
            Dim fakeBegin = SyntaxFactory.SubStatement(Nothing, Nothing, InternalSyntaxFactory.MissingKeyword(SyntaxKind.SubKeyword),
                                                  InternalSyntaxFactory.MissingIdentifier(), Nothing, Nothing, Nothing, Nothing, Nothing)
            Dim methodContext = New MethodBlockContext(SyntaxKind.SubBlock, fakeBegin, outerContext)

            GetNextToken()

            _context = methodContext

            Do
                Dim statement = _context.Parse()
                _context = _context.LinkSyntax(statement)
                _context = _context.ResyncAndProcessStatementTerminator(statement, lambdaContext:=Nothing)

            Loop While _context.Level > methodContext.Level AndAlso Not CurrentToken.IsEndOfParse

            _context.RecoverFromMissingEnd(methodContext)

            ' if we have something in method body, just return that thing
            If methodContext.Statements.Count > 0 Then
                Return methodContext.Statements(0)
            End If

            ' if body is empty, there must be something that terminated it
            Dim method = DirectCast(outerContext.Statements(0), MethodBlockBaseSyntax)

            If method.Statements.Any Then
                Return method.Statements(0)
            End If

            ' if there are no statements in the body, then return End Sub as unexpected.
            Dim unexpectedEnd = ReportSyntaxError(method.End, ERRID.ERR_InvInsideEndsProc)
            Return unexpectedEnd
        End Function

        Private Function ParseBinaryOperator() As SyntaxToken
            Dim result As SyntaxToken = CurrentToken
            Dim nextToken As SyntaxToken = Nothing

            If CurrentToken.Kind = SyntaxKind.GreaterThanToken AndAlso
                PeekToken(1).Kind = SyntaxKind.LessThanToken Then

                nextToken = PeekToken(1)

                ' The pretty lister needs to convert '><' into '<>'. It does this by
                ' looking for the tkNE token, so in the context of binary operators
                ' we return tkNe instead of tkGT-tkLT.

                result = InternalSyntaxFactory.MissingPunctuation(SyntaxKind.LessThanGreaterThanToken)

            ElseIf CurrentToken.Kind = SyntaxKind.EqualsToken Then

                If PeekToken(1).Kind = SyntaxKind.GreaterThanToken Then

                    nextToken = PeekToken(1)

                    ' The pretty lister needs to convert '=>' into '>='. Look at the next
                    ' token to decide.

                    result = InternalSyntaxFactory.MissingPunctuation(SyntaxKind.GreaterThanEqualsToken)

                ElseIf PeekToken(1).Kind = SyntaxKind.LessThanToken Then

                    nextToken = PeekToken(1)

                    result = InternalSyntaxFactory.MissingPunctuation(SyntaxKind.LessThanEqualsToken)

                End If

            End If

            If nextToken IsNot Nothing Then
                result = result.AddLeadingSyntax(SyntaxList.List(CurrentToken, nextToken), ERRID.ERR_ExpectedRelational)
                GetNextToken()
            End If

            GetNextToken()
            'eat leading EOL tokens because we allow implicit line continuations after binary operators
            TryEatNewLine()

            Return result
        End Function

        '
        '============ Methods for parsing general syntactic constructs. =======
        '

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseDeclarationStatement
        ' *
        ' * Purpose:
        ' *     Parse a declaration statement, at file, namespace, or type level.
        ' *     Current token should be set to current statement to be parsed.
        ' *
        ' **********************************************************************/
        Friend Function ParseDeclarationStatement() As StatementSyntax
            Dim oldHadImplicitLineContinuation = _hadImplicitLineContinuation
            Dim oldHadLineContinuationComment = _hadLineContinuationComment

            Try
                _hadImplicitLineContinuation = False
                _hadLineContinuationComment = False

                Dim statementSyntax = ParseDeclarationStatementInternal()

                Debug.Assert(Not _hadLineContinuationComment OrElse _hadImplicitLineContinuation)
                If _hadImplicitLineContinuation Then
                    Dim original = statementSyntax
                    statementSyntax = CheckFeatureAvailability(Feature.LineContinuation, statementSyntax)

                    If original Is statementSyntax AndAlso _hadLineContinuationComment Then
                        statementSyntax = CheckFeatureAvailability(Feature.LineContinuationComments, statementSyntax)
                    End If
                End If

                Return statementSyntax
            Finally
                _hadImplicitLineContinuation = oldHadImplicitLineContinuation
                _hadLineContinuationComment = oldHadLineContinuationComment
            End Try
        End Function

        Friend Function ParseDeclarationStatementInternal() As StatementSyntax
            _cancellationToken.ThrowIfCancellationRequested()

            ' ParseEnumMember is now handled below with case NodeKind.Identifier
            ' ParseInterfaceGroupStatement is now handled by InterfaceBlockContext
            ' ParsePropertyOrEventGroupStatement is now handed by ParsePropertyAccessor and ParseEventAccessor

            Select Case CurrentToken.Kind

                Case SyntaxKind.LessThanToken
                    ' If the attribute specifier includes "Module" or "Assembly",
                    ' it's a standalone attribute statement. Otherwise, it is
                    ' associated with a particular declaration.

                    Dim nextToken As SyntaxToken = PeekToken(1)

                    If IsContinuableEOL(1) Then
                        nextToken = PeekToken(2)
                    End If

                    Dim kind As SyntaxKind = Nothing
                    If TryTokenAsKeyword(nextToken, kind) AndAlso (kind = SyntaxKind.AssemblyKeyword OrElse
                        kind = SyntaxKind.ModuleKeyword) Then
                        ' Attribute statements can appear only at file level before any
                        ' declarations or option statements.

                        Dim attributes = ParseAttributeLists(True)

                        Return SyntaxFactory.AttributesStatement(attributes)
                    End If

                    Return ParseSpecifierDeclaration()

                Case SyntaxKind.LessThanGreaterThanToken
                    Dim attributes = ParseEmptyAttributeLists()
                    Return ParseSpecifierDeclaration(attributes)

                Case SyntaxKind.PrivateKeyword,
                    SyntaxKind.ProtectedKeyword,
                    SyntaxKind.PublicKeyword,
                    SyntaxKind.FriendKeyword,
                    SyntaxKind.MustInheritKeyword,
                    SyntaxKind.NotOverridableKeyword,
                    SyntaxKind.OverridableKeyword,
                    SyntaxKind.MustOverrideKeyword,
                    SyntaxKind.NotInheritableKeyword,
                    SyntaxKind.PartialKeyword,
                    SyntaxKind.StaticKeyword,
                    SyntaxKind.SharedKeyword,
                    SyntaxKind.ShadowsKeyword,
                    SyntaxKind.WithEventsKeyword,
                    SyntaxKind.OverloadsKeyword,
                    SyntaxKind.OverridesKeyword,
                    SyntaxKind.ConstKeyword,
                    SyntaxKind.DimKeyword,
                    SyntaxKind.ReadOnlyKeyword,
                    SyntaxKind.WriteOnlyKeyword,
                    SyntaxKind.WideningKeyword,
                    SyntaxKind.NarrowingKeyword,
                    SyntaxKind.DefaultKeyword
                    Return ParseSpecifierDeclaration()

                Case SyntaxKind.EnumKeyword
                    Return ParseEnumStatement()

                Case SyntaxKind.InheritsKeyword,
                    SyntaxKind.ImplementsKeyword
                    Return ParseInheritsImplementsStatement(Nothing, Nothing)

                Case SyntaxKind.ImportsKeyword
                    Return ParseImportsStatement(Nothing, Nothing)

                Case SyntaxKind.NamespaceKeyword
                    ' Error check moved to ParseNamespaceStatement
                    Return ParseNamespaceStatement(Nothing, Nothing)

                Case SyntaxKind.ModuleKeyword, SyntaxKind.ClassKeyword, SyntaxKind.StructureKeyword, SyntaxKind.InterfaceKeyword
                    ' Error check moved to ParseTypeStatement
                    Return ParseTypeStatement()

                Case SyntaxKind.DeclareKeyword
                    Return ParseProcDeclareStatement(Nothing, Nothing)

                Case SyntaxKind.EventKeyword
                    ' Custom Event is now handled when processing identifiers because Custom is parsed as a modifier
                    Return ParseEventDefinition(Nothing, Nothing)

                Case SyntaxKind.DelegateKeyword
                    Return ParseDelegateStatement(Nothing, Nothing)

                ' These end the module level declarations and begin
                ' the procedure definitions.

                Case SyntaxKind.SubKeyword
                    Return ParseSubStatement(Nothing, Nothing)

                Case SyntaxKind.FunctionKeyword
                    Return ParseFunctionStatement(Nothing, Nothing)

                Case SyntaxKind.OperatorKeyword
                    Return ParseOperatorStatement(Nothing, Nothing)

                Case SyntaxKind.PropertyKeyword
                    Return ParsePropertyDefinition(Nothing, Nothing)

                Case SyntaxKind.EmptyToken
                    Return ParseEmptyStatement()

                Case SyntaxKind.ColonToken,
                    SyntaxKind.StatementTerminatorToken
                    Debug.Assert(False, "Unexpected terminator: " & CurrentToken.Kind.ToString())
                    Return ParseStatementInMethodBodyInternal()

                Case SyntaxKind.IntegerLiteralToken
                    If IsFirstStatementOnLine(CurrentToken) Then
                        Return ParseLabel()
                    End If
                    Return ReportUnrecognizedStatementError(ERRID.ERR_Syntax)

                Case SyntaxKind.IdentifierToken
                    If Context.BlockKind = SyntaxKind.EnumBlock Then
                        Return ParseEnumMemberOrLabel(Nothing)
                    End If

                    ' Enables better error for wrong uses of the "Custom" modifier
                    Dim contextualKind As SyntaxKind = Nothing

                    If TryIdentifierAsContextualKeyword(CurrentToken, contextualKind) Then
                        If contextualKind = SyntaxKind.CustomKeyword Then
                            Return ParseCustomEventDefinition(Nothing, Nothing)
                        ElseIf contextualKind = SyntaxKind.TypeKeyword Then
                            ' "Type" is now "Structure"
                            Return ReportUnrecognizedStatementError(ERRID.ERR_ObsoleteStructureNotType)
                        ElseIf contextualKind = SyntaxKind.AsyncKeyword OrElse contextualKind = SyntaxKind.IteratorKeyword Then
                            Return ParseSpecifierDeclaration()
                        End If
                    End If

                    ' The following token is a keyword that starts declaration, so the current token (identifier)
                    ' is probably an incomplete specifier. Let's not parse it as a statement it will certainly be an error.
                    Dim statement = ParsePossibleDeclarationStatement()
                    If statement IsNot Nothing Then
                        Return statement
                    End If

                    If Context.BlockKind = SyntaxKind.CompilationUnit Then
                        Return ParseStatementInMethodBodyInternal()
                    End If

                    If ShouldParseAsLabel() Then
                        Return ParseLabel()
                    Else
                        Return ReportUnrecognizedStatementError(ERRID.ERR_ExpectedDeclaration)
                    End If

                Case SyntaxKind.EndKeyword
                    Return ParseGroupEndStatement()

                Case SyntaxKind.OptionKeyword
                    Return ParseOptionStatement(Nothing, Nothing)

                Case SyntaxKind.AddHandlerKeyword
                    Return ParsePropertyOrEventAccessor(SyntaxKind.AddHandlerAccessorStatement, Nothing, Nothing)

                Case SyntaxKind.RemoveHandlerKeyword
                    Return ParsePropertyOrEventAccessor(SyntaxKind.RemoveHandlerAccessorStatement, Nothing, Nothing)

                Case SyntaxKind.RaiseEventKeyword
                    Return ParsePropertyOrEventAccessor(SyntaxKind.RaiseEventAccessorStatement, Nothing, Nothing)

                Case SyntaxKind.GetKeyword
                    Return ParsePropertyOrEventAccessor(SyntaxKind.GetAccessorStatement, Nothing, Nothing)

                Case SyntaxKind.SetKeyword
                    Return ParsePropertyOrEventAccessor(SyntaxKind.SetAccessorStatement, Nothing, Nothing)

                Case SyntaxKind.GlobalKeyword
                    ' The following token is a keyword that starts declaration, so the current token (global)
                    ' shouldn't be parsed as a statement. This might happen when a member declaration 
                    ' immediately follows "Namespace Global" without a new line or if the user incorrectly 
                    ' uses Global as a modifier.
                    Dim statement = ParsePossibleDeclarationStatement()
                    If statement IsNot Nothing Then
                        Return statement
                    End If
                    Return ParseStatementInMethodBodyInternal()

                Case Else
                    ' misplaced statement errors are reported by the context
                    Return ParseStatementInMethodBodyInternal()
            End Select

        End Function

        Private Function ParsePossibleDeclarationStatement() As StatementSyntax
            Dim possibleDeclarationStart = PeekToken(1).Kind
            If SyntaxFacts.CanStartSpecifierDeclaration(possibleDeclarationStart) OrElse
               SyntaxFacts.IsSpecifier(possibleDeclarationStart) Then

                Dim idf = CurrentToken
                GetNextToken()

                Return ParseSpecifierDeclaration().AddLeadingSyntax(idf, ERRID.ERR_ExpectedDeclaration)
            Else
                Return Nothing
            End If
        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseStatementInMethodBody
        ' *
        ' * Purpose:
        ' *     Parses a statement that can occur inside a method body.
        ' *
        ' **********************************************************************/
        ' File:Parser.cpp
        ' Lines: 2606 - 2606
        ' .Parser::ParseStatementInMethodBody( [ _Inout_ bool& ErrorInConstruct ] )
        Friend Function ParseStatementInMethodBody() As StatementSyntax
            Dim oldHadImplicitLineContinuation = _hadImplicitLineContinuation
            Dim oldHadLineContinuationComment = _hadLineContinuationComment

            Try
                _hadImplicitLineContinuation = False
                _hadLineContinuationComment = False

                Dim statementSyntax = ParseStatementInMethodBodyInternal()

                Debug.Assert(Not _hadLineContinuationComment OrElse _hadImplicitLineContinuation)
                If _hadImplicitLineContinuation Then
                    Dim original = statementSyntax
                    statementSyntax = CheckFeatureAvailability(Feature.LineContinuation, statementSyntax)

                    If original Is statementSyntax AndAlso _hadLineContinuationComment Then
                        statementSyntax = CheckFeatureAvailability(Feature.LineContinuationComments, statementSyntax)
                    End If
                End If

                Return statementSyntax
            Finally
                _hadImplicitLineContinuation = oldHadImplicitLineContinuation
                _hadLineContinuationComment = oldHadLineContinuationComment
            End Try
        End Function

        Friend Function ParseStatementInMethodBodyInternal() As StatementSyntax

            Try
                _recursionDepth += 1
                StackGuard.EnsureSufficientExecutionStack(_recursionDepth)

                Return ParseStatementInMethodBodyCore()
            Finally
                _recursionDepth -= 1
            End Try
        End Function

        Private Function ParseStatementInMethodBodyCore() As StatementSyntax
            _cancellationToken.ThrowIfCancellationRequested()

            Select Case CurrentToken.Kind

                Case SyntaxKind.GoToKeyword
                    Return ParseGotoStatement()

                Case SyntaxKind.CaseKeyword
                    Return ParseCaseStatement()

                Case SyntaxKind.SelectKeyword
                    Return ParseSelectStatement()

                Case SyntaxKind.WithKeyword, SyntaxKind.WhileKeyword
                    Return ParseExpressionBlockStatement()

                Case SyntaxKind.UsingKeyword
                    Return ParseUsingStatement()

                Case SyntaxKind.SyncLockKeyword
                    Return ParseExpressionBlockStatement()

                Case SyntaxKind.TryKeyword
                    Return ParseTry()

                Case SyntaxKind.CatchKeyword
                    Return ParseCatch()

                Case SyntaxKind.FinallyKeyword
                    Return ParseFinally()

                Case SyntaxKind.IfKeyword
                    Return ParseIfStatement()

                'TODO - davidsch - In C++ code there is a call to GreedilyParseColonSeparatedStatements.  Why?

                Case SyntaxKind.ElseKeyword
                    If PeekToken(1).Kind = SyntaxKind.IfKeyword Then
                        Return ParseElseIfStatement()
                    Else
                        Return ParseElseStatement()
                    End If

                Case SyntaxKind.ElseIfKeyword
                    Return ParseElseIfStatement()

                Case SyntaxKind.DoKeyword
                    Return ParseDoStatement()

                Case SyntaxKind.LoopKeyword
                    Return ParseLoopStatement()

                Case SyntaxKind.ForKeyword
                    Return ParseForStatement()

                Case SyntaxKind.NextKeyword
                    Return ParseNextStatement()

                Case SyntaxKind.EndIfKeyword, SyntaxKind.WendKeyword
                    ' If ... Endif are anachronistic
                    ' While...Wend are anachronistic
                    Return ParseAnachronisticStatement()

                Case SyntaxKind.EndKeyword
                    Return ParseEndStatement()

                Case SyntaxKind.ReturnKeyword
                    Return ParseReturnStatement()

                Case SyntaxKind.StopKeyword
                    Return ParseStopOrEndStatement()

                Case SyntaxKind.ContinueKeyword
                    Return ParseContinueStatement()

                Case SyntaxKind.ExitKeyword
                    Return ParseExitStatement()

                Case SyntaxKind.OnKeyword
                    Return ParseOnErrorStatement()

                Case SyntaxKind.ResumeKeyword
                    Return ParseResumeStatement()

                Case SyntaxKind.CallKeyword
                    Return ParseCallStatement()

                Case SyntaxKind.RaiseEventKeyword
                    Return ParseRaiseEventStatement()

                Case SyntaxKind.ReDimKeyword
                    Return ParseRedimStatement()

                Case SyntaxKind.AddHandlerKeyword, SyntaxKind.RemoveHandlerKeyword
                    Return ParseHandlerStatement()

                Case SyntaxKind.PartialKeyword,
                 SyntaxKind.PrivateKeyword,
                 SyntaxKind.ProtectedKeyword,
                 SyntaxKind.PublicKeyword,
                 SyntaxKind.FriendKeyword,
                 SyntaxKind.NotOverridableKeyword,
                 SyntaxKind.OverridableKeyword,
                 SyntaxKind.MustInheritKeyword,
                 SyntaxKind.MustOverrideKeyword,
                 SyntaxKind.StaticKeyword,
                 SyntaxKind.SharedKeyword,
                 SyntaxKind.ShadowsKeyword,
                 SyntaxKind.WithEventsKeyword,
                 SyntaxKind.OverloadsKeyword,
                 SyntaxKind.OverridesKeyword,
                 SyntaxKind.ConstKeyword,
                 SyntaxKind.DimKeyword,
                 SyntaxKind.WideningKeyword,
                 SyntaxKind.NarrowingKeyword,
                 SyntaxKind.DefaultKeyword,
                 SyntaxKind.ReadOnlyKeyword,
                 SyntaxKind.WriteOnlyKeyword,
                 SyntaxKind.LessThanToken
                    ' ParseSpecifierDeclaration parses the way we want.
                    ' Move error check to ExecutableStatementContext

                    Dim attributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax) = Nothing

                    If Me.CurrentToken.Kind = Global.Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.LessThanToken Then
                        attributes = ParseAttributeLists(allowFileLevelAttributes:=False)
                    End If

                    Dim modifiers = ParseSpecifiers()

                    If Not modifiers.Any(SyntaxKind.DimKeyword, SyntaxKind.ConstKeyword) Then
                        ' cover the case that this is an invalid variable declaration, e.g.
                        ' Dim Namespace as Integer
                        ' this is esp. important for the keywords that would be handled in the select below.
                        ' do not treat this as variable declarations if the modifiers are used for 
                        ' sub, function, operator or property declarations. See Parser.cpp, Line 4256
                        Select Case CurrentToken.Kind
                            Case SyntaxKind.SubKeyword,
                                SyntaxKind.ClassKeyword,
                                SyntaxKind.EnumKeyword,
                                SyntaxKind.StructureKeyword,
                                SyntaxKind.InterfaceKeyword,
                                SyntaxKind.FunctionKeyword,
                                SyntaxKind.OperatorKeyword,
                                SyntaxKind.PropertyKeyword,
                                SyntaxKind.EventKeyword
                                Return ParseSpecifierDeclaration(attributes, modifiers)

                            Case SyntaxKind.IdentifierToken
                                ' Check if begins event
                                Dim contextualKind As SyntaxKind = Nothing

                                If TryIdentifierAsContextualKeyword(CurrentToken, contextualKind) Then
                                    If contextualKind = SyntaxKind.CustomKeyword AndAlso PeekToken(1).Kind = SyntaxKind.EventKeyword Then
                                        Return ParseSpecifierDeclaration(attributes, modifiers)
                                    End If
                                End If
                        End Select
                    End If

                    Return ParseVarDeclStatement(attributes, modifiers)

                Case SyntaxKind.SetKeyword, SyntaxKind.LetKeyword
                    Return ParseAssignmentStatement()

                Case SyntaxKind.ErrorKeyword
                    Return ParseError()

                Case SyntaxKind.ThrowKeyword
                    Return ParseThrowStatement()

                Case SyntaxKind.IntegerLiteralToken
                    If IsFirstStatementOnLine(CurrentToken) Then
                        Return ParseLabel()
                    End If

                Case SyntaxKind.IdentifierToken
                    'TODO Move all of this code to ParseIdentifier

                    If ShouldParseAsLabel() Then
                        Return ParseLabel()
                    End If

                    ' Check for a non-reserved keyword that can start
                    ' a special syntactic construct. Such identifiers are treated as keywords
                    ' unless the statement looks like an assignment statement.
                    Dim contextualKind As SyntaxKind = Nothing

                    If TryIdentifierAsContextualKeyword(CurrentToken, contextualKind) Then
                        If contextualKind = SyntaxKind.MidKeyword Then
                            ' it can only possibly start a mid statement assignment if Mid/Mid$ is followed by a "(".
                            ' However this will now always recognize any method call with this identifier as a mid statement,
                            ' as well as array accesses named mid. 
                            If PeekToken(1).Kind = SyntaxKind.OpenParenToken Then
                                Return ParseMid()
                            End If

                        ElseIf contextualKind = SyntaxKind.CustomKeyword AndAlso PeekToken(1).Kind = SyntaxKind.EventKeyword Then ' BeginsEvent
                            Return ParseSpecifierDeclaration()

                        ElseIf contextualKind = SyntaxKind.AsyncKeyword OrElse contextualKind = SyntaxKind.IteratorKeyword Then

                            Dim nextToken = PeekToken(1)

                            If SyntaxFacts.IsSpecifier(nextToken.Kind) OrElse SyntaxFacts.CanStartSpecifierDeclaration(nextToken.Kind) Then
                                Return ParseSpecifierDeclaration()
                            End If

                        ElseIf contextualKind = SyntaxKind.AwaitKeyword AndAlso
                               Context.IsWithinAsyncMethodOrLambda Then
                            Return ParseAwaitStatement()

                        ElseIf contextualKind = SyntaxKind.YieldKeyword AndAlso
                               Context.IsWithinIteratorMethodOrLambdaOrProperty Then
                            Return ParseYieldStatement()

                        End If
                    End If

                    Return ParseAssignmentOrInvocationStatement()

                Case SyntaxKind.DotToken,
                        SyntaxKind.ExclamationToken,
                        SyntaxKind.MyBaseKeyword,
                        SyntaxKind.MyClassKeyword,
                        SyntaxKind.MeKeyword,
                        SyntaxKind.GlobalKeyword,
                        SyntaxKind.ShortKeyword,
                        SyntaxKind.UShortKeyword,
                        SyntaxKind.IntegerKeyword,
                        SyntaxKind.UIntegerKeyword,
                        SyntaxKind.LongKeyword,
                        SyntaxKind.ULongKeyword,
                        SyntaxKind.DecimalKeyword,
                        SyntaxKind.SingleKeyword,
                        SyntaxKind.DoubleKeyword,
                        SyntaxKind.SByteKeyword,
                        SyntaxKind.ByteKeyword,
                        SyntaxKind.BooleanKeyword,
                        SyntaxKind.CharKeyword,
                        SyntaxKind.DateKeyword,
                        SyntaxKind.StringKeyword,
                        SyntaxKind.VariantKeyword,
                        SyntaxKind.ObjectKeyword,
                        SyntaxKind.DirectCastKeyword,
                        SyntaxKind.TryCastKeyword,
                        SyntaxKind.CTypeKeyword,
                        SyntaxKind.CBoolKeyword,
                        SyntaxKind.CDateKeyword,
                        SyntaxKind.CDblKeyword,
                        SyntaxKind.CSByteKeyword,
                        SyntaxKind.CByteKeyword,
                        SyntaxKind.CCharKeyword,
                        SyntaxKind.CShortKeyword,
                        SyntaxKind.CUShortKeyword,
                        SyntaxKind.CIntKeyword,
                        SyntaxKind.CUIntKeyword,
                        SyntaxKind.CLngKeyword,
                        SyntaxKind.CULngKeyword,
                        SyntaxKind.CSngKeyword,
                        SyntaxKind.CStrKeyword,
                        SyntaxKind.CDecKeyword,
                        SyntaxKind.CObjKeyword,
                        SyntaxKind.GetTypeKeyword,
                        SyntaxKind.GetXmlNamespaceKeyword
                    Return ParseAssignmentOrInvocationStatement()

                Case SyntaxKind.EmptyToken
                    Return ParseEmptyStatement()

                Case SyntaxKind.ColonToken,
                    SyntaxKind.StatementTerminatorToken
                    Debug.Assert(False, "Unexpected terminator: " & CurrentToken.Kind.ToString())

                Case SyntaxKind.EraseKeyword
                    Return ParseErase()

                Case SyntaxKind.GetKeyword
                    If (IsValidStatementTerminator(PeekToken(1)) OrElse PeekToken(1).Kind = SyntaxKind.OpenParenToken) AndAlso
                       Context.IsWithin(SyntaxKind.SetAccessorBlock, SyntaxKind.GetAccessorBlock) Then

                        Return ParsePropertyOrEventAccessor(SyntaxKind.GetAccessorStatement, Nothing, Nothing)
                    Else
                        Return ReportUnrecognizedStatementError(ERRID.ERR_ObsoleteGetStatement)
                    End If

                Case SyntaxKind.GosubKeyword
                    Return ParseAnachronisticStatement()

                'TODO - Move the check below ExecutableStatementContext.ProcessStatement

                Case SyntaxKind.InheritsKeyword,
                        SyntaxKind.ImplementsKeyword,
                        SyntaxKind.OptionKeyword,
                        SyntaxKind.ImportsKeyword,
                        SyntaxKind.DeclareKeyword,
                        SyntaxKind.DelegateKeyword,
                        SyntaxKind.InterfaceKeyword,
                        SyntaxKind.PropertyKeyword,
                        SyntaxKind.SubKeyword,
                        SyntaxKind.FunctionKeyword,
                        SyntaxKind.OperatorKeyword,
                        SyntaxKind.EventKeyword,
                        SyntaxKind.NamespaceKeyword,
                        SyntaxKind.ClassKeyword,
                        SyntaxKind.StructureKeyword,
                        SyntaxKind.EnumKeyword,
                        SyntaxKind.ModuleKeyword
                    ' This used to return a BadStatement with ERRID_InvInsideEndsProc.
                    ' Just delegate to ParseDeclarationStatement and let the context add the error
                    Return ParseDeclarationStatementInternal()

                Case SyntaxKind.QuestionToken

                    If CanStartConsequenceExpression(Me.PeekToken(1).Kind, qualified:=False) Then
                        Return ParseAssignmentOrInvocationStatement()
                    Else
                        Return ParsePrintStatement()
                    End If

                Case Else
                    If CanFollowStatement(CurrentToken) Then
                        ' It's an error for a single-statement lambda to be empty, e.g. "Console.WriteLine(Sub())"
                        ' But we're not in the best position to report that error, because we don't know span locations &c.
                        ' So what we'll do is return an empty statement. Inside ParseStatementLambda it catches the case
                        ' where the first statement is empty and reports an error. It also catches the case where the
                        ' first statement is non-empty and is followed by a colon. Therefore, if we encounter this
                        ' branch we're in right now, then we'll definitely return to ParseStatementLambda / single-line,
                        ' and we'll definitely report a good and appropriate error. The error won't be lost!
                        Return InternalSyntaxFactory.EmptyStatement
                    End If
            End Select

            'TODO - Remove when select is fully implemented
            Return ReportUnrecognizedStatementError(ERRID.ERR_Syntax)
        End Function

        Private Function ParseEmptyStatement() As EmptyStatementSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.EmptyToken)
            Dim emptyToken = DirectCast(CurrentToken, PunctuationSyntax)
            GetNextToken()
            Return InternalSyntaxFactory.EmptyStatement(emptyToken)
        End Function

        '
        '============ Methods for parsing declaration constructs ============
        '

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseSpecifierDeclaration
        ' *
        ' * Purpose:
        ' *
        ' **********************************************************************/
        ' File:Parser.cpp
        ' Lines: 4184 - 4184
        ' Statement* .Parser::ParseSpecifierDeclaration( [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ParseSpecifierDeclaration() As StatementSyntax
            Dim attributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax) = Nothing

            If Me.CurrentToken.Kind = Global.Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.LessThanToken Then
                attributes = ParseAttributeLists(False)
            End If

            Return ParseSpecifierDeclaration(attributes)
        End Function

        Private Function ParseSpecifierDeclaration(attributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax)) As StatementSyntax
            Dim modifiers = ParseSpecifiers()
            Return ParseSpecifierDeclaration(attributes, modifiers)
        End Function

        Private Function ParseSpecifierDeclaration(
            attributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax),
            modifiers As CoreInternalSyntax.SyntaxList(Of KeywordSyntax)
        ) As StatementSyntax
            Dim statement As StatementSyntax = Nothing

            ' Current token set to token after the last specifier
            Select Case (CurrentToken.Kind)

                Case SyntaxKind.PropertyKeyword
                    statement = ParsePropertyDefinition(attributes, modifiers)

                Case SyntaxKind.IdentifierToken
                    If Context.BlockKind = SyntaxKind.EnumBlock AndAlso Not modifiers.Any Then
                        statement = ParseEnumMemberOrLabel(attributes)
                    Else
                        Dim keyword As KeywordSyntax = Nothing
                        If TryIdentifierAsContextualKeyword(CurrentToken, keyword) Then
                            If keyword.Kind = SyntaxKind.CustomKeyword Then
                                Return ParseCustomEventDefinition(attributes, modifiers)

                            ElseIf keyword.Kind = SyntaxKind.TypeKeyword Then
                                Dim nextToken = PeekToken(1)
                                If nextToken.Kind = SyntaxKind.IdentifierToken AndAlso
                                IsValidStatementTerminator(PeekToken(2)) AndAlso
                                modifiers.AnyAndOnly(SyntaxKind.PublicKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.FriendKeyword, SyntaxKind.PrivateKeyword) Then
                                    ' Type is now Structure
                                    statement = ReportUnrecognizedStatementError(ERRID.ERR_ObsoleteStructureNotType, attributes, modifiers)
                                    Exit Select
                                End If
                            End If
                        End If

                        ' Dim or Const declaration.
                        statement = ParseVarDeclStatement(attributes, modifiers)
                    End If

                Case SyntaxKind.EnumKeyword
                    statement = ParseEnumStatement(attributes, modifiers)

                Case SyntaxKind.ModuleKeyword, SyntaxKind.ClassKeyword, SyntaxKind.StructureKeyword, SyntaxKind.InterfaceKeyword
                    statement = ParseTypeStatement(attributes, modifiers)

                Case SyntaxKind.DeclareKeyword
                    statement = ParseProcDeclareStatement(attributes, modifiers)

                Case SyntaxKind.EventKeyword
                    statement = ParseEventDefinition(attributes, modifiers)

                Case SyntaxKind.SubKeyword
                    statement = ParseSubStatement(attributes, modifiers)

                Case SyntaxKind.FunctionKeyword
                    statement = ParseFunctionStatement(attributes, modifiers)

                Case SyntaxKind.OperatorKeyword
                    statement = ParseOperatorStatement(attributes, modifiers)

                Case SyntaxKind.DelegateKeyword
                    statement = ParseDelegateStatement(attributes, modifiers)

                Case SyntaxKind.AddHandlerKeyword
                    statement = ParsePropertyOrEventAccessor(SyntaxKind.AddHandlerAccessorStatement, attributes, modifiers)

                Case SyntaxKind.RemoveHandlerKeyword
                    statement = ParsePropertyOrEventAccessor(SyntaxKind.RemoveHandlerAccessorStatement, attributes, modifiers)

                Case SyntaxKind.RaiseEventKeyword
                    statement = ParsePropertyOrEventAccessor(SyntaxKind.RaiseEventAccessorStatement, attributes, modifiers)

                Case SyntaxKind.GetKeyword
                    statement = ParsePropertyOrEventAccessor(SyntaxKind.GetAccessorStatement, attributes, modifiers)

                Case SyntaxKind.SetKeyword
                    statement = ParsePropertyOrEventAccessor(SyntaxKind.SetAccessorStatement, attributes, modifiers)

                ' InheritsKeyword, ImplementsKeyword, ImportsKeyword, NamespaceKeyword, OptionKeyword are all
                ' error cases.  Parse the statement anyway. The statement will report that the attributes or modifiers
                ' are not allowed.
                Case SyntaxKind.InheritsKeyword,
                    SyntaxKind.ImplementsKeyword
                    statement = ParseInheritsImplementsStatement(attributes, modifiers)

                Case SyntaxKind.ImportsKeyword
                    statement = ParseImportsStatement(attributes, modifiers)

                Case SyntaxKind.NamespaceKeyword
                    statement = ParseNamespaceStatement(attributes, modifiers)

                Case SyntaxKind.OptionKeyword
                    statement = ParseOptionStatement(attributes, modifiers)

                Case Else

                    ' Error recovery. Try to give a more descriptive error
                    ' depending on what we're currently at and possibly recover.
                    '
                    Select Case Context.BlockKind
                        Case _
                            SyntaxKind.ModuleBlock,
                            SyntaxKind.StructureBlock,
                            SyntaxKind.InterfaceBlock,
                            SyntaxKind.ClassBlock,
                            SyntaxKind.EnumBlock,
                            SyntaxKind.PropertyBlock,
                            SyntaxKind.NamespaceBlock,
                            SyntaxKind.CompilationUnit

                            ' if it's legal to declare a member in the current context then this statement should
                            ' be an IncompleteMemberSyntax

                            If attributes.Any AndAlso Not modifiers.Any Then
                                ' attributes without a modifier should report 
                                ' "Attribute specifier is not a complete statement. Use a line continuation to apply the 
                                ' attribute to the following statement."
                                ' this error usually get's reported within "ParseVarDeclStatement", which will not be called
                                ' in this path
                                statement = ReportUnrecognizedStatementError(ERRID.ERR_StandaloneAttribute, attributes, modifiers)

                            ElseIf modifiers.Any AndAlso CurrentToken.IsKeyword Then
                                ' if there is a keyword following one or more modifiers, report invalid use of keyword
                                statement = ReportUnrecognizedStatementError(ERRID.ERR_InvalidUseOfKeyword, attributes, modifiers, forceErrorOnFirstToken:=True)

                            Else
                                ' fallback: report missing identifier.

                                ' add a missing identifier token to report the error on
                                statement = ReportUnrecognizedStatementError(ERRID.ERR_ExpectedIdentifier, attributes, modifiers, createMissingIdentifier:=True)
                            End If

                        Case Else

                            ' if it cannot be a member (inside a method body) this statement should be a variable 
                            ' declaration (with a missing identifier)
                            statement = ParseVarDeclStatement(attributes, modifiers)
                    End Select
            End Select

            Return statement
        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseEnumStatement
        ' *
        ' * Purpose:
        ' *     Parses: Enum <ident>
        ' *
        ' **********************************************************************/
        ' File:Parser.cpp
        ' Lines: 4352 - 4352
        ' EnumTypeStatement* .Parser::ParseEnumStatement( [ ParseTree::AttributeSpecifierList* Attributes ] [ ParseTree::SpecifierList* Specifiers ] [ Token* Start ] [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ParseEnumStatement(
                  Optional attributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax) = Nothing,
                  Optional modifiers As CoreInternalSyntax.SyntaxList(Of KeywordSyntax) = Nothing
        ) As EnumStatementSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.EnumKeyword, "ParseEnumStatement called on the wrong token.")

            Dim enumKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            Dim optionalUnderlyingType As AsClauseSyntax = Nothing

            GetNextToken() ' Get Off ENUM

            Dim identifier = ParseIdentifier()

            If CurrentToken.Kind = SyntaxKind.OpenParenToken Then
                ' Enums cannot be generic
                Dim genericParameters = ReportSyntaxError(ParseGenericParameters, ERRID.ERR_GenericParamsOnInvalidMember)
                identifier = identifier.AddTrailingSyntax(genericParameters)
            End If

            If identifier.ContainsDiagnostics Then
                identifier = identifier.AddTrailingSyntax(ResyncAt({SyntaxKind.AsKeyword}))
            End If

            If CurrentToken.Kind = SyntaxKind.AsKeyword Then
                Dim asKeyword = DirectCast(CurrentToken, KeywordSyntax)

                GetNextToken() ' get off AS

                Dim typeName = ParseTypeName()

                If typeName.ContainsDiagnostics Then
                    typeName = typeName.AddTrailingSyntax(ResyncAt())
                End If

                optionalUnderlyingType = SyntaxFactory.SimpleAsClause(asKeyword, Nothing, typeName)
            End If

            Dim statement As EnumStatementSyntax = SyntaxFactory.EnumStatement(attributes, modifiers, enumKeyword, identifier, optionalUnderlyingType)

            Return statement
        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseEnumMember
        ' *
        ' * Purpose:
        ' *     Parses an enum member definition.
        ' *
        ' *     Does NOT advance to next line so caller can recover from errors.
        ' *
        ' **********************************************************************/

        ' File:Parser.cpp
        ' Lines: 4438 - 4438
        ' Statement* .Parser::ParseEnumMember( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseEnumMemberOrLabel(attributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax)) As StatementSyntax

            If Not attributes.Any() AndAlso ShouldParseAsLabel() Then
                Return ParseLabel()
            End If

            ' The current token should be an Identifier
            ' The Dev10 code used to look ahead to see if the statement was a declaration to exit out of en enum declaration.
            ' The new parser calls ParseEnumMember from ParseDeclaration so this look ahead is not necessary.  The enum block
            ' parsing will terminate when the bad statement is added to the enum block context.

            ' Check to see if this construct is a valid module-level declaration.
            ' If it is, end the current enum context and reparse the statement.
            ' (This case is important for automatic end insertion.)

            Dim ident As IdentifierTokenSyntax = ParseIdentifier()

            If ident.ContainsDiagnostics Then
                ident = ident.AddTrailingSyntax(ResyncAt({SyntaxKind.EqualsToken}))
            End If

            ' See if there is an expression

            Dim initializer As EqualsValueSyntax = Nothing
            Dim optionalEquals As PunctuationSyntax = Nothing
            Dim expr As ExpressionSyntax = Nothing

            If TryGetTokenAndEatNewLine(SyntaxKind.EqualsToken, optionalEquals) Then

                expr = ParseExpressionCore()

                If expr.ContainsDiagnostics Then
                    ' Resync at EOS so we don't get any more errors.
                    expr = ResyncAt(expr)
                End If

                initializer = SyntaxFactory.EqualsValue(optionalEquals, expr)

            End If

            Dim statement As EnumMemberDeclarationSyntax = SyntaxFactory.EnumMemberDeclaration(attributes, ident, initializer)

            Return statement

        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseTypeStatement
        ' *
        ' * Purpose:
        ' *
        ' **********************************************************************/

        ' [in] specifiers on decl
        ' [in] token starting Enum statement
        ' File:Parser.cpp
        ' Lines: 4563 - 4563
        ' TypeStatement* .Parser::ParseTypeStatement( [ ParseTree::AttributeSpecifierList* Attributes ] [ ParseTree::SpecifierList* Specifiers ] [ Token* Start ] [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ParseTypeStatement(
                  Optional attributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax) = Nothing,
                  Optional modifiers As CoreInternalSyntax.SyntaxList(Of KeywordSyntax) = Nothing
        ) As TypeStatementSyntax

            Dim kind As SyntaxKind
            Dim optionalTypeParameters As TypeParameterListSyntax = Nothing

            Dim typeKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

            Select Case (typeKeyword.Kind)

                Case SyntaxKind.ModuleKeyword
                    kind = SyntaxKind.ModuleStatement

                Case SyntaxKind.ClassKeyword
                    kind = SyntaxKind.ClassStatement

                Case SyntaxKind.StructureKeyword
                    kind = SyntaxKind.StructureStatement

                Case SyntaxKind.InterfaceKeyword
                    kind = SyntaxKind.InterfaceStatement

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(typeKeyword.Kind)
            End Select

            Dim ident As IdentifierTokenSyntax = ParseIdentifier()

            If ident.ContainsDiagnostics Then
                ident = ident.AddTrailingSyntax(ResyncAt({SyntaxKind.OfKeyword, SyntaxKind.OpenParenToken}))
            End If

            If CurrentToken.Kind = SyntaxKind.OpenParenToken Then
                ' Modules cannot be generic
                '
                If kind = SyntaxKind.ModuleStatement Then
                    ident = ident.AddTrailingSyntax(ReportGenericParamsDisallowedError(ERRID.ERR_ModulesCannotBeGeneric))
                Else
                    optionalTypeParameters = ParseGenericParameters()
                End If
            End If

            Dim statement As TypeStatementSyntax = InternalSyntaxFactory.TypeStatement(kind, attributes, modifiers, typeKeyword, ident, optionalTypeParameters)

            If (kind = SyntaxKind.ModuleStatement OrElse kind = SyntaxKind.InterfaceStatement) AndAlso statement.Modifiers.Any(SyntaxKind.PartialKeyword) Then
                statement = CheckFeatureAvailability(If(kind = SyntaxKind.ModuleStatement, Feature.PartialModules, Feature.PartialInterfaces), statement)
            End If

            Return statement
        End Function

        ' File:Parser.cpp
        ' Lines: 4640 - 4640
        ' .Parser::ReportGenericParamsDisallowedError( [ ERRID errid ] [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ReportGenericParamsDisallowedError(errid As ERRID) As TypeParameterListSyntax

            Dim typeParameters As TypeParameterListSyntax = ParseGenericParameters()

            If typeParameters.CloseParenToken.IsMissing Then
                typeParameters = ResyncAt(typeParameters)
            End If

            typeParameters = ReportSyntaxError(typeParameters, errid)
            typeParameters = AdjustTriviaForMissingTokens(typeParameters)

            Return typeParameters

        End Function

        ' File:Parser.cpp
        ' Lines: 4730 - 4730
        ' .Parser::RejectGenericParametersForMemberDecl( [ _In_ bool& ErrorInConstruct ] )

        Private Function TryRejectGenericParametersForMemberDecl(ByRef genericParams As TypeParameterListSyntax) As Boolean
            If Not BeginsGeneric() Then
                genericParams = Nothing
                Return False
            End If

            genericParams = ReportGenericParamsDisallowedError(ERRID.ERR_GenericParamsOnInvalidMember)
            Return True
        End Function

        Private Function ParseNamespaceStatement(attributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax), Specifiers As CoreInternalSyntax.SyntaxList(Of KeywordSyntax)) As NamespaceStatementSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.NamespaceKeyword, "ParseNamespaceStatement called on the wrong token.")

            Dim namespaceKeyword As KeywordSyntax = ReportModifiersOnStatementError(ERRID.ERR_SpecifiersInvalidOnInheritsImplOpt, attributes, Specifiers, DirectCast(CurrentToken, KeywordSyntax))

            If IsScript Then
                namespaceKeyword = AddError(namespaceKeyword, ERRID.ERR_NamespaceNotAllowedInScript)
            End If

            Dim unexpectedSyntax As CoreInternalSyntax.SyntaxList(Of SyntaxToken) = Nothing
            Dim result As NamespaceStatementSyntax

            GetNextToken() ' get off NAMESPACE token

            ' Don't require qualification
            ' Allow global
            ' No generics
            Dim namespaceName As NameSyntax = ParseName(
                requireQualification:=False,
                allowGlobalNameSpace:=True,
                allowGenericArguments:=False,
                allowGenericsWithoutOf:=True,
                isNameInNamespaceDeclaration:=True)

            If namespaceName.ContainsDiagnostics Then
                ' Resync at EOS so we don't get expecting EOS errors
                unexpectedSyntax = ResyncAt()
            End If

            result = SyntaxFactory.NamespaceStatement(namespaceKeyword, namespaceName)

            If unexpectedSyntax.Node IsNot Nothing Then
                result = result.AddTrailingSyntax(unexpectedSyntax)
            End If

            Return result
        End Function

        Private Function ParseEndStatement() As StatementSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.EndKeyword, "ParseEndStatement called on wrong token.")

            ' Dev10#708061
            ' "End" is a keyword which takes an optional next argument. Things get confusing with "End Select"...
            ' This might come from "Dim x = From i In Sub() End Select i". But Dev10 spec says "inside the body
            ' of a single-line sub, we attempt to parse one statement greedily".
            '
            ' * Therefore we treat this Select as part of an "End Select" construct (which will make the above statement
            '   an error), and not as part of the query (which would make the above statement work).
            '
            ' The confusion never arose in Orcas. That's because the set of tokens which could come after an End in
            ' a compound GroupEndStatement was disjoint from the set of tokens that could come after a statement.
            ' Now in Dev10, in the case of a single-line sub, there's just one point of contention: "Select"
            ' (A complete list of End constructs: End If, ExternalSource, Region, Namespace, Module, Enum, Structure, Interface,
            ' Class, Sub, Operator, Enum, AddHandler, RemoveHandler, RaiseEvent, Property, Get, Set, With, SyncLock, Select,
            ' Using, While, Try. I got this list from the "VBGrammar" tool in src\vb\language\tools\VBGrammar. Of these,
            ' Select is the only token that can follow an expression.)
            '
            ' A beautiful bugfix would change the code to say: "First try to parse the following token as a compound
            ' GroupEndStatement. If that fails, then try to parse it as a statement-following-thing (e.g. :, EOL, comment,
            ' an "Else" in the context of a line-else, or a thing-that-follows-expression in the context of a single-line sub).
            ' But since "Select" is the solitary point of contention, I'll go for a uglier smaller fix:

            Dim nextToken = PeekToken(1)
            If CanFollowStatementButIsNotSelectFollowingExpression(nextToken) Then
                Return ParseStopOrEndStatement()
            End If

            Return ParseGroupEndStatement()
        End Function

        ' Parse an End statement that ends a statement group.

        ' File:Parser.cpp
        ' Lines: 5054 - 5054
        ' .Parser::ParseGroupEndStatement( [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ParseGroupEndStatement() As StatementSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.EndKeyword, "ParseGroupEndStatement called on wrong token.")

            Dim endKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            Dim nextToken = PeekToken(1)
            Dim possibleBlockKeyword = If(IsValidStatementTerminator(nextToken), Nothing, nextToken)
            Dim statement As StatementSyntax
            Dim endKind = GetEndStatementKindFromKeyword(nextToken.Kind)

            If endKind = SyntaxKind.None Then
                'TODO - Consider moving the error to the Declaration context
                ' Instead of parsing as an END statement, consider building the correct matching
                ' End with a missing keyword.
                statement = ReportSyntaxError(ParseStopOrEndStatement(), ERRID.ERR_UnrecognizedEnd)
            Else
                GetNextToken()
                GetNextToken()

                statement = SyntaxFactory.EndBlockStatement(endKind, endKeyword, DirectCast(possibleBlockKeyword, KeywordSyntax))
            End If

            Return statement
        End Function

        Private Function PeekEndStatement(i As Integer) As SyntaxKind

            Select Case PeekToken(i).Kind

                Case SyntaxKind.LoopKeyword
                    Return SyntaxKind.SimpleLoopStatement

                Case SyntaxKind.NextKeyword
                    Return SyntaxKind.NextStatement

                Case SyntaxKind.EndKeyword
                    Return GetEndStatementKindFromKeyword(PeekToken(i + 1).Kind)

                ' wend and endif are anachronistic and should not be used, however they can still appear in 
                ' the lookahead
                Case SyntaxKind.EndIfKeyword
                    Return SyntaxKind.EndIfStatement

                Case SyntaxKind.WendKeyword
                    Return SyntaxKind.EndWhileStatement
            End Select

            Return SyntaxKind.None
        End Function

        Private Shared Function GetEndStatementKindFromKeyword(kind As SyntaxKind) As SyntaxKind
            Select Case kind

                Case SyntaxKind.IfKeyword
                    Return SyntaxKind.EndIfStatement

                Case SyntaxKind.UsingKeyword
                    Return SyntaxKind.EndUsingStatement

                Case SyntaxKind.WithKeyword
                    Return SyntaxKind.EndWithStatement

                Case SyntaxKind.StructureKeyword
                    Return SyntaxKind.EndStructureStatement

                Case SyntaxKind.EnumKeyword
                    Return SyntaxKind.EndEnumStatement

                Case SyntaxKind.InterfaceKeyword
                    Return SyntaxKind.EndInterfaceStatement

                Case SyntaxKind.SubKeyword
                    Return SyntaxKind.EndSubStatement

                Case SyntaxKind.FunctionKeyword
                    Return SyntaxKind.EndFunctionStatement

                Case SyntaxKind.OperatorKeyword
                    Return SyntaxKind.EndOperatorStatement

                Case SyntaxKind.SelectKeyword
                    Return SyntaxKind.EndSelectStatement

                Case SyntaxKind.TryKeyword
                    Return SyntaxKind.EndTryStatement

                Case SyntaxKind.GetKeyword
                    Return SyntaxKind.EndGetStatement

                Case SyntaxKind.SetKeyword
                    Return SyntaxKind.EndSetStatement

                Case SyntaxKind.PropertyKeyword
                    Return SyntaxKind.EndPropertyStatement

                Case SyntaxKind.AddHandlerKeyword
                    Return SyntaxKind.EndAddHandlerStatement

                Case SyntaxKind.RemoveHandlerKeyword
                    Return SyntaxKind.EndRemoveHandlerStatement

                Case SyntaxKind.RaiseEventKeyword
                    Return SyntaxKind.EndRaiseEventStatement

                Case SyntaxKind.EventKeyword
                    Return SyntaxKind.EndEventStatement

                Case SyntaxKind.ClassKeyword
                    Return SyntaxKind.EndClassStatement

                Case SyntaxKind.ModuleKeyword
                    Return SyntaxKind.EndModuleStatement

                Case SyntaxKind.NamespaceKeyword
                    Return SyntaxKind.EndNamespaceStatement

                Case SyntaxKind.SyncLockKeyword
                    Return SyntaxKind.EndSyncLockStatement

                Case SyntaxKind.WhileKeyword
                    Return SyntaxKind.EndWhileStatement

                Case Else
                    Return SyntaxKind.None

            End Select
        End Function

        ' See Parser::EndOfMultilineLambda
        'TODO - Compare this method with IsDeclarationStatement.
        'Can these two methods be unified into one?
        Private Function PeekDeclarationStatement(i As Integer) As Boolean
            Do
                Dim token = PeekToken(i)

                Select Case token.Kind
                    Case SyntaxKind.PartialKeyword,
                        SyntaxKind.PrivateKeyword,
                        SyntaxKind.ProtectedKeyword,
                        SyntaxKind.PublicKeyword,
                        SyntaxKind.FriendKeyword,
                        SyntaxKind.NotOverridableKeyword,
                        SyntaxKind.OverridableKeyword,
                        SyntaxKind.MustInheritKeyword,
                        SyntaxKind.MustOverrideKeyword,
                        SyntaxKind.NotInheritableKeyword,
                        SyntaxKind.StaticKeyword,
                        SyntaxKind.SharedKeyword,
                        SyntaxKind.WithEventsKeyword,
                        SyntaxKind.OverloadsKeyword,
                        SyntaxKind.OverridesKeyword,
                        SyntaxKind.WideningKeyword,
                        SyntaxKind.NarrowingKeyword,
                        SyntaxKind.ReadOnlyKeyword,
                        SyntaxKind.WriteOnlyKeyword,
                        SyntaxKind.DefaultKeyword,
                        SyntaxKind.ShadowsKeyword,
                        SyntaxKind.CustomKeyword,
                        SyntaxKind.AsyncKeyword,
                        SyntaxKind.IteratorKeyword

                    Case SyntaxKind.IdentifierToken
                        Select Case DirectCast(token, IdentifierTokenSyntax).PossibleKeywordKind
                            Case SyntaxKind.CustomKeyword,
                                SyntaxKind.AsyncKeyword,
                                SyntaxKind.IteratorKeyword

                            Case Else
                                Return False
                        End Select

                    Case SyntaxKind.SubKeyword,
                        SyntaxKind.FunctionKeyword,
                        SyntaxKind.OperatorKeyword,
                        SyntaxKind.PropertyKeyword,
                        SyntaxKind.NamespaceKeyword,
                        SyntaxKind.ClassKeyword,
                        SyntaxKind.ModuleKeyword,
                        SyntaxKind.StructureKeyword,
                        SyntaxKind.InterfaceKeyword,
                        SyntaxKind.EnumKeyword,
                        SyntaxKind.EventKeyword,
                        SyntaxKind.GetKeyword,
                        SyntaxKind.SetKeyword,
                        SyntaxKind.DeclareKeyword,
                        SyntaxKind.DelegateKeyword
                        Return True

                    Case Else
                        Return False
                End Select

                i += 1
            Loop

        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseSpecifiers
        ' *
        ' * Purpose:
        ' *     Parses the specifier list of a declaration. The current token
        ' *     should be at the specifier. These specifiers can occur in
        ' *     ANY order.
        ' *
        ' **********************************************************************/
        ' File: Parser.cpp
        ' Lines: 5482 - 5482
        ' SpecifierList* .Parser::ParseSpecifiers( [ _Inout_ bool& ErrorInConstruct ] )

        'TODO - davidsch - The error checking here looks like parser doing semantics.  The grammar allows
        'a list of modifiers. Deferring semantic errors is important for incremental parsing. Note that some
        ' errors are done here and one in semantics. For consistency the errors should be reports in the same
        ' component, i.e. semantics or parser.

        Private Function ParseSpecifiers() As CoreInternalSyntax.SyntaxList(Of KeywordSyntax)

            Dim kwList = _pool.Allocate(Of KeywordSyntax)()

            ' Checks for at most one specifier from each family

            Do
                Dim err As ERRID = ERRID.ERR_None
                Dim t As SyntaxToken = CurrentToken

                Select Case (t.Kind)
                    ' Access category
                    Case SyntaxKind.PublicKeyword,
                         SyntaxKind.PrivateKeyword,
                         SyntaxKind.ProtectedKeyword,
                         SyntaxKind.FriendKeyword

                        ' Storage category
                    Case SyntaxKind.SharedKeyword,
                         SyntaxKind.ShadowsKeyword

                        ' Inheritance category
                    Case SyntaxKind.MustInheritKeyword,
                         SyntaxKind.OverloadsKeyword,
                         SyntaxKind.NotInheritableKeyword,
                         SyntaxKind.OverridesKeyword

                        ' Partial types category
                    Case SyntaxKind.PartialKeyword

                        ' Modifier category
                    Case SyntaxKind.NotOverridableKeyword,
                         SyntaxKind.OverridableKeyword,
                         SyntaxKind.MustOverrideKeyword

                        ' Writability category
                    Case SyntaxKind.ReadOnlyKeyword,
                         SyntaxKind.WriteOnlyKeyword

                    Case SyntaxKind.DimKeyword,
                         SyntaxKind.ConstKeyword,
                         SyntaxKind.StaticKeyword,
                         SyntaxKind.DefaultKeyword,
                         SyntaxKind.WithEventsKeyword

                        ' Conversion category
                    Case SyntaxKind.WideningKeyword,
                         SyntaxKind.NarrowingKeyword

                    Case SyntaxKind.IdentifierToken
                        ' This enables better error reporting for invalid uses of CUSTOM as a specifier.
                        '
                        ' But note that at the same time, CUSTOM used as a variable name etc. should
                        ' continue to work. See Bug VSWhidbey 379914.
                        '
                        Dim possibleKeyword As KeywordSyntax = Nothing
                        If TryTokenAsContextualKeyword(CurrentToken, possibleKeyword) Then
                            If possibleKeyword.Kind = SyntaxKind.CustomKeyword Then

                                Dim nextToken As SyntaxToken = PeekToken(1)
                                If nextToken.Kind = SyntaxKind.EventKeyword Then
                                    Exit Do
                                End If

                                If SyntaxFacts.IsSpecifier(nextToken.Kind) OrElse SyntaxFacts.CanStartSpecifierDeclaration(nextToken.Kind) Then
                                    t = ReportSyntaxError(possibleKeyword, ERRID.ERR_InvalidUseOfCustomModifier)
                                    Exit Select
                                End If

                            ElseIf possibleKeyword.Kind = SyntaxKind.AsyncKeyword OrElse
                                   possibleKeyword.Kind = SyntaxKind.IteratorKeyword Then

                                Dim nextToken As SyntaxToken = PeekToken(1)
                                If SyntaxFacts.IsSpecifier(nextToken.Kind) OrElse
                                   SyntaxFacts.CanStartSpecifierDeclaration(nextToken.Kind) Then

                                    t = possibleKeyword
                                    t = CheckFeatureAvailability(If(possibleKeyword.Kind = SyntaxKind.AsyncKeyword, Feature.AsyncExpressions, Feature.Iterators), t)
                                    Exit Select
                                End If

                            End If
                        End If

                        Exit Do

                    Case Else
                        Exit Do

                End Select

                Dim keyword = DirectCast(t, KeywordSyntax)

                If (err <> ERRID.ERR_None) Then
                    ' Mark the current token with the error and ignore.
                    keyword = ReportSyntaxError(keyword, err)
                End If

                kwList.Add(keyword)

                GetNextToken()
            Loop

            Dim result = kwList.ToList
            _pool.Free(kwList)

            Return result
        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseVarDeclStatement
        ' *
        ' * Purpose:
        ' *
        ' **********************************************************************/

        ' [in] specifiers on declaration
        ' [in] Token starting the statement
        ' File: Parser.cpp
        ' Lines: 5992 - 5992
        ' Statement* .Parser::ParseVarDeclStatement( [ ParseTree::AttributeSpecifierList* Attributes ] [ ParseTree::SpecifierList* Specifiers ] [ _In_ Token* StmtStart ] [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ParseVarDeclStatement(
            attributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax),
            modifiers As CoreInternalSyntax.SyntaxList(Of KeywordSyntax)
        ) As StatementSyntax
            ' Parse the declarations.

            Dim isFieldDeclaration As Boolean = False
            Select Case Context.BlockKind
                Case _
                    SyntaxKind.ModuleBlock,
                    SyntaxKind.StructureBlock,
                    SyntaxKind.InterfaceBlock,
                    SyntaxKind.ClassBlock,
                    SyntaxKind.EnumBlock,
                    SyntaxKind.PropertyBlock,
                    SyntaxKind.NamespaceBlock,
                    SyntaxKind.CompilationUnit
                    isFieldDeclaration = True
            End Select

            Dim Declarations = ParseVariableDeclaration(Not isFieldDeclaration)

            Dim result As StatementSyntax

            If isFieldDeclaration Then
                result = SyntaxFactory.FieldDeclaration(attributes, modifiers, Declarations)
            Else
                ' attributes must be empty
                ' modifiers can only be Static, Dim or Const
                result = SyntaxFactory.LocalDeclarationStatement(modifiers, Declarations)

                If attributes.Any Then

                    ' Does this look like a static local?
                    If modifiers.Any(SyntaxKind.StaticKeyword) Then
                        ' Do not report parser error about attributes applied to a static local,
                        ' but still attach them as leading trivia. This is done to mimic Dev11
                        ' behavior, which silently ignores the attributes.
                        result = result.AddLeadingSyntax(attributes.Node)
                    Else
                        result = result.AddLeadingSyntax(attributes.Node, ERRID.ERR_LocalsCannotHaveAttributes)
                    End If
                End If
            End If

            '  There must be at least one specifier.
            If Not modifiers.Any Then
                result = ReportSyntaxError(result,
                                           If(attributes.Any,
                                                ERRID.ERR_StandaloneAttribute,
                                                ERRID.ERR_ExpectedSpecifier))
            End If

            Return result

        End Function

        Private Function ParseVariableDeclaration(allowAsNewWith As Boolean) As CoreInternalSyntax.SeparatedSyntaxList(Of VariableDeclaratorSyntax)
            Dim declarations = _pool.AllocateSeparated(Of VariableDeclaratorSyntax)()

            Dim comma As PunctuationSyntax
            Dim checkForCustom As Boolean = True

            Dim declarators = _pool.AllocateSeparated(Of ModifiedIdentifierSyntax)()
            Do
                declarators.Clear()

                ' Parse the declarators.
                ' name1, name2, name3, .... etc

                Do
                    Dim declarator As ModifiedIdentifierSyntax = ParseModifiedIdentifier(True, checkForCustom)
                    checkForCustom = False

                    If declarator.ContainsDiagnostics Then
                        ' Resync so we don't get more errors later.
                        ' davidsch - removed syncing on tkRem because that is now trivia
                        declarator = ResyncAt(declarator, SyntaxKind.AsKeyword, SyntaxKind.CommaToken, SyntaxKind.NewKeyword, SyntaxKind.EqualsToken)
                    End If

                    declarators.Add(declarator)

                    comma = Nothing
                    If Not TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then
                        Exit Do
                    End If

                    declarators.AddSeparator(comma)

                Loop

                Dim names = declarators.ToList

                'TODO - For better error recovery consider adding a resync here for
                ' AsKeyword, EqualsToken or CommaToken
                ' if the current token is not one of these

                ' Parse the type clause.

                Dim optionalAsClause As AsClauseSyntax = Nothing
                Dim optionalInitializer As EqualsValueSyntax = Nothing

                ParseFieldOrPropertyAsClauseAndInitializer(False, allowAsNewWith, optionalAsClause, optionalInitializer)

                Dim declaration As VariableDeclaratorSyntax = SyntaxFactory.VariableDeclarator(names, optionalAsClause, optionalInitializer)

                declarations.Add(declaration)

                comma = Nothing
                If Not TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then
                    Exit Do
                End If

                declarations.AddSeparator(comma)
            Loop

            _pool.Free(declarators)

            Dim result = declarations.ToList

            _pool.Free(declarations)

            Return result
        End Function

        ' Parses the as-clause and initializer for both locals, fields and properties
        ' Properties allow Attributes before the type and allow implicit line continuations before "FROM", otherwise, fields and
        ' properties allow the same syntax.
        Private Sub ParseFieldOrPropertyAsClauseAndInitializer(isProperty As Boolean, allowAsNewWith As Boolean, ByRef optionalAsClause As AsClauseSyntax, ByRef optionalInitializer As EqualsValueSyntax)
            Dim asKeyword As KeywordSyntax = Nothing
            Dim newKeyword As KeywordSyntax = Nothing
            Dim newArguments As ArgumentListSyntax = Nothing
            Dim typeName As TypeSyntax = Nothing
            Dim fromKeyword As KeywordSyntax = Nothing

            ' Are there attributes before the type of the property?
            Dim attributesNode As GreenNode = Nothing

            If CurrentToken.Kind = SyntaxKind.AsKeyword Then
                asKeyword = DirectCast(CurrentToken, KeywordSyntax)
                GetNextToken()

                Dim objectCollectionInitializer As ObjectCollectionInitializerSyntax = Nothing

                ' At this point, we've seen the As so we're expecting a type.
                If CurrentToken.Kind = SyntaxKind.NewKeyword Then
                    newKeyword = DirectCast(CurrentToken, KeywordSyntax)
                    GetNextToken()

                    If isProperty AndAlso CurrentToken.Kind = SyntaxKind.LessThanToken Then
                        attributesNode = ParseAttributeLists(False).Node
                    End If

                    If CurrentToken.Kind = SyntaxKind.WithKeyword Then
                        ' Roslyn supports 'As New With {...}' 
                        optionalAsClause = Nothing
                        ' the rest will be parsed and an instance of optionalAsClause will be 
                        ' created in the code section marked as 'parse the initializer', see below

                    Else
                        typeName = ParseTypeName()

                        If CurrentToken.Kind = SyntaxKind.OpenParenToken Then
                            ' New <Type> ( <Arguments> )
                            newArguments = ParseParenthesizedArguments()
                        End If

                        ' Properties allow a new line before the FROM.
                        If isProperty Then
                            TryEatNewLineIfFollowedBy(SyntaxKind.FromKeyword)  ' Dev10_509577
                        End If

                        'Parse From {expression, expression, ...}
                        ' From is consumed in ParseInitializerList
                        If TryTokenAsContextualKeyword(CurrentToken, SyntaxKind.FromKeyword, fromKeyword) Then
                            GetNextToken()

                            ' true,  //allow expressions
                            ' false //don't allow assignments.
                            objectCollectionInitializer = ParseObjectCollectionInitializer(fromKeyword)
                        End If

                        optionalAsClause =
                            SyntaxFactory.AsNewClause(asKeyword,
                                               New ObjectCreationExpressionSyntax(
                                                   SyntaxKind.ObjectCreationExpression,
                                                   newKeyword, attributesNode, typeName,
                                                   newArguments, objectCollectionInitializer))

                    End If

                Else

                    ' Are there attributes before the type of the property?
                    If isProperty AndAlso CurrentToken.Kind = SyntaxKind.LessThanToken Then
                        attributesNode = ParseAttributeLists(False).Node
                    End If

                    typeName = ParseGeneralType()

                    If typeName.ContainsDiagnostics Then
                        typeName = ResyncAt(typeName, SyntaxKind.CommaToken, SyntaxKind.EqualsToken)
                    End If

                    optionalAsClause = SyntaxFactory.SimpleAsClause(asKeyword, attributesNode, typeName)
                End If

            End If

            ' Parse the initializer.

            Dim Equals As PunctuationSyntax = Nothing

            If newKeyword Is Nothing Then
                If TryGetTokenAndEatNewLine(SyntaxKind.EqualsToken, Equals) Then
                    'Parse = Expression

                    ' Make the initializer expression a deferred expression
                    ' Allow expression initializer
                    ' Disallow assignment initializer
                    Dim value As ExpressionSyntax = ParseExpressionCore(OperatorPrecedence.PrecedenceNone) 'Dev10 was ParseInitializer

                    Debug.Assert(Equals IsNot Nothing)
                    optionalInitializer = SyntaxFactory.EqualsValue(Equals, value)

                    If optionalInitializer.ContainsDiagnostics Then
                        optionalInitializer = ResyncAt(optionalInitializer, SyntaxKind.CommaToken)
                    End If
                End If
            Else
                Dim objectMemberInitializer As ObjectMemberInitializerSyntax = Nothing

                ' TODO - Consider improving the handling of implicit line continuations.
                ' Properties allow a newline before FROM, but not before WITH. A newline should also be allowed.
                ' Fields should be the same as properties. Local variables do not allow the newline because of 
                ' the ambiguity with a WITH statement and ambiguity with FROM used as an identifier. The latter 
                ' two ambiguities could be solved by looking ahead for the '{' token. 
                If CurrentToken.Kind = SyntaxKind.WithKeyword Then

                    'Handle the "With" clause in the following syntax:
                    'Dim x as new Customer With {.Id = 1, .Name = "A"}

                    Dim withKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)

                    If fromKeyword IsNot Nothing Then

                        Debug.Assert(optionalAsClause IsNot Nothing)

                        'With clause is not allowed after a From initializer
                        withKeyword = ReportSyntaxError(withKeyword, ERRID.ERR_CantCombineInitializers)
                        optionalAsClause = optionalAsClause.AddTrailingSyntax(withKeyword)

                        ' need to get off "With" keyword
                        GetNextToken()
                    Else

                        ' Parse With { ... }
                        objectMemberInitializer = ParseObjectInitializerList(anonymousTypeInitializer:=typeName Is Nothing,
                                                                             anonymousTypesAllowedHere:=allowAsNewWith)

                        Dim possibleKeyword As KeywordSyntax = Nothing
                        If CurrentToken.Kind = SyntaxKind.IdentifierToken AndAlso TryIdentifierAsContextualKeyword(CurrentToken, possibleKeyword) Then
                            Debug.Assert(possibleKeyword IsNot Nothing)

                            If possibleKeyword.Kind = SyntaxKind.FromKeyword Then
                                'From clause is not allowed after a With initializer
                                objectMemberInitializer = objectMemberInitializer.AddTrailingSyntax(possibleKeyword, ERRID.ERR_CantCombineInitializers)

                                ' need to get off "With" keyword
                                GetNextToken()
                            End If
                        End If

                        Dim creationExpression As NewExpressionSyntax = Nothing
                        If typeName Is Nothing Then
                            Debug.Assert(optionalAsClause Is Nothing)

                            ' If anonymous type is actually no allowed
                            If Not allowAsNewWith Then
                                withKeyword = ReportSyntaxError(withKeyword, ERRID.ERR_UnrecognizedTypeKeyword)
                            End If

                            ' NOTE: 'As New With {.x=1}' is legal in Roslyn
                            creationExpression = New AnonymousObjectCreationExpressionSyntax(
                                SyntaxKind.AnonymousObjectCreationExpression, newKeyword, Nothing, objectMemberInitializer)
                        Else
                            Debug.Assert(optionalAsClause IsNot Nothing)
                            creationExpression = New ObjectCreationExpressionSyntax(
                                SyntaxKind.ObjectCreationExpression, newKeyword,
                                        attributesNode, typeName, newArguments, objectMemberInitializer)
                        End If
                        optionalAsClause = SyntaxFactory.AsNewClause(asKeyword, creationExpression)
                    End If

                End If

            End If
        End Sub

        ''' <summary>
        '''  Parses a CollectionInitializer 
        '''         CollectionInitializer -> "{" CollectionInitializerList "}"
        '''         CollectionInitializerList ->  CollectionElement {"," CollectionElement}*
        '''         CollectionElement -> Expression | CollectionInitializer
        ''' </summary>
        ''' <returns>CollectionInitializerSyntax</returns>
        ''' <remarks>In the grammar ArrayLiteralExpression is a rename of CollectionInitializer</remarks>
        Private Function ParseCollectionInitializer() As CollectionInitializerSyntax

            Dim openBrace As PunctuationSyntax = Nothing
            If Not TryGetTokenAndEatNewLine(SyntaxKind.OpenBraceToken, openBrace, createIfMissing:=True) Then
                Return SyntaxFactory.CollectionInitializer(openBrace, Nothing, InternalSyntaxFactory.MissingPunctuation(SyntaxKind.CloseBraceToken))
            End If

            Dim initializers As CoreInternalSyntax.SeparatedSyntaxList(Of ExpressionSyntax) = Nothing

            If CurrentToken.Kind <> SyntaxKind.CloseBraceToken Then

                Dim expressions = _pool.AllocateSeparated(Of ExpressionSyntax)()

                Do
                    'This used to call ParseInitializer
                    Dim Initializer As ExpressionSyntax = ParseExpressionCore(OperatorPrecedence.PrecedenceNone) 'Dev 10 was ParseInitializer

                    If Initializer.ContainsDiagnostics Then
                        Initializer = ResyncAt(Initializer, SyntaxKind.CommaToken, SyntaxKind.CloseBraceToken)
                    End If

                    expressions.Add(Initializer)

                    Dim comma As PunctuationSyntax = Nothing
                    If TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then
                        expressions.AddSeparator(comma)
                    Else
                        Exit Do
                    End If

                Loop

                initializers = expressions.ToList
                _pool.Free(expressions)

            End If

            Dim closeBrace = GetClosingRightBrace()
            Return SyntaxFactory.CollectionInitializer(openBrace, initializers, closeBrace)
        End Function

        Private Function GetClosingRightBrace() As PunctuationSyntax
            Dim closeBrace As PunctuationSyntax = Nothing
            Dim skipped As CoreInternalSyntax.SyntaxList(Of SyntaxToken) = Nothing

            ' Dev10 does not resync but this seems to give better results
            ' when there is an error. See bug 904910.

            If CurrentToken.Kind <> SyntaxKind.CloseBraceToken Then
                skipped = ResyncAt({SyntaxKind.CloseBraceToken})
            End If

            TryEatNewLineAndGetToken(SyntaxKind.CloseBraceToken, closeBrace, createIfMissing:=True)

            If skipped.Node IsNot Nothing Then
                closeBrace = closeBrace.AddLeadingSyntax(skipped, ERRID.ERR_ExpectedRbrace)
            End If

            Return closeBrace
        End Function

        ''' <summary>
        ''' Parses
        ''' "With "{" FieldInitializerList "}"
        ''' FieldInitializerList -> FieldInitializer {"," FieldInitializer}*
        ''' FieldInitializer -> {Key? "." IdentifierOrKeyword "="}? Expression
        ''' 
        '''  e.g.
        '''  Dim x as new Customer With {.Id = 1, .Name = "A"}
        ''' </summary>
        ''' <returns>ObjectMemberInitializer</returns>
        Private Function ParseObjectInitializerList(Optional anonymousTypeInitializer As Boolean = False, Optional anonymousTypesAllowedHere As Boolean = True) As ObjectMemberInitializerSyntax

            Debug.Assert(CurrentToken.Kind = SyntaxKind.WithKeyword, "ParseObjectInitializerList called with wrong token")

            ' Handle the "With" clause in the following syntax:
            '  Dim x as new Customer With {.Id = 1, .Name = "A"}

            Dim withKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)

            ' the parsed type name already had this diagnostic attached, but in case of anonymous types the type name 
            ' will be dropped. Therefore we attach the error to the first token of the object initializer.
            If anonymousTypeInitializer AndAlso Not anonymousTypesAllowedHere Then
                withKeyword = ReportSyntaxError(withKeyword, ERRID.ERR_UnrecognizedTypeKeyword)
            End If

            GetNextToken() ' Get off WITH
            If PeekPastStatementTerminator().Kind = SyntaxKind.OpenBraceToken Then
                TryEatNewLine() ' Dev10 622723 allow implicit line continuation after WITH
            End If

            ' Parse the initializer list after the "With" keyword

            ' Dev10 was call to ParseInitializerList with 
            '   disallow expression initializers
            '   allow assignment initializers
            '   not an anonymous type initializer

            Dim openBrace As PunctuationSyntax = Nothing
            If Not TryGetTokenAndEatNewLine(SyntaxKind.OpenBraceToken, openBrace, createIfMissing:=True) Then
                Return SyntaxFactory.ObjectMemberInitializer(withKeyword, openBrace, Nothing, InternalSyntaxFactory.MissingPunctuation(SyntaxKind.CloseBraceToken))
            End If

            Dim initializers As CoreInternalSyntax.SeparatedSyntaxList(Of FieldInitializerSyntax) = Nothing

            If CurrentToken.Kind <> SyntaxKind.CloseBraceToken AndAlso
                CurrentToken.Kind <> SyntaxKind.StatementTerminatorToken AndAlso
                CurrentToken.Kind <> SyntaxKind.ColonToken Then

                Dim expressions = _pool.AllocateSeparated(Of FieldInitializerSyntax)()

                Do
                    'TODO - davidsch - This used to call ParseInitializer which checked for DotToken before calling ParseAssignmentInitializer
                    ' Verify that the error path is still the same.
                    ' Named initializer of form "."<Identifier>"="
                    Dim initializer As FieldInitializerSyntax = ParseAssignmentInitializer(anonymousTypeInitializer) 'Dev10 was ParseInitializer

                    If initializer.ContainsDiagnostics Then
                        initializer = ResyncAt(initializer, SyntaxKind.CommaToken, SyntaxKind.CloseBraceToken)
                    End If

                    expressions.Add(initializer)

                    Dim comma As PunctuationSyntax = Nothing
                    If TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then
                        expressions.AddSeparator(comma)
                    Else
                        Exit Do
                    End If

                Loop

                initializers = expressions.ToList
                _pool.Free(expressions)

            Else
                ' Create a missing initializer
                openBrace = ReportSyntaxError(openBrace, If(anonymousTypeInitializer, ERRID.ERR_AnonymousTypeNeedField, ERRID.ERR_InitializerExpected))
                ' NOTE: ERR_AnonymousTypeNeedField error will be reported on a different span then it was reported by Dev10
            End If

            Dim closeBrace = GetClosingRightBrace()
            Return SyntaxFactory.ObjectMemberInitializer(withKeyword, openBrace, initializers, closeBrace)

        End Function

        ''' <summary>
        '''   Parses an ObjectCollectionInitializer
        '''         ObjectCollectionInitializer -> "from" CollectionInitializer
        ''' 
        ''' </summary>
        ''' <returns>ObjectCollectionInitializer</returns>
        ''' <remarks>In Dev10 this was called ParseInitializerList.  It also took several boolean parameters.  
        '''  These were always set as 
        '''       AllowExpressionInitializers = true
        '''       AllowAssignmentInitializers = false
        '''       AnonymousTypeInitializer = false
        '''       RequireAtleastOneInitializer = false
        ''' 
        '''  While the grammar uses the nonterminal CollectionInitializer is modeled as an
        '''  AnonymousArrayCreationExpression which has the identical syntax "{" Expression {"," Expression }* "}"
        ''' </remarks>
        ''' 
        Private Function ParseObjectCollectionInitializer(fromKeyword As KeywordSyntax) As ObjectCollectionInitializerSyntax
            Debug.Assert(fromKeyword IsNot Nothing)

            fromKeyword = CheckFeatureAvailability(Feature.CollectionInitializers, fromKeyword)

            ' Allow implicit line continuation after FROM (dev10_508839) but only if followed by "{". 
            ' This is to avoid reporting an error at the beginning of then next line and then skipping the next statement.
            If CurrentToken.Kind = SyntaxKind.StatementTerminatorToken AndAlso PeekToken(1).Kind = SyntaxKind.OpenBraceToken Then
                TryEatNewLine()
            End If

            Dim initializer = ParseCollectionInitializer()

            Return SyntaxFactory.ObjectCollectionInitializer(fromKeyword, initializer)

        End Function

        ''' <summary>
        ''' Parses a FieldInitializer
        ''' 
        ''' FieldInitializer -> ("key"? "." IdentifierOrKeyword "=")? Expression
        ''' </summary>
        ''' <param name="anonymousTypeInitializer">If true then allow the keyword "key" to prefix the field initializer</param>
        ''' <returns></returns>
        Private Function ParseAssignmentInitializer(anonymousTypeInitializer As Boolean) As FieldInitializerSyntax
            Dim optionalKey As KeywordSyntax = Nothing
            Dim dot As PunctuationSyntax = Nothing
            Dim id As IdentifierTokenSyntax = Nothing
            Dim equals As PunctuationSyntax = Nothing
            Dim expression As ExpressionSyntax

            ' Parse form: Key? '.'<IdentifierOrKeyword> '=' <Expression>

            If anonymousTypeInitializer AndAlso
                TryTokenAsContextualKeyword(CurrentToken, SyntaxKind.KeyKeyword, optionalKey) Then
                GetNextToken() ' consume "key"
            End If

            If CurrentToken.Kind = SyntaxKind.DotToken Then
                dot = DirectCast(CurrentToken, PunctuationSyntax)
                GetNextToken()

                id = ParseIdentifierAllowingKeyword()

                If SyntaxKind.QuestionToken = CurrentToken.Kind Then
                    id = id.AddTrailingSyntax(CurrentToken)
                    'TODO - davidsch - Dev10 error is on .Name?
                    ' Here is it Name?
                    id = ReportSyntaxError(id, ERRID.ERR_NullableTypeInferenceNotSupported)
                    GetNextToken()
                End If

                If CurrentToken.Kind = SyntaxKind.EqualsToken Then
                    equals = DirectCast(CurrentToken, PunctuationSyntax)
                    GetNextToken()
                    TryEatNewLine()
                Else
                    equals = InternalSyntaxFactory.MissingPunctuation(SyntaxKind.EqualsToken)
                    equals = ReportSyntaxError(equals, ERRID.ERR_ExpectedAssignmentOperatorInInit)

                    ' Name is bad because only a simple name is allowed. But this is arguable.
                    ' This is required for semantics to avoid giving more confusing errors to the user in this context.
                End If

            ElseIf anonymousTypeInitializer Then
                expression = ParseExpressionCore() 'Dev10 was ParseInitializer()

                Dim propertyName As SyntaxToken
                Dim isNameDictionaryAccess As Boolean = False
                Dim isRejectedXmlName As Boolean = False

                propertyName = expression.ExtractAnonymousTypeMemberName(
                                                              isNameDictionaryAccess,
                                                              isRejectedXmlName)

                If propertyName Is Nothing OrElse propertyName.IsMissing Then

                    Select Case expression.Kind

                        Case SyntaxKind.NumericLiteralExpression,
                            SyntaxKind.CharacterLiteralExpression,
                            SyntaxKind.StringLiteralExpression,
                            SyntaxKind.DateLiteralExpression
                            expression = ReportSyntaxError(expression, ERRID.ERR_AnonymousTypeExpectedIdentifier)

                        Case Else
                            If expression.Kind = SyntaxKind.EqualsExpression Then
                                Dim binaryExpr = DirectCast(expression, BinaryExpressionSyntax)
                                If binaryExpr.Left.Kind = SyntaxKind.IdentifierName Then
                                    expression = ReportSyntaxError(expression, ERRID.ERR_AnonymousTypeNameWithoutPeriod)
                                    Exit Select
                                End If
                            End If

                            Dim skipped = ResyncAt({SyntaxKind.CommaToken, SyntaxKind.CloseBraceToken})

                            If isRejectedXmlName Then
                                ' TODO -  In Dev 10 error is on the xmlName
                                expression = ReportSyntaxError(expression, ERRID.ERR_AnonTypeFieldXMLNameInference)
                            Else
                                expression = ReportSyntaxError(expression, ERRID.ERR_AnonymousTypeFieldNameInference)
                            End If

                            expression = expression.AddTrailingSyntax(skipped)

                    End Select

                End If

                Return SyntaxFactory.InferredFieldInitializer(optionalKey, expression)

            Else
                ' Assume that the "'.'<IdentifierOrKeyword> '='" was left out.

                dot = InternalSyntaxFactory.MissingPunctuation(SyntaxKind.DotToken)
                id = InternalSyntaxFactory.MissingIdentifier()
                id = ReportSyntaxError(id, ERRID.ERR_ExpectedQualifiedNameInInit)
                equals = InternalSyntaxFactory.MissingPunctuation(SyntaxKind.EqualsToken)
            End If

            ' allow expression initializer
            ' disallow assignment initializer
            expression = ParseExpressionCore() 'Dev10 was ParseInitializer()

            Return SyntaxFactory.NamedFieldInitializer(optionalKey, dot, SyntaxFactory.IdentifierName(id), equals, expression)
        End Function

        ' See Parser::ParseInitializerList and how it is used by the Parser::ParseNewExpression

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseDeclarator
        ' *
        ' * Purpose:
        ' *     Parses: Identifier[ArrayList]
        ' *     in a variable declaration or a type field declaration.
        ' *
        ' *     Current token should be at beginning of expected declarator.
        ' *
        ' *     The result will have been created by the caller.
        ' *
        ' **********************************************************************/

        ' File: Parser.cpp
        ' Lines: 6816 - 6816
        ' .Parser::ParseDeclarator( [ bool AllowExplicitArraySizes ] [ _Out_ ParseTree::Declarator* Result ] [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseModifiedIdentifier(AllowExplicitArraySizes As Boolean, checkForCustom As Boolean) As ModifiedIdentifierSyntax
            Dim identifierStartPrev As SyntaxToken = PrevToken
            Dim identifierStart As SyntaxToken = CurrentToken
            Dim id As IdentifierTokenSyntax
            Dim optionalNullable As PunctuationSyntax = Nothing
            Dim customModifierError As Boolean = False

            If checkForCustom Then
                Dim keyword As KeywordSyntax = Nothing
                If TryTokenAsContextualKeyword(identifierStart, SyntaxKind.CustomKeyword, keyword) Then
                    ' This enables better error reporting for invalid uses of CUSTOM as a specifier.
                    '
                    ' But note that at the same time, CUSTOM used as a variable name etc. should
                    ' continue to work. See Bug VSWhidbey 379914.
                    '
                    ' Even though CUSTOM is not a reserved keyword, the Dev10 scanner always converts a CUSTOM followed
                    ' by EVENT to a keyword. As a result CUSTOM EVENT never comes here because the tokens are tkCustom, tkEvent. 
                    ' With the new scanner CUSTOM is returned as an identifier so the following must check for EVENT and not
                    ' signal an error.
                    Dim nextToken As SyntaxToken = PeekToken(1)
                    customModifierError = SyntaxFacts.IsSpecifier(nextToken.Kind) OrElse SyntaxFacts.CanStartSpecifierDeclaration(nextToken.Kind)
                End If
            End If

            ' Often, programmers put extra decl specifiers where they are
            ' not required. Eg:
            '    Dim x as Integer, Dim y as Long
            ' We want to check for this and give a more informative error.
            If SyntaxFacts.IsSpecifier(identifierStart.Kind) Then

                ' We don't want to look for specifiers if the erroneous declarator starts on a new line.
                ' This is because we want to recover the error on the previous line and treat the line with the
                ' specifier as a new statement
                If identifierStartPrev IsNot Nothing AndAlso identifierStartPrev.IsEndOfLine Then

                    id = InternalSyntaxFactory.MissingIdentifier()
                    id = ReportSyntaxError(id, ERRID.ERR_ExpectedIdentifier)
                    Return SyntaxFactory.ModifiedIdentifier(id, Nothing, Nothing, Nothing)
                End If

                Dim modifiers = ParseSpecifiers()

                ' Try to parse a declarator again. We don't mark the
                ' declarator with an error even though there really was an error.
                ' If we do get back a valid declarator, we have a well-formed tree.
                ' We've corrected the error. Otherwise, the second parse is necessary in order
                ' to produce a diagnostic.

                id = ParseNullableIdentifier(optionalNullable).AddLeadingSyntax(modifiers.Node, ERRID.ERR_ExtraSpecifiers)

            Else
                ' /*allowNullable*/
                id = ParseNullableIdentifier(optionalNullable)
                If customModifierError Then
                    id = ReportSyntaxError(id, ERRID.ERR_InvalidUseOfCustomModifier)
                End If

            End If

            ' Check for an array declarator.

            If CurrentToken.Kind = SyntaxKind.OpenParenToken Then
                Return ParseArrayModifiedIdentifier(id, optionalNullable, AllowExplicitArraySizes)
            End If

            Return SyntaxFactory.ModifiedIdentifier(id, optionalNullable, Nothing, Nothing)

        End Function

        ' Parse an identifier followed by optional? (but not optional array bounds), and return modified identifier
        ' Used inside LINQ queries.
        Private Function ParseNullableModifiedIdentifier() As ModifiedIdentifierSyntax
            Dim optionalNullable As PunctuationSyntax = Nothing
            Dim id As IdentifierTokenSyntax = ParseNullableIdentifier(optionalNullable)

            Return SyntaxFactory.ModifiedIdentifier(id, optionalNullable, Nothing, Nothing)
        End Function

        ' File: Parser.cpp
        ' Lines: 6908 - 6908
        ' bool .Parser::CanTokenStartTypeName( [ _In_
        ' Token* Token ] )

        Private Shared Function CanTokenStartTypeName(Token As SyntaxToken) As Boolean
            Debug.Assert(Token IsNot Nothing)

            If SyntaxFacts.IsPredefinedTypeOrVariant(Token.Kind) Then
                Return True
            End If

            Select Case (Token.Kind)

                Case SyntaxKind.GlobalKeyword,
                    SyntaxKind.IdentifierToken

                    Return True
            End Select

            Return False
        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseTypeName
        ' *
        ' * Purpose:
        ' *     Parses a Type name.
        ' **********************************************************************/
        ' File: Parser.cpp
        ' Lines: 6939 - 6939
        ' Type* .Parser::ParseTypeName( [ _Inout_ bool& ErrorInConstruct ] [ bool AllowEmptyGenericArguments ] [ _Out_opt_ bool* AllowedEmptyGenericArguments ] )

        ''' <summary>
        ''' Parse and return a TypeName.  Assumes the CurrentToken is on the name.
        ''' </summary>
        ''' <param name="allowEmptyGenericArguments">Controls generic argument parsing</param>
        ''' <param name="allowedEmptyGenericArguments">Controls generic argument parsing</param>
        ''' <returns>TypeName</returns>
        Friend Function ParseTypeName(
            Optional nonArrayName As Boolean = False,
            Optional allowEmptyGenericArguments As Boolean = False,
            Optional ByRef allowedEmptyGenericArguments As Boolean = False
        ) As TypeSyntax

            Dim Start As SyntaxToken = CurrentToken
            Dim prev As SyntaxToken = PrevToken
            Dim typeName As TypeSyntax = Nothing
            Dim name As NameSyntax = Nothing
            Dim errorID As ERRID

            If SyntaxFacts.IsPredefinedTypeKeyword(Start.Kind) Then
                typeName = SyntaxFactory.PredefinedType(DirectCast(Start, KeywordSyntax))
            Else
                Select Case (Start.Kind)

                    Case SyntaxKind.VariantKeyword
                        name = SyntaxFactory.IdentifierName(_scanner.MakeIdentifier(DirectCast(Start, KeywordSyntax)))
                        name = ReportSyntaxError(name, ERRID.ERR_ObsoleteObjectNotVariant)

                    Case SyntaxKind.GlobalKeyword,
                        SyntaxKind.IdentifierToken
                        ' AllowGlobalNameSpace
                        ' Allow generic arguments
                        ' Don't disallow generic arguments on last qualified name
                        name = ParseName(
                            requireQualification:=False,
                            allowGlobalNameSpace:=True,
                            allowGenericArguments:=True,
                            allowGenericsWithoutOf:=True,
                            disallowGenericArgumentsOnLastQualifiedName:=False,
                            nonArrayName:=nonArrayName,
                            allowEmptyGenericArguments:=allowEmptyGenericArguments,
                            allowedEmptyGenericArguments:=allowedEmptyGenericArguments)

                        Debug.Assert(CanTokenStartTypeName(Start), "Inconsistency in type parsing routines!!!")
                        GoTo checkNullable

                    Case SyntaxKind.OpenParenToken
                        ' tuple type
                        ' (a as Integer, b as Long)
                        Dim openParen As PunctuationSyntax = Nothing
                        TryGetTokenAndEatNewLine(SyntaxKind.OpenParenToken, openParen)
                        typeName = ParseTupleType(openParen)
                        GoTo checkNullable

                    Case Else
                        If Start.Kind = SyntaxKind.NewKeyword AndAlso PeekToken(1).Kind = SyntaxKind.IdentifierToken Then
                            errorID = ERRID.ERR_InvalidNewInType

                            ' prev may be null when InternalSyntaxFactory.ParseTypeName is called.
                        ElseIf Start.Kind = SyntaxKind.OpenBraceToken AndAlso prev IsNot Nothing AndAlso prev.Kind = SyntaxKind.NewKeyword Then
                            errorID = ERRID.ERR_UnrecognizedTypeOrWith

                        ElseIf Start.IsKeyword() Then
                            errorID = ERRID.ERR_UnrecognizedTypeKeyword
                        Else
                            errorID = ERRID.ERR_UnrecognizedType
                        End If

                        ' Also Dev10 code does NOT consume any tokens here.
                        ' Should this error check be done in the parser or when the expression is evaluated?
                        ' Parser global should be removed
                        typeName = ReportSyntaxError(SyntaxFactory.IdentifierName(InternalSyntaxFactory.MissingIdentifier()), errorID)

                        Debug.Assert(Not CanTokenStartTypeName(Start), "Inconsistency in type parsing routines!!!")

                        Return typeName
                End Select
            End If

            Debug.Assert(CanTokenStartTypeName(Start), "Inconsistency in type parsing routines!!!")

            GetNextToken()

checkNullable:
            If typeName Is Nothing Then
                Debug.Assert(name IsNot Nothing)
                typeName = name
            End If

            If SyntaxKind.QuestionToken = CurrentToken.Kind Then
                If _evaluatingConditionCompilationExpression Then

                    typeName = typeName.AddTrailingSyntax(CurrentToken, ERRID.ERR_BadNullTypeInCCExpression)
                    GetNextToken()

                    Return typeName
                Else
                    If allowedEmptyGenericArguments Then
                        ' If there were empty generic arguments and the type is followed by "?" then report unrecognized type on the closing ")"
                        typeName = ReportUnrecognizedTypeInGeneric(typeName)
                    End If

                    Debug.Assert(typeName IsNot Nothing)

                    Dim questionMark As PunctuationSyntax = DirectCast(CurrentToken, PunctuationSyntax)

                    Dim nullableTypeName As NullableTypeSyntax = SyntaxFactory.NullableType(typeName, questionMark)

                    GetNextToken()

                    typeName = nullableTypeName
                End If
            End If

            Return typeName
        End Function

        Private Function ParseTupleType(openParen As PunctuationSyntax) As TypeSyntax
            Dim elementBuilder = _pool.AllocateSeparated(Of TupleElementSyntax)()
            Dim unexpected As GreenNode = Nothing

            Do
                Dim identifierNameOpt As IdentifierTokenSyntax = Nothing
                Dim asKeywordOpt As KeywordSyntax = Nothing

                ' if there is a type character or As, then this must be a name
                If CurrentToken.Kind = SyntaxKind.IdentifierToken AndAlso
                        (DirectCast(CurrentToken, IdentifierTokenSyntax).TypeCharacter <> TypeCharacter.None OrElse
                        PeekNextToken().Kind = SyntaxKind.AsKeyword) Then

                    identifierNameOpt = ParseIdentifier()
                    TryGetToken(SyntaxKind.AsKeyword, asKeywordOpt)
                End If

                Dim typeOpt As TypeSyntax = Nothing
                ' if have "As" or have no element name, must have a type
                If asKeywordOpt IsNot Nothing OrElse identifierNameOpt Is Nothing Then
                    typeOpt = ParseGeneralType()
                End If

                Dim element As TupleElementSyntax

                If identifierNameOpt IsNot Nothing Then
                    Dim simpleAsClause As SimpleAsClauseSyntax = Nothing
                    If asKeywordOpt IsNot Nothing Then
                        Debug.Assert(typeOpt IsNot Nothing)
                        simpleAsClause = SyntaxFactory.SimpleAsClause(asKeywordOpt, attributeLists:=Nothing, type:=typeOpt)
                    End If

                    element = SyntaxFactory.NamedTupleElement(identifierNameOpt, simpleAsClause)

                Else
                    Debug.Assert(typeOpt IsNot Nothing)
                    element = SyntaxFactory.TypedTupleElement(typeOpt)
                End If

                elementBuilder.Add(element)

                Dim commaToken As PunctuationSyntax = Nothing
                If TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, commaToken) Then
                    elementBuilder.AddSeparator(commaToken)
                    Continue Do

                ElseIf CurrentToken.Kind = SyntaxKind.CloseParenToken OrElse MustEndStatement(CurrentToken) Then
                    Exit Do

                Else
                    ' There is a syntax error of some kind.

                    Dim skipped = ResyncAt({SyntaxKind.CommaToken, SyntaxKind.CloseParenToken}).Node
                    If skipped IsNot Nothing AndAlso Not element.ContainsDiagnostics Then
                        skipped = ReportSyntaxError(skipped, ERRID.ERR_ArgumentSyntax)
                    End If

                    If CurrentToken.Kind = SyntaxKind.CommaToken Then
                        commaToken = DirectCast(CurrentToken, PunctuationSyntax)
                        commaToken = commaToken.AddLeadingSyntax(skipped)
                        elementBuilder.AddSeparator(commaToken)
                        GetNextToken()
                    Else
                        unexpected = skipped
                        Exit Do
                    End If
                End If
            Loop

            Dim closeParen As PunctuationSyntax = Nothing
            TryEatNewLineAndGetToken(SyntaxKind.CloseParenToken, closeParen, createIfMissing:=True)

            If unexpected IsNot Nothing Then
                closeParen = closeParen.AddLeadingSyntax(unexpected)
            End If

            If elementBuilder.Count < 2 Then
                Debug.Assert(elementBuilder.Count > 0)
                elementBuilder.AddSeparator(InternalSyntaxFactory.MissingToken(SyntaxKind.CommaToken))

                Dim missing = SyntaxFactory.IdentifierName(InternalSyntaxFactory.MissingIdentifier())
                missing = ReportSyntaxError(missing, ERRID.ERR_TupleTooFewElements)
                elementBuilder.Add(_syntaxFactory.TypedTupleElement(missing))
            End If

            Dim tupleElements = elementBuilder.ToList
            _pool.Free(elementBuilder)

            Dim tupleType = SyntaxFactory.TupleType(openParen, tupleElements, closeParen)

            tupleType = CheckFeatureAvailability(Feature.Tuples, tupleType)
            Return tupleType
        End Function

        Private Function ReportUnrecognizedTypeInGeneric(typeName As TypeSyntax) As TypeSyntax
            Select Case typeName.Kind
                Case SyntaxKind.QualifiedName
                    ' The open generic can be on either the right or left side of the qualified name.
                    Dim qualifiedName = DirectCast(typeName, QualifiedNameSyntax)
                    Dim genericName As GenericNameSyntax = TryCast(qualifiedName.Right, GenericNameSyntax)
                    If genericName IsNot Nothing Then
                        ' Report error on right
                        genericName = ReportUnrecognizedTypeInGeneric(genericName)
                        typeName = SyntaxFactory.QualifiedName(qualifiedName.Left, qualifiedName.DotToken, genericName)
                    Else
                        ' Report error on left
                        Dim leftName = DirectCast(ReportUnrecognizedTypeInGeneric(qualifiedName.Left), NameSyntax)
                        typeName = SyntaxFactory.QualifiedName(leftName, qualifiedName.DotToken, qualifiedName.Right)
                    End If

                Case SyntaxKind.GenericName
                    typeName = ReportUnrecognizedTypeInGeneric(DirectCast(typeName, GenericNameSyntax))

            End Select
            Return typeName
        End Function

        Private Function ReportUnrecognizedTypeInGeneric(genericName As GenericNameSyntax) As GenericNameSyntax
            Dim typeArgumentList = genericName.TypeArgumentList
            typeArgumentList = SyntaxFactory.TypeArgumentList(typeArgumentList.OpenParenToken,
                                                       typeArgumentList.OfKeyword,
                                                       typeArgumentList.Arguments,
                                                       ReportSyntaxError(typeArgumentList.CloseParenToken, ERRID.ERR_UnrecognizedType))
            genericName = SyntaxFactory.GenericName(genericName.Identifier, typeArgumentList)
            Return genericName
        End Function

        ' Parse a simple type followed by an optional array list.

        ' File: Parser.cpp
        ' Lines: 7117 - 7117
        ' Type* .Parser::ParseGeneralType( [ _Inout_ bool& ErrorInConstruct ] [ bool AllowEmptyGenericArguments ] )

        Friend Function ParseGeneralType(Optional allowEmptyGenericArguments As Boolean = False) As TypeSyntax

            Dim start As SyntaxToken = CurrentToken
            Dim result As TypeSyntax

            If _evaluatingConditionCompilationExpression AndAlso Not SyntaxFacts.IsPredefinedTypeOrVariant(start.Kind) Then

                'TODO - 
                ' 1. Dev10 code does NOT consume any tokens here.
                ' 2. Should this error check be done in the parser or when the expression is evaluated?
                Dim ident = InternalSyntaxFactory.MissingIdentifier()
                ident = ident.AddTrailingSyntax(start, ERRID.ERR_BadTypeInCCExpression)
                result = SyntaxFactory.IdentifierName(ident)
                GetNextToken()

                Return result
            End If

            Dim allowedEmptyGenericArguments As Boolean = False

            result = ParseTypeName(
                allowEmptyGenericArguments:=allowEmptyGenericArguments,
                allowedEmptyGenericArguments:=allowedEmptyGenericArguments)

            If CurrentToken.Kind = SyntaxKind.OpenParenToken Then

                Dim elementType = result
                Dim rankSpecifiers As CoreInternalSyntax.SyntaxList(Of ArrayRankSpecifierSyntax) = ParseArrayRankSpecifiers()

                If allowedEmptyGenericArguments Then
                    ' Need to eat up the array syntax to avoid spuriously parsing
                    ' the array syntax "(10)" as default property syntax for
                    ' constructs such a GetType(A(Of )()) and GetType(A(Of )()()()).
                    ' Even resyncing to tkRParen will not help in the array of array
                    ' cases. So instead use ParseArrayDeclarator to help skip
                    ' all of the array syntax.
                    rankSpecifiers = New CoreInternalSyntax.SyntaxList(Of ArrayRankSpecifierSyntax)(ReportSyntaxError(rankSpecifiers.Node, ERRID.ERR_ArrayOfRawGenericInvalid))
                End If

                result = SyntaxFactory.ArrayType(elementType, rankSpecifiers)
            End If

            Return result
        End Function

        ' [in] the start token of the statement or expression containing the generic arguments
        ' File: Parser.cpp
        ' Lines: 6625 - 6625
        ' .Parser::ParseGenericArguments( [ Token* Start ] [ ParseTree::GenericArguments& Arguments ] [ _Inout_ bool& AllowEmptyGenericArguments ] [ _Inout_ bool& AllowNonEmptyGenericArguments ] [ _Inout_ bool& ErrorInConstruct ] )

        ' File: Parser.cpp
        ' Lines: 6659 - 6659
        ' TypeList* .Parser::ParseGenericArguments( [ _Out_ Token*& Of ] [ _Out_ Token*& openParen ] [ _Out_ Token*& closeParen ] [ _Inout_ bool& AllowEmptyGenericArguments ] [ _Inout_ bool& AllowNonEmptyGenericArguments ] [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseGenericArguments(
            ByRef allowEmptyGenericArguments As Boolean,
            ByRef AllowNonEmptyGenericArguments As Boolean
        ) As TypeArgumentListSyntax

            Debug.Assert(allowEmptyGenericArguments OrElse AllowNonEmptyGenericArguments,
                "Cannot disallow both empty and non-empty generic arguments!!!")

            Dim [of] As KeywordSyntax = Nothing
            Dim openParen As PunctuationSyntax
            Dim closeParen As PunctuationSyntax = Nothing
            Dim genericArguments As TypeArgumentListSyntax
            Dim typeArguments As CoreInternalSyntax.SeparatedSyntaxList(Of TypeSyntax)

            Debug.Assert(CurrentToken.Kind = SyntaxKind.OpenParenToken, "Generic arguments parsing lost!!!")

            openParen = DirectCast(CurrentToken, PunctuationSyntax)
            GetNextToken() ' get off '('
            TryEatNewLine()  ' '(' allows implicit line continuation

            TryGetTokenAndEatNewLine(SyntaxKind.OfKeyword, [of], createIfMissing:=True)

            Dim typeNames = _pool.AllocateSeparated(Of TypeSyntax)()
            Dim typeName As TypeSyntax
            Dim comma As PunctuationSyntax

            Do
                typeName = Nothing

                ' Either all generic arguments should be unspecified or all need to be specified.
                If CurrentToken.Kind = SyntaxKind.CommaToken OrElse CurrentToken.Kind = SyntaxKind.CloseParenToken Then
                    If allowEmptyGenericArguments Then
                        ' If a non-empty type argument is already specified, then need to always look for
                        ' non-empty type arguments, else we can allow empty type arguments.

                        typeName = SyntaxFactory.IdentifierName(InternalSyntaxFactory.MissingIdentifier)
                        AllowNonEmptyGenericArguments = False
                    Else
                        typeName = ParseGeneralType()
                    End If

                Else
                    ' If an empty type argument is already specified, then need to always look for
                    ' empty type arguments and reject non-empty type arguments, else we can allow
                    ' non-empty type arguments.

                    typeName = ParseGeneralType()
                    If AllowNonEmptyGenericArguments Then
                        allowEmptyGenericArguments = False
                    Else
                        typeName = ReportSyntaxError(typeName, ERRID.ERR_TypeParamMissingCommaOrRParen)
                    End If
                End If

                Debug.Assert(allowEmptyGenericArguments OrElse AllowNonEmptyGenericArguments,
                    "Cannot disallow both empty and non-empty generic arguments!!!")

                If typeName.ContainsDiagnostics Then
                    typeName = ResyncAt(typeName, SyntaxKind.CloseParenToken, SyntaxKind.CommaToken)
                End If

                typeNames.Add(typeName)

                comma = Nothing
                If Not TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then
                    Exit Do
                End If

                Debug.Assert(comma IsNot Nothing)

                typeNames.AddSeparator(comma)
            Loop While True

            If openParen IsNot Nothing Then
                TryEatNewLineAndGetToken(SyntaxKind.CloseParenToken, closeParen, createIfMissing:=True)
            End If

            typeArguments = typeNames.ToList
            _pool.Free(typeNames)
            genericArguments = SyntaxFactory.TypeArgumentList(openParen, [of], typeArguments, closeParen)

            Return genericArguments
        End Function

        Private Function ParseArrayRankSpecifiers(Optional errorForExplicitArraySizes As ERRID = ERRID.ERR_NoExplicitArraySizes) As CoreInternalSyntax.SyntaxList(Of ArrayRankSpecifierSyntax)

            Debug.Assert(CurrentToken.Kind = SyntaxKind.OpenParenToken, "should be a (.")

            Dim arrayModifiers As SyntaxListBuilder(Of ArrayRankSpecifierSyntax) = Nothing

            Do
                Dim openParen As PunctuationSyntax = Nothing
                Dim commas As CoreInternalSyntax.SyntaxList(Of PunctuationSyntax) = Nothing
                Dim closeParen As PunctuationSyntax = Nothing
                Dim arguments As CoreInternalSyntax.SeparatedSyntaxList(Of ArgumentSyntax) = Nothing

                Debug.Assert(Me.CurrentToken.Kind = Global.Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.OpenParenToken)
                TryGetTokenAndEatNewLine(SyntaxKind.OpenParenToken, openParen)

                If CurrentToken.Kind = SyntaxKind.CommaToken Then

                    commas = ParseSeparators(SyntaxKind.CommaToken)

                ElseIf CurrentToken.Kind <> SyntaxKind.CloseParenToken Then
                    ' Previously allowExplicitSizes was passed to control whether sizes are allowed.  Now if we get here it is
                    ' always an error.  For backward compatibility we try to parse for array sizes and then report it as an error
                    ' below.

                    arguments = ParseArgumentList()
                End If

                TryEatNewLineAndGetToken(SyntaxKind.CloseParenToken, closeParen, createIfMissing:=True)

                If arrayModifiers.IsNull Then
                    arrayModifiers = _pool.Allocate(Of ArrayRankSpecifierSyntax)()
                End If

                If arguments.Count <> 0 Then
                    closeParen = closeParen.AddLeadingSyntax(arguments.Node, errorForExplicitArraySizes)
                End If

                Dim arrayModifier As ArrayRankSpecifierSyntax = SyntaxFactory.ArrayRankSpecifier(openParen, commas, closeParen)

                arrayModifiers.Add(arrayModifier)

            Loop While CurrentToken.Kind = SyntaxKind.OpenParenToken

            Dim result As CoreInternalSyntax.SyntaxList(Of ArrayRankSpecifierSyntax) = arrayModifiers.ToList
            _pool.Free(arrayModifiers)

            Return result
        End Function

        ' davidsch - Just as ParseIdentifier was split into two ParseIdentifiers (nullable and non-nullable cases), ParseArrayDeclarator was split 
        ' to handle ArrayTypeName and ModifiedIdentifier cases

        Private Function ParseArrayModifiedIdentifier(
            elementType As IdentifierTokenSyntax,
            optionalNullable As PunctuationSyntax,
            allowExplicitSizes As Boolean
         ) As ModifiedIdentifierSyntax
            Debug.Assert(elementType IsNot Nothing)

            Debug.Assert(CurrentToken.Kind = SyntaxKind.OpenParenToken, "should be a (.")

            Dim optionalArrayBounds As ArgumentListSyntax = Nothing
            Dim arrayModifiers As SyntaxListBuilder(Of ArrayRankSpecifierSyntax) = Nothing
            Dim arguments As CoreInternalSyntax.SeparatedSyntaxList(Of ArgumentSyntax)
            Dim openParen As PunctuationSyntax = Nothing
            Dim commas As CoreInternalSyntax.SyntaxList(Of PunctuationSyntax)
            Dim closeParen As PunctuationSyntax
            Dim innerArrayType As Boolean = False

            Do
                commas = Nothing
                arguments = Nothing
                closeParen = Nothing

                Debug.Assert(CurrentToken.Kind = SyntaxKind.OpenParenToken)
                TryGetTokenAndEatNewLine(SyntaxKind.OpenParenToken, openParen)

                If CurrentToken.Kind = SyntaxKind.CommaToken Then

                    commas = ParseSeparators(SyntaxKind.CommaToken)

                ElseIf CurrentToken.Kind <> SyntaxKind.CloseParenToken Then

                    arguments = ParseArgumentList()

                End If

                TryEatNewLineAndGetToken(SyntaxKind.CloseParenToken, closeParen, createIfMissing:=True)

                If arrayModifiers.IsNull Then
                    arrayModifiers = _pool.Allocate(Of ArrayRankSpecifierSyntax)()
                End If

                If arguments.Count <> 0 Then

                    If Not innerArrayType Then
                        optionalArrayBounds = SyntaxFactory.ArgumentList(openParen, arguments, closeParen)

                        If Not allowExplicitSizes Then
                            optionalArrayBounds = ReportSyntaxError(optionalArrayBounds, ERRID.ERR_NoExplicitArraySizes)
                        End If

                    Else
                        ' Create an arrayModifier with the bad array bounds
                        closeParen = closeParen.AddLeadingSyntax(arguments.Node, ERRID.ERR_NoConstituentArraySizes)
                        arrayModifiers.Add(SyntaxFactory.ArrayRankSpecifier(openParen, commas, closeParen))
                    End If

                Else
                    arrayModifiers.Add(SyntaxFactory.ArrayRankSpecifier(openParen, commas, closeParen))
                End If

                ' Explicit sizes are only allowed once in the first ().  
                innerArrayType = True
            Loop While CurrentToken.Kind = SyntaxKind.OpenParenToken

            Dim modifiersArr As CoreInternalSyntax.SyntaxList(Of ArrayRankSpecifierSyntax) = arrayModifiers.ToList
            _pool.Free(arrayModifiers)

            Return SyntaxFactory.ModifiedIdentifier(elementType, optionalNullable, optionalArrayBounds, modifiersArr)
        End Function

        Private Function TryReinterpretAsArraySpecifier(argumentList As ArgumentListSyntax, ByRef arrayModifiers As CoreInternalSyntax.SyntaxList(Of ArrayRankSpecifierSyntax)) As Boolean
            Dim builder = _pool.Allocate(Of PunctuationSyntax)()

            ' Try to reinterpret the argumentList as arrayRankSpecifier syntax
            Dim interpretAsArrayModifiers = True
            Dim arguments = argumentList.Arguments

            For i = 0 To arguments.Count - 1
                Dim arg = arguments(i)

                If arg.Kind <> SyntaxKind.OmittedArgument Then
                    interpretAsArrayModifiers = False
                    Exit For
                End If
            Next

            If interpretAsArrayModifiers Then
                Dim argsAndSeparators = arguments.GetWithSeparators

                For i = 0 To arguments.SeparatorCount - 1
                    builder.Add(DirectCast(argsAndSeparators(2 * i + 1), PunctuationSyntax))
                Next

                arrayModifiers = SyntaxFactory.ArrayRankSpecifier(argumentList.OpenParenToken, builder.ToList, argumentList.CloseParenToken)
            End If

            _pool.Free(builder)
            Return interpretAsArrayModifiers
        End Function

        Private Function ParseSeparators(kind As SyntaxKind) As CoreInternalSyntax.SyntaxList(Of PunctuationSyntax)
            Dim separators = _pool.Allocate(Of PunctuationSyntax)()

            While CurrentToken.Kind = kind
                Dim sep As PunctuationSyntax = DirectCast(CurrentToken, PunctuationSyntax)
                GetNextToken()
                TryEatNewLine()
                separators.Add(sep)
            End While

            Dim result = separators.ToList
            _pool.Free(separators)

            Return result
        End Function

        ' In Dev10 this was ParseArgument.
        Private Function ParseArgumentList() As CoreInternalSyntax.SeparatedSyntaxList(Of ArgumentSyntax)
            Dim comma As PunctuationSyntax

            Dim arguments = _pool.AllocateSeparated(Of ArgumentSyntax)()

            Do
                Dim lowerBound As ExpressionSyntax = Nothing
                Dim toKeyword As KeywordSyntax = Nothing
                Dim upperBound As ExpressionSyntax = ParseExpressionCore()

                If upperBound.ContainsDiagnostics Then
                    upperBound = ResyncAt(upperBound, SyntaxKind.CommaToken, SyntaxKind.CloseParenToken, SyntaxKind.AsKeyword)

                ElseIf CurrentToken.Kind = SyntaxKind.ToKeyword Then
                    toKeyword = DirectCast(CurrentToken, KeywordSyntax)
                    lowerBound = upperBound

                    ' Check that lower bound is equal to 0 moved to binder.

                    GetNextToken() ' consume To keyword

                    upperBound = ParseExpressionCore()
                End If

                If upperBound.ContainsDiagnostics OrElse (toKeyword IsNot Nothing AndAlso lowerBound.ContainsDiagnostics) Then
                    upperBound = ResyncAt(upperBound, SyntaxKind.CommaToken, SyntaxKind.CloseParenToken, SyntaxKind.AsKeyword)
                End If

                Dim arg As ArgumentSyntax

                If toKeyword Is Nothing Then
                    arg = SyntaxFactory.SimpleArgument(Nothing, upperBound)
                Else
                    arg = SyntaxFactory.RangeArgument(lowerBound, toKeyword, upperBound)
                End If

                arguments.Add(arg)

                comma = Nothing
                If Not TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then
                    Exit Do
                End If

                arguments.AddSeparator(comma)
            Loop

            Dim result = arguments.ToList
            _pool.Free(arguments)

            Return result
        End Function

        ' This used to be ParsePropertyOrEventProcedureDefinition
        Private Function ParsePropertyOrEventAccessor(accessorKind As SyntaxKind, attributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax), modifiers As CoreInternalSyntax.SyntaxList(Of KeywordSyntax)) As AccessorStatementSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.GetKeyword OrElse CurrentToken.Kind = SyntaxKind.SetKeyword OrElse
                     CurrentToken.Kind = SyntaxKind.AddHandlerKeyword OrElse CurrentToken.Kind = SyntaxKind.RemoveHandlerKeyword OrElse CurrentToken.Kind = SyntaxKind.RaiseEventKeyword)

            Dim methodKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            If Not IsFirstStatementOnLine(CurrentToken) Then
                methodKeyword = ReportSyntaxError(methodKeyword, ERRID.ERR_MethodMustBeFirstStatementOnLine)
            End If
            GetNextToken()

            Dim genericParams As TypeParameterListSyntax = Nothing
            Dim optionalParameters As ParameterListSyntax = Nothing
            Dim openParen As PunctuationSyntax = Nothing
            Dim parameters As CoreInternalSyntax.SeparatedSyntaxList(Of ParameterSyntax) = Nothing
            Dim closeParen As PunctuationSyntax = Nothing

            TryRejectGenericParametersForMemberDecl(genericParams)

            If genericParams IsNot Nothing Then
                methodKeyword = methodKeyword.AddTrailingSyntax(genericParams)
            End If

            If methodKeyword.Kind <> SyntaxKind.GetKeyword AndAlso
               CurrentToken.Kind = SyntaxKind.OpenParenToken Then

                parameters = ParseParameters(openParen, closeParen)
                optionalParameters = SyntaxFactory.ParameterList(openParen, parameters, closeParen)
            End If

            ' Specifiers only allowed for property accessors, not for event accessors
            ' Specifiers are not valid on 'AddHandler', 'RemoveHandler' and 'RaiseEvent' methods.

            If modifiers.Any AndAlso
                (methodKeyword.Kind = SyntaxKind.AddHandlerKeyword OrElse
                methodKeyword.Kind = SyntaxKind.RemoveHandlerKeyword OrElse
                methodKeyword.Kind = SyntaxKind.RaiseEventKeyword) Then

                methodKeyword = ReportModifiersOnStatementError(ERRID.ERR_SpecifiersInvOnEventMethod, Nothing, modifiers, methodKeyword)
                modifiers = Nothing
            End If

            Dim statement = SyntaxFactory.AccessorStatement(accessorKind, attributes, modifiers, methodKeyword, optionalParameters)

            Return statement
        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseImplementsList
        ' *
        ' **********************************************************************/

        ' File: Parser.cpp
        ' Lines: 8018 - 8018
        ' NameList* .Parser::ParseImplementsList( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseImplementsList() As ImplementsClauseSyntax

            Debug.Assert(CurrentToken.Kind = SyntaxKind.ImplementsKeyword, "Implements list parsing lost.")

            Dim implementsKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            Dim ImplementsClauses As SeparatedSyntaxListBuilder(Of QualifiedNameSyntax) =
                Me._pool.AllocateSeparated(Of QualifiedNameSyntax)()

            Dim comma As PunctuationSyntax

            GetNextToken()

            Do

                'TODO - davidsch
                ' The old parser did not make a distinction between TypeNames and Names
                ' While there is a ParseTypeName function, the old parser called ParseName.  For now
                ' call ParseName and then break up the name to make a ImplementsClauseItem. The
                ' parameters passed to ParseName guarantee that the name is qualified. The first
                ' parameter ensures qualification.  The last parameter ensures that it is not generic.

                ' AllowGlobalNameSpace
                ' Allow generic arguments

                Dim term = DirectCast(ParseName(
                    requireQualification:=True,
                    allowGlobalNameSpace:=True,
                    allowGenericArguments:=True,
                    allowGenericsWithoutOf:=True,
                    nonArrayName:=True,
                    disallowGenericArgumentsOnLastQualifiedName:=True), QualifiedNameSyntax) ' Disallow generic arguments on last qualified name i.e. on the method name

                ImplementsClauses.Add(term)

                comma = Nothing
                If Not TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then
                    Exit Do
                End If

                ImplementsClauses.AddSeparator(comma)
            Loop

            Dim result = ImplementsClauses.ToList
            Me._pool.Free(ImplementsClauses)

            Return SyntaxFactory.ImplementsClause(implementsKeyword, result)
        End Function

        ' File: Parser.cpp
        ' Lines: 8062 - 8062
        ' NameList* .Parser::ParseHandlesList( [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ParseHandlesList() As HandlesClauseSyntax

            Debug.Assert(CurrentToken.Kind = SyntaxKind.HandlesKeyword, "Handles list parsing lost.")

            Dim handlesKeyword = DirectCast(CurrentToken, KeywordSyntax)
            Dim handlesClauseItems As SeparatedSyntaxListBuilder(Of HandlesClauseItemSyntax) = Me._pool.AllocateSeparated(Of HandlesClauseItemSyntax)()
            Dim comma As PunctuationSyntax

            GetNextToken() ' get off the handles / comma token
            Do
                Dim eventContainer As EventContainerSyntax
                Dim eventMember As IdentifierNameSyntax

                If CurrentToken.Kind = SyntaxKind.MyBaseKeyword OrElse
                    CurrentToken.Kind = SyntaxKind.MyClassKeyword OrElse
                    CurrentToken.Kind = SyntaxKind.MeKeyword Then

                    eventContainer = SyntaxFactory.KeywordEventContainer(DirectCast(CurrentToken, KeywordSyntax))
                    GetNextToken()

                ElseIf CurrentToken.Kind = SyntaxKind.GlobalKeyword Then
                    ' A handles name can't start with Global, it is local.
                    ' Produce the error, ignore the token and let the name parse for sync.

                    ' we are not consuming Global keyword here as the only acceptable keywords are: Me, MyBase, MyClass
                    eventContainer = SyntaxFactory.WithEventsEventContainer(InternalSyntaxFactory.MissingIdentifier())
                    eventContainer = ReportSyntaxError(eventContainer, ERRID.ERR_NoGlobalInHandles)

                Else
                    eventContainer = SyntaxFactory.WithEventsEventContainer(ParseIdentifier())

                End If

                Dim Dot As PunctuationSyntax = Nothing

                ' allow implicit line continuation after '.' in handles list - dev10_503311
                If TryGetTokenAndEatNewLine(SyntaxKind.DotToken, Dot, createIfMissing:=True) Then
                    eventMember = InternalSyntaxFactory.IdentifierName(ParseIdentifierAllowingKeyword())

                    ' check if we actually have "withEventsMember.Property.Event"
                    Dim identContainer = TryCast(eventContainer, WithEventsEventContainerSyntax)
                    Dim secondDot As PunctuationSyntax = Nothing

                    If identContainer IsNot Nothing AndAlso TryGetTokenAndEatNewLine(SyntaxKind.DotToken, secondDot, createIfMissing:=True) Then
                        ' former member and dot are shifted into property container.
                        eventContainer = SyntaxFactory.WithEventsPropertyEventContainer(identContainer, Dot, eventMember)
                        ' secondDot becomes the event's dot
                        Dot = secondDot
                        ' parse another event member since the former one has become a property
                        eventMember = InternalSyntaxFactory.IdentifierName(ParseIdentifierAllowingKeyword())
                    End If

                Else
                    eventMember = InternalSyntaxFactory.IdentifierName(InternalSyntaxFactory.MissingIdentifier())
                End If

                Dim item As HandlesClauseItemSyntax = SyntaxFactory.HandlesClauseItem(eventContainer, Dot, eventMember)

                If eventContainer.ContainsDiagnostics OrElse Dot.ContainsDiagnostics OrElse eventMember.ContainsDiagnostics Then

                    If CurrentToken.Kind <> SyntaxKind.CommaToken Then
                        item = ResyncAt(item, SyntaxKind.CommaToken)
                    End If
                End If

                handlesClauseItems.Add(item)

                comma = Nothing
                If Not TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then
                    Exit Do
                End If

                handlesClauseItems.AddSeparator(comma)
            Loop

            Dim result = handlesClauseItems.ToList
            Me._pool.Free(handlesClauseItems)

            Return SyntaxFactory.HandlesClause(handlesKeyword, result)
        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseSubDeclaration
        ' *
        ' * Purpose:
        ' *
        ' **********************************************************************/

        ' [in] specifiers on definition
        ' [in] token starting definition

        ' File: Parser.cpp
        ' Lines: 8358 - 8358
        ' MethodDeclarationStatement* .Parser::ParseSubDeclaration( [ ParseTree::AttributeSpecifierList* Attributes ] [ ParseTree::SpecifierList* Specifiers ] [ _In_ Token* Start ] [ bool IsDelegate ] [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseSubStatement(
            attributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax),
            modifiers As CoreInternalSyntax.SyntaxList(Of KeywordSyntax)
        ) As MethodBaseSyntax

            Dim subKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)

            Debug.Assert(subKeyword.Kind = SyntaxKind.SubKeyword, "must be at a Sub.")

            GetNextToken()

            Dim save_isInMethodDeclarationHeader As Boolean = _isInMethodDeclarationHeader
            _isInMethodDeclarationHeader = True

            Dim save_isInAsyncMethodDeclarationHeader As Boolean = _isInAsyncMethodDeclarationHeader
            Dim save_isInIteratorMethodDeclarationHeader As Boolean = _isInIteratorMethodDeclarationHeader

            _isInAsyncMethodDeclarationHeader = modifiers.Any(SyntaxKind.AsyncKeyword)
            _isInIteratorMethodDeclarationHeader = modifiers.Any(SyntaxKind.IteratorKeyword)

            Dim newKeyword As KeywordSyntax = Nothing
            Dim name As IdentifierTokenSyntax = Nothing
            Dim genericParams As TypeParameterListSyntax = Nothing
            Dim parameters As ParameterListSyntax = Nothing
            Dim handlesClause As HandlesClauseSyntax = Nothing
            Dim implementsClause As ImplementsClauseSyntax = Nothing

            ' Dev10_504604 we are parsing a method declaration and will need to let the scanner know that we
            ' are so the scanner can correctly identify attributes vs. xml while scanning the declaration.

            'davidsch - It is not longer necessary to force the scanner state here.  The scanner will only scan xml when the parser explicitly tells it to scan xml.

            ' Nodekind.NewKeyword is allowed as a Sub name but no other keywords.
            If CurrentToken.Kind = SyntaxKind.NewKeyword Then
                newKeyword = DirectCast(CurrentToken, KeywordSyntax)
                GetNextToken()
            End If

            ParseSubOrDelegateStatement(If(newKeyword Is Nothing, SyntaxKind.SubStatement, SyntaxKind.SubNewStatement), name, genericParams, parameters, handlesClause, implementsClause)

            ' We should be at the end of the statement.
            _isInMethodDeclarationHeader = save_isInMethodDeclarationHeader
            _isInAsyncMethodDeclarationHeader = save_isInAsyncMethodDeclarationHeader
            _isInIteratorMethodDeclarationHeader = save_isInIteratorMethodDeclarationHeader

            'Create the Sub declaration
            If newKeyword Is Nothing Then
                Return SyntaxFactory.SubStatement(attributes, modifiers, subKeyword, name, genericParams, parameters, Nothing, handlesClause, implementsClause)
            Else
                If handlesClause IsNot Nothing Then
                    newKeyword = newKeyword.AddError(ERRID.ERR_NewCannotHandleEvents) ' error should be on "New"
                End If

                If implementsClause IsNot Nothing Then
                    newKeyword = newKeyword.AddError(ERRID.ERR_ImplementsOnNew) ' error should be on "New"
                End If

                If genericParams IsNot Nothing Then
                    newKeyword = newKeyword.AddTrailingSyntax(genericParams)
                End If

                Dim ctorDecl = SyntaxFactory.SubNewStatement(attributes, modifiers, subKeyword, newKeyword, parameters)

                ' do not forget unexpected handles and implements even if unexpected
                ctorDecl = ctorDecl.AddTrailingSyntax(handlesClause)
                ctorDecl = ctorDecl.AddTrailingSyntax(implementsClause)

                Return ctorDecl
            End If

        End Function

        Private Sub ParseSubOrDelegateStatement(
                                          kind As SyntaxKind,
                                          ByRef ident As IdentifierTokenSyntax,
                                          ByRef optionalGenericParams As TypeParameterListSyntax,
                                          ByRef optionalParameters As ParameterListSyntax,
                                          ByRef handlesClause As HandlesClauseSyntax,
                                          ByRef implementsClause As ImplementsClauseSyntax)

            Debug.Assert(kind = SyntaxKind.SubStatement OrElse
                         kind = SyntaxKind.SubNewStatement OrElse
                         kind = SyntaxKind.DelegateSubStatement, "Wrong kind passed to ParseSubOrDelegateStatement")

            'The current token is on the Sub or Delegate's name

            ' Parse the name only for Delegates and Subs.  Constructors have already grabbed the New keyword.
            If kind <> SyntaxKind.SubNewStatement Then
                ident = ParseIdentifier()

                If ident.ContainsDiagnostics Then
                    ident = ident.AddTrailingSyntax(ResyncAt({SyntaxKind.OpenParenToken, SyntaxKind.OfKeyword}))
                End If
            End If

            ' Dev10_504604 we are parsing a method declaration and will need to let the scanner know that we
            ' are so the scanner can correctly identify attributes vs. xml while scanning the declaration.

            If BeginsGeneric() Then
                If kind = SyntaxKind.SubNewStatement Then

                    ' We want to do this error checking here during parsing and not in
                    ' declared (which would have been more ideal) because for the invalid
                    ' case, when this error occurs, we don't want any parse errors for
                    ' parameters to show up.

                    ' We want other errors such as those on regular parameters reported too,
                    ' so don't mark ErrorInConstruct, but instead use a temp.
                    '
                    optionalGenericParams = ReportGenericParamsDisallowedError(ERRID.ERR_GenericParamsOnInvalidMember)
                Else
                    optionalGenericParams = ParseGenericParameters()
                End If
            End If

            optionalParameters = ParseParameterList()

            ' See if we have the HANDLES or the IMPLEMENTS clause on this procedure.

            If CurrentToken.Kind = SyntaxKind.HandlesKeyword Then
                handlesClause = ParseHandlesList()

                If kind = SyntaxKind.DelegateSubStatement Then
                    ' davidsch - This error was reported in Declared in Dev10
                    handlesClause = ReportSyntaxError(handlesClause, ERRID.ERR_DelegateCantHandleEvents)
                End If
            ElseIf CurrentToken.Kind = SyntaxKind.ImplementsKeyword Then
                implementsClause = ParseImplementsList()

                If kind = SyntaxKind.DelegateSubStatement Then
                    ' davidsch - This error was reported in Declared in Dev10
                    implementsClause = ReportSyntaxError(implementsClause, ERRID.ERR_DelegateCantImplement)
                End If
            End If
        End Sub

        Friend Function ParseParameterList() As ParameterListSyntax
            If CurrentToken.Kind = SyntaxKind.OpenParenToken Then

                Dim openParen As PunctuationSyntax = Nothing
                Dim closeParen As PunctuationSyntax = Nothing
                Dim parameters = ParseParameters(openParen, closeParen)

                Return SyntaxFactory.ParameterList(openParen, parameters, closeParen)
            Else
                Return Nothing
            End If
        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseFunctionDeclaration
        ' *
        ' * Purpose:
        ' *     Parses a Function definition.
        ' *
        ' **********************************************************************/

        ' [in] specifiers on definition
        ' [in] token starting definition
        ' File: Parser.cpp
        ' Lines: 8470 - 8470
        ' MethodDeclarationStatement* .Parser::ParseFunctionDeclaration( [ ParseTree::AttributeSpecifierList* Attributes ] [ ParseTree::SpecifierList* Specifiers ] [ _In_ Token* Start ] [ bool IsDelegate ] [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseFunctionStatement(
                attributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax),
                modifiers As CoreInternalSyntax.SyntaxList(Of KeywordSyntax)
            ) As MethodStatementSyntax

            Dim functionKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)

            Debug.Assert(functionKeyword.Kind = SyntaxKind.FunctionKeyword, "Function parsing lost.")

            GetNextToken()

            Dim save_isInMethodDeclarationHeader As Boolean = _isInMethodDeclarationHeader
            _isInMethodDeclarationHeader = True

            Dim save_isInAsyncMethodDeclarationHeader As Boolean = _isInAsyncMethodDeclarationHeader
            Dim save_isInIteratorMethodDeclarationHeader As Boolean = _isInIteratorMethodDeclarationHeader

            _isInAsyncMethodDeclarationHeader = modifiers.Any(SyntaxKind.AsyncKeyword)
            _isInIteratorMethodDeclarationHeader = modifiers.Any(SyntaxKind.IteratorKeyword)

            ' Dev10_504604 we are parsing a method declaration and will need to let the scanner know
            ' that we are so the scanner can correctly identify attributes vs. xml while scanning
            ' the declaration.

            'davidsch - It is not longer necessary to force the scanner state here.  The scanner will
            'only scan xml when the parser explicitly tells it to scan xml.

            Dim name As IdentifierTokenSyntax = Nothing
            Dim genericParams As TypeParameterListSyntax = Nothing
            Dim parameters As ParameterListSyntax = Nothing
            Dim asClause As SimpleAsClauseSyntax = Nothing
            Dim handlesClause As HandlesClauseSyntax = Nothing
            Dim implementsClause As ImplementsClauseSyntax = Nothing

            ParseFunctionOrDelegateStatement(SyntaxKind.FunctionStatement, name, genericParams, parameters, asClause, handlesClause, implementsClause)

            _isInMethodDeclarationHeader = save_isInMethodDeclarationHeader
            _isInAsyncMethodDeclarationHeader = save_isInAsyncMethodDeclarationHeader
            _isInIteratorMethodDeclarationHeader = save_isInIteratorMethodDeclarationHeader

            'Create the Sub statement.
            Dim methodStatement = SyntaxFactory.FunctionStatement(attributes, modifiers, functionKeyword, name, genericParams, parameters, asClause, handlesClause, implementsClause)

            Return methodStatement

        End Function

        Private Sub ParseFunctionOrDelegateStatement(kind As SyntaxKind,
                                                       ByRef ident As IdentifierTokenSyntax,
                                                       ByRef optionalGenericParams As TypeParameterListSyntax,
                                                       ByRef optionalParameters As ParameterListSyntax,
                                                       ByRef asClause As SimpleAsClauseSyntax,
                                                       ByRef handlesClause As HandlesClauseSyntax,
                                                       ByRef implementsClause As ImplementsClauseSyntax)

            Debug.Assert(
                kind = SyntaxKind.FunctionStatement OrElse
                kind = SyntaxKind.DelegateFunctionStatement, "Wrong kind passed to ParseFunctionOrDelegateStatement")

            'TODO - davidsch Can ParseFunctionOrDelegateDeclaration and
            'ParseSubOrDelegateDeclaration share more code? They are nearly the same.

            ' The current token is on the function or delegate's name

            If CurrentToken.Kind = SyntaxKind.NewKeyword Then
                ' "New" gets special attention because attempting to declare a constructor as a
                ' function is, we expect, a common error.
                ident = ParseIdentifierAllowingKeyword()

                ident = ReportSyntaxError(ident, ERRID.ERR_ConstructorFunction)
            Else
                ident = ParseIdentifier()

                ' TODO - davidsch - Why do ParseFunctionDeclaration and ParseSubDeclaration have
                ' different error recovery here?
                If ident.ContainsDiagnostics Then
                    ident = ident.AddTrailingSyntax(ResyncAt({SyntaxKind.OpenParenToken, SyntaxKind.AsKeyword}))
                End If
            End If

            If BeginsGeneric() Then
                optionalGenericParams = ParseGenericParameters()
            End If

            optionalParameters = ParseParameterList()

            Dim returnType As TypeSyntax = Nothing
            Dim returnTypeAttributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax) = Nothing

            Dim asKeyword As KeywordSyntax = Nothing

            ' Check the return type.

            If CurrentToken.Kind = SyntaxKind.AsKeyword Then
                asKeyword = DirectCast(CurrentToken, KeywordSyntax)
                GetNextToken()

                If CurrentToken.Kind = SyntaxKind.LessThanToken Then
                    returnTypeAttributes = ParseAttributeLists(False)
                End If

                returnType = ParseGeneralType()

                If returnType.ContainsDiagnostics Then
                    returnType = ResyncAt(returnType)
                End If

                asClause = SyntaxFactory.SimpleAsClause(asKeyword, returnTypeAttributes, returnType)
            End If

            ' See if we have the HANDLES or the IMPLEMENTS clause on this procedure.

            If CurrentToken.Kind = SyntaxKind.HandlesKeyword Then
                handlesClause = ParseHandlesList()

                If kind = SyntaxKind.DelegateFunctionStatement Then
                    ' davidsch - This error was reported in Declared in Dev10
                    handlesClause = ReportSyntaxError(handlesClause, ERRID.ERR_DelegateCantHandleEvents)
                End If

            ElseIf CurrentToken.Kind = SyntaxKind.ImplementsKeyword Then
                implementsClause = ParseImplementsList()

                If kind = SyntaxKind.DelegateFunctionStatement Then
                    ' davidsch - This error was reported in Declared in Dev10
                    implementsClause = ReportSyntaxError(implementsClause, ERRID.ERR_DelegateCantImplement)
                End If

            End If

        End Sub

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseOperatorDeclaration
        ' *
        ' * Purpose:
        ' *     Parses an Operator definition.
        ' *
        ' **********************************************************************/

        ' [in] specifiers on definition
        ' [in] token starting definition

        ' File: Parser.cpp
        ' Lines: 8711 - 8711
        ' MethodDeclarationStatement* .Parser::ParseOperatorDeclaration( [ ParseTree::AttributeSpecifierList* Attributes ] [ ParseTree::SpecifierList* Specifiers ] [ _In_ Token* Start ] [ bool IsDelegate ] [ _Inout_ bool& ErrorInConstruct ] )
        Private Function ParseOperatorStatement(
                attributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax),
                modifiers As CoreInternalSyntax.SyntaxList(Of KeywordSyntax)
            ) As OperatorStatementSyntax

            'TODO - davidsch 
            ' Can ParseFunctionDeclaration and ParseSubDeclaration share more code? They are nearly the same.
            Dim operatorKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            Debug.Assert(operatorKeyword.Kind = SyntaxKind.OperatorKeyword, "Operator parsing lost.")

            ' Dev10_504604 we are parsing a method declaration and will need to let the scanner know that we
            ' are so the scanner can correctly identify attributes vs. xml while scanning the declaration.

            'davidsch - It is not longer necessary to force the scanner state here.  The scanner will only scan xml when the parser explicitly tells it to scan xml.

            GetNextToken()

            ' Under the IDE, we accept the Widening or Narrowing specifier coming after the Operator keyword.
            '
            ' Example:  Public Shared Operator Widening CType( ...
            '
            ' This is still a syntax error, but the pretty lister can move the specifier to before the Operator keyword.
            ' This used to be recorded as a dangling specifier.  Now the overloadable operator will be Widening with unexpected 
            ' syntax CType following it.

            Dim keyword As KeywordSyntax = Nothing
            Dim operatorToken As SyntaxToken

            If TryTokenAsContextualKeyword(CurrentToken, keyword) Then
                operatorToken = keyword
            Else
                operatorToken = CurrentToken
            End If

            Dim operatorKind = operatorToken.Kind

            ' Check that this is a valid overloadable operator
            If SyntaxFacts.IsOperatorStatementOperatorToken(operatorKind) Then
                GetNextToken()

            Else
                'TODO - davidsch - What should be created here? For now use + as a canonical operator
                Dim validMissingOperator = InternalSyntaxFactory.MissingToken(SyntaxKind.PlusToken)
                ' Is this any kind of operator?
                If SyntaxFacts.IsOperator(operatorKind) Then
                    operatorToken = validMissingOperator.AddTrailingSyntax(operatorToken, ERRID.ERR_OperatorNotOverloadable)
                    GetNextToken()
                ElseIf operatorKind <> SyntaxKind.OpenParenToken AndAlso Not IsValidStatementTerminator(operatorToken) Then
                    operatorToken = validMissingOperator.AddTrailingSyntax(operatorToken, ERRID.ERR_UnknownOperator)
                    GetNextToken()
                Else
                    operatorToken = ReportSyntaxError(validMissingOperator, ERRID.ERR_UnknownOperator)
                End If
            End If

            Dim genericParams As TypeParameterListSyntax = Nothing
            If TryRejectGenericParametersForMemberDecl(genericParams) Then
                operatorToken = operatorToken.AddTrailingSyntax(genericParams)
            End If

            Dim optionalParameters As ParameterListSyntax = Nothing
            Dim params As CoreInternalSyntax.SeparatedSyntaxList(Of ParameterSyntax) = Nothing

            Dim openParenIsMissing As Boolean = False
            Dim openParen As PunctuationSyntax = Nothing
            Dim closeParen As PunctuationSyntax = Nothing

            If CurrentToken.Kind <> SyntaxKind.OpenParenToken Then
                'TODO - davidsch - Why does operator resync here with different condition than Sub and Function? Seems like these should be consistent.
                openParenIsMissing = True
                operatorToken = operatorToken.AddTrailingSyntax(ResyncAt({SyntaxKind.OpenParenToken, SyntaxKind.AsKeyword}))
            End If

            If CurrentToken.Kind = SyntaxKind.OpenParenToken Then
                params = ParseParameters(openParen, closeParen)
            End If

            If openParenIsMissing Then
                If openParen Is Nothing Then
                    openParen = DirectCast(HandleUnexpectedToken(SyntaxKind.OpenParenToken), PunctuationSyntax)
                Else
                    openParen = ReportSyntaxError(openParen, ERRID.ERR_ExpectedLparen)
                End If

                If closeParen Is Nothing Then
                    closeParen = DirectCast(HandleUnexpectedToken(SyntaxKind.CloseParenToken), PunctuationSyntax)
                End If
            End If

            If openParen IsNot Nothing Then
                optionalParameters = SyntaxFactory.ParameterList(openParen, params, closeParen)
            End If

            Dim returnType As TypeSyntax = Nothing
            Dim returnTypeAttributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax) = Nothing
            Dim asClause As SimpleAsClauseSyntax = Nothing

            Dim asKeyword As KeywordSyntax = Nothing

            ' Check the return type.

            If CurrentToken.Kind = SyntaxKind.AsKeyword Then
                asKeyword = DirectCast(CurrentToken, KeywordSyntax)
                GetNextToken()

                If CurrentToken.Kind = SyntaxKind.LessThanToken Then
                    returnTypeAttributes = ParseAttributeLists(False)
                End If

                returnType = ParseGeneralType()

                If returnType.ContainsDiagnostics Then
                    returnType = ResyncAt(returnType)
                End If

                asClause = SyntaxFactory.SimpleAsClause(asKeyword, returnTypeAttributes, returnType)
            End If

            Debug.Assert(optionalParameters IsNot Nothing, "Operators always require parameters - use missing if necessary")

            'Create the Operator statement.
            Dim operatorStatement = SyntaxFactory.OperatorStatement(attributes, modifiers, operatorKeyword, operatorToken, optionalParameters, asClause)

            ' HANDLES and IMPLEMENTS clauses are not allowed on Operator statements.

            Dim handlesOrImplementsKeyword As SyntaxToken = Nothing
            Dim err As ERRID = ERRID.ERR_None

            If CurrentToken.Kind = SyntaxKind.HandlesKeyword Then
                handlesOrImplementsKeyword = CurrentToken
                GetNextToken()
                err = ERRID.ERR_InvalidHandles

            ElseIf CurrentToken.Kind = SyntaxKind.ImplementsKeyword Then
                handlesOrImplementsKeyword = CurrentToken
                GetNextToken()
                err = ERRID.ERR_InvalidImplements
            End If

            If handlesOrImplementsKeyword IsNot Nothing Then
                Debug.Assert(err <> ERRID.ERR_None)
                operatorStatement = operatorStatement.AddTrailingSyntax(handlesOrImplementsKeyword, err)
            End If

            Return operatorStatement
        End Function

        ' /*****************************************************************************************
        ' ;ParsePropertyDefinition
        ' 
        ' Parses a property definition.  This will deal with both regular properties and 
        ' auto-properties.  There are interesting challenges here to be aware of.  The biggest
        ' problem is that the syntax for auto-properties requires potentially massive lookahead
        ' to figure out if the property is auto or regular.  There are some clues up front as to
        ' whether you are looking at a regular property (they have readonly/writeonly specifiers, 
        ' for instance) but often you have to go find the get/set/end property to know.  That requires
        ' look ahead parsing that has side effects as you can encounter #if, 'comments, and <attributes>
        ' along the way.  Parsing those things throws statements onto the context block but since
        ' we don't know at the time if we have an auto or regular property, the context isn't set up
        ' yet.  So we have to do some evil stuff and move the statements to the property context when
        ' we finally create one if it turns out that we are looking at a regular property instead of
        ' an auto property.
        ' 
        ' I've tried to keep lookahead to a minimum as implicit line continuation is another thorn here.
        ' We really need to use the parser to look ahead because it understand line continuation in all
        ' the many places we may encounter it before getting to the get/set/end property statements.
        ' Parameters can have implicit line continuation as can the property type, and the property
        ' initializer, etc.  So we parse as far as we can before doing speculative parsing.  But doing
        ' so requires that we haul along enough information that we discover along the way so that when
        ' we finally do know what kind of property tree to build, we can build it.
        ' ******************************************************************************************/
        ' the property tree for the auto/regular property we are on now

        ' [in] attributes that preceded the property definition
        ' [in] specifiers on the property definition
        ' [in] token starting definition (should be tkPROPERTY)
        ' [out] whether we encounter errors trying to parse the property
        ' [in] whether the property is defined within the context of an interface 
        ' Used to reorder StatementList in LinkStatement if necessary.
        ' Used to reorder StatementList in LinkStatement if necessary.

        Private Function ParsePropertyDefinition(
            attributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax),
            modifiers As CoreInternalSyntax.SyntaxList(Of KeywordSyntax)
        ) As PropertyStatementSyntax

            Debug.Assert(CurrentToken.Kind = SyntaxKind.PropertyKeyword, "ParsePropertyDefinition called on the wrong token.")

            Dim propertyKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken() ' get off PROPERTY

            ' ====== Check for the obsolete style (Property Get, Property Set, Property Let)- not allowed any longer.
            Dim ident As IdentifierTokenSyntax

            If CurrentToken.Kind = SyntaxKind.GetKeyword OrElse
                CurrentToken.Kind = SyntaxKind.SetKeyword OrElse
                CurrentToken.Kind = SyntaxKind.LetKeyword Then

                ident = ReportSyntaxError(ParseIdentifierAllowingKeyword(), ERRID.ERR_ObsoletePropertyGetLetSet)

                ' This is to handle the obsolete syntax GET identifier.  The Get becomes a simpleName and
                ' the identifier is unexpected syntax.  The Dev10 code kept the identifier as the property
                ' name but dropped the GET/SET/LET on the floor and used it only for error message span.

                If CurrentToken.Kind = SyntaxKind.IdentifierToken Then
                    ident = ident.AddTrailingSyntax(ParseIdentifier())
                End If

            Else
                ' ===== Parse the property name

                ident = ParseIdentifier()
            End If

            Dim genericParams As TypeParameterListSyntax = Nothing
            If TryRejectGenericParametersForMemberDecl(genericParams) Then
                ident = ident.AddTrailingSyntax(genericParams)
            End If

            ' ===== Parse the Property parameters, e.g. Property bob(x as integer, y as integer)

            Dim openParen As PunctuationSyntax = Nothing ' Track where this is so we can set the punctuators when we build the tree
            Dim closeParen As PunctuationSyntax = Nothing ' Track where this is so we can set the punctuators when we build the tree
            Dim propertyParameters As CoreInternalSyntax.SeparatedSyntaxList(Of ParameterSyntax) = Nothing
            Dim optionalParameters As ParameterListSyntax = Nothing

            If CurrentToken.Kind = SyntaxKind.OpenParenToken Then
                propertyParameters = ParseParameters(openParen, closeParen)
                optionalParameters = SyntaxFactory.ParameterList(openParen, propertyParameters, closeParen)
            Else
                If ident.ContainsDiagnostics Then
                    ' If we blow up on the name try to resume on the AS, =, or Implements
                    Dim unexpected = ResyncAt({SyntaxKind.AsKeyword, SyntaxKind.ImplementsKeyword, SyntaxKind.EqualsToken})
                    ident = ident.AddTrailingSyntax(unexpected)
                End If
            End If

            ' ===== Parse the property's type (e.g. Property Goo(params) AS type )

            Dim asClause As AsClauseSyntax = Nothing
            Dim initializer As EqualsValueSyntax = Nothing

            ' ===== Parse AS [NEW] <attributes> TYPE[(ctor args)] [ObjectCreationExpressionInitializer]
            ParseFieldOrPropertyAsClauseAndInitializer(True, False, asClause, initializer)

            ' Parse the IMPLEMENTS statement if any.  Note that the Implements statement
            ' must be on the same line as the Property definition statement.  In cases of
            ' implicit line continuation, it must be on the same logical line as the Property
            ' definition, e.g. following the initializer or the property type

            Dim implementsClause As ImplementsClauseSyntax = Nothing
            If CurrentToken.Kind = SyntaxKind.ImplementsKeyword Then
                implementsClause = ParseImplementsList()
            End If

            ' Checks for expanded property (property block) have been moved into the ContextBlock.

            ' Build the tree for the property and do some simple semantics like making sure a regular property doesn't have an initializer, etc.
            Dim propertyStatement As PropertyStatementSyntax = SyntaxFactory.PropertyStatement(attributes, modifiers, propertyKeyword, ident, optionalParameters, asClause, initializer, implementsClause)

            ' Need to look ahead to the next token, after the statement terminator, to see if this is an
            ' auto property or not.
            If CurrentToken.Kind <> SyntaxKind.EndOfFileToken Then
                Dim peek = PeekToken(1)
                If peek.Kind <> SyntaxKind.GetKeyword AndAlso peek.Kind <> SyntaxKind.SetKeyword Then
                    If Context.BlockKind <> SyntaxKind.InterfaceBlock AndAlso Not propertyStatement.Modifiers.Any(SyntaxKind.MustOverrideKeyword) Then
                        Dim originalStatement = propertyStatement
                        propertyStatement = CheckFeatureAvailability(Feature.AutoProperties, propertyStatement)

                        If propertyStatement Is originalStatement AndAlso propertyStatement.Modifiers.Any(SyntaxKind.ReadOnlyKeyword) Then
                            propertyStatement = CheckFeatureAvailability(Feature.ReadonlyAutoProperties, propertyStatement)
                        End If
                    End If
                End If
            End If

            Return propertyStatement
        End Function

        ' Parse a declaration of a delegate.
        ' [in] procedure specifiers
        ' [in] token starting statement

        ' File:Parser.cpp
        ' Lines: 8928 - 8928
        ' MethodDeclarationStatement* .Parser::ParseDelegateStatement( [ ParseTree::AttributeSpecifierList* Attributes ] [ ParseTree::SpecifierList* Specifiers ] [ _In_ Token* Start ] [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseDelegateStatement(
            attributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax),
            modifiers As CoreInternalSyntax.SyntaxList(Of KeywordSyntax)
        ) As DelegateStatementSyntax

            Debug.Assert(CurrentToken.Kind = SyntaxKind.DelegateKeyword, "ParseDelegateStatement called on the wrong token.")

            Dim delegateKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)

            Dim delegateKind As SyntaxKind
            Dim methodKeyword As KeywordSyntax = Nothing
            Dim name As IdentifierTokenSyntax = Nothing
            Dim genericParams As TypeParameterListSyntax = Nothing
            Dim parameters As ParameterListSyntax = Nothing
            Dim asClause As SimpleAsClauseSyntax = Nothing
            Dim handlesClause As HandlesClauseSyntax = Nothing
            Dim implementsClause As ImplementsClauseSyntax = Nothing

            GetNextToken()

            Select Case (CurrentToken.Kind)

                Case SyntaxKind.SubKeyword
                    delegateKind = SyntaxKind.DelegateSubStatement
                    methodKeyword = DirectCast(CurrentToken, KeywordSyntax)
                    GetNextToken()
                    ParseSubOrDelegateStatement(SyntaxKind.DelegateSubStatement, name, genericParams, parameters, handlesClause, implementsClause)

                Case SyntaxKind.FunctionKeyword
                    delegateKind = SyntaxKind.DelegateFunctionStatement
                    methodKeyword = DirectCast(CurrentToken, KeywordSyntax)
                    GetNextToken()
                    ParseFunctionOrDelegateStatement(SyntaxKind.DelegateFunctionStatement, name, genericParams, parameters, asClause, handlesClause, implementsClause)

                Case Else
                    ' Syntax error. Try to produce a delegate declaration.

                    ' TODO - Which keyword SUB or FUNCTION?
                    delegateKind = SyntaxKind.DelegateSubStatement
                    ' TODO - Consider adding this as another case of VerifyExpectedToken
                    methodKeyword = InternalSyntaxFactory.MissingKeyword(SyntaxKind.SubKeyword)

                    methodKeyword = ReportSyntaxError(methodKeyword, ERRID.ERR_ExpectedSubOrFunction)

                    ' The old code was just a normal parse of a sub so why not just call ParseSubOrDelegate instead.
                    ParseSubOrDelegateStatement(SyntaxKind.DelegateSubStatement, name, genericParams, parameters, handlesClause, implementsClause)

            End Select

            ' We should be at the end of the statement.

            'Create the delegate statement.
            Dim delegateStatement As DelegateStatementSyntax =
                SyntaxFactory.DelegateStatement(delegateKind,
                                         attributes,
                                         modifiers,
                                         delegateKeyword,
                                         methodKeyword,
                                         name,
                                         genericParams,
                                         parameters,
                                         asClause)

            If handlesClause IsNot Nothing Then
                delegateStatement = delegateStatement.AddTrailingSyntax(handlesClause)
            End If
            If implementsClause IsNot Nothing Then
                delegateStatement = delegateStatement.AddTrailingSyntax(implementsClause)
            End If

            Return delegateStatement
        End Function

        ' File:Parser.cpp
        ' Lines: 8128 - 8128
        ' GenericParameterList* .Parser::ParseGenericParameters( [ _Out_ Token*& Of ] [ _Out_ Token*& openParen ] [ _Out_ Token*& closeParen ] [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseGenericParameters() As TypeParameterListSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.OpenParenToken)

            Dim openParen As PunctuationSyntax = Nothing
            Dim ofKeyword As KeywordSyntax = Nothing
            Dim closeParen As PunctuationSyntax = Nothing
            Dim comma As PunctuationSyntax = Nothing

            TryGetTokenAndEatNewLine(SyntaxKind.OpenParenToken, openParen)

            ' Consume Of keyword
            TryGetTokenAndEatNewLine(SyntaxKind.OfKeyword, ofKeyword, createIfMissing:=True)

            Dim typeParameters = Me._pool.AllocateSeparated(Of TypeParameterSyntax)()
            Dim asKeyword As KeywordSyntax

            Do
                Dim name As IdentifierTokenSyntax = Nothing

                ' (Of In T) or (Of Out T) or just (Of T). If the current token is "Out" or "In"
                ' then we have to consume it and get the next token...

                Dim optionalVarianceModifier As KeywordSyntax = Nothing

                If CurrentToken.Kind = SyntaxKind.InKeyword Then
                    optionalVarianceModifier = DirectCast(CurrentToken, KeywordSyntax)
                    optionalVarianceModifier = CheckFeatureAvailability(Feature.CoContraVariance, optionalVarianceModifier)
                    GetNextToken()

                Else
                    Dim outKeyword As KeywordSyntax = Nothing
                    If TryTokenAsContextualKeyword(CurrentToken, SyntaxKind.OutKeyword, outKeyword) Then
                        Dim id = DirectCast(CurrentToken, IdentifierTokenSyntax)
                        GetNextToken()

                        TryEatNewLineIfFollowedBy(SyntaxKind.CloseParenToken) ' dev10_503122 Allow EOL before ')'

                        ' ... unless the next token is ) or , or As -- which indicate that the "Out" we just consumed
                        ' should have been taken as the identifier instead.
                        If CurrentToken.Kind = SyntaxKind.CloseParenToken OrElse CurrentToken.Kind = SyntaxKind.CommaToken OrElse CurrentToken.Kind = SyntaxKind.AsKeyword Then
                            ' Use Out keyword as the identifier and not as the modifier
                            name = id
                            optionalVarianceModifier = Nothing
                        Else
                            outKeyword = CheckFeatureAvailability(Feature.CoContraVariance, outKeyword)
                            optionalVarianceModifier = outKeyword
                        End If
                    End If
                End If

                If name Is Nothing Then
                    name = ParseIdentifier()
                End If

                Dim typeParameterConstraintClause As TypeParameterConstraintClauseSyntax = Nothing
                asKeyword = Nothing

                If CurrentToken.Kind = SyntaxKind.AsKeyword Then

                    asKeyword = DirectCast(CurrentToken, KeywordSyntax)

                    GetNextToken()

                    Dim openBrace As PunctuationSyntax = Nothing

                    If TryGetTokenAndEatNewLine(SyntaxKind.OpenBraceToken, openBrace) Then
                        Dim constraints = Me._pool.AllocateSeparated(Of ConstraintSyntax)()

                        Do
                            Dim constraint = ParseConstraintSyntax()

                            If constraint.ContainsDiagnostics Then
                                constraint = ResyncAt(constraint, SyntaxKind.CommaToken, SyntaxKind.CloseBraceToken, SyntaxKind.CloseParenToken)
                            End If

                            constraints.Add(constraint)

                            comma = Nothing
                            If TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then
                                constraints.AddSeparator(comma)
                            Else
                                Exit Do
                            End If
                        Loop

                        Dim closeBrace As PunctuationSyntax = Nothing
                        TryEatNewLineAndGetToken(SyntaxKind.CloseBraceToken, closeBrace, createIfMissing:=True)

                        Dim constraintList = constraints.ToList
                        Me._pool.Free(constraints)

                        typeParameterConstraintClause = SyntaxFactory.TypeParameterMultipleConstraintClause(asKeyword, openBrace, constraintList, closeBrace)

                    Else
                        Dim constraint = ParseConstraintSyntax()

                        If constraint.ContainsDiagnostics Then
                            constraint = ResyncAt(constraint, SyntaxKind.CloseParenToken)
                        End If

                        typeParameterConstraintClause = SyntaxFactory.TypeParameterSingleConstraintClause(asKeyword, constraint)

                    End If
                End If

                Dim typeParameter = SyntaxFactory.TypeParameter(optionalVarianceModifier, name, typeParameterConstraintClause)

                typeParameters.Add(typeParameter)

                comma = Nothing
                If TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then
                    typeParameters.AddSeparator(comma)
                Else
                    Exit Do
                End If
            Loop

            If openParen IsNot Nothing Then
                If Not TryEatNewLineAndGetToken(SyntaxKind.CloseParenToken, closeParen, createIfMissing:=False) Then
                    closeParen = InternalSyntaxFactory.MissingPunctuation(SyntaxKind.CloseParenToken)

                    closeParen = ReportSyntaxError(closeParen,
                        If(asKeyword Is Nothing,
                            ERRID.ERR_TypeParamMissingAsCommaOrRParen,
                            ERRID.ERR_TypeParamMissingCommaOrRParen))
                End If
            End If

            Dim separatedTypeParameters = typeParameters.ToList
            Me._pool.Free(typeParameters)

            Dim result As TypeParameterListSyntax = SyntaxFactory.TypeParameterList(openParen, ofKeyword, separatedTypeParameters, closeParen)

            Debug.Assert(result IsNot Nothing)
            Return result
        End Function

        Private Function ParseConstraintSyntax() As ConstraintSyntax
            Dim constraint As ConstraintSyntax = Nothing
            Dim keyword As KeywordSyntax

            If CurrentToken.Kind = SyntaxKind.NewKeyword Then
                ' New constraint
                keyword = DirectCast(CurrentToken, KeywordSyntax)

                constraint = SyntaxFactory.NewConstraint(keyword)

                GetNextToken()

            ElseIf CurrentToken.Kind = SyntaxKind.ClassKeyword Then
                ' Class constraint
                keyword = DirectCast(CurrentToken, KeywordSyntax)
                constraint = SyntaxFactory.ClassConstraint(keyword)
                GetNextToken()

            ElseIf CurrentToken.Kind = SyntaxKind.StructureKeyword Then
                ' Struct constraint

                keyword = DirectCast(CurrentToken, KeywordSyntax)
                constraint = SyntaxFactory.StructureConstraint(keyword)
                GetNextToken()

            Else
                Dim syntaxError As DiagnosticInfo = Nothing

                If Not CanTokenStartTypeName(CurrentToken) Then
                    syntaxError = ErrorFactory.ErrorInfo(ERRID.ERR_BadConstraintSyntax)

                    ' Continue parsing as a type constraint
                End If

                ' Type constraint
                Dim typeName As TypeSyntax = ParseGeneralType()

                If syntaxError IsNot Nothing Then
                    typeName = DirectCast(typeName.AddError(syntaxError), TypeSyntax)
                End If

                constraint = SyntaxFactory.TypeConstraint(typeName)
            End If

            Return constraint
        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseParameters
        ' *
        ' * Purpose:
        ' *     Parses a parenthesized parameter list of non-optional followed by
        ' *     optional parameters (if any).
        ' *
        ' **********************************************************************/

        ' File:Parser.cpp
        ' Lines: 9598 - 9598
        ' ParameterList* .Parser::ParseParameters( [ _Inout_ bool& ErrorInConstruct ] [ _Out_ Token*& openParen ] [ _Out_ Token*& closeParen ] )

        Private Function ParseParameters(ByRef openParen As PunctuationSyntax, ByRef closeParen As PunctuationSyntax) As CoreInternalSyntax.SeparatedSyntaxList(Of ParameterSyntax)
            Debug.Assert(CurrentToken.Kind = SyntaxKind.OpenParenToken, "Parameter list parsing confused.")
            TryGetTokenAndEatNewLine(SyntaxKind.OpenParenToken, openParen)

            Dim parameters = _pool.AllocateSeparated(Of ParameterSyntax)()

            If CurrentToken.Kind <> SyntaxKind.CloseParenToken Then

                ' Loop through the list of parameters.

                Do
                    Dim attributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax) = Nothing

                    If Me.CurrentToken.Kind = Global.Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.LessThanToken Then
                        attributes = ParseAttributeLists(False)
                    End If

                    Dim paramSpecifiers As ParameterSpecifiers = 0
                    Dim modifiers = ParseParameterSpecifiers(paramSpecifiers)
                    Dim param = ParseParameter(attributes, modifiers)

                    ' TODO - Bug 889301 - Dev10 does a resync here when there is an error.  That prevents ERRID_InvalidParameterSyntax below from
                    ' being reported. For now keep backwards compatibility.
                    If param.ContainsDiagnostics Then
                        param = param.AddTrailingSyntax(ResyncAt({SyntaxKind.CommaToken, SyntaxKind.CloseParenToken}))
                    End If

                    Dim comma As PunctuationSyntax = Nothing
                    If Not TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then

                        If CurrentToken.Kind <> SyntaxKind.CloseParenToken AndAlso Not MustEndStatement(CurrentToken) Then

                            ' Check the ')' on the next line
                            If IsContinuableEOL() Then
                                If PeekToken(1).Kind = SyntaxKind.CloseParenToken Then
                                    parameters.Add(param)
                                    Exit Do
                                End If
                            End If

                            param = param.AddTrailingSyntax(ResyncAt({SyntaxKind.CommaToken, SyntaxKind.CloseParenToken}), ERRID.ERR_InvalidParameterSyntax)

                            If Not TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then
                                parameters.Add(param)
                                Exit Do
                            End If

                        Else
                            parameters.Add(param)
                            Exit Do

                        End If
                    End If

                    parameters.Add(param)
                    parameters.AddSeparator(comma)
                Loop

            End If

            ' Current token is left at either tkRParen, EOS

            TryEatNewLineAndGetToken(SyntaxKind.CloseParenToken, closeParen, createIfMissing:=True)

            Dim result = parameters.ToList()

            _pool.Free(parameters)

            Return result

        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseParameterSpecifiers
        ' *
        ' * Purpose:
        ' *
        ' **********************************************************************/

        ' File:Parser.cpp
        ' Lines: 9748 - 9748
        ' ParameterSpecifierList* .Parser::ParseParameterSpecifiers( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseParameterSpecifiers(ByRef specifiers As ParameterSpecifiers) As CoreInternalSyntax.SyntaxList(Of KeywordSyntax)
            Dim keywords = Me._pool.Allocate(Of KeywordSyntax)()

            specifiers = 0

            'TODO - Move these checks to Binder_Utils.DecodeParameterModifiers  

            Do
                Dim specifier As ParameterSpecifiers
                Dim keyword As KeywordSyntax

                Select Case (CurrentToken.Kind)

                    Case SyntaxKind.ByValKeyword
                        keyword = DirectCast(CurrentToken, KeywordSyntax)
                        If (specifiers And ParameterSpecifiers.ByRef) <> 0 Then
                            keyword = ReportSyntaxError(keyword, ERRID.ERR_MultipleParameterSpecifiers)
                        End If
                        specifier = ParameterSpecifiers.ByVal

                    Case SyntaxKind.ByRefKeyword
                        keyword = DirectCast(CurrentToken, KeywordSyntax)
                        If (specifiers And ParameterSpecifiers.ByVal) <> 0 Then
                            keyword = ReportSyntaxError(keyword, ERRID.ERR_MultipleParameterSpecifiers)

                        ElseIf (specifiers And ParameterSpecifiers.ParamArray) <> 0 Then
                            keyword = ReportSyntaxError(keyword, ERRID.ERR_ParamArrayMustBeByVal)
                        End If
                        specifier = ParameterSpecifiers.ByRef

                    Case SyntaxKind.OptionalKeyword
                        keyword = DirectCast(CurrentToken, KeywordSyntax)
                        If (specifiers And ParameterSpecifiers.ParamArray) <> 0 Then
                            keyword = ReportSyntaxError(keyword, ERRID.ERR_MultipleOptionalParameterSpecifiers)
                        End If
                        specifier = ParameterSpecifiers.Optional

                    Case SyntaxKind.ParamArrayKeyword
                        keyword = DirectCast(CurrentToken, KeywordSyntax)
                        If (specifiers And ParameterSpecifiers.Optional) <> 0 Then
                            keyword = ReportSyntaxError(keyword, ERRID.ERR_MultipleOptionalParameterSpecifiers)
                        ElseIf (specifiers And ParameterSpecifiers.ByRef) <> 0 Then
                            keyword = ReportSyntaxError(keyword, ERRID.ERR_ParamArrayMustBeByVal)
                        End If
                        specifier = ParameterSpecifiers.ParamArray

                    Case Else
                        Dim result = keywords.ToList
                        Me._pool.Free(keywords)

                        Return result
                End Select

                If (specifiers And specifier) <> 0 Then
                    keyword = ReportSyntaxError(keyword, ERRID.ERR_DuplicateParameterSpecifier)
                Else
                    specifiers = specifiers Or specifier
                End If

                keywords.Add(keyword)

                GetNextToken()
            Loop
        End Function

        ''' <summary>
        '''     Parameter -> Attributes? ParameterModifiers* ParameterIdentifier ("as" TypeName)? ("=" ConstantExpression)?
        ''' </summary>
        ''' <param name="attributes"></param>
        ''' <param name="modifiers"></param>
        ''' <returns></returns>
        ''' <remarks>>This replaces both ParseParameter and ParseOptionalParameter in Dev10</remarks>
        Private Function ParseParameter(attributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax), modifiers As CoreInternalSyntax.SyntaxList(Of KeywordSyntax)) As ParameterSyntax
            Dim paramName = ParseModifiedIdentifier(False, False)

            If paramName.ContainsDiagnostics Then

                ' If we see As before a comma or RParen, then assume that
                ' we are still on the same parameter. Otherwise, don't resync
                ' and allow the caller to decide how to recover.

                If PeekAheadFor(SyntaxKind.AsKeyword, SyntaxKind.CommaToken, SyntaxKind.CloseParenToken) = SyntaxKind.AsKeyword Then
                    paramName = ResyncAt(paramName, SyntaxKind.AsKeyword)
                End If
            End If

            Dim optionalAsClause As SimpleAsClauseSyntax = Nothing
            Dim asKeyword As KeywordSyntax = Nothing

            If TryGetToken(SyntaxKind.AsKeyword, asKeyword) Then
                Dim typeName = ParseGeneralType()

                optionalAsClause = SyntaxFactory.SimpleAsClause(asKeyword, Nothing, typeName)

                If optionalAsClause.ContainsDiagnostics Then
                    optionalAsClause = ResyncAt(optionalAsClause, SyntaxKind.EqualsToken, SyntaxKind.CommaToken, SyntaxKind.CloseParenToken)
                End If

            End If

            Dim equals As PunctuationSyntax = Nothing
            Dim value As ExpressionSyntax = Nothing

            ' TODO - Move these errors (ERRID.ERR_DefaultValueForNonOptionalParamout, ERRID.ERR_ObsoleteOptionalWithoutValue) of the parser. 
            ' These are semantic errors. The grammar allows the syntax. 
            If TryGetTokenAndEatNewLine(SyntaxKind.EqualsToken, equals) Then

                If Not (modifiers.Any AndAlso modifiers.Any(SyntaxKind.OptionalKeyword)) Then
                    equals = ReportSyntaxError(equals, ERRID.ERR_DefaultValueForNonOptionalParam)
                End If

                value = ParseExpressionCore()

            ElseIf modifiers.Any AndAlso modifiers.Any(SyntaxKind.OptionalKeyword) Then

                equals = ReportSyntaxError(InternalSyntaxFactory.MissingPunctuation(SyntaxKind.EqualsToken), ERRID.ERR_ObsoleteOptionalWithoutValue)
                value = ParseExpressionCore()

            End If

            Dim initializer As EqualsValueSyntax = Nothing

            If value IsNot Nothing Then

                If value.ContainsDiagnostics Then
                    value = ResyncAt(value, SyntaxKind.CommaToken, SyntaxKind.CloseParenToken)
                End If

                initializer = SyntaxFactory.EqualsValue(equals, value)
            End If

            Return SyntaxFactory.Parameter(attributes, modifiers, paramName, optionalAsClause, initializer)
        End Function

        ' File:Parser.cpp
        ' Lines: 10120 - 10120
        ' ImportsStatement* .Parser::ParseImportsStatement( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseImportsStatement(Attributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax), Specifiers As CoreInternalSyntax.SyntaxList(Of KeywordSyntax)) As ImportsStatementSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.ImportsKeyword, "called on wrong token")

            Dim importsKeyword As KeywordSyntax = ReportModifiersOnStatementError(Attributes, Specifiers, DirectCast(CurrentToken, KeywordSyntax))
            Dim importsClauses = Me._pool.AllocateSeparated(Of ImportsClauseSyntax)()

            GetNextToken()

            Do

                Dim ImportsClause As ImportsClauseSyntax = ParseOneImportsDirective()

                importsClauses.Add(ImportsClause)

                Dim comma As PunctuationSyntax = Nothing
                If Not TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then
                    Exit Do
                End If

                importsClauses.AddSeparator(comma)
            Loop

            Dim result = importsClauses.ToList
            Me._pool.Free(importsClauses)
            Dim statement As ImportsStatementSyntax = SyntaxFactory.ImportsStatement(importsKeyword, result)

            Return statement
        End Function

        ' File:Parser.cpp
        ' Lines: 10156 - 10156
        ' ImportDirective* .Parser::ParseOneImportsDirective( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseOneImportsDirective() As ImportsClauseSyntax

            Dim importsClause As ImportsClauseSyntax = Nothing

            ' If the imports directive begins with '<', then it is an Xml imports directive

            If CurrentToken.Kind = SyntaxKind.LessThanToken Then
                ResetCurrentToken(ScannerState.Element)

                Dim lessToken As PunctuationSyntax = Nothing
                Dim xmlNamespace As XmlAttributeSyntax

                ' Verify the '<' is still a '<' in the XML ScannerState
                ' and not a compound token such as '<%='.
                If VerifyExpectedToken(SyntaxKind.LessThanToken, lessToken, ScannerState.Element) Then
                    If CurrentToken.Kind = SyntaxKind.XmlNameToken AndAlso
                        CurrentToken.ToFullString = "xmlns" AndAlso
                        Not lessToken.HasTrailingTrivia Then

                        ' Parse namespace declaration as a regular attribute
                        xmlNamespace = DirectCast(ParseXmlAttribute(False, False, Nothing), XmlAttributeSyntax)

                    Else
                        xmlNamespace = ReportSyntaxError(CreateMissingXmlAttribute(), ERRID.ERR_ExpectedXmlns)
                    End If

                    Dim unexpected = ResyncAt(ScannerState.Element, {SyntaxKind.GreaterThanToken})
                    If unexpected.Any() Then
                        xmlNamespace = xmlNamespace.AddTrailingSyntax(unexpected, ERRID.ERR_ExpectedGreater)
                    End If

                Else
                    xmlNamespace = CreateMissingXmlAttribute()

                    Dim unexpected = ResyncAt(ScannerState.Element, {SyntaxKind.GreaterThanToken})
                    Debug.Assert(unexpected.Any())
                    xmlNamespace = xmlNamespace.AddTrailingSyntax(unexpected)
                End If

                Dim greaterToken As PunctuationSyntax = Nothing
                VerifyExpectedToken(SyntaxKind.GreaterThanToken, greaterToken, ScannerState.Element)

                importsClause = SyntaxFactory.XmlNamespaceImportsClause(lessToken, xmlNamespace, greaterToken)
                importsClause = AdjustTriviaForMissingTokens(importsClause)
                importsClause = TransitionFromXmlToVB(importsClause)

            Else
                ' Handle Clr namespace imports if we have currently have tokens for ID =

                If (CurrentToken.Kind = SyntaxKind.IdentifierToken AndAlso
                   PeekToken(1).Kind = SyntaxKind.EqualsToken) OrElse
                   CurrentToken.Kind = SyntaxKind.EqualsToken Then

                    ' If we find "id =" or "=" parse as an imports alias.  While "=" without the id is an error,
                    ' for error recovery purposes, we insert a missing id and continue parsing.  This allows the ide
                    ' to handle the error better.

                    Dim aliasIdentifier = ParseIdentifier()

                    If aliasIdentifier.TypeCharacter <> TypeCharacter.None Then
                        aliasIdentifier = ReportSyntaxError(aliasIdentifier, ERRID.ERR_NoTypecharInAlias)
                    End If

                    Dim equalsToken As PunctuationSyntax = DirectCast(CurrentToken, PunctuationSyntax)

                    GetNextToken() ' Get off the '='
                    TryEatNewLine() ' Dev10_496850 Allow implicit line continuation after the '=', e.g. Imports a=

                    Dim name = ParseName(
                        requireQualification:=False,
                        allowGlobalNameSpace:=False,
                        allowGenericArguments:=True,
                        allowGenericsWithoutOf:=True)
                    importsClause = SyntaxFactory.SimpleImportsClause(SyntaxFactory.ImportAliasClause(aliasIdentifier, equalsToken), name)
                Else
                    Dim name = ParseName(
                        requireQualification:=False,
                        allowGlobalNameSpace:=False,
                        allowGenericArguments:=True,
                        allowGenericsWithoutOf:=True)

                    importsClause = SyntaxFactory.SimpleImportsClause(Nothing, name)
                End If

            End If

            If importsClause.ContainsDiagnostics AndAlso CurrentToken.Kind <> SyntaxKind.CommaToken Then
                ' Just resync at the end so we don't get any expecting EOS errors. But only skip to the end
                ' of the line if we are not on the expected comma token.
                importsClause = importsClause.AddTrailingSyntax(ResyncAt({SyntaxKind.CommaToken}))
            End If

            Return importsClause
        End Function

        Private Function CreateMissingXmlString() As XmlStringSyntax
            Dim missingDoubleQuote = InternalSyntaxFactory.MissingPunctuation(SyntaxKind.DoubleQuoteToken)
            Return SyntaxFactory.XmlString(missingDoubleQuote, Nothing, missingDoubleQuote)
        End Function

        Private Function CreateMissingXmlAttribute() As XmlAttributeSyntax
            Dim missingXmlName = DirectCast(InternalSyntaxFactory.MissingToken(SyntaxKind.XmlNameToken), XmlNameTokenSyntax)
            Dim missingColon = InternalSyntaxFactory.MissingPunctuation(SyntaxKind.ColonToken)
            Dim missingEquals = InternalSyntaxFactory.MissingPunctuation(SyntaxKind.EqualsToken)
            Return SyntaxFactory.XmlAttribute(SyntaxFactory.XmlName(SyntaxFactory.XmlPrefix(missingXmlName, missingColon), missingXmlName),
                                                                        missingEquals,
                                                                        CreateMissingXmlString())
        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseInheritsImplementsStatement
        ' *
        ' * Purpose:
        ' *
        ' **********************************************************************/

        ' File:Parser.cpp
        ' Lines: 10274 - 10274
        ' Statement* .Parser::ParseInheritsImplementsStatement( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseInheritsImplementsStatement(Attributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax), Specifiers As CoreInternalSyntax.SyntaxList(Of KeywordSyntax)) As InheritsOrImplementsStatementSyntax

            Debug.Assert(CurrentToken.Kind = SyntaxKind.InheritsKeyword OrElse CurrentToken.Kind = SyntaxKind.ImplementsKeyword,
                "ParseInheritsImplementsStatement called on the wrong token.")

            Dim keyword As KeywordSyntax = ReportModifiersOnStatementError(Attributes, Specifiers, DirectCast(CurrentToken, KeywordSyntax))
            Dim typeNames = Me._pool.AllocateSeparated(Of TypeSyntax)()

            GetNextToken()

            Do
                Dim typeName As TypeSyntax = ParseTypeName(nonArrayName:=True)

                If typeName.ContainsDiagnostics Then
                    typeName = ResyncAt(typeName, SyntaxKind.CommaToken)
                End If

                typeNames.Add(typeName)

                'Eat a new line after "," but not "INHERITS" or "IMPLEMENTS"
                Dim comma As PunctuationSyntax = Nothing
                If Not TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then
                    Exit Do
                End If

                typeNames.AddSeparator(comma)
            Loop

            Dim separatedTypeNames = typeNames.ToList
            Me._pool.Free(typeNames)

            Dim result As InheritsOrImplementsStatementSyntax = Nothing
            Select Case (keyword.Kind)
                Case SyntaxKind.InheritsKeyword
                    result = SyntaxFactory.InheritsStatement(keyword, separatedTypeNames)

                Case SyntaxKind.ImplementsKeyword
                    result = SyntaxFactory.ImplementsStatement(keyword, separatedTypeNames)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(keyword.Kind)
            End Select

            Return result
        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseOptionStatement
        ' *
        ' * Purpose:
        ' *
        ' **********************************************************************/

        ' File:Parser.cpp
        ' Lines: 10345 - 10345
        ' Statement* .Parser::ParseOptionStatement( [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseOptionStatement(Attributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax), Specifiers As CoreInternalSyntax.SyntaxList(Of KeywordSyntax)) As StatementSyntax
            Dim ErrorId As ERRID = ERRID.ERR_None
            Dim optionType As KeywordSyntax = Nothing
            Dim optionValue As KeywordSyntax = Nothing

            Debug.Assert(CurrentToken.Kind = SyntaxKind.OptionKeyword, "must be at Option.")

            Dim optionKeyword = ReportModifiersOnStatementError(Attributes, Specifiers, DirectCast(CurrentToken, KeywordSyntax))
            GetNextToken()

            If TryTokenAsContextualKeyword(CurrentToken, optionType) Then

                Select Case (optionType.Kind)

                    Case SyntaxKind.CompareKeyword

                        GetNextToken()

                        If TryTokenAsContextualKeyword(CurrentToken, optionValue) Then

                            If optionValue.Kind = SyntaxKind.TextKeyword Then
                                GetNextToken()

                            ElseIf optionValue.Kind = SyntaxKind.BinaryKeyword Then
                                GetNextToken()

                            Else
                                ' Create a missing option value.  Binary/Text is not optional
                                optionValue = InternalSyntaxFactory.MissingKeyword(SyntaxKind.BinaryKeyword)
                                ErrorId = ERRID.ERR_InvalidOptionCompare
                            End If

                        Else
                            ' Create a missing option value.  Binary/Text is not optional
                            optionValue = InternalSyntaxFactory.MissingKeyword(SyntaxKind.BinaryKeyword)
                            ErrorId = ERRID.ERR_InvalidOptionCompare
                        End If

                    Case SyntaxKind.ExplicitKeyword,
                            SyntaxKind.StrictKeyword,
                             SyntaxKind.InferKeyword

                        GetNextToken()

                        If CurrentToken.Kind = SyntaxKind.OnKeyword Then
                            optionValue = DirectCast(CurrentToken, KeywordSyntax)
                            GetNextToken()

                        ElseIf TryTokenAsContextualKeyword(CurrentToken, optionValue) AndAlso
                            optionValue.Kind = SyntaxKind.OffKeyword Then
                            GetNextToken()

                        ElseIf Not IsValidStatementTerminator(CurrentToken) Then
                            ' Skip over the invalid token.

                            If optionType.Kind = SyntaxKind.StrictKeyword Then
                                If optionValue IsNot Nothing AndAlso optionValue.Kind = SyntaxKind.CustomKeyword Then
                                    ErrorId = ERRID.ERR_InvalidOptionStrictCustom
                                Else
                                    ErrorId = ERRID.ERR_InvalidOptionStrict
                                End If

                            ElseIf optionType.Kind = SyntaxKind.ExplicitKeyword Then
                                ErrorId = ERRID.ERR_InvalidOptionExplicit
                            Else
                                ErrorId = ERRID.ERR_InvalidOptionInfer
                            End If

                            optionValue = Nothing
                        End If

                    Case SyntaxKind.TextKeyword, SyntaxKind.BinaryKeyword
                        ' Error recovery.
                        ' The following are errors but we can probably guess what was intended
                        optionType = InternalSyntaxFactory.MissingKeyword(SyntaxKind.CompareKeyword)
                        ErrorId = ERRID.ERR_ExpectedOptionCompare

                    Case Else
                        optionType = InternalSyntaxFactory.MissingKeyword(SyntaxKind.StrictKeyword)
                        ErrorId = ERRID.ERR_ExpectedForOptionStmt
                End Select
            Else
                optionType = InternalSyntaxFactory.MissingKeyword(SyntaxKind.StrictKeyword)
                ErrorId = ERRID.ERR_ExpectedForOptionStmt
            End If

            Dim statement = SyntaxFactory.OptionStatement(optionKeyword, optionType, optionValue)

            If ErrorId <> ERRID.ERR_None Then
                ' Resync at EOS so we don't get anymore errors
                statement = statement.AddTrailingSyntax(ResyncAt(), ErrorId)
            End If

            Return statement
        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseProcDeclareStatement
        ' *
        ' * Purpose:
        ' *
        ' **********************************************************************/

        ' File:Parser.cpp
        ' Lines: 10586 - 10586
        ' ForeignMethodDeclarationStatement* .Parser::ParseProcDeclareStatement( [ ParseTree::AttributeSpecifierList* Attributes ] [ ParseTree::SpecifierList* Specifiers ] [ _In_ Token* Start ] [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseProcDeclareStatement(attributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax), modifiers As CoreInternalSyntax.SyntaxList(Of KeywordSyntax)) As DeclareStatementSyntax
            Debug.Assert(CurrentToken.Kind = SyntaxKind.DeclareKeyword, "ParseProcDeclareStatement called on wrong token. Must be at a Declare.")

            ' Dev10_667800 we are parsing a method declaration and will need to let the scanner know that we
            ' are so the scanner can correctly identify attributes vs. xml while scanning the declaration.

            'davidsch - no need to force scanner state anymore

            Dim declareKeyword = DirectCast(CurrentToken, KeywordSyntax)

            ' Skip DECLARE.
            GetNextToken()

            Dim contextualKeyword As KeywordSyntax = Nothing
            Dim optionalCharSet As KeywordSyntax = Nothing

            If TryTokenAsContextualKeyword(CurrentToken, contextualKeyword) Then

                Select Case contextualKeyword.Kind
                    Case SyntaxKind.UnicodeKeyword, SyntaxKind.AnsiKeyword, SyntaxKind.AutoKeyword
                        optionalCharSet = contextualKeyword
                        GetNextToken()
                End Select

            End If

            Dim methodKeyword As KeywordSyntax
            Dim externalKind As SyntaxKind

            If CurrentToken.Kind = SyntaxKind.SubKeyword Then
                methodKeyword = DirectCast(CurrentToken, KeywordSyntax)
                GetNextToken()
                externalKind = SyntaxKind.DeclareSubStatement

            ElseIf CurrentToken.Kind = SyntaxKind.FunctionKeyword Then
                methodKeyword = DirectCast(CurrentToken, KeywordSyntax)
                GetNextToken()
                externalKind = SyntaxKind.DeclareFunctionStatement

            Else
                methodKeyword = ReportSyntaxError(InternalSyntaxFactory.MissingKeyword(SyntaxKind.SubKeyword), ERRID.ERR_ExpectedSubFunction)
                externalKind = SyntaxKind.DeclareSubStatement

            End If

            ' Parse the function name.
            Dim name = ParseIdentifier()

            If name.ContainsDiagnostics Then
                name = name.AddTrailingSyntax(ResyncAt({SyntaxKind.LibKeyword, SyntaxKind.OpenParenToken}))
            End If

            Dim unexpected As CoreInternalSyntax.SyntaxList(Of SyntaxToken) = Nothing
            Dim missingLib As Boolean = False

            If CurrentToken.Kind <> SyntaxKind.LibKeyword Then
                ' See if there was a Lib component somewhere and the user
                ' just put it in the wrong place.

                If PeekAheadFor(SyntaxKind.LibKeyword) = SyntaxKind.LibKeyword Then
                    unexpected = ResyncAt({SyntaxKind.LibKeyword})
                Else
                    unexpected = ResyncAt({SyntaxKind.AliasKeyword, SyntaxKind.OpenParenToken})
                    missingLib = True
                End If
            End If

            Dim libKeyword As KeywordSyntax = Nothing
            Dim libraryName As LiteralExpressionSyntax = Nothing
            Dim optionalAliasKeyword As KeywordSyntax = Nothing
            Dim optionalAliasName As LiteralExpressionSyntax = Nothing

            ParseDeclareLibClause(libKeyword, libraryName, optionalAliasKeyword, optionalAliasName)

            If unexpected.Node IsNot Nothing Then
                ' When lib is missing the error is on the missing lib keyword so don't add it again.  
                ' was skipped and lib was found.
                If missingLib Then
                    libKeyword = libKeyword.AddLeadingSyntax(unexpected)
                Else
                    ' Resyncing must have found a lib, in this case, put an error on the skipped text.
                    libKeyword = libKeyword.AddLeadingSyntax(unexpected, ERRID.ERR_MissingLibInDeclare)
                End If
            End If

            Dim genericParams As TypeParameterListSyntax = Nothing

            If TryRejectGenericParametersForMemberDecl(genericParams) Then

                If optionalAliasName IsNot Nothing Then
                    optionalAliasName = optionalAliasName.AddTrailingSyntax(genericParams)

                Else
                    libraryName = libraryName.AddTrailingSyntax(genericParams)

                End If
            End If

            Dim optionalParameters As ParameterListSyntax = Nothing
            optionalParameters = ParseParameterList()

            Dim optionalAsClause As SimpleAsClauseSyntax = Nothing
            If methodKeyword.Kind = SyntaxKind.FunctionKeyword AndAlso
               CurrentToken.Kind = SyntaxKind.AsKeyword Then

                Dim asKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
                'todo - davidsch - if sub/function keyword is missing. Use the existence of AS to infer Function
                GetNextToken()

                Dim returnAttributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax) = Nothing
                If Me.CurrentToken.Kind = Global.Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.LessThanToken Then
                    returnAttributes = ParseAttributeLists(False)
                End If

                Dim returnType = ParseGeneralType()

                If returnType.ContainsDiagnostics Then
                    ' Sync at EOS to avoid any more errors.
                    returnType = ResyncAt(returnType)
                End If

                optionalAsClause = SyntaxFactory.SimpleAsClause(asKeyword, returnAttributes, returnType)
            End If

            Dim statement = SyntaxFactory.DeclareStatement(externalKind,
                                                             attributes,
                                                             modifiers,
                                                             declareKeyword,
                                                             optionalCharSet,
                                                             methodKeyword,
                                                             name,
                                                             libKeyword,
                                                             libraryName,
                                                             optionalAliasKeyword,
                                                             optionalAliasName,
                                                             optionalParameters,
                                                             optionalAsClause)

            Return statement

        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseDeclareLibClause
        ' *
        ' * Purpose:
        ' *
        ' **********************************************************************/

        ' [out] string literal representing Lib clause
        ' [out] string literal representing Alias clause

        ' File:Parser.cpp
        ' Lines: 10707 - 10707
        ' .Parser::ParseDeclareLibClause( [ _Deref_out_ ParseTree::Expression** LibResult ] [ _Deref_out_ ParseTree::Expression** AliasResult ] [ _Deref_out_ Token*& Lib ] [ _Deref_out_ Token*& Alias ] [ _Inout_ bool& ErrorInConstruct ] )

        Private Sub ParseDeclareLibClause(
                ByRef libKeyword As KeywordSyntax,
                ByRef libraryName As LiteralExpressionSyntax,
                ByRef optionalAliasKeyword As KeywordSyntax,
                ByRef optionalAliasName As LiteralExpressionSyntax
            )

            ' Syntax: LIB StringLiteral [ALIAS StringLiteral]

            libKeyword = Nothing
            optionalAliasKeyword = Nothing

            If VerifyExpectedToken(SyntaxKind.LibKeyword, libKeyword) Then

                libraryName = ParseStringLiteral()

                If libraryName.ContainsDiagnostics Then
                    libraryName = ResyncAt(libraryName, SyntaxKind.AliasKeyword, SyntaxKind.OpenParenToken)
                End If

            Else
                libraryName = SyntaxFactory.StringLiteralExpression(InternalSyntaxFactory.MissingStringLiteral())
            End If

            If CurrentToken.Kind = SyntaxKind.AliasKeyword Then
                optionalAliasKeyword = DirectCast(CurrentToken, KeywordSyntax)
                GetNextToken()

                optionalAliasName = ParseStringLiteral()

                If optionalAliasName.ContainsDiagnostics Then
                    optionalAliasName = ResyncAt(optionalAliasName, SyntaxKind.OpenParenToken)
                End If
            End If
        End Sub

        ''' <summary>
        ''' Parse a CustomEventMemberDeclaration
        ''' </summary>
        ''' <param name="attributes"></param>
        ''' <param name="modifiers"></param>
        ''' <returns></returns>
        ''' <remarks>This code used to be in ParseEventDefinition.</remarks>
        Private Function ParseCustomEventDefinition(
                attributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax),
                modifiers As CoreInternalSyntax.SyntaxList(Of KeywordSyntax)
        ) As StatementSyntax

            Debug.Assert(CurrentToken.Kind = SyntaxKind.IdentifierToken AndAlso DirectCast(CurrentToken, IdentifierTokenSyntax).PossibleKeywordKind = SyntaxKind.CustomKeyword, "ParseCustomEventDefinition called on the wrong token.")

            ' This enables better error reporting for invalid uses of CUSTOM as a specifier.
            '
            ' But note that at the same time, CUSTOM used as a variable name etc. should
            ' continue to work. See Bug VSWhidbey 379914.
            '
            ' Even though CUSTOM is not a reserved keyword, the Dev10 scanner always converts a CUSTOM followed
            ' by EVENT to a keyword. As a result CUSTOM EVENT never comes here because the tokens are tkCustom, tkEvent. 
            ' With the new scanner CUSTOM is returned as an identifier so the following must check for EVENT and not
            ' signal an error.

            Dim optionalCustomKeyword As KeywordSyntax = Nothing
            Dim nextToken = PeekToken(1)

            ' Only signal an error if the next token is not the EVENT keyword.
            If nextToken.Kind <> SyntaxKind.EventKeyword Then
                Return ParseVarDeclStatement(attributes, modifiers)

            Else
                optionalCustomKeyword = _scanner.MakeKeyword(DirectCast(CurrentToken, IdentifierTokenSyntax))
                GetNextToken()

            End If

            Dim eventKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)
            GetNextToken()

            Dim ident As IdentifierTokenSyntax = ParseIdentifier()

            Dim asKeyword As KeywordSyntax = Nothing
            Dim ReturnType As TypeSyntax = Nothing
            Dim optionalAsClause As SimpleAsClauseSyntax = Nothing

            If CurrentToken.Kind = SyntaxKind.AsKeyword Then
                asKeyword = DirectCast(CurrentToken, KeywordSyntax)
                GetNextToken()

                ReturnType = ParseGeneralType()

                If ReturnType.ContainsDiagnostics Then
                    ReturnType = ResyncAt(ReturnType)
                End If

                optionalAsClause = SyntaxFactory.SimpleAsClause(asKeyword, Nothing, ReturnType)
            Else
                Dim genericParams As TypeParameterListSyntax = Nothing
                If TryRejectGenericParametersForMemberDecl(genericParams) Then
                    ident = ident.AddTrailingSyntax(genericParams)
                End If

                If CurrentToken.Kind = SyntaxKind.OpenParenToken Then
                    Dim openParen As PunctuationSyntax = Nothing
                    Dim parameters As CoreInternalSyntax.SeparatedSyntaxList(Of ParameterSyntax)
                    Dim closeParen As PunctuationSyntax = Nothing

                    parameters = ParseParameters(openParen, closeParen)
                    ident = ident.AddTrailingSyntax(SyntaxFactory.ParameterList(openParen, parameters, closeParen))
                End If

                ' Give a good error if they attempt to do a return type

                If CurrentToken.Kind = SyntaxKind.AsKeyword Then
                    asKeyword = DirectCast(CurrentToken, KeywordSyntax)
                    asKeyword = ReportSyntaxError(asKeyword, ERRID.ERR_EventsCantBeFunctions)
                    GetNextToken()
                    asKeyword = asKeyword.AddTrailingSyntax(ResyncAt({SyntaxKind.ImplementsKeyword}))
                Else
                    asKeyword = InternalSyntaxFactory.MissingKeyword(SyntaxKind.AsKeyword)
                End If

                optionalAsClause = SyntaxFactory.SimpleAsClause(asKeyword, Nothing, SyntaxFactory.IdentifierName(InternalSyntaxFactory.MissingIdentifier()))

            End If

            Dim optionalImplementsClause As ImplementsClauseSyntax = Nothing

            If CurrentToken.Kind = SyntaxKind.ImplementsKeyword Then
                optionalImplementsClause = ParseImplementsList()
            End If

            ' Build a block event if all the requirements for one are met.
            '
            'Create the Event statement.
            Dim eventStatement = SyntaxFactory.EventStatement(attributes, modifiers, optionalCustomKeyword, eventKeyword, ident, Nothing, optionalAsClause, optionalImplementsClause)

            Return eventStatement
        End Function

        ' /*********************************************************************
        ' *
        ' * Function:
        ' *     Parser::ParseEventDefinition
        ' *
        ' * Purpose:
        ' *
        ' **********************************************************************/

        ' File:Parser.cpp
        ' Lines: 10779 - 10779
        ' Statement* .Parser::ParseEventDefinition( [ ParseTree::AttributeSpecifierList* Attributes ] [ ParseTree::SpecifierList* Specifiers ] [ _In_ Token* StatementStart ] [ _Inout_ bool& ErrorInConstruct ] )

        Private Function ParseEventDefinition(
                attributes As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax),
                modifiers As CoreInternalSyntax.SyntaxList(Of KeywordSyntax)
            ) As EventStatementSyntax

            Debug.Assert(CurrentToken.Kind = SyntaxKind.EventKeyword, "ParseEventDefinition called on the wrong token.")

            Dim eventKeyword As KeywordSyntax = DirectCast(CurrentToken, KeywordSyntax)

            GetNextToken()

            Dim ident As IdentifierTokenSyntax = ParseIdentifier()

            Dim optionalParameters As ParameterListSyntax = Nothing
            Dim openParen As PunctuationSyntax = Nothing
            Dim parameters As CoreInternalSyntax.SeparatedSyntaxList(Of ParameterSyntax) = Nothing
            Dim closeParen As PunctuationSyntax = Nothing

            Dim asKeyword As KeywordSyntax = Nothing
            Dim returnType As TypeSyntax = Nothing
            Dim optionalAsClause As SimpleAsClauseSyntax = Nothing

            If CurrentToken.Kind = SyntaxKind.AsKeyword Then
                asKeyword = DirectCast(CurrentToken, KeywordSyntax)
                GetNextToken()

                returnType = ParseGeneralType()

                If returnType.ContainsDiagnostics Then
                    returnType = ResyncAt(returnType)
                End If

                optionalAsClause = SyntaxFactory.SimpleAsClause(asKeyword, Nothing, returnType)
            Else

                Dim genericParams As TypeParameterListSyntax = Nothing
                If TryRejectGenericParametersForMemberDecl(genericParams) Then
                    ident = ident.AddTrailingSyntax(genericParams)
                End If

                If CurrentToken.Kind = SyntaxKind.OpenParenToken Then
                    parameters = ParseParameters(openParen, closeParen)
                End If

                ' Give a good error if they attempt to do a return type

                If CurrentToken.Kind = SyntaxKind.AsKeyword Then
                    If closeParen IsNot Nothing Then
                        closeParen = closeParen.AddTrailingSyntax(ResyncAt({SyntaxKind.ImplementsKeyword}), ERRID.ERR_EventsCantBeFunctions)
                    Else
                        ident = ident.AddTrailingSyntax(ResyncAt({SyntaxKind.ImplementsKeyword}), ERRID.ERR_EventsCantBeFunctions)
                    End If
                End If

            End If

            If openParen IsNot Nothing Then
                optionalParameters = SyntaxFactory.ParameterList(openParen, parameters, closeParen)
            End If

            Dim optionalImplementsClause As ImplementsClauseSyntax = Nothing

            If CurrentToken.Kind = SyntaxKind.ImplementsKeyword Then
                optionalImplementsClause = ParseImplementsList()
            End If

            ' Build a block event if all the requirements for one are met.
            '
            'Create the Event statement.
            Dim eventStatement = SyntaxFactory.EventStatement(attributes, modifiers, Nothing, eventKeyword, ident, optionalParameters, optionalAsClause, optionalImplementsClause)

            Return eventStatement
        End Function

        Private Function ParseEmptyAttributeLists() As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax)
            Debug.Assert(CurrentToken.Kind = SyntaxKind.LessThanGreaterThanToken)

            Dim token = CurrentToken
            Dim tokenText = token.Text
            Dim tokenLength = token.Text.Length

            Debug.Assert(tokenLength >= 2)

            Dim lessThanText = tokenText.Substring(0, 1)
            Dim greaterThanText = tokenText.Substring(tokenLength - 1, 1)
            Dim separatorTrivia = If(tokenLength > 2, _scanner.MakeWhiteSpaceTrivia(tokenText.Substring(1, tokenLength - 2)), Nothing)

            Debug.Assert(lessThanText = "<" OrElse lessThanText = SyntaxFacts.FULLWIDTH_LESS_THAN_SIGN_STRING)
            Debug.Assert(greaterThanText = ">" OrElse greaterThanText = SyntaxFacts.FULLWIDTH_GREATER_THAN_SIGN_STRING)

            Dim lessThan = _scanner.MakePunctuationToken(
                SyntaxKind.LessThanToken,
                lessThanText,
                token.GetLeadingTrivia(),
                separatorTrivia)
            Dim greaterThan = _scanner.MakePunctuationToken(
                SyntaxKind.GreaterThanToken,
                greaterThanText,
                Nothing,
                token.GetTrailingTrivia())

            GetNextToken()

            Dim attributeBlocks = _pool.Allocate(Of AttributeListSyntax)()
            Dim attributes = _pool.AllocateSeparated(Of AttributeSyntax)()

            Dim typeName = SyntaxFactory.IdentifierName(ReportSyntaxError(InternalSyntaxFactory.MissingIdentifier(), ERRID.ERR_ExpectedIdentifier))
            Dim attribute = SyntaxFactory.Attribute(
                Nothing,
                typeName,
                Nothing)

            attributes.Add(attribute)
            attributeBlocks.Add(SyntaxFactory.AttributeList(lessThan, attributes.ToList(), greaterThan))
            Dim result = attributeBlocks.ToList()
            _pool.Free(attributes)
            _pool.Free(attributeBlocks)
            Return result
        End Function

        ' File:Parser.cpp
        ' Lines: 10927 - 10927
        ' AttributeSpecifierList* .Parser::ParseAttributeSpecifier( [ ExpectedAttributeKind Expected ] [ _Inout_ bool& ErrorInConstruct ] )

        ' TODO: this function is so complex (n^2 loop?) it times out in CC verifier.
        Private Function ParseAttributeLists(allowFileLevelAttributes As Boolean) As CoreInternalSyntax.SyntaxList(Of AttributeListSyntax)
            Debug.Assert(CurrentToken.Kind = SyntaxKind.LessThanToken, "ParseAttributeSpecifier called on the wrong token.")

            Dim attributeBlocks = _pool.Allocate(Of AttributeListSyntax)()
            Dim attributes = _pool.AllocateSeparated(Of AttributeSyntax)()

            Do
                Dim lessThan As PunctuationSyntax = Nothing
                ' Eat a new line following "<"
                TryGetTokenAndEatNewLine(SyntaxKind.LessThanToken, lessThan)

                Do
                    Dim optionalTarget As AttributeTargetSyntax = Nothing
                    Dim arguments As ArgumentListSyntax = Nothing

                    If allowFileLevelAttributes Then
                        Dim assemblyOrModuleKeyword = GetTokenAsAssemblyOrModuleKeyword(CurrentToken)
                        Dim colonToken As PunctuationSyntax

                        ' The attributes are parsed in a loop. If an attribute starts with an Attribute Target, then it's 
                        ' assumed that all the others also start with one.
                        ' Error example (missing attribute target for second attribute:
                        ' <Assembly: Reflection.AssemblyVersionAttribute("4.3.2.1"), Reflection.AssemblyCultureAttribute("de")>

                        ' if the keyword is not Module or Assembly.
                        If assemblyOrModuleKeyword Is Nothing Then
                            ' the attribute targets can be mixed, so there's no way of determining which one to take.
                            ' therefore we're hard coding the missing target to be the assembly keyword.
                            assemblyOrModuleKeyword = InternalSyntaxFactory.MissingKeyword(SyntaxKind.AssemblyKeyword)
                            assemblyOrModuleKeyword = ReportSyntaxError(assemblyOrModuleKeyword, ERRID.ERR_FileAttributeNotAssemblyOrModule)
                            colonToken = InternalSyntaxFactory.MissingPunctuation(SyntaxKind.ColonToken)

                        Else
                            GetNextToken(ScannerState.VB)

                            If CurrentToken.Kind = SyntaxKind.ColonToken Then
                                ' The colon will have been attached as trailing trivia on the Assembly
                                ' or Module keyword, and the colon token will be zero-width.
                                ' Drop the colon trivia from the target token and rescan the colon as a token.
                                Dim previous As SyntaxToken = Nothing
                                Dim current As SyntaxToken = Nothing
                                RescanTrailingColonAsToken(previous, current)
                                GetNextToken(ScannerState.VB)

                                assemblyOrModuleKeyword = GetTokenAsAssemblyOrModuleKeyword(previous)
                                Debug.Assert(assemblyOrModuleKeyword IsNot Nothing)
                                Debug.Assert(current.Kind = SyntaxKind.ColonToken)
                                colonToken = DirectCast(current, PunctuationSyntax)

                            Else
                                colonToken = DirectCast(HandleUnexpectedToken(SyntaxKind.ColonToken), PunctuationSyntax)

                            End If

                        End If

                        optionalTarget = SyntaxFactory.AttributeTarget(assemblyOrModuleKeyword, colonToken)
                    End If

                    ' Make sure the scanner is back in normal VB state
                    ResetCurrentToken(ScannerState.VB)

                    Dim typeName = ParseName(
                        requireQualification:=False,
                        allowGlobalNameSpace:=True,
                        allowGenericArguments:=False,
                        allowGenericsWithoutOf:=True)

                    If BeginsGeneric() Then
                        ' Don't want to mark the construct after the attribute bad, so pass in
                        ' temporary instead of ErrorInThisAttribute

                        typeName = ReportSyntaxError(typeName, ERRID.ERR_GenericArgsOnAttributeSpecifier)

                        ' Resyncing to something more meaningful is hard, so just resync to ">"
                        typeName = ResyncAt(typeName, SyntaxKind.GreaterThanToken)

                    ElseIf CurrentToken.Kind = SyntaxKind.OpenParenToken Then
                        arguments = ParseParenthesizedArguments(attributeListParent:=True)
                    End If

                    Dim attribute As AttributeSyntax = SyntaxFactory.Attribute(optionalTarget, typeName, arguments)

                    attributes.Add(attribute)

                    Dim comma As PunctuationSyntax = Nothing
                    If Not TryGetTokenAndEatNewLine(SyntaxKind.CommaToken, comma) Then
                        Exit Do
                    End If

                    attributes.AddSeparator(comma)
                Loop

                ResetCurrentToken(ScannerState.VB)

                'Deleted the pTokenToUseForEndLocation comment and code.  The parser no longer handles position information.

                Dim greaterThan As PunctuationSyntax = Nothing
                Dim endsWithGreaterThan As Boolean = TryEatNewLineAndGetToken(SyntaxKind.GreaterThanToken, greaterThan, createIfMissing:=True)

                If endsWithGreaterThan AndAlso Not allowFileLevelAttributes AndAlso IsContinuableEOL() Then
                    ' We want to introduce an implicit line continuation after the ending ">" in an attribute declaration when we are parsing 
                    ' non file level attributes. Per TWhitney - implicit line continuations after file level attributes cause big problems. But
                    ' why would anyone want an implicit line continuation after a file level attribute?  It is a statement and should end shouldn't it?

                    TryEatNewLine()
                End If

                attributeBlocks.Add(SyntaxFactory.AttributeList(lessThan, attributes.ToList, greaterThan))
                attributes.Clear()

            Loop While CurrentToken.Kind = SyntaxKind.LessThanToken

            Dim result = attributeBlocks.ToList
            _pool.Free(attributes)
            _pool.Free(attributeBlocks)

            Return result
        End Function

        Private Function GetTokenAsAssemblyOrModuleKeyword(token As SyntaxToken) As KeywordSyntax
            If token.Kind = SyntaxKind.ModuleKeyword Then
                Return DirectCast(token, KeywordSyntax)
            End If

            Dim keyword As KeywordSyntax = Nothing
            TryTokenAsContextualKeyword(token, SyntaxKind.AssemblyKeyword, keyword)
            Return keyword
        End Function

        ' File:Parser.cpp
        ' Lines: 486 - 486
        ' Opcodes .::GetBinaryOperatorHelper( [ _In_ Token* T ] )

        Friend Shared Function GetBinaryOperatorHelper(t As SyntaxToken) As SyntaxKind
            Debug.Assert(t IsNot Nothing)
            Return SyntaxFacts.GetBinaryExpression(t.Kind)
        End Function

        ' File:Parser.cpp
        ' Lines: 19755 - 19755
        ' bool .Parser::StartsValidConditionalCompilationExpr( [ _In_ Token* T ] )

        Private Shared Function StartsValidConditionalCompilationExpr(t As SyntaxToken) As Boolean
            Select Case (t.Kind)
                ' Identifiers - note that only simple identifiers are allowed.
                ' This check is done in ParseTerm.

                ' Parenthesized expressions

                ' Literals

                ' Conversion operators

                ' Unary operators

                ' Allow "EOL" to start CC expressions to enable better error reporting.

                Case SyntaxKind.IdentifierToken,
                    SyntaxKind.OpenParenToken,
                    SyntaxKind.IntegerLiteralToken,
                    SyntaxKind.CharacterLiteralToken,
                    SyntaxKind.DateLiteralToken,
                    SyntaxKind.FloatingLiteralToken,
                    SyntaxKind.DecimalLiteralToken,
                    SyntaxKind.StringLiteralToken,
                    SyntaxKind.TrueKeyword,
                    SyntaxKind.FalseKeyword,
                    SyntaxKind.NothingKeyword,
                    SyntaxKind.CBoolKeyword,
                    SyntaxKind.CDateKeyword,
                    SyntaxKind.CDblKeyword,
                    SyntaxKind.CSByteKeyword,
                    SyntaxKind.CByteKeyword,
                    SyntaxKind.CCharKeyword,
                    SyntaxKind.CShortKeyword,
                    SyntaxKind.CUShortKeyword,
                    SyntaxKind.CIntKeyword,
                    SyntaxKind.CUIntKeyword,
                    SyntaxKind.CLngKeyword,
                    SyntaxKind.CULngKeyword,
                    SyntaxKind.CSngKeyword,
                    SyntaxKind.CStrKeyword,
                    SyntaxKind.CDecKeyword,
                    SyntaxKind.CObjKeyword,
                    SyntaxKind.CTypeKeyword,
                    SyntaxKind.IfKeyword,
                    SyntaxKind.DirectCastKeyword,
                    SyntaxKind.TryCastKeyword,
                    SyntaxKind.NotKeyword,
                    SyntaxKind.PlusToken,
                    SyntaxKind.MinusToken,
                    SyntaxKind.StatementTerminatorToken
                    Return True
            End Select

            Return False
        End Function

        ' File:Parser.cpp
        ' Lines: 19816 - 19816
        ' bool .Parser::IsValidOperatorForConditionalCompilationExpr( [ _In_ Token* T ] )

        Private Shared Function IsValidOperatorForConditionalCompilationExpr(t As SyntaxToken) As Boolean
            Select Case (t.Kind)

                Case SyntaxKind.NotKeyword,
                    SyntaxKind.AndKeyword,
                    SyntaxKind.AndAlsoKeyword,
                    SyntaxKind.OrKeyword,
                    SyntaxKind.OrElseKeyword,
                    SyntaxKind.XorKeyword,
                    SyntaxKind.AsteriskToken,
                    SyntaxKind.PlusToken,
                    SyntaxKind.MinusToken,
                    SyntaxKind.SlashToken,
                    SyntaxKind.BackslashToken,
                    SyntaxKind.ModKeyword,
                    SyntaxKind.CaretToken,
                    SyntaxKind.LessThanToken,
                    SyntaxKind.LessThanEqualsToken,
                    SyntaxKind.LessThanGreaterThanToken,
                    SyntaxKind.EqualsToken,
                    SyntaxKind.GreaterThanToken,
                    SyntaxKind.GreaterThanEqualsToken,
                    SyntaxKind.LessThanLessThanToken,
                    SyntaxKind.GreaterThanGreaterThanToken,
                    SyntaxKind.AmpersandToken

                    Return True
            End Select

            Return False
        End Function

        Friend ReadOnly Property Context As BlockContext
            Get
                Return _context
            End Get
        End Property

        Friend ReadOnly Property SyntaxFactory As ContextAwareSyntaxFactory
            Get
                Return _syntaxFactory
            End Get
        End Property

        Friend Function IsFirstStatementOnLine(node As VisualBasicSyntaxNode) As Boolean
            If _possibleFirstStatementOnLine = PossibleFirstStatementKind.No Then
                Return False
            End If

            If node.HasLeadingTrivia Then
                Dim triviaList = New CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(node.GetLeadingTrivia)

                For triviaIndex = triviaList.Count - 1 To 0 Step -1
                    Dim kind = triviaList(triviaIndex).Kind

                    Select Case kind
                        Case SyntaxKind.EndOfLineTrivia,
                            SyntaxKind.DocumentationCommentTrivia,
                            SyntaxKind.IfDirectiveTrivia,
                            SyntaxKind.ElseIfDirectiveTrivia,
                            SyntaxKind.ElseDirectiveTrivia,
                            SyntaxKind.EndIfDirectiveTrivia,
                            SyntaxKind.RegionDirectiveTrivia,
                            SyntaxKind.EndRegionDirectiveTrivia,
                            SyntaxKind.ConstDirectiveTrivia,
                            SyntaxKind.ExternalSourceDirectiveTrivia,
                            SyntaxKind.EndExternalSourceDirectiveTrivia,
                            SyntaxKind.ExternalChecksumDirectiveTrivia,
                            SyntaxKind.EnableWarningDirectiveTrivia,
                            SyntaxKind.DisableWarningDirectiveTrivia,
                            SyntaxKind.ReferenceDirectiveTrivia,
                            SyntaxKind.BadDirectiveTrivia
                            Return True

                        Case Else
                            If kind <> SyntaxKind.WhitespaceTrivia AndAlso kind <> SyntaxKind.LineContinuationTrivia Then
                                Return False
                            End If
                    End Select
                Next
            End If

            Return _possibleFirstStatementOnLine = PossibleFirstStatementKind.Yes
        End Function

        Friend Function ConsumeStatementTerminatorAfterDirective(ByRef stmt As DirectiveTriviaSyntax) As DirectiveTriviaSyntax
            If CurrentToken.Kind = SyntaxKind.StatementTerminatorToken AndAlso
                Not CurrentToken.HasLeadingTrivia Then

                GetNextToken()
            Else
                Dim unexpected = ResyncAndConsumeStatementTerminator()

                If unexpected.Node IsNot Nothing Then
                    If stmt.Kind <> SyntaxKind.BadDirectiveTrivia Then
                        stmt = stmt.AddTrailingSyntax(unexpected, ERRID.ERR_ExpectedEOS)
                    Else
                        ' Don't report ERRID_ExpectedEOS when the statement is known to be bad
                        stmt = stmt.AddTrailingSyntax(unexpected)
                    End If
                End If
            End If

            Return stmt
        End Function

        Friend Sub ConsumedStatementTerminator(allowLeadingMultilineTrivia As Boolean)
            ConsumedStatementTerminator(allowLeadingMultilineTrivia, If(allowLeadingMultilineTrivia, PossibleFirstStatementKind.Yes, PossibleFirstStatementKind.No))
        End Sub

        Private Sub ConsumedStatementTerminator(allowLeadingMultilineTrivia As Boolean, possibleFirstStatementOnLine As PossibleFirstStatementKind)
            Debug.Assert(allowLeadingMultilineTrivia = (possibleFirstStatementOnLine <> PossibleFirstStatementKind.No))
            _allowLeadingMultilineTrivia = allowLeadingMultilineTrivia
            _possibleFirstStatementOnLine = possibleFirstStatementOnLine
        End Sub

        Friend Sub ConsumeColonInSingleLineExpression()
            Debug.Assert(CurrentToken.Kind = SyntaxKind.ColonToken)
            ConsumedStatementTerminator(allowLeadingMultilineTrivia:=False)
            GetNextToken()
        End Sub

        Friend Sub ConsumeStatementTerminator(colonAsSeparator As Boolean)
            ' CurrentToken may be EmptyToken if there is extra trivia at EOF.
            Debug.Assert(SyntaxFacts.IsTerminator(CurrentToken.Kind) OrElse CurrentToken.Kind = SyntaxKind.EmptyToken)

            Select Case CurrentToken.Kind
                Case SyntaxKind.EndOfFileToken
                    ' Leave terminator as current token since we'll need the token
                    ' as is (with leading trivia) to add to the CompilationUnitSyntax.
                    ConsumedStatementTerminator(allowLeadingMultilineTrivia:=True)
                Case SyntaxKind.StatementTerminatorToken
                    ConsumedStatementTerminator(allowLeadingMultilineTrivia:=True)
                    GetNextToken()
                Case SyntaxKind.ColonToken
                    If colonAsSeparator Then
                        ConsumedStatementTerminator(allowLeadingMultilineTrivia:=False)
                        GetNextToken()
                    Else
                        ' If a colon token is recognized as a statement terminator token, the next non trivia token might be the first
                        ' token on a line if the trivia after the colon contains a line break.
                        ' If this flag is true the trivia gets checked more thoroughly in IsFirstStatementOnLine anyway later on.
                        ConsumedStatementTerminator(
                            allowLeadingMultilineTrivia:=True,
                            possibleFirstStatementOnLine:=PossibleFirstStatementKind.IfPrecededByLineBreak)
                        GetNextToken()
                    End If
            End Select
        End Sub

        Friend Function IsNextStatementInsideLambda(context As BlockContext, lambdaContext As BlockContext, allowLeadingMultilineTrivia As Boolean) As Boolean
            Debug.Assert(context.IsWithinLambda)
            Debug.Assert(SyntaxFacts.IsTerminator(CurrentToken.Kind))

            ' Ensure that scanner is set to scan a new statement
            _allowLeadingMultilineTrivia = allowLeadingMultilineTrivia

            ' Any End statement that closes a block outside of the lambda terminates the lambda

            ' Peek for an End, Next or Loop
            Dim peekedEndKind = PeekEndStatement(1)

            If peekedEndKind <> SyntaxKind.None Then
                Dim closedContext = context.FindNearest(Function(c) c.KindEndsBlock(peekedEndKind))
                If closedContext IsNot Nothing AndAlso closedContext.Level < lambdaContext.Level Then
                    ' End statement closes block containing lambda
                    Return False
                End If
            ElseIf PeekDeclarationStatement(1) Then
                ' A declaration closes the lambda
                Return False
            Else
                Dim nextToken = PeekToken(1)

                Select Case nextToken.Kind
                    Case SyntaxKind.LessThanToken
                        'This looks like the beginning of an attribute.  Assume this implies a declaration follows so close the lambda.
                        Return False

                    Case SyntaxKind.CatchKeyword,
                        SyntaxKind.FinallyKeyword
                        ' Check if catch/finally close a try containing the lambda
                        Dim closedContext = context.FindNearest(SyntaxKind.TryBlock, SyntaxKind.CatchBlock)
                        Return closedContext Is Nothing OrElse closedContext.Level >= lambdaContext.Level

                    Case SyntaxKind.ElseKeyword,
                        SyntaxKind.ElseIfKeyword
                        ' Check if else/elseif close an if containing the lambda
                        Dim closedContext = context.FindNearest(SyntaxKind.SingleLineIfStatement, SyntaxKind.MultiLineIfBlock)
                        Return closedContext Is Nothing OrElse closedContext.Level >= lambdaContext.Level
                End Select
            End If

            Return True
        End Function

        Private Function TryGetToken(Of T As SyntaxToken)(kind As SyntaxKind, ByRef token As T) As Boolean
            If CurrentToken.Kind = kind Then
                token = DirectCast(CurrentToken, T)
                GetNextToken()
                Return True
            End If

            Return False
        End Function

        Private Function TryGetContextualKeyword(
            kind As SyntaxKind,
            ByRef keyword As KeywordSyntax,
            Optional createIfMissing As Boolean = False) As Boolean

            If TryTokenAsContextualKeyword(CurrentToken, kind, keyword) Then
                GetNextToken()
                Return True
            End If

            If createIfMissing Then
                keyword = HandleUnexpectedKeyword(kind)
            End If
            Return False
        End Function

        ' This is for contextual keywords like "From"
        Private Function TryGetContextualKeywordAndEatNewLine(
            kind As SyntaxKind,
            ByRef keyword As KeywordSyntax,
            Optional createIfMissing As Boolean = False) As Boolean

            Dim result = TryGetContextualKeyword(kind, keyword, createIfMissing)
            If result Then
                TryEatNewLine()
            End If
            Return result
        End Function

        ' This is for contextual keywords like "From"
        Private Function TryEatNewLineAndGetContextualKeyword(
            kind As SyntaxKind,
            ByRef keyword As KeywordSyntax,
            Optional createIfMissing As Boolean = False) As Boolean

            If TryGetContextualKeyword(kind, keyword, createIfMissing) Then
                Return True
            End If

            If CurrentToken.Kind = SyntaxKind.StatementTerminatorToken AndAlso
                TryTokenAsContextualKeyword(PeekToken(1), kind, keyword) Then

                TryEatNewLine()
                GetNextToken()
                Return True
            End If

            If createIfMissing Then
                keyword = HandleUnexpectedKeyword(kind)
            End If
            Return False
        End Function

        Private Function TryGetTokenAndEatNewLine(Of T As SyntaxToken)(
            kind As SyntaxKind,
            ByRef token As T,
            Optional createIfMissing As Boolean = False,
            Optional state As ScannerState = ScannerState.VB) As Boolean

            Debug.Assert(CanUseInTryGetToken(kind))

            If CurrentToken.Kind = kind Then
                token = DirectCast(CurrentToken, T)
                GetNextToken(state)
                If CurrentToken.Kind = SyntaxKind.StatementTerminatorToken Then

                    TryEatNewLine(state)
                End If
                Return True
            End If

            If createIfMissing Then
                token = DirectCast(HandleUnexpectedToken(kind), T)
            End If
            Return False
        End Function

        Private Function TryEatNewLineAndGetToken(Of T As SyntaxToken)(
            kind As SyntaxKind,
            ByRef token As T,
            Optional createIfMissing As Boolean = False,
            Optional state As ScannerState = ScannerState.VB) As Boolean

            Debug.Assert(CanUseInTryGetToken(kind))

            If CurrentToken.Kind = kind Then
                token = DirectCast(CurrentToken, T)
                GetNextToken(state)
                Return True
            End If

            If TryEatNewLineIfFollowedBy(kind) Then
                token = DirectCast(CurrentToken, T)
                GetNextToken(state)
                Return True
            End If

            If createIfMissing Then
                token = DirectCast(HandleUnexpectedToken(kind), T)
            End If
            Return False
        End Function

        ''' <summary>
        ''' Peeks in a stream of VB tokens.
        ''' Note that the first token will be picked according to _allowLeadingMultilineTrivia
        ''' The rest will be picked as regular VB as scanner does not always know what to do with
        ''' line terminators and we assume that multiple token lookahead makes sense inside a single statement.
        ''' </summary>
        Private Function PeekToken(offset As Integer) As SyntaxToken
            Dim state = If(_allowLeadingMultilineTrivia, ScannerState.VBAllowLeadingMultilineTrivia, ScannerState.VB)
            Return _scanner.PeekToken(offset, state)
        End Function

        Friend Function PeekNextToken(Optional state As ScannerState = ScannerState.VB) As SyntaxToken
            If _allowLeadingMultilineTrivia AndAlso state = ScannerState.VB Then
                state = ScannerState.VBAllowLeadingMultilineTrivia
            End If
            Return _scanner.PeekNextToken(state)
        End Function

        Private ReadOnly Property PrevToken As SyntaxToken
            Get
                Return _scanner.PrevToken
            End Get
        End Property

        Private _currentToken As SyntaxToken
        Friend ReadOnly Property CurrentToken As SyntaxToken
            Get
                Dim tk = _currentToken
                If tk Is Nothing Then
                    tk = _scanner.GetCurrentToken

                    ' no more multiline trivia unless parser says so
                    _allowLeadingMultilineTrivia = False

                    _currentToken = tk
                End If
                Return tk
            End Get
        End Property

        Private Sub ResetCurrentToken(state As ScannerState)
            _scanner.ResetCurrentToken(state)
            _currentToken = Nothing
        End Sub

        ''' <summary>
        ''' Consumes current token and gets the next one with desired state.
        ''' </summary>
        Friend Sub GetNextToken(Optional state As ScannerState = ScannerState.VB)
            If _allowLeadingMultilineTrivia AndAlso state = ScannerState.VB Then
                state = ScannerState.VBAllowLeadingMultilineTrivia
            End If

            _scanner.GetNextTokenInState(state)
            _currentToken = Nothing
        End Sub

        ''' <summary>
        ''' Consumes current node and gets next one. 
        ''' </summary>
        Friend Sub GetNextSyntaxNode()
            _scanner.MoveToNextSyntaxNode()
            _currentToken = Nothing
        End Sub

        '============ Methods to test properties of NodeKind. ====================
        '

        ' IdentifierAsKeyword returns the token type of a identifier token,
        ' interpreting non-bracketed identifiers as (non-reserved) keywords as appropriate.

        Private Shared Function TryIdentifierAsContextualKeyword(id As SyntaxToken, ByRef kind As SyntaxKind) As Boolean
            Debug.Assert(id IsNot Nothing)
            Debug.Assert(DirectCast(id, IdentifierTokenSyntax) IsNot Nothing)

            Return Scanner.TryIdentifierAsContextualKeyword(DirectCast(id, IdentifierTokenSyntax), kind)
        End Function

        Private Function TryIdentifierAsContextualKeyword(id As SyntaxToken, ByRef k As KeywordSyntax) As Boolean
            Debug.Assert(id IsNot Nothing)
            Debug.Assert(DirectCast(id, IdentifierTokenSyntax) IsNot Nothing)

            Return _scanner.TryIdentifierAsContextualKeyword(DirectCast(id, IdentifierTokenSyntax), k)
        End Function

        Private Function TryTokenAsContextualKeyword(t As SyntaxToken, kind As SyntaxKind, ByRef k As KeywordSyntax) As Boolean
            Dim keyword As KeywordSyntax = Nothing
            If _scanner.TryTokenAsContextualKeyword(t, keyword) AndAlso keyword.Kind = kind Then
                k = keyword
                Return True
            Else
                Return False
            End If
        End Function

        Private Function TryTokenAsContextualKeyword(t As SyntaxToken, ByRef k As KeywordSyntax) As Boolean
            Return _scanner.TryTokenAsContextualKeyword(t, k)
        End Function

        Private Shared Function TryTokenAsKeyword(t As SyntaxToken, ByRef kind As SyntaxKind) As Boolean
            Return Scanner.TryTokenAsKeyword(t, kind)
        End Function

        Private Shared ReadOnly s_isTokenOrKeywordFunc As Func(Of SyntaxToken, SyntaxKind(), Boolean) = AddressOf IsTokenOrKeyword

        Private Shared Function IsTokenOrKeyword(token As SyntaxToken, kinds As SyntaxKind()) As Boolean
            Debug.Assert(Not kinds.Contains(SyntaxKind.IdentifierToken))
            If token.Kind = SyntaxKind.IdentifierToken Then
                Return Scanner.IsContextualKeyword(token, kinds)
            Else
                Return IsToken(token, kinds)
            End If
        End Function

        Private Shared Function IsToken(token As SyntaxToken, ParamArray kinds As SyntaxKind()) As Boolean
            Return kinds.Contains(token.Kind)
        End Function

        Friend Function ConsumeUnexpectedTokens(Of TNode As VisualBasicSyntaxNode)(node As TNode) As TNode
            If Me.CurrentToken.Kind = SyntaxKind.EndOfFileToken Then Return node
            Dim b As SyntaxListBuilder(Of SyntaxToken) = SyntaxListBuilder(Of SyntaxToken).Create()
            While (Me.CurrentToken.Kind <> SyntaxKind.EndOfFileToken)
                b.Add(Me.CurrentToken)
                GetNextToken()
            End While

            Return node.AddTrailingSyntax(b.ToList(), ERRID.ERR_Syntax)
        End Function

        ''' <summary>
        ''' Check to see if the given <paramref name="feature"/> is available with the <see cref="LanguageVersion"/>
        ''' of the parser.  If it is not available a diagnostic will be added to the returned value.
        ''' </summary>
        Private Function CheckFeatureAvailability(Of TNode As VisualBasicSyntaxNode)(feature As Feature, node As TNode) As TNode
            Return CheckFeatureAvailability(feature, node, _scanner.Options.LanguageVersion)
        End Function

        Friend Shared Function CheckFeatureAvailability(Of TNode As VisualBasicSyntaxNode)(feature As Feature, node As TNode, languageVersion As LanguageVersion) As TNode
            If CheckFeatureAvailability(languageVersion, feature) Then
                Return node
            End If

            Return ReportFeatureUnavailable(feature, node, languageVersion)
        End Function

        Private Shared Function ReportFeatureUnavailable(Of TNode As VisualBasicSyntaxNode)(feature As Feature, node As TNode, languageVersion As LanguageVersion) As TNode
            Dim featureName = ErrorFactory.ErrorInfo(feature.GetResourceId())
            Dim requiredVersion = New VisualBasicRequiredLanguageVersion(feature.GetLanguageVersion())
            Return ReportSyntaxError(node, ERRID.ERR_LanguageVersion, languageVersion.GetErrorName(), featureName, requiredVersion)
        End Function

        Friend Function ReportFeatureUnavailable(Of TNode As VisualBasicSyntaxNode)(feature As Feature, node As TNode) As TNode
            Return ReportFeatureUnavailable(feature, node, _scanner.Options.LanguageVersion)
        End Function

        Friend Function CheckFeatureAvailability(feature As Feature) As Boolean
            Return CheckFeatureAvailability(_scanner.Options.LanguageVersion, feature)
        End Function

        Friend Shared Function CheckFeatureAvailability(languageVersion As LanguageVersion, feature As Feature) As Boolean
            Dim required = feature.GetLanguageVersion()
            Return required <= languageVersion
        End Function

        ''' <summary>
        ''' Returns false and reports an error if the feature is un-available
        ''' </summary>
        Friend Shared Function CheckFeatureAvailability(diagnosticsOpt As DiagnosticBag, location As Location, languageVersion As LanguageVersion, feature As Feature) As Boolean
            If Not CheckFeatureAvailability(languageVersion, feature) Then
                If diagnosticsOpt IsNot Nothing Then
                    Dim featureName = ErrorFactory.ErrorInfo(feature.GetResourceId())
                    Dim requiredVersion = New VisualBasicRequiredLanguageVersion(feature.GetLanguageVersion())
                    diagnosticsOpt.Add(ERRID.ERR_LanguageVersion, location, languageVersion.GetErrorName(), featureName, requiredVersion)
                End If

                Return False
            End If
            Return True
        End Function

        Friend Shared Function CheckFeatureAvailability(diagnostics As BindingDiagnosticBag, location As Location, languageVersion As LanguageVersion, feature As Feature) As Boolean
            Return CheckFeatureAvailability(diagnostics.DiagnosticBag, location, languageVersion, feature)
        End Function

    End Class

    'TODO - These should be removed.  Checks should be in binding.
    <Flags()>
    Friend Enum ParameterSpecifiers
        [ByRef] = &H1
        [ByVal] = &H2
        [Optional] = &H4
        [ParamArray] = &H8
    End Enum

End Namespace
