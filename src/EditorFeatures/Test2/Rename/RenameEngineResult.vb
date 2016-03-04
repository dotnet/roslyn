' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Rename
Imports Microsoft.CodeAnalysis.Rename.ConflictEngine
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Xunit.Sdk
Imports Microsoft.CodeAnalysis.Options
Imports Xunit.Abstractions

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename
    ''' <summary>
    ''' A class that holds the result of a rename engine call, and asserts
    ''' various things about it. This is used in the tests for the rename engine
    ''' to make asserting that certain spans were converted to what they should be.
    ''' </summary>
    Friend Class RenameEngineResult
        Implements IDisposable

        Private ReadOnly _workspace As TestWorkspace
        Private ReadOnly _resolution As ConflictResolution

        ''' <summary>
        ''' The list of related locations that haven't been asserted about yet. Items are
        ''' removed from here when they are asserted on, so the set should be empty once we're
        ''' done.
        ''' </summary>
        Private ReadOnly _unassertedRelatedLocations As HashSet(Of RelatedLocation)

        ''' <summary>
        ''' The list of text replacements that haven't been asserted about yet. Items are removed
        ''' from here when they are asserted, so the set should be empty once we're done.
        ''' </summary>
        ''' <remarks></remarks>
        Private ReadOnly _unassertedTextChanges As HashSet(Of Location)

        Private ReadOnly _renameTo As String

        Private _failedAssert As Boolean

        Private Sub New(workspace As TestWorkspace, resolution As ConflictResolution, renameTo As String)
            _workspace = workspace
            _resolution = resolution

            If resolution IsNot Nothing Then
                _unassertedRelatedLocations = New HashSet(Of RelatedLocation)(resolution.RelatedLocations)
            Else
                _unassertedRelatedLocations = New HashSet(Of RelatedLocation)()
            End If

            _renameTo = renameTo
        End Sub

        Public Shared Function Create(helper As ITestOutputHelper, workspaceXml As XElement, renameTo As String, Optional changedOptionSet As Dictionary(Of OptionKey, Object) = Nothing) As RenameEngineResult
            Dim workspace = TestWorkspace.CreateWorkspace(workspaceXml)
            workspace.SetTestLogger(AddressOf helper.WriteLine)

            Dim engineResult As RenameEngineResult = Nothing
            Try
                If workspace.Documents.Where(Function(d) d.CursorPosition.HasValue).Count <> 1 Then
                    AssertEx.Fail("The test must have a single $$ marking the symbol being renamed.")
                End If

                Dim cursorDocument = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue)
                Dim cursorPosition = cursorDocument.CursorPosition.Value

                Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)

                Dim symbol = RenameLocations.ReferenceProcessing.GetRenamableSymbolAsync(document, cursorPosition, CancellationToken.None).Result

                If symbol Is Nothing Then
                    AssertEx.Fail("The symbol touching the $$ could not be found.")
                End If

                Dim optionSet = workspace.Options

                If changedOptionSet IsNot Nothing Then
                    For Each entry In changedOptionSet
                        optionSet = optionSet.WithChangedOption(entry.Key, entry.Value)
                    Next
                End If

                AssertIsComplete(workspace.CurrentSolution)

                Dim locations = RenameLocations.FindAsync(symbol, workspace.CurrentSolution, optionSet, CancellationToken.None).Result
                Dim originalName = symbol.Name.Split("."c).Last()

                Dim result = ConflictResolver.ResolveConflictsAsync(locations, originalName, renameTo, optionSet, hasConflict:=Nothing, cancellationToken:=CancellationToken.None).Result

                AssertIsComplete(result.OldSolution)
                AssertIsComplete(result.NewSolution)

                engineResult = New RenameEngineResult(workspace, result, renameTo)
                engineResult.AssertUnlabeledSpansRenamedAndHaveNoConflicts()
            Catch
                ' Something blew up, so we still own the test workspace
                If engineResult IsNot Nothing Then
                    engineResult.Dispose()
                Else
                    workspace.Dispose()
                End If
                Throw
            End Try

            Return engineResult
        End Function

        Private Shared Sub AssertIsComplete(currentSolution As Solution)
            ' Ensure we don't have a partial solution. This is to detect for possible root causes of
            ' https://github.com/dotnet/roslyn/issues/9298

            If currentSolution.Projects.Any(Function(p) Not p.HasCompleteReferencesAsync().Result) Then
                AssertEx.Fail("We have an incomplete project floating around which we should not.")
            End If
        End Sub

        Friend ReadOnly Property ConflictResolution As ConflictResolution
            Get
                Return _resolution
            End Get
        End Property

        Private Sub AssertUnlabeledSpansRenamedAndHaveNoConflicts()
            For Each documentWithSpans In _workspace.Documents
                Dim oldSyntaxTree = _workspace.CurrentSolution.GetDocument(documentWithSpans.Id).GetSyntaxTreeAsync().Result

                For Each span In documentWithSpans.SelectedSpans
                    Dim location = oldSyntaxTree.GetLocation(span)

                    AssertLocationReferencedAs(location, RelatedLocationType.NoConflict)
                    AssertLocationReplacedWith(location, _renameTo)
                Next
            Next
        End Sub

        Public Sub AssertLabeledSpansInStringsAndCommentsAre(label As String, replacement As String)
            AssertLabeledSpansAre(label, replacement, RelatedLocationType.NoConflict, isRenameWithinStringOrComment:=True)
        End Sub

        Public Sub AssertLabeledSpansAre(label As String, Optional replacement As String = Nothing, Optional type As RelatedLocationType? = Nothing, Optional isRenameWithinStringOrComment As Boolean = False)
            For Each location In GetLabeledLocations(label)
                If replacement IsNot Nothing Then
                    If type.HasValue Then
                        AssertLocationReplacedWith(location, replacement, isRenameWithinStringOrComment)
                    End If
                End If

                If type.HasValue AndAlso Not isRenameWithinStringOrComment Then
                    AssertLocationReferencedAs(location, type.Value)
                End If

            Next
        End Sub

        Public Sub AssertLabeledSpecialSpansAre(label As String, replacement As String, type As RelatedLocationType?)
            For Each location In GetLabeledLocations(label)
                If replacement IsNot Nothing Then
                    If type.HasValue Then
                        AssertLocationReplacedWith(location, replacement)
                        AssertLocationReferencedAs(location, type.Value)
                    End If
                End If
            Next
        End Sub

        Private Function GetLabeledLocations(label As String) As IEnumerable(Of Location)
            Dim locations As New List(Of Location)

            For Each document In _workspace.Documents
                Dim annotatedSpans = document.AnnotatedSpans

                If annotatedSpans.ContainsKey(label) Then
                    Dim syntaxTree = _workspace.CurrentSolution.GetDocument(document.Id).GetSyntaxTreeAsync().Result

                    For Each span In annotatedSpans(label)
                        locations.Add(syntaxTree.GetLocation(span))
                    Next
                End If
            Next

            If locations.Count = 0 Then
                _failedAssert = True
                AssertEx.Fail(String.Format("The label '{0}' was not mentioned in the test.", label))
            End If

            Return locations
        End Function

        Private Sub AssertLocationReplacedWith(location As Location, replacementText As String, Optional isRenameWithinStringOrComment As Boolean = False)
            Try
                Dim documentId = ConflictResolution.OldSolution.GetDocumentId(location.SourceTree)
                Dim newLocation = ConflictResolution.GetResolutionTextSpan(location.SourceSpan, documentId)

                Dim newTree = ConflictResolution.NewSolution.GetDocument(documentId).GetSyntaxTreeAsync().Result
                Dim newToken = newTree.GetRoot.FindToken(newLocation.Start, findInsideTrivia:=True)
                Dim newText As String
                If newToken.Span = newLocation Then
                    newText = newToken.ToString()
                ElseIf isRenameWithinStringOrComment AndAlso newToken.FullSpan.Contains(newLocation) Then
                    newText = newToken.ToFullString().Substring(newLocation.Start - newToken.FullSpan.Start, newLocation.Length)
                Else
                    Dim newNode = newToken.Parent
                    While (newNode IsNot Nothing AndAlso newNode.Span <> newLocation)
                        newNode = newNode.Parent
                    End While

                    newText = newNode.ToString()
                End If

                Assert.Equal(replacementText, newText)
            Catch ex As XunitException
                _failedAssert = True
                Throw
            End Try
        End Sub

        Private Sub AssertLocationReferencedAs(location As Location, type As RelatedLocationType)
            Try
                Dim documentId = ConflictResolution.OldSolution.GetDocumentId(location.SourceTree)
                Dim reference = _unassertedRelatedLocations.SingleOrDefault(
                    Function(r) r.ConflictCheckSpan = location.SourceSpan AndAlso r.DocumentId = documentId)

                Assert.NotNull(reference)
                Assert.True(type.HasFlag(reference.Type))

                _unassertedRelatedLocations.Remove(reference)
            Catch ex As XunitException
                _failedAssert = True
                Throw
            End Try
        End Sub

        Private Sub Dispose() Implements IDisposable.Dispose
            ' Make sure we're cleaned up. Don't want the test harness crashing...
            GC.SuppressFinalize(Me)
            _workspace.Dispose()

            ' If we failed some other assert, we know we're going to have things left
            ' over. So let's just suppress these so we don't lose the root cause
            If Not _failedAssert Then
                If _unassertedRelatedLocations.Count > 0 Then
                    AssertEx.Fail("There were additional related locations than were unasserted.")
                End If
            End If
        End Sub

        Protected Overrides Sub Finalize()
            If Not Environment.HasShutdownStarted Then
                Throw New Exception("Dispose was not called in a Rename test.")
            End If
        End Sub

        Public Sub AssertReplacementTextInvalid()
            Try
                Assert.False(_resolution.ReplacementTextValid)
            Catch ex As XunitException
                _failedAssert = True
                Throw
            End Try
        End Sub

        Public Sub AssertIsInvalidResolution()
            Assert.Null(_resolution)
        End Sub

    End Class
End Namespace
