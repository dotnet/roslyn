' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
    ''' <summary>
    ''' This class watches for buffer-based events, tracks the dirty regions, and invokes the formatter as appropriate
    ''' </summary>
    Partial Friend Class CommitBufferManager
        Inherits ForegroundThreadAffinitizedObject

        Private ReadOnly _buffer As ITextBuffer
        Private ReadOnly _commitFormatter As ICommitFormatter
        Private ReadOnly _inlineRenameService As IInlineRenameService

        Private _referencingViews As Integer

        ''' <summary>
        ''' An object to use as a sync lock for <see cref="_referencingViews"/>.
        ''' </summary>
        Private ReadOnly _referencingViewsLock As Object = New Object()

        ''' <summary>
        ''' The tracking span which is the currently "dirty" region in the buffer. May be null if there is no dirty region.
        ''' </summary>
        Private _dirtyState As DirtyState

        Private _documentBeforePreviousEdit As Document

        ''' <summary>
        ''' The number of times BeginSuppressingCommits() has been called.
        ''' </summary>
        Private _suppressions As Integer

        Public Sub New(
            buffer As ITextBuffer,
            commitFormatter As ICommitFormatter,
            inlineRenameService As IInlineRenameService,
            threadingContext As IThreadingContext)
            MyBase.New(threadingContext, assertIsForeground:=False)

            Contract.ThrowIfNull(buffer)
            Contract.ThrowIfNull(commitFormatter)
            Contract.ThrowIfNull(inlineRenameService)

            _buffer = buffer
            _commitFormatter = commitFormatter
            _inlineRenameService = inlineRenameService
        End Sub

        Public Sub AddReferencingView()
            ThisCanBeCalledOnAnyThread()

            SyncLock _referencingViewsLock
                _referencingViews += 1

                If _referencingViews = 1 Then
                    AddHandler _buffer.Changing, AddressOf OnTextBufferChanging
                    AddHandler _buffer.Changed, AddressOf OnTextBufferChanged
                End If
            End SyncLock
        End Sub

        Public Sub RemoveReferencingView()
            ThisCanBeCalledOnAnyThread()

            SyncLock _referencingViewsLock
                ' If someone enables line commit with a file already open, we might end up decrementing
                ' the ref count too many times, so only do work if we are still above 0.
                If _referencingViews > 0 Then
                    _referencingViews -= 1

                    If _referencingViews = 0 Then
                        RemoveHandler _buffer.Changed, AddressOf OnTextBufferChanged
                        RemoveHandler _buffer.Changing, AddressOf OnTextBufferChanging
                    End If
                End If
            End SyncLock
        End Sub

        Public ReadOnly Property HasDirtyRegion As Boolean
            Get
                Return _dirtyState IsNot Nothing
            End Get
        End Property

        ''' <summary>
        ''' Commits any dirty region, if one exists.
        '''
        ''' To improve perf, passing false to isExplicitFormat will avoid semantic checks when expanding
        ''' the formatting span to an entire block
        ''' </summary>
        Public Sub CommitDirty(isExplicitFormat As Boolean, cancellationToken As CancellationToken)
            If _inlineRenameService.ActiveSession IsNot Nothing Then
                _dirtyState = Nothing
                Return
            End If

            If _dirtyState Is Nothing Then
                Return
            End If

            ' Is something else in the commit process suppressing the commit?
            If _suppressions > 0 Then
                Return
            End If

            Try
                ' Start to suppress commits to ensure we don't have any sort of re-entrancy in this process.
                ' We've seen bugs (17015) where waits triggered by some computation might re-enter.
                Using BeginSuppressingCommits()
                    ' It's possible that an edit may already be in progress. In this scenario, there's
                    ' really nothing we can do, so we'll just skip the format
                    If _buffer.EditInProgress Then
                        Return
                    End If

                    Dim dirtyRegion = _dirtyState.DirtyRegion.GetSpan(_buffer.CurrentSnapshot)
                    Dim info As FormattingInfo
                    If Not TryComputeExpandedSpanToFormat(dirtyRegion, info, cancellationToken) Then
                        Return
                    End If

                    Dim useSemantics = info.UseSemantics
                    If useSemantics AndAlso Not isExplicitFormat Then
                        ' Avoid using semantics for formatting extremely large dirty spans without an explicit request
                        ' from the user. The "large span threshold" is 7000 lines. The 7000 line threshold is an
                        ' estimated value accounting for a lower-bound of the algorithmic complexity of text
                        ' differencing in designer cases along with measurements of a pathological example demonstrated
                        ' at 14000 lines. We expect Windows Forms designer formatting operations to run in under ~15
                        ' seconds on average current hardware when nearing the threshold.
                        Dim startLineNumber = 0
                        Dim startCharIndex = 0
                        Dim endLineNumber = 0
                        Dim endCharIndex = 0
                        info.SpanToFormat.GetLinesAndCharacters(startLineNumber, startCharIndex, endLineNumber, endCharIndex)
                        If endLineNumber - startLineNumber > 7000 Then
                            useSemantics = False
                        End If
                    End If

                    Dim tree = _dirtyState.BaseDocument.GetSyntaxTreeSynchronously(cancellationToken)
                    _commitFormatter.CommitRegion(info.SpanToFormat, isExplicitFormat, useSemantics, dirtyRegion, _dirtyState.BaseSnapshot, tree, cancellationToken)
                End Using
            Finally
                ' We may have tracked a dirty region while committing or it may have been aborted.
                ' In any case, we want to guarantee we have no dirty region once we're done
                _dirtyState = Nothing
            End Try
        End Sub

        Private Structure FormattingInfo
            Public UseSemantics As Boolean
            Public SpanToFormat As SnapshotSpan
        End Structure

        Public Sub ExpandDirtyRegion(snapshotSpan As SnapshotSpan)
            If _dirtyState Is Nothing Then
                Dim document = snapshotSpan.Snapshot.GetOpenDocumentInCurrentContextWithChanges()
                If document IsNot Nothing Then
                    _dirtyState = New DirtyState(snapshotSpan, snapshotSpan.Snapshot, document)
                End If
            Else
                _dirtyState = _dirtyState.WithExpandedDirtySpan(snapshotSpan)
            End If
        End Sub

        Private Shared Function TryComputeExpandedSpanToFormat(dirtySpan As SnapshotSpan, ByRef formattingInfo As FormattingInfo, cancellationToken As CancellationToken) As Boolean
            Dim document = dirtySpan.Snapshot.GetOpenDocumentInCurrentContextWithChanges()
            If document Is Nothing Then
                Return False
            End If

            formattingInfo.UseSemantics = True
            Dim tree = document.GetSyntaxTreeSynchronously(cancellationToken)

            ' No matter what, we will always include the dirty span
            Dim finalSpanStart = dirtySpan.Start.Position
            Dim finalSpanEnd = dirtySpan.End.Position

            ' Find the containing statements
            Dim startingStatementInfo = ContainingStatementInfo.GetInfo(dirtySpan.Start, tree, cancellationToken)
            If startingStatementInfo IsNot Nothing Then
                finalSpanStart = Math.Min(finalSpanStart, startingStatementInfo.TextSpan.Start)

                If startingStatementInfo.MatchingBlockConstruct IsNot Nothing Then
                    ' If we're expanding backwards because of editing an end construct, we don't wan to run
                    ' expensive semantic formatting checks.  We really just want to fix up indentation.
                    formattingInfo.UseSemantics = finalSpanStart <= startingStatementInfo.MatchingBlockConstruct.SpanStart

                    finalSpanStart = Math.Min(finalSpanStart, startingStatementInfo.MatchingBlockConstruct.SpanStart)
                End If
            End If

            Dim endingStatementInfo = If(ContainingStatementInfo.GetInfo(dirtySpan.End, tree, cancellationToken), startingStatementInfo)
            If endingStatementInfo IsNot Nothing Then
                finalSpanEnd = Math.Max(finalSpanEnd, endingStatementInfo.TextSpan.End)

                If endingStatementInfo.MatchingBlockConstruct IsNot Nothing Then
                    finalSpanEnd = Math.Max(finalSpanEnd, endingStatementInfo.MatchingBlockConstruct.Span.End)
                End If
            End If

            Dim startingLine = dirtySpan.Snapshot.GetLineFromPosition(finalSpanStart)

            If startingLine.LineNumber = 0 Then
                finalSpanStart = 0
            Else
                ' We want to include the line break into the line before
                finalSpanStart = dirtySpan.Snapshot.GetLineFromLineNumber(startingLine.LineNumber - 1).End
            End If

            formattingInfo.SpanToFormat = New SnapshotSpan(dirtySpan.Snapshot, Span.FromBounds(finalSpanStart, finalSpanEnd))
            Return True
        End Function

        Public Shared Function IsMovementBetweenStatements(oldPoint As SnapshotPoint, newPoint As SnapshotPoint, cancellationToken As CancellationToken) As Boolean
            ' If they are the same line, then definitely no
            If oldPoint.GetContainingLineNumber() = newPoint.GetContainingLineNumber() Then
                Return False
            End If

            Dim document = newPoint.Snapshot.GetOpenDocumentInCurrentContextWithChanges()
            If document Is Nothing Then
                Return False
            End If

            Dim tree = document.GetSyntaxTreeSynchronously(cancellationToken)

            Dim oldStatement = ContainingStatementInfo.GetInfo(oldPoint, tree, cancellationToken)
            Dim newStatement = ContainingStatementInfo.GetInfo(newPoint, tree, cancellationToken)

            If oldStatement Is Nothing AndAlso newStatement Is Nothing Then
                Return True
            End If

            If (oldStatement Is Nothing) <> (newStatement Is Nothing) Then
                Return True
            End If

            Return oldStatement.TextSpan <> newStatement.TextSpan
        End Function

        Private Sub OnTextBufferChanging(sender As Object, e As TextContentChangingEventArgs)
            If _dirtyState Is Nothing Then
                ' Grab the current document for the text buffer before it changes so we can get any
                ' cached versions
                Dim documentBeforePreviousEdit = e.Before.GetOpenDocumentInCurrentContextWithChanges()
                If documentBeforePreviousEdit IsNot Nothing Then
                    _documentBeforePreviousEdit = documentBeforePreviousEdit
                    ' Kick off a task to eagerly force compute InternalsVisibleTo semantics for all the references.
                    ' This provides a noticeable perf improvement when code cleanup is subsequently invoked on this document.
                    Task.Run(Async Function()
                                 Await ForceComputeInternalsVisibleToAsync(documentBeforePreviousEdit, CancellationToken.None).ConfigureAwait(False)
                             End Function)
                End If
            End If
        End Sub

        Private Sub OnTextBufferChanged(sender As Object, e As TextContentChangedEventArgs)
            ' Before we do anything else, ensure the field is nulled back out
            Dim documentBeforePreviousEdit = _documentBeforePreviousEdit
            _documentBeforePreviousEdit = Nothing

            If e.Changes.Count = 0 Then
                Return
            End If

            ' If this is a reiterated version, then it's part of undo/redo and we should ignore it
            If e.AfterVersion.ReiteratedVersionNumber <> e.AfterVersion.VersionNumber Then
                Return
            End If

            ' Add this region into our dirty region
            Dim encompassingNewSpan = New SnapshotSpan(e.After, Span.FromBounds(e.Changes.First().NewPosition, e.Changes.Last().NewEnd))

            If _dirtyState Is Nothing Then
                ' Some times, we won't get a documentBeforePreviousEdit. This happens because sometimes OnTextBufferChanging
                ' isn't called before OnTextBufferChanged in undo scenarios. In those cases, since we can't set up valid state,
                ' just throw it out
                If documentBeforePreviousEdit IsNot Nothing Then
                    _dirtyState = New DirtyState(encompassingNewSpan, e.Before, documentBeforePreviousEdit)
                End If
            Else
                _dirtyState = _dirtyState.WithExpandedDirtySpan(encompassingNewSpan)
            End If
        End Sub

        Private Shared Async Function ForceComputeInternalsVisibleToAsync(document As Document, cancellationToken As CancellationToken) As Task
            Dim project = document.Project
            Dim compilation = Await project.GetCompilationAsync(cancellationToken).ConfigureAwait(False)

            For Each reference In project.ProjectReferences
                Dim refProject = project.Solution.GetProject(reference.ProjectId)
                If refProject IsNot Nothing Then
                    Dim refCompilation = Await refProject.GetCompilationAsync(cancellationToken).ConfigureAwait(False)
                    refCompilation.Assembly().GivesAccessTo(compilation.Assembly)
                End If
            Next

            For Each reference In project.MetadataReferences
                Dim refAssemblyOrModule = compilation.GetAssemblyOrModuleSymbol(reference)
                If refAssemblyOrModule.MatchesKind(SymbolKind.Assembly) Then
                    Dim refAssembly = DirectCast(refAssemblyOrModule, IAssemblySymbol)
                    refAssembly.GivesAccessTo(compilation.Assembly)
                End If
            Next
        End Function

        ''' <summary>
        ''' Suppresses future commits, causing all calls to CommitDirty() to be a simple no-op, even
        ''' if there is a dirty span.
        ''' </summary>
        ''' <returns>An IDisposable that should be disposed when the caller wants to resume
        ''' submissions.</returns>
        Friend Function BeginSuppressingCommits() As IDisposable
            _suppressions += 1
            Return New SuppressionHandle(Me)
        End Function

        Private Class SuppressionHandle
            Implements IDisposable

            Private _manager As CommitBufferManager

            Public Sub New(manager As CommitBufferManager)
                Contract.ThrowIfNull(manager)
                _manager = manager
            End Sub

            Public Sub Dispose() Implements IDisposable.Dispose
                _manager._suppressions -= 1
                _manager = Nothing
            End Sub
        End Class
    End Class
End Namespace
