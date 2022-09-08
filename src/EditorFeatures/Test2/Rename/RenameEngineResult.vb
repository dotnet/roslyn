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
Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename
    ''' <summary>
    ''' A class that holds the result of a rename engine call, and asserts
    ''' various things about it. This is used in the tests for the rename engine
    ''' to make asserting that certain spans were converted to what they should be.
    ''' </summary>
    Friend Class RenameEngineResult
        Implements IDisposable
        Private Const CaretString = "$$"
        Private Const SelectionSpanString = "[||]"

        Private ReadOnly _workspace As TestWorkspace
        Private ReadOnly _resolution As ConflictResolution

        ''' <summary>
        ''' The list of related locations that haven't been asserted about yet. Items are
        ''' removed from here when they are asserted on, so the set should be empty once we're
        ''' done.
        ''' </summary>
        Private ReadOnly _unassertedRelatedLocations As HashSet(Of RelatedLocation)
        Private ReadOnly _nonConflictLocationToReplacementText As ImmutableDictionary(Of DocumentId, ImmutableDictionary(Of TextSpan, String))
        Private ReadOnly _annotatedRenameTagToSymbolsMap As ImmutableDictionary(Of String, ISymbol)
        Private ReadOnly _assertedLocations As Dictionary(Of DocumentId, Dictionary(Of String, List(Of TextSpan)))

        Private _failedAssert As Boolean

        Private Sub New(
                workspace As TestWorkspace,
                resolution As ConflictResolution,
                nonConflictLocationToReplacementText As ImmutableDictionary(Of DocumentId, ImmutableDictionary(Of TextSpan, String)),
                annotatedTagToSymbolsMap As ImmutableDictionary(Of String, ISymbol))
            _workspace = workspace
            _resolution = resolution
            _unassertedRelatedLocations = New HashSet(Of RelatedLocation)(resolution.RelatedLocations)
            _nonConflictLocationToReplacementText = nonConflictLocationToReplacementText
            _annotatedRenameTagToSymbolsMap = annotatedTagToSymbolsMap
            _assertedLocations = New Dictionary(Of DocumentId, Dictionary(Of String, List(Of TextSpan)))()
        End Sub

        Private Shared Function CreateTestWorkspace(helper As ITestOutputHelper,
            workspaceXml As XElement,
            Host As RenameTestHost,
            Optional sourceGenerator As ISourceGenerator = Nothing) As TestWorkspace
            Dim composition = EditorTestCompositions.EditorFeatures.AddParts(GetType(NoCompilationContentTypeLanguageService), GetType(NoCompilationContentTypeDefinitions))
            If Host = RenameTestHost.OutOfProcess_SingleCall OrElse Host = RenameTestHost.OutOfProcess_SplitCall Then
                composition = composition.WithTestHostParts(TestHost.OutOfProcess)
            End If

            Dim workspace = TestWorkspace.CreateWorkspace(workspaceXml, composition:=composition)
            workspace.SetTestLogger(AddressOf helper.WriteLine)

            If sourceGenerator IsNot Nothing Then
                workspace.OnAnalyzerReferenceAdded(workspace.CurrentSolution.ProjectIds.Single(), New TestGeneratorReference(sourceGenerator))
            End If
            Return workspace
        End Function

        Private Shared Async Function GetTagToSymbolMapAsync(
            workspace As TestWorkspace,
            renameSymbolTagOptions As Dictionary(Of String, (replacementText As String, RenameOptions As SymbolRenameOptions))) As Task(Of ImmutableDictionary(Of String, ISymbol))
            Dim builder = New Dictionary(Of String, ISymbol)()

            For Each testDocument In workspace.Documents
                Dim document = workspace.CurrentSolution.GetRequiredDocument(testDocument.Id)
                For Each annotatedSpan In testDocument.AnnotatedSpans
                    Dim tag = annotatedSpan.Key
                    Dim spans = annotatedSpan.Value
                    Dim replacementTextAndOptions As (replacementText As String, renameOptions As SymbolRenameOptions) = Nothing
                    If renameSymbolTagOptions.TryGetValue(tag, replacementTextAndOptions) Then
                        For Each span In spans
                            Dim symbol = Await RenameUtilities.TryGetRenamableSymbolAsync(document, span.Start, CancellationToken.None).ConfigureAwait(False)
                            builder(tag) = symbol
                        Next
                    End If
                Next
            Next

            Return builder.ToImmutableDictionary()
        End Function

        Private Shared Async Function GetRenamedSymbolInfoMapAsync(
            workspace As TestWorkspace,
            renameSymbolTagOptions As Dictionary(Of String, (replacementText As String, renameOptions As SymbolRenameOptions))) As Task(Of ImmutableDictionary(Of ISymbol, (replacementText As String, renameOptions As SymbolRenameOptions)))
            Dim builder = New Dictionary(Of ISymbol, (String, SymbolRenameOptions))()

            For Each testDocument In workspace.Documents
                Dim document = workspace.CurrentSolution.GetRequiredDocument(testDocument.Id)
                For Each annotatedSpan In testDocument.AnnotatedSpans
                    Dim tag = annotatedSpan.Key
                    Dim spans = annotatedSpan.Value
                    Dim replacementTextAndOptions As (replacementText As String, renameOptions As SymbolRenameOptions) = Nothing
                    If renameSymbolTagOptions.TryGetValue(tag, replacementTextAndOptions) Then
                        For Each span In spans
                            Dim symbol = Await RenameUtilities.TryGetRenamableSymbolAsync(document, span.Start, CancellationToken.None).ConfigureAwait(False)
                            builder(symbol) = replacementTextAndOptions
                        Next
                    End If
                Next
            Next
            Return builder.ToImmutableDictionary()
        End Function

        Public Shared Async Function CreateForRenamingMultipleSymbolsAsync(
                helper As ITestOutputHelper,
                workspaceXml As XElement,
                inProcess As Boolean,
                renameSymbolTagToReplacementStringAndOptions As Dictionary(Of String, (replacementText As String, renameOptions As SymbolRenameOptions)),
                nonConflictLocationTagToReplacementText As Dictionary(Of String, String),
                Optional expectFailure As Boolean = False,
                Optional sourceGenerator As ISourceGenerator = Nothing) As Task(Of RenameEngineResult)

            Dim success = False
            Dim engineResult As RenameEngineResult = Nothing
            Dim workspace = CreateTestWorkspace(helper, workspaceXml, If(inProcess, RenameTestHost.InProcess, RenameTestHost.OutOfProcess_SingleCall), sourceGenerator)
            Try
                Dim symbolToRenameInfo = Await GetRenamedSymbolInfoMapAsync(workspace, renameSymbolTagToReplacementStringAndOptions).ConfigureAwait(False)
                Dim tagToSymbol = Await GetTagToSymbolMapAsync(workspace, renameSymbolTagToReplacementStringAndOptions).ConfigureAwait(False)
                Dim nonConflictLocationsToReplacementTextMapBuilder = New Dictionary(Of DocumentId, Dictionary(Of TextSpan, String))
                AddNonConflictRenameLocationForAnnotatedLocations(
                    workspace,
                    nonConflictLocationTagToReplacementText,
                    nonConflictLocationsToReplacementTextMapBuilder)

                Dim nonConflictLocationsToReplacementTextMap = nonConflictLocationsToReplacementTextMapBuilder.ToImmutableDictionary(
                    Function(pair) pair.Key,
                    Function(pair) pair.Value.ToImmutableDictionary())

                Dim solution = workspace.CurrentSolution
                Dim conflictResolution = Await Renamer.RenameSymbolsAsync(
                    solution,
                    symbolToRenameInfo,
                    CodeActionOptions.DefaultProvider,
                    ImmutableArray(Of SymbolKey).Empty,
                    CancellationToken.None).ConfigureAwait(False)

                If expectFailure Then
                    Assert.False(conflictResolution.IsSuccessful)
                    Assert.NotNull(conflictResolution.ErrorMessage)
                Else
                    Assert.True(conflictResolution.IsSuccessful)
                End If

                Dim assertedTags = tagToSymbol.Keys.Union(nonConflictLocationTagToReplacementText.Keys).ToHashSet()
                engineResult = New RenameEngineResult(
                    workspace,
                    conflictResolution,
                    nonConflictLocationsToReplacementTextMap,
                    tagToSymbol)
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
                Dim renameTagToSymbolMap = ImmutableDictionary(Of String, ISymbol).Empty.Add(CaretString, symbol)
                Dim symbolToReplacementTextMap = ImmutableDictionary(Of ISymbol, String).Empty.Add(symbol, renameTo)
                Dim nonConflictLocationsToReplacementTextMapBuilder = New Dictionary(Of DocumentId, Dictionary(Of TextSpan, String))
                AddNonConflictRenameLocationForSelectionSpans(
                    workspace,
                    renameTo,
                    nonConflictLocationsToReplacementTextMapBuilder)

                Dim nonConflictLocationsToReplacementTextMap = nonConflictLocationsToReplacementTextMapBuilder.ToImmutableDictionary(
                    Function(pair) pair.Key,
                    Function(pair) pair.Value.ToImmutableDictionary())

                Dim result = GetConflictResolution(renameTo, workspace.CurrentSolution, symbol, renameOptions, host)

                If expectFailure Then
                    Assert.False(result.IsSuccessful)
                    Assert.NotNull(result.ErrorMessage)
                    Return engineResult
                Else
                    Assert.Null(result.ErrorMessage)
                End If

                engineResult = New RenameEngineResult(workspace, result, nonConflictLocationsToReplacementTextMap, renameTagToSymbolMap)
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

        Private Shared Sub AddNonConflictRenameLocationForSelectionSpans(
                workspace As TestWorkspace,
                replacementText As String,
                builder As Dictionary(Of DocumentId, Dictionary(Of TextSpan, String)))
            Dim documentsWithSpans = workspace.Documents.Where(Function(doc) Not doc.IsSourceGenerated AndAlso doc.SelectedSpans.Count > 0)
            For Each doc In documentsWithSpans
                Dim selectedSpans = doc.SelectedSpans
                Dim existingTextSpanToReplacementText As Dictionary(Of TextSpan, String) = Nothing
                If Not builder.TryGetValue(doc.Id, existingTextSpanToReplacementText) Then
                    existingTextSpanToReplacementText = New Dictionary(Of TextSpan, String)
                End If

                For Each span In selectedSpans
                    existingTextSpanToReplacementText(span) = replacementText
                Next
                builder(doc.Id) = existingTextSpanToReplacementText
            Next
        End Sub

        Private Shared Sub AddNonConflictRenameLocationForAnnotatedLocations(
                workspace As TestWorkspace,
                nonConflictLocationTagToReplacementText As Dictionary(Of String, String),
                builder As Dictionary(Of DocumentId, Dictionary(Of TextSpan, String)))
            Dim documentsWithSpans = workspace.Documents.Where(Function(doc) Not doc.IsSourceGenerated AndAlso doc.AnnotatedSpans.Count > 0)
            For Each doc In documentsWithSpans
                Dim annotatedSpans = doc.AnnotatedSpans

                For Each kvp In annotatedSpans
                    Dim tag = kvp.Key
                    Dim spans = kvp.Value
                    Dim replacementText As String = Nothing
                    If nonConflictLocationTagToReplacementText.TryGetValue(tag, replacementText) Then
                        Dim existingTextSpanToReplacementText As Dictionary(Of TextSpan, String) = Nothing
                        If Not builder.TryGetValue(doc.Id, existingTextSpanToReplacementText) Then
                            existingTextSpanToReplacementText = New Dictionary(Of TextSpan, String)
                        End If

                        For Each span In spans
                            existingTextSpanToReplacementText(span) = replacementText
                        Next

                        builder(doc.Id) = existingTextSpanToReplacementText
                    End If
                Next
            Next
        End Sub

        Private Sub AssertLabels(label As String)
            Dim labels = GetLabeledLocations(label)
        End Sub

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

                AssertAllTaggedLocationsAreAsserted()
            End If
            _workspace.Dispose()
        End Sub

        Private Sub AssertAllTaggedLocationsAreAsserted()
            Dim allLocationsShouldBeAsserted = New Dictionary(Of DocumentId, Dictionary(Of String, List(Of TextSpan)))
            Dim allTaggedLocations = _workspace.Documents.ToDictionary(Function(doc) doc.Id, Function(doc) doc.AnnotatedSpans.ToDictionary(Function(pair) pair.Key, Function(pair) pair.Value.ToList()))
            MergeTaggedLocationDictionary(allTaggedLocations, allLocationsShouldBeAsserted)

            Dim allSelectionLocations = _workspace.Documents.ToDictionary(
                Function(doc) doc.Id,
                Function(doc)
                    Dim dictionary = New Dictionary(Of String, List(Of TextSpan))
                    dictionary(SelectionSpanString) = doc.SelectedSpans.ToList()
                    Return dictionary
                End Function)
            MergeTaggedLocationDictionary(allTaggedLocations, allSelectionLocations)

            Dim allCaretLocations = _workspace.Documents.Where(Function(doc) doc.CursorPosition IsNot Nothing).ToDictionary(
                    Function(doc) doc.Id,
                    Function(doc)
                        Dim dictionary = New Dictionary(Of String, List(Of TextSpan))
                        dictionary(CaretString) = New List(Of TextSpan) From {New TextSpan(doc.CursorPosition.Value, 0)}
                        Return dictionary
                    End Function)
            MergeTaggedLocationDictionary(allTaggedLocations, allCaretLocations)

            AssertEx.SetEqual(_assertedLocations.Keys, allTaggedLocations.Keys)
            For Each documentToTaggedSpanPair In _assertedLocations
                Dim documentId = documentToTaggedSpanPair.Key
                Dim assertedSpanInDocument = documentToTaggedSpanPair.Value
                Dim allTaggedSpansInDocument = allLocationsShouldBeAsserted(documentId)
                AssertEx.SetEqual(allTaggedSpansInDocument.Keys, assertedSpanInDocument.Keys)
                For Each tagToSpans In assertedSpanInDocument
                    Dim tag = tagToSpans.Key
                    Dim assertedSpans = tagToSpans.Value.OrderBy(Function(span) span)
                    Dim taggedSpans = allTaggedSpansInDocument(tag).OrderBy(Function(span) span)
                    Assert.Equal(taggedSpans, assertedSpans)
                Next
            Next
        End Sub

        Private Shared Sub MergeTaggedLocationDictionary(dict1 As Dictionary(Of DocumentId, Dictionary(Of String, List(Of TextSpan))), builder As Dictionary(Of DocumentId, Dictionary(Of String, List(Of TextSpan))))
            For Each documentIdAndTagLocationsPair In dict1
                Dim documentId = documentIdAndTagLocationsPair.Key
                Dim tagToSpan = documentIdAndTagLocationsPair.Value
                If Not builder.ContainsKey(documentId) Then
                    builder(documentId) = New Dictionary(Of String, List(Of TextSpan))()
                End If

                Dim existingTagToSpan = builder(documentId)
                For Each tagAndSpanPair In tagToSpan
                    Dim tag = tagAndSpanPair.Key
                    Dim spans = tagAndSpanPair.Value
                    If Not existingTagToSpan.ContainsKey(tag) Then
                        existingTagToSpan(tag) = New List(Of TextSpan)()
                    End If
                    existingTagToSpan(tag).AddRange(spans)
                Next
            Next
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
