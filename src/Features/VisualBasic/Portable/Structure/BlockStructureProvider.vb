' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure

    Friend MustInherit Class BlockStructureProvider(Of TBlock As SyntaxNode, THeaderStatement As SyntaxNode, TInnerBlock As SyntaxNode, TPreEndBlock As SyntaxNode, TEndOfBlockStatement As SyntaxNode)
        Inherits AbstractSyntaxNodeStructureProvider(Of TBlock)

        Public ReadOnly IncludeAdditionalInternalStructuralOutlinings As Boolean

        Friend Sub New(IncludeAdditionalInternalStructuralOutlinings As Boolean)
            MyBase.New()
            Me.IncludeAdditionalInternalStructuralOutlinings = IncludeAdditionalInternalStructuralOutlinings
        End Sub

#Region "Block Provider Specific Methods"

        ''' <summary>
        ''' The <see cref="BlockSpan"/> of the complete structure.
        ''' <code>
        ''' Select Case value
        '''   Case ...
        '''   Cass ...
        '''   Case Else
        ''' End Select
        ''' </code>
        ''' In the above example, this would be 
        ''' <code>
        ''' Select Case value
        '''   ...
        ''' End Select
        ''' </code>
        ''' </summary>
        ''' <param name="block"></param>
        ''' <returns></returns>
        Friend MustOverride Function FullStructuralBlockOutlining(block As TBlock) As BlockSpan?

        Friend Overridable Function FullStructuralBlockOutlining(block As TBlock, statement As THeaderStatement) As BlockSpan?
            Return CreateBlockSpanFromBlock(
                             block, statement, autoCollapse:=False,
                             type:=BlockTypes.Statement, isCollapsible:=True)
        End Function

        ''' <summary>
        ''' Return the Block Header for the Block Structure
        ''' Eg Select Case value
        ''' </summary>
        ''' <param name="block"></param>
        ''' <returns></returns>
        Friend MustOverride Function GetBannerTextOfFullStructuralBlock(block As TBlock) As THeaderStatement

        ''' <summary>
        ''' Return the internal <see cref="BlockSpan"/>s of the strucure. 
        ''' </summary>
        ''' <param name="block"></param>
        ''' <param name="cancellationToken"></param>
        ''' <returns></returns>
        Friend Function GetInternalStructuralOutlings(block As TBlock, cancellationToken As Threading.CancellationToken) As ImmutableArray(Of BlockSpan)
            Dim InternalSpans = ArrayBuilder(Of BlockSpan).GetInstance
            InternalSpans.AddIfNotNull(GetPreambleOutlining(block, cancellationToken))
            InternalSpans.AddRange(GetInternalStructuralOutlinings(block, cancellationToken))
            InternalSpans.AddIfNotNull(GetEpilogueBlockOutlining(block, cancellationToken))
            Return InternalSpans.ToImmutableOrEmptyAndFree
        End Function

        ''' <summary>
        ''' Returns the internal structural bLocks of the Block Structure.
        ''' Eg Case .... 
        ''' </summary>
        ''' <param name="block"></param>
        ''' <returns></returns>
        Friend MustOverride Function GetInternalStructuralBlocks(block As TBlock) As SyntaxList(Of TInnerBlock)
        ''' <summary>
        ''' Some block structure allow statements aftet the header and before the First Inner Block.
        ''' Eg
        ''' <code>
        ''' If ... Then
        '''   ' Comment 1
        '''   Console.WriteLine("Hello World!")
        '''   ' Comment 2
        ''' Else If ... The
        ''' End If
        ''' </code>
        ''' In the example this would be
        ''' <code>
        '''   ' Comment 1
        '''   Console.WriteLine("Hello World!")
        '''   ' Comment 2
        ''' </code>
        ''' </summary>
        ''' <param name="block"></param>
        ''' <param name="cancellationToken"></param>
        ''' <returns></returns>
        Friend Overridable Function GetPreambleOutlining(block As TBlock, cancellationToken As Threading.CancellationToken) As BlockSpan?
            Return Nothing
        End Function

        Friend Function GetPreambleOutlining(block As TBlock) As BlockSpan?
            Dim Header = GetBannerTextOfFullStructuralBlock(block)
            Dim NextSection = GetFirstStructuralBlockAfterPreamble(block)
            Return If(NextSection IsNot Nothing, GetBlockSpan(block, Header, NextSection, Ellipsis, IgnoreHeader:=True), Nothing)
        End Function

        ''' <summary>
        ''' Return the first statement of the Preamble <seealso cref="GetPreambleOutlining"/>
        ''' <code>
        '''   ' Comment 1
        '''   Console.WriteLine("Hello World!")
        '''   ' Comment 2
        ''' </code>
        ''' In the example the result would be
        ''' <code>
        ''' Console.WriteLine("Hello World!")
        ''' </code>
        ''' </summary>
        ''' <param name="block"></param>
        ''' <returns></returns>
        Friend Overridable Function GetFirstStatementOfPreamble(block As TBlock) As SyntaxNode
            Return Nothing
        End Function

        Friend Function GetFirstStructuralBlockAfterPreamble(block As TBlock) As SyntaxNode
            Dim InnerBlocks = GetInternalStructuralBlocks(block)
            If InnerBlocks.Count > 0 Then
                Return InnerBlocks(0)
            End If
            Dim NextSection As SyntaxNode = GetEpilogueBlock(block)
            If NextSection Is Nothing Then
                NextSection = GetEnd_XXX_Statement(block)
            End If
            Return NextSection
        End Function

        ''' <summary>
        ''' Return the Banner Text  internal structural bLock.
        ''' </summary>
        ''' <param name="InnerBlock"></param>
        ''' <returns></returns>
        Friend MustOverride Function GetBannerTextOfInternalStructuralBlock(InnerBlock As TInnerBlock) As String

        ''' <summary>
        ''' Return the Epilogue of the block structure.
        ''' </summary>
        ''' <param name="block"></param>
        ''' <returns></returns>
        Friend Overridable Function GetEpilogueBlock(block As TBlock) As TPreEndBlock
            Return Nothing
        End Function

        Friend Overridable Function GetEpilogueBlockOutlining(block As TBlock, cancellationToken As Threading.CancellationToken) As BlockSpan?
            Return Nothing
        End Function

        ''' <summary>
        ''' Return the End Statement of the full structral block.
        ''' Eg End Select
        ''' </summary>
        ''' <param name="block"></param>
        ''' <returns></returns>
        Friend MustOverride Function GetEnd_XXX_Statement(block As TBlock) As TEndOfBlockStatement





#End Region

        Protected Overrides Sub CollectBlockSpans(node As TBlock, spans As ArrayBuilder(Of BlockSpan), options As OptionSet, cancellationToken As CancellationToken)
            Dim FullBlock = FullStructuralBlockOutlining(node)
            spans.AddIfNotNull(FullBlock)
            If IncludeAdditionalInternalStructuralOutlinings Then
                Dim internalSpans = GetInternalStructuralOutlings(node, cancellationToken)
                spans.AddRange(internalSpans)
            End If
        End Sub

        Friend Function GetInternalStructuralOutlinings(block As TBlock, cancellationToken As Threading.CancellationToken) As ImmutableArray(Of BlockSpan)
            Dim InnerBlocksBlockSpans = ArrayBuilder(Of BlockSpan).GetInstance
            If block IsNot Nothing Then
                Dim InnerBlocks = GetInternalStructuralBlocks(block)
                If InnerBlocks.Count > 0 Then
                    Dim edx = InnerBlocks.Count - 1
                    For idx = 0 To edx
                        If cancellationToken.IsCancellationRequested Then
                            Exit For
                        End If
                        Dim NextBlock As SyntaxNode = Nothing
                        Dim InnerBlock = InnerBlocks(idx)
                        If idx = edx Then
                            NextBlock = GetEpilogueBlock(block)
                            If NextBlock Is Nothing Then
                                NextBlock = GetEnd_XXX_Statement(block)
                                If NextBlock Is Nothing Then Continue For
                            End If
                        Else
                            NextBlock = InnerBlocks(idx + 1)
                        End If
                        If NextBlock IsNot Nothing Then
                            Dim ThisBlockSpan = GetBlockSpan(InnerBlock, InnerBlock, NextBlock, GetBannerTextOfInternalStructuralBlock(InnerBlock), False)
                            InnerBlocksBlockSpans.AddIfNotNull(ThisBlockSpan)
                        End If
                    Next
                End If
            End If
            Return InnerBlocksBlockSpans.ToImmutableOrEmptyAndFree
        End Function

        Friend Function GetBlockSpan(block As SyntaxNode, Header As SyntaxNode, NextSection As SyntaxNode, BannerText As String, IgnoreHeader As Boolean) As BlockSpan?
            Dim Statements As SyntaxList(Of StatementSyntax) = If(block.IsStatementContainerNode(), block.GetStatements(), Nothing)
            If Statements.Count = 0 Then
                Return BlockHasNoStatements(block, If(IgnoreHeader,
                                                      DirectCast(If(FirstTriviaAfterFirstEndOfLine(Header.GetTrailingTrivia),
                                                                    NextSection.GetLeadingTrivia.FirstOrNullable), Object), Header), NextSection, BannerText)
            Else
                Return BlockHasStatements(block, If(IgnoreHeader,
                                                    DirectCast(If(FirstTriviaAfterFirstEndOfLine(Header.GetTrailingTrivia),
                                                                  Statements(0).GetLeadingTrivia.FirstOrNullable), Object),
                                                    Header), NextSection, Statements, BannerText)
            End If
        End Function

        Friend Function BlockHasNoStatements(block As SyntaxNode, StartingAt As Object, NextSection As SyntaxNode, BannerText As String) As BlockSpan?
            Return BlockHas(NoStatements:=True, block, StartingAt, NextSection, Nothing, BannerText)
        End Function

        Friend Function BlockHasStatements(block As SyntaxNode, StartingAt As Object, NextSection As SyntaxNode, Statements As SyntaxList(Of StatementSyntax), BannerText As String) As BlockSpan?
            Return BlockHas(NoStatements:=False, block, StartingAt, NextSection, Statements, BannerText)
        End Function

        Friend Function BlockHas(NoStatements As Boolean, block As SyntaxNode, StartingAt As Object, NextSection As SyntaxNode, Statements As SyntaxList(Of StatementSyntax), BannerText As String) As BlockSpan?
            If StartingAt IsNot Nothing Then
                Dim MinLineDelta = 0
                Dim FinishingAt As SyntaxTrivia?
                If NoStatements Then
                    FinishingAt = LastEndOfLineOrNullable(NextSection.GetLeadingTrivia)
                    MinLineDelta = 1
                Else
                    FinishingAt = If(LastEndOfLineOrNullable(NextSection.GetLeadingTrivia),
                                        LastEndOfLineOrNullable(Statements.Last.GetTrailingTrivia))
                    MinLineDelta = 0
                End If
                If FinishingAt IsNot Nothing Then
                    If TypeOf StartingAt Is SyntaxTrivia? Then
                        Return MakeIfHasLineDeltaGreaterThanX(MinLineDelta, DirectCast(StartingAt, SyntaxTrivia?).Value, FinishingAt.Value, BannerText)
                    ElseIf TypeOf StartingAt Is SyntaxNode Then
                        Return MakeIfHasLineDeltaGreaterThanX(MinLineDelta, DirectCast(StartingAt, SyntaxNode), FinishingAt.Value, BannerText)
                    End If
                End If
            End If
            Return Nothing
        End Function

        Private Function MakeBlockSpan(Start As Integer, Finish As Integer, BannerText As String) As BlockSpan?
            Dim Span = Text.TextSpan.FromBounds(Start, Finish)
            Return New BlockSpan(BlockTypes.Statement, True, Span, BannerText)
        End Function

#Region "MakeIfHasLineDeltaGreaterThanX Overloads"
        Private Function MakeIfHasLineDeltaGreaterThanX(X As Integer, StartingAt As SyntaxTrivia, FinishingAt As SyntaxTrivia, BannerText As String) As BlockSpan?
            Return If(LineDelta(StartingAt, FinishingAt) > X, MakeBlockSpan(StartingAt.SpanStart, FinishingAt.SpanStart, BannerText), Nothing)
        End Function

        Private Function MakeIfHasAtLeastOneLineDelta(X As Integer, StartingAt As SyntaxTrivia, FinishingAt As SyntaxNode, BannerText As String) As BlockSpan?
            Return If(LineDelta(StartingAt, FinishingAt) > X, MakeBlockSpan(StartingAt.SpanStart, FinishingAt.SpanStart, BannerText), Nothing)
        End Function

        Private Function MakeIfHasLineDeltaGreaterThanX(X As Integer, StartingAt As SyntaxNode, FinishingAt As SyntaxTrivia, BannerText As String) As BlockSpan?
            Return If(LineDelta(StartingAt, FinishingAt) > X, MakeBlockSpan(StartingAt.SpanStart, FinishingAt.SpanStart, BannerText), Nothing)
        End Function

        Private Function MakeIfHasAtLeastOneLineDelta(X As Integer, StartingAt As SyntaxNode, FinishingAt As SyntaxNode, BannerText As String) As BlockSpan?
            Return If(LineDelta(StartingAt, FinishingAt) > X, MakeBlockSpan(StartingAt.SpanStart, FinishingAt.SpanStart, BannerText), Nothing)
        End Function
#End Region

#Region "Line Delta Helpers"
        Friend Function FirstTriviaAfterFirstEndOfLine(Trivias As SyntaxTriviaList) As SyntaxTrivia?
            Dim IsFirstOne = True
            Dim edx = Trivias.Count - 1
            For idx = 0 To edx
                If Trivias(idx).IsEndOfLine Then
                    If IsFirstOne Then
                        IsFirstOne = False
                    Else
                        Return If(idx < edx, Trivias(idx + 1), Nothing)
                    End If
                End If
            Next
            Return Nothing
        End Function

        Private Function LastEndOfLineOrNullable(Trivias As SyntaxTriviaList) As SyntaxTrivia?
            Dim IsFirstOne = True
            Dim edx = Trivias.Count - 1
            For idx = edx To 0 Step -1
                If Trivias(idx).IsEndOfLine Then
                    Return New SyntaxTrivia?(Trivias(idx))
                End If
            Next
            Return New SyntaxTrivia?()
        End Function

        Private Function LineDelta(A As Int32, B As Int32) As Int32
            Return B - A
        End Function

        Private Function LineDelta(a As SyntaxTrivia, b As SyntaxTrivia) As Integer
            Return LineDelta(a.GetLocation.GetMappedLineSpan.StartLinePosition.Line, b.GetLocation.GetMappedLineSpan.StartLinePosition.Line)
        End Function

        Private Function LineDelta(a As SyntaxNode, b As SyntaxNode) As Integer
            Return LineDelta(a.GetLocation.GetMappedLineSpan.StartLinePosition.Line, b.GetLocation.GetMappedLineSpan.StartLinePosition.Line)
        End Function

        Private Function LineDelta(a As SyntaxNode, b As SyntaxTrivia) As Integer
            Return LineDelta(a.GetLocation.GetMappedLineSpan.StartLinePosition.Line, b.GetLocation.GetMappedLineSpan.StartLinePosition.Line)
        End Function

        Private Function LineDelta(a As SyntaxTrivia, b As SyntaxNode) As Integer
            Return LineDelta(a.GetLocation.GetMappedLineSpan.StartLinePosition.Line, b.GetLocation.GetMappedLineSpan.StartLinePosition.Line)
        End Function
#End Region

    End Class
End Namespace
