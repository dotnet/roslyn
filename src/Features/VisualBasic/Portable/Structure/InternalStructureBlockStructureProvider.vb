' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure

    ''' <summary>
    ''' Acts as base class for <see cref="BlockStructureProvider"/>'s that also
    ''' supplies outlinings of the internal structure.
    ''' </summary>
    ''' <typeparam name="TFullBlock">Type Of Block</typeparam>
    ''' <typeparam name="TFullBlockHeader">Block Header's Type</typeparam>
    ''' <typeparam name="TInnerBlock">Inner Blocks Type</typeparam>
    ''' <typeparam name="TEpilogue">Epilogue's Type</typeparam>
    ''' <typeparam name="TEpilogueStatement">Epilogue Statement's Type</typeparam>
    ''' <typeparam name="TEndOfBlockStatement">End Of Block Type</typeparam>
    Friend MustInherit Class InternalStructureBlockStructureProvider _
        (Of TFullBlock As SyntaxNode,
            TFullBlockHeader As SyntaxNode,
            TInnerBlock As SyntaxNode,
            TEpilogue As SyntaxNode,
            TEpilogueStatement As SyntaxNode,
            TEndOfBlockStatement As SyntaxNode
        )
        Inherits AbstractSyntaxNodeStructureProvider(Of TFullBlock)

        ''' <summary>
        ''' Implements the required method from <see cref="BlockStructureProvider"/>
        ''' </summary>
        Protected NotOverridable Overrides Sub CollectBlockSpans(block As TFullBlock,
                                                                 spans As ArrayBuilder(Of BlockSpan),
                                                                 options As OptionSet,
                                                                 ct As CancellationToken)
            ' (PROTOTYPE) Placeholder for state from user configurability.
            Dim IncludeAdditionalInternalStructuralOutlinings = True
            ' Pre-Existing outlining of the full structure of block
            spans.AddIfNotNull(OutliningOfFullBlock(block))
            ' If are allowed to include internal structural aspects of the block.
            If IncludeAdditionalInternalStructuralOutlinings Then
                ' Then add them to the collection of spans.
                spans.AddRange(OutliningsOfInternalStructure(block, ct))
            End If
        End Sub

#Region " Implementers"
#Region "  MustOverride"

        ''' <summary>
        ''' Return the Block Header for the Block Structure
        ''' Eg Select Case value
        ''' </summary>
        Friend MustOverride Function HeaderOfFullBlock(fullBlock As TFullBlock) As TFullBlockHeader


        ''' <summary>
        ''' Returns the inner blocks out of the full block.
        ''' Eg Case .... 
        ''' </summary>
        Friend MustOverride Function GetInnerBlocks(fullBlock As TFullBlock) As SyntaxList(Of TInnerBlock)

        ''' <summary>
        ''' Return the Banner Text  internal structural bLock.
        ''' </summary>
        Friend MustOverride Function InnerBlock_Text(InnerBlock As TInnerBlock) As String

        ''' <summary>
        ''' Return the End Statement of the full structral block.
        ''' Eg End Select
        ''' </summary>
        Friend MustOverride Function EndOfBlockStatement(fullBlock As TFullBlock) As TEndOfBlockStatement

#End Region
#Region "  Overridable"
        ''' <summary>
        ''' Return the Epilogue of the block structure.
        ''' </summary>
        Friend Overridable Function Epilogue(fullBlock As TFullBlock) As TEpilogue
            Return Nothing
        End Function

        Friend Overridable Function Epilogue_Text(epilogueNode As TEpilogueStatement) As String
            Return String.Empty
        End Function

        Friend Overridable Function Epilogue_Statement(epilogueNode As TEpilogue) As TEpilogueStatement
            Return Nothing
        End Function
#End Region
#End Region
#Region " Implementation"
        ' Full Block
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
        Private Function OutliningOfFullBlock(fullBlock As TFullBlock) As BlockSpan?
            Return MakeOutliningOfFullBlock(fullBlock, HeaderOfFullBlock(fullBlock))
        End Function

        ' Preamble
        ''' <summary>
        ''' Returns the outlining of the preamble section of code.
        ''' </summary>
        ''' <example>
        ''' <code>
        ''' If ... Then
        '''   ' Preamble Start
        '''   Console.WriteLine("Hello World!")
        '''   ' Preamble End
        ''' Else If ... The
        ''' End If
        ''' </code>
        ''' </example>
        ''' <returns>
        ''' The outlining of the preamble section of code.
        ''' eg
        ''' <code>
        '''   ' Preamble Start
        '''   Console.WriteLine("Hello World!")
        '''   ' Preamble End
        ''' </code>
        ''' Otherwise returns Nothing
        ''' </returns>
        ''' <remarks>
        ''' Preamble is section of code that is the headeer (<see cref="HeaderOfFullBlock(TFullBlock)"/>) and before the ...
        ''' <list type="" >
        ''' <item>First Inner Block <see cref="GetInnerBlocks(TFullBlock)"/></item>
        ''' <item>Epilogue <see cref="Epilogue(TFullBlock)"/></item>
        ''' <item>End Of Block Statement <see cref="EndOfBlockStatement(TFullBlock)"/></item>
        ''' </list>
        ''' If it does not have a preamble then returns nothing.
        ''' </remarks>
        Private Function OutliningOfPreamble(fullblock As TFullBlock,
                                             InnerBlocks As SyntaxList(Of TInnerBlock)) As BlockSpan?
            Dim Header = HeaderOfFullBlock(fullblock)
            Dim [Next] = NextAfterPreamble(fullblock, InnerBlocks)
            Return If([Next] IsNot Nothing, CalculateOutliningSpan(fullblock, Header, [Next], Ellipsis, ignoreHeader:=True), Nothing)
        End Function

        Private Function NextAfterPreamble(fullBlock As TFullBlock,
                                           InnerBlocks As SyntaxList(Of TInnerBlock)) As SyntaxNode
            Return If(InnerBlocks.FirstOrDefault, If(DirectCast(Epilogue(fullBlock), SyntaxNode), EndOfBlockStatement(fullBlock)))
        End Function

        ' Inner Blocks
        ''' <summary>
        ''' Return the internal outlining <see cref="BlockSpan"/>s from the full block.
        ''' </summary>
        Private Function OutliningsOfInternalStructure(fullBlock As TFullBlock,
                                                       ct As CancellationToken) As ImmutableArray(Of BlockSpan)
            Dim InternalSpans = ArrayBuilder(Of BlockSpan).GetInstance
            With InternalSpans
                ' Retrieve the inner blocks from the full block.
                Dim InnerBlocks = GetInnerBlocks(fullBlock)
                ' Preamble
                .AddIfNotNull(OutliningOfPreamble(fullBlock, InnerBlocks))
                ' Inner Blocks
                .AddRange(OutliningsOfInnerBlocks(fullBlock, InnerBlocks, ct))
                ' Epilogue
                .AddIfNotNull(OutliningOfEpilogue(Epilogue(fullBlock), ct))
                Return .ToImmutableOrEmptyAndFree
            End With
        End Function

        Private Function OutliningsOfInnerBlocks(fullBlock As TFullBlock,
                                                 InnerBlocks As SyntaxList(Of TInnerBlock),
                                                 ct As CancellationToken) As ImmutableArray(Of BlockSpan)
            Dim InnerBlocksBlockSpans = ArrayBuilder(Of BlockSpan).GetInstance
            If fullBlock IsNot Nothing Then
                ' So long as we have at least one inner block.
                If InnerBlocks.Count > 0 Then
                    ' Calculate the index of the last inner block.
                    Dim endIndex = InnerBlocks.Count - 1
                    For index = 0 To endIndex
                        ' Play nice and handle Cancellation Requests.
                        If ct.IsCancellationRequested Then
                            ct.ThrowIfCancellationRequested()
                        End If
                        ' Work the what the next structure is after this one.
                        Dim NextBlock As SyntaxNode = Nothing
                        If index < endIndex Then
                            ' It is still with the inner block collection.
                            NextBlock = InnerBlocks(index + 1)
                        Else
                            ' See if it is an optional Epilogue,
                            ' if not then maybe a the End Of Block Statement
                            ' it neither Nothing is return.
                            NextBlock = If(DirectCast(Epilogue(fullBlock), SyntaxNode),
                                           EndOfBlockStatement(fullBlock))
                        End If
                        If NextBlock IsNot Nothing Then
                            ' Now that we have a next struture and the this block.
                            Dim ThisBlock = InnerBlocks(index)
                            ' Calculate the block span required for the outlining.
                            Dim ThisBlockSpan = CalculateOutliningSpan(ThisBlock, ThisBlock, NextBlock, InnerBlock_Text(ThisBlock), False)
                            ' and add to the collection.
                            InnerBlocksBlockSpans.AddIfNotNull(ThisBlockSpan)
                        End If
                    Next
                End If
            End If
            Return InnerBlocksBlockSpans.ToImmutableOrEmptyAndFree
        End Function

        ' Epilogue
        Private Function OutliningOfEpilogue(epilogueNode As TEpilogue,
                                             ct As CancellationToken) As BlockSpan?
            If (epilogueNode Is Nothing) OrElse epilogueNode.IsMissing Then
                Return Nothing
            End If
            Dim [end] = EndOfBlockStatement(TryCast(epilogueNode.Parent, TFullBlock))
            If ([end] Is Nothing) OrElse [end].IsMissing Then
                Return Nothing
            End If
            Return CalculateOutliningSpan(epilogueNode,
                                          Epilogue_Statement(epilogueNode),
                                          [end],
                                          Epilogue_Text(Epilogue_Statement(epilogueNode)),
                                          ignoreHeader:=False)
        End Function

        ' Helper Functions

        ''' <summary> Helper Function to create the outlining for the Full Block. </summary>
        Friend Function MakeOutliningOfFullBlock(fullBlock As TFullBlock,
                                                 header As TFullBlockHeader) As BlockSpan?
            Return CreateBlockSpanFromBlock(
                             fullBlock, header, autoCollapse:=False,
                             type:=BlockTypes.Statement, isCollapsible:=True)
        End Function

#Region "   Calculate Outlining Span"
        Private Function CalculateOutliningSpan(block As SyntaxNode,
                                               header As SyntaxNode,
                                               nextSection As SyntaxNode,
                                               bannerText As String,
                                               ignoreHeader As Boolean
                                               ) As BlockSpan?
            If block.IsStatementContainerNode Then
                Dim Statements = block.GetStatements()
                If Statements.Count > 0 Then
                    Return CalculateSpanWithStatements(block, header, Statements, nextSection, bannerText, ignoreHeader)
                End If
            End If
            Return CalculateSpanWithoutStatements(block, header, nextSection, bannerText, ignoreHeader)
        End Function

        Private Function CalculateSpanWithoutStatements(block As SyntaxNode,
                                                        header As SyntaxNode,
                                                        nextSection As SyntaxNode,
                                                        bannerText As String,
                                                        ignoreHeader As Boolean
                                                        ) As BlockSpan?
            Dim arg As Object = header
            If ignoreHeader AndAlso FirstTriviaAfterFirstEndOfLine(header.GetTrailingTrivia) Is Nothing Then
                arg = nextSection.GetLeadingTrivia.FirstOrNullable
            End If
            Return Section(hasCode:=False, block, arg, nextSection, Nothing, bannerText)
        End Function

        Private Function CalculateSpanWithStatements(block As SyntaxNode,
                                                     header As SyntaxNode,
                                                     statements As SyntaxList(Of StatementSyntax),
                                                     nextSection As SyntaxNode,
                                                     bannerText As String,
                                                     ignoreHeader As Boolean) As BlockSpan?
            Dim arg As Object = header
            If ignoreHeader AndAlso (FirstTriviaAfterFirstEndOfLine(header.GetTrailingTrivia) Is Nothing) Then
                arg = statements(0).GetLeadingTrivia.FirstOrNullable
            End If
            Return Section(hasCode:=True, block, arg, nextSection, statements, bannerText)
        End Function

        Private Function Section(hasCode As Boolean,
                                 this As SyntaxNode,
                                 [From] As Object,
                                 [Next] As SyntaxNode,
                                 statements As SyntaxList(Of StatementSyntax),
                                 text As String) As BlockSpan?
            If [From] IsNot Nothing Then
                Dim MinLineDelta = 0
                Dim [End] As SyntaxTrivia?
                If hasCode Then
                    [End] = If(LastEndOfLineOrNullable([Next].GetLeadingTrivia),
                               LastEndOfLineOrNullable(statements.Last.GetTrailingTrivia))
                    MinLineDelta = 0
                Else
                    MinLineDelta = 1
                    [End] = LastEndOfLineOrNullable([Next].GetLeadingTrivia)
                End If
                If [End] IsNot Nothing Then
                    If TypeOf [From] Is SyntaxTrivia? Then
                        Return MakeIfHasLineDeltaGreaterThanX(MinLineDelta, DirectCast([From], SyntaxTrivia?).Value, [End].Value, text)
                    ElseIf TypeOf [From] Is SyntaxNode Then
                        Return MakeIfHasLineDeltaGreaterThanX(MinLineDelta, DirectCast([From], SyntaxNode), [End].Value, text)
                    End If
                End If
            End If
            Return Nothing
        End Function

#Region "MakeIfHasLineDeltaGreaterThanX Overloads"
        Private Function MakeIfHasLineDeltaGreaterThanX(min As Integer,
                                                        [from] As SyntaxTrivia,
                                                        [end] As SyntaxTrivia,
                                                        text As String
                                                        ) As BlockSpan?
            Return If(LineDelta([from], [end]) > min, MakeBlockSpan([from].SpanStart, [end].SpanStart, text), Nothing)
        End Function
        Private Function MakeIfHasLineDeltaGreaterThanX(min As Integer,
                                                        [from] As SyntaxNode,
                                                        [end] As SyntaxTrivia,
                                                        text As String
                                                        ) As BlockSpan?
            Return If(LineDelta([from], [end]) > min, MakeBlockSpan([from].SpanStart, [end].SpanStart, text), Nothing)
        End Function
#End Region

        Private Function MakeBlockSpan(Start As Integer, Finish As Integer, BannerText As String) As BlockSpan?
            Dim Span = Text.TextSpan.FromBounds(Start, Finish)
            Return New BlockSpan(BlockTypes.Statement, isCollapsible:=True, Span, BannerText)
        End Function

        Private Function FirstTriviaAfterFirstEndOfLine(Trivias As SyntaxTriviaList) As SyntaxTrivia?
            Dim isFirstOne = True
            Dim endIndex = Trivias.Count - 1
            For index = 0 To endIndex
                If Trivias(index).IsEndOfLine Then
                    If isFirstOne Then
                        isFirstOne = False
                    Else
                        Return If(index < endIndex, Trivias(index + 1), Nothing)
                    End If
                End If
            Next
            Return Nothing
        End Function

        Private Function LastEndOfLineOrNullable(Trivias As SyntaxTriviaList) As SyntaxTrivia?
            Return (From trivia As SyntaxTrivia? In Trivias Where trivia.HasValue AndAlso trivia.Value.IsEndOfLine
                    Select trivia).LastOrDefault
        End Function

#Region "Line Delta Helpers"
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
#End Region
#End Region
    End Class
End Namespace
