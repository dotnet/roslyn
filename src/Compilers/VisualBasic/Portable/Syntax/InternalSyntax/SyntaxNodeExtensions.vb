' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------------------------------------
'
'  Extension methods to st leading/trailing trivia in syntax nodes.
'-----------------------------------------------------------------------------------------------------------

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax
Imports CoreInternalSyntax = Microsoft.CodeAnalysis.Syntax.InternalSyntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend Module SyntaxExtensions

        <Extension()>
        Public Function WithAnnotations(Of TNode As VisualBasicSyntaxNode)(node As TNode, ParamArray annotations() As SyntaxAnnotation) As TNode
            If annotations Is Nothing Then Throw New ArgumentNullException(NameOf(annotations))
            Return CType(node.SetAnnotations(annotations), TNode)
        End Function

        <Extension()>
        Public Function WithAdditionalAnnotations(Of TNode As VisualBasicSyntaxNode)(node As TNode, ParamArray annotations() As SyntaxAnnotation) As TNode
            If annotations Is Nothing Then Throw New ArgumentNullException(NameOf(annotations))
            Return CType(node.SetAnnotations(node.GetAnnotations().Concat(annotations).ToArray()), TNode)
        End Function

        <Extension()>
        Public Function WithoutAnnotations(Of TNode As VisualBasicSyntaxNode)(node As TNode, ParamArray removalAnnotations() As SyntaxAnnotation) As TNode
            Dim newAnnotations = ArrayBuilder(Of SyntaxAnnotation).GetInstance()
            Dim annotations = node.GetAnnotations()
            For Each candidate In annotations
                If Array.IndexOf(removalAnnotations, candidate) < 0 Then
                    newAnnotations.Add(candidate)
                End If
            Next
            Return CType(node.SetAnnotations(newAnnotations.ToArrayAndFree()), TNode)
        End Function

        <Extension()>
        Public Function WithAdditionalDiagnostics(Of TNode As GreenNode)(node As TNode, ParamArray diagnostics As DiagnosticInfo()) As TNode
            Dim current As DiagnosticInfo() = node.GetDiagnostics
            If current IsNot Nothing Then
                Return DirectCast(node.SetDiagnostics(current.Concat(diagnostics).ToArray()), TNode)
            Else
                Return node.WithDiagnostics(diagnostics)
            End If
        End Function

        <Extension()>
        Public Function WithDiagnostics(Of TNode As GreenNode)(node As TNode, ParamArray diagnostics As DiagnosticInfo()) As TNode
            Return DirectCast(node.SetDiagnostics(diagnostics), TNode)
        End Function

        <Extension()>
        Public Function WithoutDiagnostics(Of TNode As VisualBasicSyntaxNode)(node As TNode) As TNode
            Dim current As DiagnosticInfo() = node.GetDiagnostics
            If ((current Is Nothing) OrElse (current.Length = 0)) Then
                Return node
            End If
            Return DirectCast(node.SetDiagnostics(Nothing), TNode)
        End Function

        <Extension()>
        Public Function LastTriviaIfAny(node As VisualBasicSyntaxNode) As VisualBasicSyntaxNode
            Dim trailingTriviaNode = node.GetTrailingTrivia()
            If trailingTriviaNode Is Nothing Then
                Return Nothing
            End If
            Return New CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(trailingTriviaNode).Last
        End Function

        <Extension()>
        Public Function EndsWithEndOfLineOrColonTrivia(node As VisualBasicSyntaxNode) As Boolean
            Dim trailingTrivia = node.LastTriviaIfAny()
            Return trailingTrivia IsNot Nothing AndAlso
                (trailingTrivia.Kind = SyntaxKind.EndOfLineTrivia OrElse trailingTrivia.Kind = SyntaxKind.ColonTrivia)
        End Function
    End Module

    Friend Module SyntaxNodeExtensions

        Private Function IsMissingToken(token As SyntaxToken) As Boolean
            Return token.Width = 0 AndAlso token.Kind <> SyntaxKind.EmptyToken
        End Function

#Region "AddLeading"
        ' Add "trivia" as a leading trivia of node. If node is not a token, traverses down to the tree to add it to the first token.
        <Extension()>
        Private Function AddLeadingTrivia(Of TSyntax As VisualBasicSyntaxNode)(node As TSyntax, trivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As TSyntax
            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            If Not trivia.Any Then
                Return node
            End If

            Dim tk = TryCast(node, SyntaxToken)
            Dim result As TSyntax
            If tk IsNot Nothing Then
                ' Cannot add unexpected tokens as leading trivia on a missing token since
                ' if the unexpected tokens end with a statement terminator, the missing
                ' token would follow the statement terminator. That would result in an
                ' incorrect syntax tree and if this missing token is the end of an expression,
                ' and the expression represents a transition between VB and XML, the
                ' terminator will be overlooked (see ParseXmlEmbedded for instance).
                If IsMissingToken(tk) Then
                    Dim leadingTrivia = trivia.GetStartOfTrivia()
                    Dim trailingTrivia = trivia.GetEndOfTrivia()
                    tk = SyntaxToken.AddLeadingTrivia(tk, leadingTrivia).AddTrailingTrivia(trailingTrivia)
                Else
                    tk = SyntaxToken.AddLeadingTrivia(tk, trivia)
                End If

                result = DirectCast(CObj(tk), TSyntax)
            Else
                result = FirstTokenReplacer.Replace(node, Function(t) SyntaxToken.AddLeadingTrivia(t, trivia))
            End If

            'Debug.Assert(result.hasDiagnostics)

            Return result
        End Function

        ' Add "unexpected" as skipped leading trivia to "node". Leaves any diagnostics in place, and also adds a diagnostic with code "errorId"
        ' to the first token in the list.
        <Extension()>
        Friend Function AddLeadingSyntax(Of TSyntax As VisualBasicSyntaxNode)(node As TSyntax, unexpected As CoreInternalSyntax.SyntaxList(Of GreenNode), errorId As ERRID) As TSyntax
            Dim diagnostic = ErrorFactory.ErrorInfo(errorId)
            If unexpected.Node IsNot Nothing Then
                Dim trivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode) = CreateSkippedTrivia(unexpected.Node,
                                                                              preserveDiagnostics:=True,
                                                                              addDiagnosticToFirstTokenOnly:=True,
                                                                              addDiagnostic:=diagnostic)
                Return AddLeadingTrivia(node, trivia)
            Else
                Return DirectCast(node.AddError(diagnostic), TSyntax)
            End If
            Return node
        End Function

        ' Add "unexpected" as skipped leading trivia of "node". Removes all diagnostics from "unexpected", replacing them with 
        ' a new diagnostic with the given "errorId".
        <Extension()>
        Friend Function AddLeadingSyntax(Of TSyntax As VisualBasicSyntaxNode)(node As TSyntax, unexpected As SyntaxToken, errorId As ERRID) As TSyntax
            Return node.AddLeadingSyntax(DirectCast(unexpected, VisualBasicSyntaxNode), errorId)
        End Function

        ' Add "unexpected" as skipped leading trivia of "node". Leaves any diagnostics in place (possibly reattaching them to the created trivia node).
        <Extension()>
        Friend Function AddLeadingSyntax(Of TSyntax As VisualBasicSyntaxNode)(node As TSyntax, unexpected As CoreInternalSyntax.SyntaxList(Of SyntaxToken)) As TSyntax
            Return node.AddLeadingSyntax(unexpected.Node)
        End Function

        ' Add "unexpected" as skipped leading trivia of "node". Removes all diagnostics from "unexpected", replacing them with 
        ' a new diagnostic with the given "errorId".
        <Extension()>
        Friend Function AddLeadingSyntax(Of TSyntax As VisualBasicSyntaxNode)(node As TSyntax, unexpected As GreenNode, errorId As ERRID) As TSyntax
            Dim diagnostic = ErrorFactory.ErrorInfo(errorId)
            If unexpected IsNot Nothing Then
                Dim trivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode) = CreateSkippedTrivia(unexpected,
                                                                              preserveDiagnostics:=False,
                                                                              addDiagnosticToFirstTokenOnly:=False,
                                                                              addDiagnostic:=diagnostic)
                Return AddLeadingTrivia(node, trivia)
            Else
                Return DirectCast(node.AddError(diagnostic), TSyntax)
            End If
            Return node
        End Function

        ' Add "unexpected" as skipped leading trivia of "node". Leaves any diagnostics in place (possibly reattaching them to the created trivia node).
        <Extension()>
        Friend Function AddLeadingSyntax(Of TSyntax As VisualBasicSyntaxNode)(node As TSyntax, unexpected As GreenNode) As TSyntax
            If unexpected IsNot Nothing Then
                Dim trivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode) = CreateSkippedTrivia(unexpected,
                                                                              preserveDiagnostics:=True,
                                                                              addDiagnosticToFirstTokenOnly:=False,
                                                                              addDiagnostic:=Nothing)
                Return AddLeadingTrivia(node, trivia)
            End If
            Return node
        End Function

#End Region

#Region "AddTrailing"
        ' Add "trivia" as a trailing trivia of node. If node is not a token, traverses down to the tree to add it to the last token.
        <Extension()>
        Friend Function AddTrailingTrivia(Of TSyntax As GreenNode)(node As TSyntax, trivia As CoreInternalSyntax.SyntaxList(Of GreenNode)) As TSyntax
            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            Dim tk = TryCast(node, SyntaxToken)
            Dim result As TSyntax
            If tk IsNot Nothing Then
                result = DirectCast(CObj(SyntaxToken.AddTrailingTrivia(tk, trivia)), TSyntax)
            Else
                result = LastTokenReplacer.Replace(node, Function(t) SyntaxToken.AddTrailingTrivia(t, trivia))
            End If

            'Debug.Assert(result.ContainsDiagnostics)
            Return result
        End Function

        ' Add "unexpected" as skipped trailing trivia to "node". Leaves any diagnostics in place, and also adds a diagnostic with code "errorId"
        ' to the first token in the list.
        <Extension()>
        Friend Function AddTrailingSyntax(Of TSyntax As VisualBasicSyntaxNode)(node As TSyntax, unexpected As CoreInternalSyntax.SyntaxList(Of SyntaxToken), errorId As ERRID) As TSyntax
            Dim diagnostic = ErrorFactory.ErrorInfo(errorId)
            If unexpected.Node IsNot Nothing Then
                Dim trivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode) = CreateSkippedTrivia(unexpected.Node,
                                                                                  preserveDiagnostics:=True,
                                                                                  addDiagnosticToFirstTokenOnly:=True,
                                                                                  addDiagnostic:=diagnostic)
                Return AddTrailingTrivia(node, trivia)
            Else
                Return DirectCast(node.AddError(diagnostic), TSyntax)
            End If
            Return node
        End Function

        ' Add "unexpected" as skipped trailing trivia of "node". Removes all diagnostics from "unexpected", replacing them with 
        ' a new diagnostic with the given "errorId".
        <Extension()>
        Friend Function AddTrailingSyntax(Of TSyntax As VisualBasicSyntaxNode)(node As TSyntax, unexpected As SyntaxToken, errorId As ERRID) As TSyntax
            Return node.AddTrailingSyntax(DirectCast(unexpected, VisualBasicSyntaxNode), errorId)
        End Function

        ' Add "unexpected" as skipped trailing trivia of "node". Leaves any diagnostics in place (possibly reattaching them to the created trivia node).
        <Extension()>
        Friend Function AddTrailingSyntax(Of TSyntax As VisualBasicSyntaxNode)(node As TSyntax, unexpected As CoreInternalSyntax.SyntaxList(Of SyntaxToken)) As TSyntax
            Return node.AddTrailingSyntax(unexpected.Node)
        End Function

        ' Add "unexpected" as skipped trailing trivia of "node". Leaves any diagnostics in place (possibly reattaching them to the created trivia node).
        <Extension()>
        Friend Function AddTrailingSyntax(Of TSyntax As VisualBasicSyntaxNode)(node As TSyntax, unexpected As SyntaxToken) As TSyntax
            Return node.AddTrailingSyntax(DirectCast(unexpected, VisualBasicSyntaxNode))
        End Function

        ' Add "unexpected" as skipped trailing trivia of "node". Removes all diagnostics from "unexpected", replacing them with 
        ' a new diagnostic with the given "errorId".
        <Extension()>
        Friend Function AddTrailingSyntax(Of TSyntax As GreenNode)(node As TSyntax, unexpected As GreenNode, errorId As ERRID) As TSyntax
            Dim diagnostic = ErrorFactory.ErrorInfo(errorId)
            If unexpected IsNot Nothing Then
                Dim trivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode) = CreateSkippedTrivia(unexpected,
                                                               preserveDiagnostics:=False,
                                                               addDiagnosticToFirstTokenOnly:=False,
                                                               addDiagnostic:=diagnostic)
                Return AddTrailingTrivia(node, trivia)
            Else
                Return DirectCast(node.AddError(diagnostic), TSyntax)
            End If
            Return node
        End Function

        ' Add "unexpected" as skipped trailing trivia of "node". Leaves any diagnostics in place (possibly reattaching them to the created trivia node).
        <Extension()>
        Friend Function AddTrailingSyntax(Of TSyntax As GreenNode)(node As TSyntax, unexpected As GreenNode) As TSyntax
            If unexpected IsNot Nothing Then
                Dim trivia As CoreInternalSyntax.SyntaxList(Of GreenNode) = CreateSkippedTrivia(unexpected, preserveDiagnostics:=True, addDiagnosticToFirstTokenOnly:=False, addDiagnostic:=Nothing)
                Return AddTrailingTrivia(node, trivia)
            End If
            Return node
        End Function

        <Extension()>
        Friend Function AddError(Of TSyntax As VisualBasicSyntaxNode)(node As TSyntax, errorId As ERRID) As TSyntax
            Return DirectCast(node.AddError(ErrorFactory.ErrorInfo(errorId)), TSyntax)
        End Function

#End Region

        <Extension()>
        Friend Function GetStartOfTrivia(trivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)
            Return trivia.GetStartOfTrivia(trivia.GetIndexOfEndOfTrivia())
        End Function

        <Extension()>
        Friend Function GetStartOfTrivia(trivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), indexOfEnd As Integer) As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)
            If indexOfEnd = 0 Then
                Return Nothing
            ElseIf indexOfEnd = trivia.Count Then
                Return trivia
            Else
                Dim builder = SyntaxListBuilder(Of VisualBasicSyntaxNode).Create()
                For i = 0 To indexOfEnd - 1
                    builder.Add(trivia(i))
                Next
                Return builder.ToList()
            End If
        End Function

        <Extension()>
        Friend Function GetEndOfTrivia(trivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)
            Return trivia.GetEndOfTrivia(trivia.GetIndexOfEndOfTrivia())
        End Function

        <Extension()>
        Friend Function GetEndOfTrivia(trivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), indexOfEnd As Integer) As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)
            If indexOfEnd = 0 Then
                Return trivia
            ElseIf indexOfEnd = trivia.Count Then
                Return Nothing
            Else
                Dim builder = SyntaxListBuilder(Of VisualBasicSyntaxNode).Create()
                For i = indexOfEnd To trivia.Count - 1
                    builder.Add(trivia(i))
                Next
                Return builder.ToList()
            End If
        End Function

        ''' <summary>
        ''' Return the length of the common ending between the two
        ''' sets of trivia. The valid trivia (following skipped tokens)
        ''' of one must be contained in the valid trivia of the other. 
        ''' </summary>
        Friend Function GetLengthOfCommonEnd(trivia1 As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode), trivia2 As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As Integer
            Dim n1 = trivia1.Count
            Dim n2 = trivia2.Count
            Dim offset1 = trivia1.GetIndexAfterLastSkippedToken()
            Dim offset2 = trivia2.GetIndexAfterLastSkippedToken()
            Dim n = Math.Min(n1 - offset1, n2 - offset2)
#If DEBUG Then
            For i = 0 To n - 1
                Dim t1 = trivia1(i + n1 - n)
                Dim t2 = trivia2(i + n2 - n)
                Debug.Assert(t1.Kind = t2.Kind)
                Debug.Assert(t1.ToFullString() = t2.ToFullString())
            Next
#End If
            Return n
        End Function

        <Extension()>
        Private Function GetIndexAfterLastSkippedToken(trivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As Integer
            Dim n = trivia.Count
            For i = n - 1 To 0 Step -1
                If trivia(i).Kind = SyntaxKind.SkippedTokensTrivia Then
                    Return i + 1
                End If
            Next
            Return 0
        End Function

        ''' <summary>
        ''' Return the index within the trivia of what would be considered trailing
        ''' single-line trivia by the Scanner. This behavior must match ScanSingleLineTrivia.
        ''' In short, search walks backwards and stops at the second terminator
        ''' (colon or EOL) from the end, ignoring EOLs preceded by line continuations.
        ''' </summary>
        <Extension()>
        Private Function GetIndexOfEndOfTrivia(trivia As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)) As Integer
            Dim n = trivia.Count
            If n > 0 Then
                Dim i = n - 1
                Select Case trivia(i).Kind
                    Case SyntaxKind.ColonTrivia
                        Return i

                    Case SyntaxKind.EndOfLineTrivia
                        If i > 0 Then
                            Select Case trivia(i - 1).Kind
                                Case SyntaxKind.LineContinuationTrivia
                                    ' An EOL preceded by a line continuation should
                                    ' be considered whitespace rather than EOL.
                                    Return n
                                Case SyntaxKind.CommentTrivia
                                    Return i - 1
                                Case Else
                                    Return i
                            End Select
                        Else
                            Return i
                        End If

                    Case SyntaxKind.LineContinuationTrivia,
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

                        Throw ExceptionUtilities.UnexpectedValue(trivia(i).Kind)
                End Select
            End If
            Return n
        End Function

#Region "Skipped trivia creation"
        ' In order to handle creating SkippedTokens trivia correctly, we need to know if any structured
        ' trivia is present in a trivia list (because structured trivia can't contain structured trivia). 
        Private Function TriviaListContainsStructuredTrivia(triviaList As GreenNode) As Boolean
            If triviaList Is Nothing Then
                Return False
            End If

            Dim trivia = New CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(triviaList)

            For i = 0 To trivia.Count - 1
                Select Case trivia.ItemUntyped(i).RawKind
                    Case SyntaxKind.XmlDocument,
                        SyntaxKind.SkippedTokensTrivia,
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
                        SyntaxKind.ReferenceDirectiveTrivia,
                        SyntaxKind.EnableWarningDirectiveTrivia,
                        SyntaxKind.DisableWarningDirectiveTrivia,
                        SyntaxKind.BadDirectiveTrivia
                        Return True
                End Select
            Next

            Return False
        End Function

        ' Simple class to create the best representation of skipped trivia as a combination of "regular" trivia
        ' and SkippedNode trivia. The initial trivia and trailing trivia are preserved as regular trivia, as well
        ' as any structured trivia. We also remove any missing tokens and promote their trivia. Otherwise we try to put
        ' as many consecutive tokens as possible into a SkippedTokens trivia node.
        Private Class SkippedTriviaBuilder
            ' Maintain the list of trivia that we're accumulating.
#Disable Warning IDE0044 ' Add readonly modifier - Adding readonly generates compile error - see https://github.com/dotnet/roslyn/issues/47198
            Private _triviaListBuilder As SyntaxListBuilder(Of GreenNode) = SyntaxListBuilder(Of GreenNode).Create()
#Enable Warning IDE0044 ' Add readonly modifier

            ' Maintain a list of tokens we're accumulating to put into a SkippedNodes trivia.
            Private ReadOnly _skippedTokensBuilder As SyntaxListBuilder(Of SyntaxToken) = SyntaxListBuilder(Of SyntaxToken).Create()

            Private ReadOnly _preserveExistingDiagnostics As Boolean
            Private _addDiagnosticsToFirstTokenOnly As Boolean
            Private _diagnosticsToAdd As IEnumerable(Of DiagnosticInfo)

            ' Add a trivia to the trivia we are accumulating.
            Private Sub AddTrivia(trivia As GreenNode)
                FinishInProgressTokens()
                _triviaListBuilder.AddRange(trivia)
            End Sub

            ' Create a SkippedTokens trivia from any tokens currently accumulated into the skippedTokensBuilder. If not,
            ' don't do anything.
            Private Sub FinishInProgressTokens()
                If _skippedTokensBuilder.Count > 0 Then
                    Dim skippedTokensTrivia As GreenNode = SyntaxFactory.SkippedTokensTrivia(_skippedTokensBuilder.ToList())

                    If _diagnosticsToAdd IsNot Nothing Then
                        For Each d In _diagnosticsToAdd
                            skippedTokensTrivia = skippedTokensTrivia.AddError(d)
                        Next
                        _diagnosticsToAdd = Nothing ' only add once.
                    End If

                    _triviaListBuilder.Add(skippedTokensTrivia)

                    _skippedTokensBuilder.Clear()
                End If
            End Sub

            Public Sub New(preserveExistingDiagnostics As Boolean,
                           addDiagnosticsToFirstTokenOnly As Boolean,
                           diagnosticsToAdd As IEnumerable(Of DiagnosticInfo))
                Me._addDiagnosticsToFirstTokenOnly = addDiagnosticsToFirstTokenOnly
                Me._preserveExistingDiagnostics = preserveExistingDiagnostics
                Me._diagnosticsToAdd = diagnosticsToAdd
            End Sub

            ' Process a token. and add to the list of trivia/tokens we're accumulating.
            Public Sub AddToken(token As SyntaxToken, isFirst As Boolean, isLast As Boolean)
                Dim isMissing As Boolean = token.IsMissing

                If token.HasLeadingTrivia() AndAlso (isFirst OrElse isMissing OrElse TriviaListContainsStructuredTrivia(token.GetLeadingTrivia())) Then
                    FinishInProgressTokens()
                    AddTrivia(token.GetLeadingTrivia())
                    token = DirectCast(token.WithLeadingTrivia(Nothing), SyntaxToken)
                End If

                If Not _preserveExistingDiagnostics Then
                    token = token.WithoutDiagnostics()
                End If

                Dim trailingTrivia As GreenNode = Nothing

                If token.HasTrailingTrivia() AndAlso (isLast OrElse isMissing OrElse TriviaListContainsStructuredTrivia(token.GetTrailingTrivia())) Then
                    trailingTrivia = token.GetTrailingTrivia()
                    token = DirectCast(token.WithTrailingTrivia(Nothing), SyntaxToken)
                End If

                If isMissing Then
                    ' Don't add missing tokens to skipped tokens, but preserve their diagnostics.
                    If token.ContainsDiagnostics() Then
                        ' Move diagnostics on missing token to next token.
                        If _diagnosticsToAdd IsNot Nothing Then
                            _diagnosticsToAdd = _diagnosticsToAdd.Concat(token.GetDiagnostics())
                        Else
                            _diagnosticsToAdd = token.GetDiagnostics()
                        End If
                        _addDiagnosticsToFirstTokenOnly = True
                    End If
                Else
                    _skippedTokensBuilder.Add(token)
                End If

                If trailingTrivia IsNot Nothing Then
                    FinishInProgressTokens()
                    AddTrivia(trailingTrivia)
                End If

                If isFirst AndAlso _addDiagnosticsToFirstTokenOnly Then
                    FinishInProgressTokens() ' implicitly adds the diagnostics.
                End If
            End Sub

            ' Get the final list of trivia nodes we should attached.
            Public Function GetTriviaList() As CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)
                FinishInProgressTokens()
                If _diagnosticsToAdd IsNot Nothing AndAlso _diagnosticsToAdd.Any() Then
                    ' Still have diagnostics. Add to the last item.
                    If _triviaListBuilder.Count > 0 Then
                        _triviaListBuilder(_triviaListBuilder.Count - 1) = _triviaListBuilder(_triviaListBuilder.Count - 1).WithAdditionalDiagnostics(_diagnosticsToAdd.ToArray())
                    End If
                End If
                Return _triviaListBuilder.ToList()
            End Function
        End Class

        ' From a syntax node, create a list of trivia node that encapsulates the same text. We use SkippedTokens trivia
        ' to encapsulate the tokens, plus extract trivia from those tokens into the trivia list because:
        '    - We want leading trivia and trailing trivia to be directly visible in the trivia list, not on the tokens
        '      inside the skipped tokens trivia.
        '    - We have to expose structured trivia directives.
        '
        ' Several options controls how diagnostics are handled:
        '   "preserveDiagnostics" means existing diagnostics are preserved, otherwise they are thrown away
        '   "addDiagnostic", if not Nothing, is added as a diagnostics
        '   "addDiagnosticsToFirstTokenOnly" means that "addDiagnostics" is attached only to the first token, otherwise
        '    it is attached to all tokens.
        Private Function CreateSkippedTrivia(node As GreenNode,
                                             preserveDiagnostics As Boolean,
                                             addDiagnosticToFirstTokenOnly As Boolean,
                                             addDiagnostic As DiagnosticInfo) As CoreInternalSyntax.SyntaxList(Of GreenNode)
            If node.RawKind = SyntaxKind.SkippedTokensTrivia Then
                ' already skipped trivia
                If addDiagnostic IsNot Nothing Then
                    node = node.AddError(addDiagnostic)
                End If
                Return node
            End If

            ' Get the tokens and diagnostics.
            Dim diagnostics As IList(Of DiagnosticInfo) = New List(Of DiagnosticInfo)
            Dim tokenListBuilder = SyntaxListBuilder(Of SyntaxToken).Create

            CollectConstituentTokensAndDiagnostics(node, tokenListBuilder, diagnostics)

            ' Adjust diagnostics based on input.
            If Not preserveDiagnostics Then
                diagnostics.Clear()
            End If
            If addDiagnostic IsNot Nothing Then
                diagnostics.Add(addDiagnostic)
            End If

            Dim skippedTriviaBuilder As New SkippedTriviaBuilder(preserveDiagnostics, addDiagnosticToFirstTokenOnly, diagnostics)

            ' Get through each token and add it. 
            For i As Integer = 0 To tokenListBuilder.Count - 1
                Dim currentToken As SyntaxToken = tokenListBuilder(i)

                skippedTriviaBuilder.AddToken(currentToken, isFirst:=(i = 0), isLast:=(i = tokenListBuilder.Count - 1))
            Next

            Return skippedTriviaBuilder.GetTriviaList()
        End Function

        ''' <summary>
        ''' Add all the tokens in this node and children to the build token list builder. While doing this, add any
        ''' diagnostics not on tokens to the given diagnostic info list.
        ''' </summary>
        Friend Sub CollectConstituentTokensAndDiagnostics(
                this As GreenNode,
                tokenListBuilder As SyntaxListBuilder(Of SyntaxToken),
                nonTokenDiagnostics As IList(Of DiagnosticInfo))
            If this Is Nothing Then
                Return
            End If

            If this.IsToken Then
                tokenListBuilder.Add(DirectCast(this, SyntaxToken))
                Return
            End If

            ' Add diagnostics.
            Dim diagnostics As DiagnosticInfo() = this.GetDiagnostics()
            If diagnostics IsNot Nothing AndAlso diagnostics.Length > 0 Then
                For Each diag In diagnostics
                    nonTokenDiagnostics.Add(diag)
                Next
            End If

            ' Recurse to subtrees.
            For i = 0 To this.SlotCount() - 1
                Dim green = this.GetSlot(i)
                If green IsNot Nothing Then
                    CollectConstituentTokensAndDiagnostics(green, tokenListBuilder, nonTokenDiagnostics)
                End If
            Next
        End Sub

#End Region

#Region "Whitespace Containment"
        <Extension()>
        Friend Function ContainsWhitespaceTrivia(this As GreenNode) As Boolean
            If this Is Nothing Then
                Return False
            End If

            Dim trivia = New CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(this)

            For i = 0 To trivia.Count - 1
                Dim kind = trivia.ItemUntyped(i).RawKind
                If kind = SyntaxKind.WhitespaceTrivia OrElse
                    kind = SyntaxKind.EndOfLineTrivia Then

                    Return True
                End If
            Next

            Return False
        End Function
#End Region

        <Extension()>
        Friend Function ContainsCommentTrivia(this As GreenNode) As Boolean
            If this Is Nothing Then
                Return False
            End If

            Dim trivia = New CoreInternalSyntax.SyntaxList(Of VisualBasicSyntaxNode)(this)

            For i = 0 To trivia.Count - 1
                Dim kind = trivia.ItemUntyped(i).RawKind
                If kind = SyntaxKind.CommentTrivia Then
                    Return True
                End If
            Next

            Return False
        End Function

        ' This was Semantics::ExtractAnonTypeMemberName in Dev 10
        <Extension()>
        Friend Function ExtractAnonymousTypeMemberName(input As ExpressionSyntax,
                                           ByRef isNameDictionaryAccess As Boolean,
                                           ByRef isRejectedXmlName As Boolean) As SyntaxToken
            Dim conditionalAccessStack As ArrayBuilder(Of ConditionalAccessExpressionSyntax) = Nothing
            Dim result As SyntaxToken = ExtractAnonymousTypeMemberName(conditionalAccessStack, input, isNameDictionaryAccess, isRejectedXmlName)

            If conditionalAccessStack IsNot Nothing Then
                conditionalAccessStack.Free()
            End If

            Return result
        End Function

        <Extension()>
        Private Function ExtractAnonymousTypeMemberName(
            ByRef conditionalAccessStack As ArrayBuilder(Of ConditionalAccessExpressionSyntax),
            input As ExpressionSyntax,
            ByRef isNameDictionaryAccess As Boolean,
            ByRef isRejectedXmlName As Boolean
        ) As SyntaxToken
TryAgain:
            Select Case input.Kind
                Case SyntaxKind.IdentifierName
                    Return DirectCast(input, IdentifierNameSyntax).Identifier

                Case SyntaxKind.XmlName
                    Dim xmlNameInferredFrom = DirectCast(input, XmlNameSyntax)
                    If Not Scanner.IsIdentifier(xmlNameInferredFrom.LocalName.ToString) Then
                        isRejectedXmlName = True
                        Return Nothing
                    End If

                    Return xmlNameInferredFrom.LocalName

                Case SyntaxKind.XmlBracketedName
                    ' handles something like <a-a>
                    Dim xmlNameInferredFrom = DirectCast(input, XmlBracketedNameSyntax)
                    input = xmlNameInferredFrom.Name
                    GoTo TryAgain

                Case SyntaxKind.SimpleMemberAccessExpression,
                     SyntaxKind.DictionaryAccessExpression

                    Dim memberAccess = DirectCast(input, MemberAccessExpressionSyntax)
                    Dim receiver As ExpressionSyntax = If(memberAccess.Expression, PopAndGetConditionalAccessReceiver(conditionalAccessStack))

                    If input.Kind = SyntaxKind.SimpleMemberAccessExpression Then
                        ' See if this is an identifier qualified with XmlElementAccessExpression or XmlDescendantAccessExpression
                        If receiver IsNot Nothing Then
                            Select Case receiver.Kind
                                Case SyntaxKind.XmlElementAccessExpression,
                                    SyntaxKind.XmlDescendantAccessExpression

                                    input = receiver
                                    GoTo TryAgain
                            End Select
                        End If
                    End If

                    ClearConditionalAccessStack(conditionalAccessStack)

                    isNameDictionaryAccess = input.Kind = SyntaxKind.DictionaryAccessExpression
                    input = memberAccess.Name
                    GoTo TryAgain

                Case SyntaxKind.XmlElementAccessExpression,
                     SyntaxKind.XmlAttributeAccessExpression,
                     SyntaxKind.XmlDescendantAccessExpression

                    Dim xmlAccess = DirectCast(input, XmlMemberAccessExpressionSyntax)
                    ClearConditionalAccessStack(conditionalAccessStack)

                    input = xmlAccess.Name
                    GoTo TryAgain

                Case SyntaxKind.InvocationExpression
                    Dim invocation = DirectCast(input, InvocationExpressionSyntax)
                    Dim target As ExpressionSyntax = If(invocation.Expression, PopAndGetConditionalAccessReceiver(conditionalAccessStack))

                    If target Is Nothing Then
                        Exit Select
                    End If

                    If invocation.ArgumentList Is Nothing OrElse invocation.ArgumentList.Arguments.Count = 0 Then
                        input = target
                        GoTo TryAgain
                    End If

                    Debug.Assert(invocation.ArgumentList IsNot Nothing)

                    If invocation.ArgumentList.Arguments.Count = 1 Then
                        ' See if this is an indexed XmlElementAccessExpression or XmlDescendantAccessExpression
                        Select Case target.Kind
                            Case SyntaxKind.XmlElementAccessExpression,
                                SyntaxKind.XmlDescendantAccessExpression
                                input = target
                                GoTo TryAgain
                        End Select
                    End If

                Case SyntaxKind.ConditionalAccessExpression
                    Dim access = DirectCast(input, ConditionalAccessExpressionSyntax)

                    If conditionalAccessStack Is Nothing Then
                        conditionalAccessStack = ArrayBuilder(Of ConditionalAccessExpressionSyntax).GetInstance()
                    End If

                    conditionalAccessStack.Push(access)

                    input = access.WhenNotNull
                    GoTo TryAgain
            End Select

            Return Nothing
        End Function

        Private Sub ClearConditionalAccessStack(conditionalAccessStack As ArrayBuilder(Of ConditionalAccessExpressionSyntax))
            If conditionalAccessStack IsNot Nothing Then
                conditionalAccessStack.Clear()
            End If
        End Sub

        Private Function PopAndGetConditionalAccessReceiver(conditionalAccessStack As ArrayBuilder(Of ConditionalAccessExpressionSyntax)) As ExpressionSyntax
            If conditionalAccessStack Is Nothing OrElse conditionalAccessStack.Count = 0 Then
                Return Nothing
            End If

            Return conditionalAccessStack.Pop().Expression
        End Function

        Friend Function IsExecutableStatementOrItsPart(node As VisualBasicSyntaxNode) As Boolean
            If TypeOf node Is ExecutableStatementSyntax Then
                Return True
            End If

            ' Parser parses some statements part-by-part and then wraps them with executable 
            ' statements, so we may stumble on such a part in error-recovery scenarios in case parser 
            ' didn't wrap it because of some parse error; in some cases such statements should 
            ' be considered equal to executable statements
            ' 
            ' Example: parsing a simple text 'If True' produces just an IfStatementSyntax (which
            ' is not an executable statement) which is supposed to be wrapped with MultiLineIfBlockSyntax
            ' or SingleLineIfStatement in non-error scenario.
            Select Case node.Kind
                Case SyntaxKind.IfStatement,
                     SyntaxKind.ElseIfStatement,
                     SyntaxKind.ElseStatement,
                     SyntaxKind.WithStatement,
                     SyntaxKind.TryStatement,
                     SyntaxKind.CatchStatement,
                     SyntaxKind.FinallyStatement,
                     SyntaxKind.SyncLockStatement,
                     SyntaxKind.WhileStatement,
                     SyntaxKind.UsingStatement,
                     SyntaxKind.SelectStatement,
                     SyntaxKind.CaseStatement,
                     SyntaxKind.CaseElseStatement,
                     SyntaxKind.SimpleDoStatement,
                     SyntaxKind.DoWhileStatement,
                     SyntaxKind.DoUntilStatement,
                     SyntaxKind.ForStatement,
                     SyntaxKind.ForEachStatement
                    Return True
            End Select

            Return False
        End Function

    End Module
End Namespace
