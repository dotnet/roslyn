' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeCleanup
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Remote.Testing
Imports Microsoft.CodeAnalysis.Rename
Imports Microsoft.CodeAnalysis.Rename.ConflictEngine
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Xunit.Abstractions
Imports Xunit.Sdk

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename
    ''' <summary>
    ''' A class that holds the result of a rename engine call, and asserts
    ''' various things about it. This is used in the tests for the rename engine
    ''' to make asserting that certain spans were converted to what they should be.
    ''' </summary>
    Friend Class RenameEngineResult
        Implements IDisposable
        Private Const CaretString = "$$"

        Private ReadOnly _workspace As TestWorkspace
        Private ReadOnly _resolution As ConflictResolution
        Private ReadOnly _renameSymbol As ImmutableArray(Of ISymbol)

        ''' <summary>
        ''' The list of related locations that haven't been asserted about yet. Items are
        ''' removed from here when they are asserted on, so the set should be empty once we're
        ''' done.
        ''' </summary>
        Private ReadOnly _unassertedRelatedLocations As HashSet(Of RelatedLocation)
        Private ReadOnly _nonConflictLocationToReplacementText As ImmutableDictionary(Of DocumentId, ImmutableDictionary(Of TextSpan, String))

        Private ReadOnly _symbolToReplacementText As ImmutableDictionary(Of ISymbol, String)
        Private ReadOnly _annotatedRenameTagToSymbolsMap As ImmutableDictionary(Of String, ISymbol)

        'Private ReadOnly _symbolAtCaretLocation As ISymbol
        'Private ReadOnly _replacementTextForSymbolAtCaretLocation As String

        Private _failedAssert As Boolean

        Private Sub New(
                workspace As TestWorkspace,
                resolution As ConflictResolution,
                nonConflictTextSpanToReplacementText As ImmutableDictionary(Of (DocumentId, TextSpan), String),
                symbolToReplacementText As ImmutableDictionary(Of ISymbol, String),
                annotatedTagToSymbolsMap As ImmutableDictionary(Of String, ISymbol))
            _workspace = workspace
            _resolution = resolution
            _unassertedRelatedLocations = New HashSet(Of RelatedLocation)(resolution.RelatedLocations)
            _nonConflictTextSpanToReplacementText = nonConflictTextSpanToReplacementText
            _symbolToReplacementText = symbolToReplacementText
            _annotatedRenameTagToSymbolsMap = annotatedTagToSymbolsMap
        End Sub

        Private Shared Function CreateTestWorkspace(helper As ITestOutputHelper,
                                                    workspaceXml As XElement,
                                                    Host As RenameTestHost,
                                                    Optional sourceGenerator As ISourceGenerator = Nothing) As TestWorkspace
            Dim composition = EditorTestCompositions.EditorFeatures.AddParts(GetType(NoCompilationContentTypeLanguageService), GetType(NoCompilationContentTypeDefinitions))
            If Host = RenameTestHost.OutOfProcess_SingleCall OrElse Host = RenameTestHost.OutOfProcess_SplitCall Then
                composition = composition.WithTestHostParts(Remote.Testing.TestHost.OutOfProcess)
            End If

            Dim workspace = TestWorkspace.CreateWorkspace(workspaceXml, composition:=composition)
            workspace.SetTestLogger(AddressOf helper.WriteLine)

            If sourceGenerator IsNot Nothing Then
                workspace.OnAnalyzerReferenceAdded(workspace.CurrentSolution.ProjectIds.Single(), New TestGeneratorReference(sourceGenerator))
            End If
            Return workspace
        End Function

        Private Shared Async Function GetRenamedSymbolInfoMapAsync(
            workspace As TestWorkspace,
            renameTags() As String,
            renameTagOptions As Dictionary(Of String, SymbolRenameOptions)) As Task(Of ImmutableDictionary(Of ISymbol, (String, SymbolRenameOptions)))
            Dim builder As PooledDictionary(Of ISymbol, (String, SymbolRenameOptions)) = Nothing
            Using disposer = PooledDictionary(Of ISymbol, (String, SymbolRenameOptions)).GetInstance(builder)
                For Each testDocument In workspace.Documents
                    Dim document = workspace.CurrentSolution.GetRequiredDocument(testDocument.Id)
                    For Each annotatedSpan In testDocument.AnnotatedSpans
                        Dim newName = annotatedSpan.Key
                        Dim symbolRenameOption = If(renameTagOptions IsNot Nothing, renameTagOptions(newName), New SymbolRenameOptions())
                        Dim spans = annotatedSpan.Value
                        If renameTags.Contains(newName) Then
                            For Each span In spans
                                Dim symbol = Await RenameUtilities.TryGetRenamableSymbolAsync(document, span.Start, CancellationToken.None).ConfigureAwait(False)
                                builder(symbol) = (newName, symbolRenameOption)
                            Next
                        End If
                    Next
                Next
                Return builder.ToImmutableDictionary()
            End Using
        End Function

        Public Shared Async Function CreateForRenamingMultipleSymbolsAsync(
                helper As ITestOutputHelper,
                workspaceXml As XElement,
                inProcess As Boolean,
                renameTags() As String,
                Optional RenameTagOptions As Dictionary(Of String, SymbolRenameOptions) = Nothing,
                Optional expectFailure As Boolean = False,
                Optional sourceGenerator As ISourceGenerator = Nothing) As Task(Of RenameEngineResult)
            Dim workspace = CreateTestWorkspace(helper, workspaceXml, If(inProcess, RenameTestHost.InProcess, RenameTestHost.OutOfProcess_SingleCall), sourceGenerator)
            Dim renameSymbolInfo = Await GetRenamedSymbolInfoMapAsync(workspace, renameTags, RenameTagOptions).ConfigureAwait(False)
            Dim solution = workspace.CurrentSolution
            Dim conflictResolution = Await Renamer.RenameSymbolsAsync(
                solution,
                renameSymbolInfo,
                CodeActionOptions.DefaultProvider,
                ImmutableArray(Of SymbolKey).Empty,
                CancellationToken.None).ConfigureAwait(False)
            If expectFailure Then
                Assert.False(conflictResolution.IsSuccessful)
                Assert.NotNull(conflictResolution.ErrorMessage)
            Else
                Assert.True(conflictResolution.IsSuccessful)
            End If

            Dim engineResult = New RenameEngineResult(
                workspace,
                conflictResolution,
                renameSymbolInfo.ToImmutableDictionary(Function(pair) pair.Key, Function(pair) pair.Value.Item1),
                renameSymbolInfo.Keys.ToImmutableArray())

            Return engineResult
        End Function

        Public Shared Function Create(
                helper As ITestOutputHelper,
                workspaceXml As XElement,
                renameTo As String,
                host As RenameTestHost,
                Optional renameOptions As SymbolRenameOptions = Nothing,
                Optional expectFailure As Boolean = False,
                Optional sourceGenerator As ISourceGenerator = Nothing) As RenameEngineResult
            Dim workspace = CreateTestWorkspace(helper, workspaceXml, host, sourceGenerator)

            Dim success = False
            Dim engineResult As RenameEngineResult = Nothing
            Try
                If workspace.Documents.Where(Function(d) d.CursorPosition.HasValue).Count <> 1 Then
                    AssertEx.Fail("The test must have a single $$ marking the symbol being renamed.")
                End If

                Dim cursorDocument = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue)
                Dim cursorPosition = cursorDocument.CursorPosition.Value

                Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)

                Dim symbol = RenameUtilities.TryGetRenamableSymbolAsync(document, cursorPosition, CancellationToken.None).Result
                If symbol Is Nothing Then
                    AssertEx.Fail("The symbol touching the $$ could not be found.")
                End If

                Dim result = GetConflictResolution(renameTo, workspace.CurrentSolution, symbol, renameOptions, host)

                If expectFailure Then
                    Assert.False(result.IsSuccessful)
                    Assert.NotNull(result.ErrorMessage)
                    Return engineResult
                Else
                    Assert.Null(result.ErrorMessage)
                End If

                engineResult = New RenameEngineResult(workspace, result, renameTo, symbol)
                engineResult.AssertUnlabeledSpansRenamedAndHaveNoConflicts()
                success = True
            Finally
                If Not success Then
                    ' Something blew up, so we still own the test workspace
                    If engineResult IsNot Nothing Then
                        engineResult.Dispose()
                    Else
                        workspace.Dispose()
                    End If
                End If
            End Try

            Return engineResult
        End Function

        Private Shared Function GetConflictResolution(
                renameTo As String,
                solution As Solution,
                symbol As ISymbol,
                renameOptions As SymbolRenameOptions,
                host As RenameTestHost) As ConflictResolution

            If host = RenameTestHost.OutOfProcess_SplitCall Then
                ' This tests that each portion of rename can properly marshal to/from the OOP process. It validates
                ' features that need to call each part independently and operate on the intermediary values.

                Dim locations = Renamer.FindRenameLocationsAsync(
                    solution, symbol, renameOptions, CancellationToken.None).GetAwaiter().GetResult()

                Return locations.ResolveConflictsAsync(symbol, renameTo, nonConflictSymbolKeys:=Nothing, CodeActionOptions.DefaultProvider, CancellationToken.None).GetAwaiter().GetResult()
            Else
                ' This tests that rename properly works when the entire call is remoted to OOP and the final result is
                ' marshaled back.

                Return Renamer.RenameSymbolsAsync(
                    solution, ImmutableDictionary(Of ISymbol, (String, SymbolRenameOptions)).Empty.Add(symbol, (renameTo, renameOptions)), CodeActionOptions.DefaultProvider,
                    nonConflictSymbolKeys:=Nothing, CancellationToken.None).GetAwaiter().GetResult()
            End If
        End Function

        Friend ReadOnly Property ConflictResolution As ConflictResolution
            Get
                Return _resolution
            End Get
        End Property

        Private Sub AssertUnlabeledSpansRenamedAndHaveNoConflicts()
            For Each documentWithSpans In _workspace.Documents.Where(Function(d) Not d.IsSourceGenerated)
                Dim documentId = documentWithSpans.Id
                Dim oldSyntaxTree = _workspace.CurrentSolution.GetDocument(documentId).GetSyntaxTreeAsync().Result

                Dim nonConflictTextSpanToReplacementText As ImmutableDictionary(Of TextSpan, String) = Nothing
                If _nonConflictLocationToReplacementText.TryGetValue(documentId, nonConflictTextSpanToReplacementText) Then
                    For Each kvp In nonConflictTextSpanToReplacementText
                        Dim textSpan = kvp.Key
                        Dim replacementText = kvp.Value

                        Dim location = oldSyntaxTree.GetLocation(textSpan)
                        AssertLocationReferencedAs(location, RelatedLocationType.NoConflict)
                        AssertLocationReplacedWith(location, replacementText)
                    Next
                End If

                'Dim nonConflictTextSpansInDocument =
                'Dim selectedSpans = documentWithSpans.SelectedSpans

                'For Each span In documentWithSpans.SelectedSpans
                '    Dim location = oldSyntaxTree.GetLocation(span)

                '    AssertLocationReferencedAs(location, RelatedLocationType.NoConflict)
                '    AssertLocationReplacedWith(location, _renameTo)
                'Next
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
                    newText = newTree.GetText().ToString(newLocation)
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

            ' If we failed some other assert, we know we're going to have things left
            ' over. So let's just suppress these so we don't lose the root cause
            If Not _failedAssert Then
                If _unassertedRelatedLocations.Count > 0 Then
                    AssertEx.Fail(
                        "There were additional related locations that were unasserted:" + Environment.NewLine _
                        + String.Join(Environment.NewLine,
                            From location In _unassertedRelatedLocations
                            Let document = _workspace.CurrentSolution.GetDocument(location.DocumentId)
                            Let spanText = document.GetTextSynchronously(CancellationToken.None).ToString(location.ConflictCheckSpan)
                            Select $"{spanText} @{document.Name}[{location.ConflictCheckSpan.Start}..{location.ConflictCheckSpan.End})"))
                End If
            End If

            _workspace.Dispose()
        End Sub

        Protected Overrides Sub Finalize()
            If Not Environment.HasShutdownStarted Then
                Throw New Exception("Dispose was not called in a Rename test.")
            End If
        End Sub

        Public Sub AssertReplacementTextValidForSymbolAtTag(tagName As String)
            Try
                Dim symbol = _annotatedRenameTagToSymbolsMap(tagName)
                Assert.False(_resolution.SymbolToReplacementTextValid(symbol))
            Catch ex As XunitException
                _failedAssert = True
                Throw
            End Try
        End Sub

        Public Sub AssertReplacementTextInvalidForTheSymbolAtCaret()
            AssertReplacementTextValidForSymbolAtTag(CaretString)
        End Sub

        Public Sub AssertIsInvalidResolution()
            Assert.Null(_resolution)
        End Sub
    End Class
End Namespace
