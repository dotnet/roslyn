' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.FindUsages
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Remote.Testing
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Utilities
Imports Xunit.Abstractions

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <[UseExportProvider]>
    Partial Public Class FindReferencesTests
        Private Shared ReadOnly s_composition As TestComposition = EditorTestCompositions.EditorFeatures.AddParts(
            GetType(NoCompilationContentTypeDefinitions),
            GetType(NoCompilationContentTypeLanguageService))

        Private Const DefinitionKey As String = "Definition"
        Private Const ValueUsageInfoKey As String = "ValueUsageInfo."
        Private Const TypeOrNamespaceUsageInfoKey As String = "TypeOrNamespaceUsageInfo."
        Private Const AdditionalPropertyKey As String = "AdditionalProperty."

        Private ReadOnly _outputHelper As ITestOutputHelper

        Public Sub New(outputHelper As ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        Public Enum TestKind
            API
            StreamingFeature
        End Enum

        Private Async Function TestAPIAndFeature(definition As XElement, kind As TestKind, host As TestHost, Optional searchSingleFileOnly As Boolean = False, Optional uiVisibleOnly As Boolean = False) As Task
            If kind = TestKind.API Then
                Await TestAPI(definition, host, searchSingleFileOnly, uiVisibleOnly)
            Else
                Assert.Equal(TestKind.StreamingFeature, kind)
                Await TestStreamingFeature(definition, host, searchSingleFileOnly, uiVisibleOnly)
            End If
        End Function

        Private Shared Async Function TestStreamingFeature(element As XElement, host As TestHost, Optional searchSingleFileOnly As Boolean = False, Optional uiVisibleOnly As Boolean = False) As Task
            Await TestStreamingFeature(element, searchSingleFileOnly, uiVisibleOnly, host)
        End Function

        Private Shared Async Function TestStreamingFeature(
                element As XElement,
                searchSingleFileOnly As Boolean,
                uiVisibleOnly As Boolean,
                host As TestHost) As Task
            ' We don't support testing features that only expect partial results.
            If searchSingleFileOnly OrElse uiVisibleOnly Then
                Return
            End If

            Using workspace = TestWorkspace.Create(element, composition:=s_composition.WithTestHostParts(host))
                Assert.True(workspace.Documents.Any(Function(d) d.CursorPosition.HasValue))

                For Each cursorDocument In workspace.Documents.Where(Function(d) d.CursorPosition.HasValue)
                    Dim cursorPosition = cursorDocument.CursorPosition.Value

                    Dim startDocument = If(workspace.CurrentSolution.GetDocument(cursorDocument.Id),
                                      Await workspace.CurrentSolution.GetSourceGeneratedDocumentAsync(cursorDocument.Id, CancellationToken.None))
                    Assert.NotNull(startDocument)

                    Dim findRefsService = startDocument.GetLanguageService(Of IFindUsagesService)
                    Dim context = New TestContext()
                    Await findRefsService.FindReferencesAsync(context, startDocument, cursorPosition, CancellationToken.None)

                    Dim expectedDefinitions =
                        workspace.Documents.Where(Function(d) d.AnnotatedSpans.ContainsKey(DefinitionKey) AndAlso d.AnnotatedSpans(DefinitionKey).Any()).
                                            OrderBy(Function(d) d.Name).
                                            Select(Function(d) New FileNameAndSpans(
                                                   d.Name, d.AnnotatedSpans(DefinitionKey).ToList())).ToList()

                    Dim actualDefinitions = GetFileNamesAndSpans(
                        context.Definitions.Where(AddressOf context.ShouldShow).
                                            SelectMany(Function(d) d.SourceSpans))

                    Assert.Equal(expectedDefinitions, actualDefinitions)

                    Dim expectedReferences =
                        workspace.Documents.Where(Function(d) d.SelectedSpans.Any()).
                                            OrderBy(Function(d) d.Name).
                                            Select(Function(d) New FileNameAndSpans(
                                                   d.Name, d.SelectedSpans.ToList())).ToList()

                    Dim actualReferences = GetFileNamesAndSpans(
                        context.References.Select(Function(r) r.SourceSpan))

                    Assert.Equal(expectedReferences, actualReferences)

                    Dim valueUsageInfoKeys = workspace.Documents.SelectMany(Function(d) d.AnnotatedSpans.Keys.Where(Function(key) key.StartsWith(ValueUsageInfoKey)))
                    For Each key In valueUsageInfoKeys
                        Dim expected =
                            workspace.Documents.Where(Function(d) d.AnnotatedSpans.ContainsKey(key) AndAlso d.AnnotatedSpans(key).Any()).
                                            OrderBy(Function(d) d.Name).
                                            Select(Function(d) New FileNameAndSpans(
                                                   d.Name, d.AnnotatedSpans(key).ToList())).ToList()
                        Dim valueUsageInfoField = key.Substring(ValueUsageInfoKey.Length)
                        Dim actual = GetFileNamesAndSpans(
                            context.References.Where(Function(r) r.SymbolUsageInfo.ValueUsageInfoOpt?.ToString() = valueUsageInfoField).Select(Function(r) r.SourceSpan))

                        Assert.Equal(expected, actual)
                    Next

                    Dim typeOrNamespaceUsageInfoKeys = workspace.Documents.SelectMany(Function(d) d.AnnotatedSpans.Keys.Where(Function(key) key.StartsWith(TypeOrNamespaceUsageInfoKey)))
                    For Each key In typeOrNamespaceUsageInfoKeys
                        Dim expected =
                            workspace.Documents.Where(Function(d) d.AnnotatedSpans.ContainsKey(key) AndAlso d.AnnotatedSpans(key).Any()).
                                            OrderBy(Function(d) d.Name).
                                            Select(Function(d) New FileNameAndSpans(
                                                   d.Name, d.AnnotatedSpans(key).ToList())).ToList()
                        Dim typeOrNamespaceUsageInfoFieldNames = key.Substring(TypeOrNamespaceUsageInfoKey.Length).Split(","c).Select(Function(s) s.Trim)
                        Dim actual = GetFileNamesAndSpans(
                            context.References.Where(Function(r)
                                                         Return r.SymbolUsageInfo.TypeOrNamespaceUsageInfoOpt IsNot Nothing AndAlso
                                                                r.SymbolUsageInfo.TypeOrNamespaceUsageInfoOpt.ToString().Split(","c).Select(Function(s) s.Trim).SetEquals(typeOrNamespaceUsageInfoFieldNames)
                                                     End Function).Select(Function(r) r.SourceSpan))

                        Assert.Equal(expected, actual)
                    Next

                    Dim additionalPropertiesMap = GetExpectedAdditionalPropertiesMap(workspace)
                    For Each kvp In additionalPropertiesMap
                        Dim propertyName = kvp.Key
                        For Each propertyValue In kvp.Value
                            Dim annotationKey = AdditionalPropertyKey + propertyName + "." + propertyValue
                            Dim expected =
                                workspace.Documents.Where(Function(d) d.AnnotatedSpans.ContainsKey(annotationKey) AndAlso d.AnnotatedSpans(annotationKey).Any()).
                                                OrderBy(Function(d) d.Name).
                                                Select(Function(d) New FileNameAndSpans(
                                                       d.Name, d.AnnotatedSpans(annotationKey).ToList())).ToList()
                            Dim actual = GetFileNamesAndSpans(
                                context.References.Where(Function(r)
                                                             Dim actualValue As String = Nothing
                                                             If r.AdditionalProperties.TryGetValue(propertyName, actualValue) Then
                                                                 Return actualValue = propertyValue
                                                             End If

                                                             Return propertyValue.Length = 0
                                                         End Function).Select(Function(r) r.SourceSpan))

                            Assert.Equal(expected, actual)
                        Next
                    Next
                Next
            End Using
        End Function

        Private Shared Function GetExpectedAdditionalPropertiesMap(workspace As TestWorkspace) As Dictionary(Of String, HashSet(Of String))
            Dim additionalPropertyKeys = workspace.Documents.SelectMany(Function(d) d.AnnotatedSpans.Keys.Where(Function(key) key.StartsWith(AdditionalPropertyKey)).Select(Function(key) key.Substring(AdditionalPropertyKey.Length)))
            Dim additionalPropertiesMap As New Dictionary(Of String, HashSet(Of String))
            For Each key In additionalPropertyKeys
                Dim index = key.IndexOf(".")
                Assert.True(index > 0)
                Dim propertyName = key.Substring(0, index)
                Dim propertyValue = key.Substring(index + 1)
                Dim propertyValues As HashSet(Of String) = Nothing
                If Not additionalPropertiesMap.TryGetValue(propertyName, propertyValues) Then
                    propertyValues = New HashSet(Of String)()
                    additionalPropertiesMap.Add(propertyName, propertyValues)
                End If

                propertyValues.Add(propertyValue)
            Next

            Return additionalPropertiesMap
        End Function

        Private Shared Function GetFileNamesAndSpans(items As IEnumerable(Of DocumentSpan)) As List(Of FileNameAndSpans)
            Return items.Where(Function(i) i.Document IsNot Nothing).
                         GroupBy(Function(i) i.Document).
                         OrderBy(Function(g) g.Key.Name).
                         Select(Function(g) GetFileNameAndSpans(g)).ToList()
        End Function

        Private Shared Function GetFileNameAndSpans(g As IGrouping(Of Document, DocumentSpan)) As FileNameAndSpans
            Return New FileNameAndSpans(
                g.Key.Name,
                g.Select(Function(i) i.SourceSpan).OrderBy(Function(s) s.Start).
                                                   Distinct().ToList())
        End Function

        Private Structure FileNameAndSpans
            Public ReadOnly FileName As String
            Public ReadOnly Spans As List(Of TextSpan)

            Public Sub New(fileName As String, spans As List(Of TextSpan))
                Me.FileName = fileName
                Me.Spans = spans
            End Sub

            Public Overrides Function Equals(obj As Object) As Boolean
                Return Equals(DirectCast(obj, FileNameAndSpans))
            End Function

            Public Overloads Function Equals(f As FileNameAndSpans) As Boolean
                Assert.Equal(Me.FileName, f.FileName)
                Assert.Equal(Me.Spans.Count, f.Spans.Count)

                For i = 0 To Me.Spans.Count - 1
                    Assert.Equal(Me.Spans(i), f.Spans(i))
                Next

                Return True
            End Function

        End Structure

        Friend Class TestContext
            Inherits FindUsagesContext

            Private ReadOnly gate As Object = New Object()

            Public ReadOnly Definitions As List(Of DefinitionItem) = New List(Of DefinitionItem)()
            Public ReadOnly References As List(Of SourceReferenceItem) = New List(Of SourceReferenceItem)()

            Public Sub New()
            End Sub

            Public Overrides Function GetOptionsAsync(language As String, cancellationToken As CancellationToken) As ValueTask(Of FindUsagesOptions)
                Return ValueTaskFactory.FromResult(FindUsagesOptions.Default)
            End Function

            Public Function ShouldShow(definition As DefinitionItem) As Boolean
                If References.Any(Function(r) r.Definition Is definition) Then
                    Return True
                End If

                Return definition.DisplayIfNoReferences
            End Function

            Public Overrides Function OnDefinitionFoundAsync(definition As DefinitionItem, cancellationToken As CancellationToken) As ValueTask
                SyncLock gate
                    Me.Definitions.Add(definition)
                End SyncLock

                Return Nothing
            End Function

            Public Overrides Function OnReferenceFoundAsync(reference As SourceReferenceItem, cancellationToken As CancellationToken) As ValueTask
                SyncLock gate
                    References.Add(reference)
                End SyncLock

                Return Nothing
            End Function
        End Class

        Private Async Function TestAPI(
                definition As XElement,
                host As TestHost,
                Optional searchSingleFileOnly As Boolean = False,
                Optional uiVisibleOnly As Boolean = False) As Task

            Await TestAPI(definition, host, searchSingleFileOnly, uiVisibleOnly, New FindReferencesSearchOptions(Explicit:=False))
            Await TestAPI(definition, host, searchSingleFileOnly, uiVisibleOnly, New FindReferencesSearchOptions(Explicit:=True))
        End Function

        Private Async Function TestAPI(
                definition As XElement,
                host As TestHost,
                searchSingleFileOnly As Boolean,
                uiVisibleOnly As Boolean,
                options As FindReferencesSearchOptions) As Task

            Using workspace = TestWorkspace.Create(definition, composition:=s_composition.WithTestHostParts(host))
                workspace.SetTestLogger(AddressOf _outputHelper.WriteLine)

                For Each cursorDocument In workspace.Documents.Where(Function(d) d.CursorPosition.HasValue)
                    Dim cursorPosition = cursorDocument.CursorPosition.Value

                    Dim document = If(workspace.CurrentSolution.GetDocument(cursorDocument.Id),
                                      Await workspace.CurrentSolution.GetSourceGeneratedDocumentAsync(cursorDocument.Id, CancellationToken.None))
                    Assert.NotNull(document)

                    Dim symbol = Await SymbolFinder.FindSymbolAtPositionAsync(document, cursorPosition)
                    Dim result = ImmutableArray(Of ReferencedSymbol).Empty
                    If symbol IsNot Nothing Then

                        Dim scope = If(searchSingleFileOnly, ImmutableHashSet.Create(Of Document)(document), Nothing)

                        Dim project = document.Project
                        result = result.Concat(
                            Await SymbolFinder.TestAccessor.FindReferencesAsync(
                                symbol, project.Solution,
                                progress:=Nothing, documents:=scope, options, CancellationToken.None))
                    End If

                    Dim actualDefinitions =
                        result.FilterToItemsToShow(options).
                               Where(Function(s) Not IsImplicitNamespace(s)).
                               SelectMany(Function(r) r.Definition.GetDefinitionLocationsToShow()).
                               Where(Function(loc) IsInSource(loc, uiVisibleOnly)).
                               GroupBy(Function(loc) loc.SourceTree).
                               ToDictionary(
                                    Function(g) GetFilePathAndProjectLabel(document.Project.Solution, g.Key),
                                    Function(g) g.Select(Function(loc) loc.SourceSpan).Distinct().ToList())

                    Dim documentsWithAnnotatedSpans = workspace.Documents.Where(Function(d) d.AnnotatedSpans.Any())
                    Assert.Equal(Of String)(documentsWithAnnotatedSpans.Select(Function(d) GetFilePathAndProjectLabel(d)).Order(), actualDefinitions.Keys.Order())
                    For Each doc In documentsWithAnnotatedSpans

                        Dim expected = If(doc.AnnotatedSpans.ContainsKey(DefinitionKey), doc.AnnotatedSpans(DefinitionKey), ImmutableArray(Of TextSpan).Empty).Order()
                        Dim actual = actualDefinitions(GetFilePathAndProjectLabel(doc)).Order()

                        If Not TextSpansMatch(expected, actual) Then
                            Assert.True(False, PrintSpans(expected, actual, workspace.CurrentSolution.GetDocument(doc.Id), "{|Definition:", "|}"))
                        End If
                    Next

                    Dim actualReferences = GetActualReferences(result, uiVisibleOnly, options, document)

                    Dim expectedDocuments = workspace.Documents.Where(Function(d) d.SelectedSpans.Any())
                    Assert.Equal(expectedDocuments.Select(Function(d) GetFilePathAndProjectLabel(d)).Order(), actualReferences.Keys.Order())

                    For Each doc In expectedDocuments
                        Dim expectedSpans = doc.SelectedSpans.Order()
                        Dim actualSpans = actualReferences(GetFilePathAndProjectLabel(doc)).Order()

                        Dim expectedDocument =
                            If(workspace.CurrentSolution.GetDocument(doc.Id),
                               Await workspace.CurrentSolution.GetSourceGeneratedDocumentAsync(doc.Id, CancellationToken.None))

                        AssertEx.Equal(expectedSpans, actualSpans,
                                       message:=PrintSpans(expectedSpans, actualSpans, expectedDocument, "[|", "|]", messageOnly:=True))
                    Next

                    Dim valueUsageInfoKeys = workspace.Documents.SelectMany(Function(d) d.AnnotatedSpans.Keys.Where(Function(key) key.StartsWith(ValueUsageInfoKey)))
                    For Each key In valueUsageInfoKeys
                        For Each doc In documentsWithAnnotatedSpans.Where(Function(d) d.AnnotatedSpans.ContainsKey(key))

                            Dim expectedSpans = doc.AnnotatedSpans(key).Order()

                            Dim valueUsageInfoField = key.Substring(ValueUsageInfoKey.Length)
                            actualReferences = GetActualReferences(result, uiVisibleOnly, options, document, Function(r) r.SymbolUsageInfo.ValueUsageInfoOpt?.ToString() = valueUsageInfoField)
                            Dim actualSpans = actualReferences(GetFilePathAndProjectLabel(doc)).Order()

                            If Not TextSpansMatch(expectedSpans, actualSpans) Then
                                Assert.True(False, PrintSpans(expectedSpans, actualSpans, workspace.CurrentSolution.GetDocument(doc.Id), $"{{|{key}:", "|}"))
                            End If
                        Next
                    Next

                    Dim typeOrNamespaceUsageInfoKeys = workspace.Documents.SelectMany(Function(d) d.AnnotatedSpans.Keys.Where(Function(key) key.StartsWith(TypeOrNamespaceUsageInfoKey)))
                    For Each key In typeOrNamespaceUsageInfoKeys
                        For Each doc In documentsWithAnnotatedSpans.Where(Function(d) d.AnnotatedSpans.ContainsKey(key))

                            Dim expectedSpans = doc.AnnotatedSpans(key).Order()

                            Dim typeOrNamespaceUsageInfoFieldNames = key.Substring(TypeOrNamespaceUsageInfoKey.Length).Split(","c).Select(Function(s) s.Trim)
                            actualReferences = GetActualReferences(result, uiVisibleOnly, options, document, Function(r)
                                                                                                                 Return r.SymbolUsageInfo.TypeOrNamespaceUsageInfoOpt IsNot Nothing AndAlso
                                                                                                                                   r.SymbolUsageInfo.TypeOrNamespaceUsageInfoOpt.ToString().Split(","c).Select(Function(s) s.Trim).SetEquals(typeOrNamespaceUsageInfoFieldNames)
                                                                                                             End Function)
                            Dim actualSpans = actualReferences(GetFilePathAndProjectLabel(doc)).Order()

                            If Not TextSpansMatch(expectedSpans, actualSpans) Then
                                Assert.True(False, PrintSpans(expectedSpans, actualSpans, workspace.CurrentSolution.GetDocument(doc.Id), $"{{|{key}:", "|}"))
                            End If
                        Next
                    Next

                    Dim additionalPropertiesMap = GetExpectedAdditionalPropertiesMap(workspace)
                    For Each kvp In additionalPropertiesMap
                        Dim propertyName = kvp.Key
                        For Each propertyValue In kvp.Value
                            Dim annotationKey = AdditionalPropertyKey + propertyName + "." + propertyValue
                            For Each doc In documentsWithAnnotatedSpans.Where(Function(d) d.AnnotatedSpans.ContainsKey(annotationKey))

                                Dim expectedSpans = doc.AnnotatedSpans(annotationKey).Order()

                                actualReferences = GetActualReferences(result, uiVisibleOnly, options, document, Function(r)
                                                                                                                     Dim actualValue As String = Nothing
                                                                                                                     If r.AdditionalProperties.TryGetValue(propertyName, actualValue) Then
                                                                                                                         Return actualValue = propertyValue
                                                                                                                     End If

                                                                                                                     Return propertyValue.Length = 0
                                                                                                                 End Function)
                                Dim actualSpans = actualReferences(GetFilePathAndProjectLabel(doc)).Order()

                                If Not TextSpansMatch(expectedSpans, actualSpans) Then
                                    Assert.True(False, PrintSpans(expectedSpans, actualSpans, workspace.CurrentSolution.GetDocument(doc.Id), $"{{|{annotationKey}:", "|}"))
                                End If
                            Next
                        Next
                    Next
                Next
            End Using
        End Function

        Private Shared Function GetActualReferences(result As ImmutableArray(Of ReferencedSymbol),
                                                    uiVisibleOnly As Boolean,
                                                    options As FindReferencesSearchOptions,
                                                    document As Document,
                                                    Optional locationFilterOpt As Func(Of ReferenceLocation, Boolean) = Nothing) As Dictionary(Of String, List(Of TextSpan))
            Dim referenceLocations = result.FilterToItemsToShow(options).SelectMany(Function(r) r.Locations)
            If locationFilterOpt IsNot Nothing Then
                referenceLocations = referenceLocations.Where(locationFilterOpt)
            End If

            Return referenceLocations.
                       Select(Function(loc) loc.Location).
                       Where(Function(loc) IsInSource(loc, uiVisibleOnly)).
                       Distinct().
                       GroupBy(Function(loc) loc.SourceTree).
                       ToDictionary(
                           Function(g) GetFilePathAndProjectLabel(document.Project.Solution, g.Key),
                           Function(g) g.Select(Function(loc) loc.SourceSpan).Distinct().ToList())
        End Function

        Private Shared Function PrintSpans(expected As IOrderedEnumerable(Of TextSpan), actual As IOrderedEnumerable(Of TextSpan), doc As Document, prefix As String, suffix As String, Optional messageOnly As Boolean = False) As String
            Debug.Assert(expected IsNot Nothing)
            Debug.Assert(actual IsNot Nothing)

            Dim instance = PooledStringBuilder.GetInstance()
            Dim builder = instance.Builder

            builder.AppendLine()
            If Not messageOnly Then
                builder.AppendLine($"Expected: {String.Join(", ", expected.Select(Function(e) e.ToString()))}")
                builder.AppendLine($"Actual: {String.Join(", ", actual.Select(Function(a) a.ToString()))}")
            End If

            Dim text As SourceText = Nothing
            doc.TryGetText(text)
            Dim position = 0

            For Each span In actual
                builder.Append(text.GetSubText(New TextSpan(position, span.Start - position)))
                builder.Append(prefix)
                builder.Append(text.GetSubText(span))
                builder.Append(suffix)
                position = span.End
            Next

            builder.Append(text.GetSubText(New TextSpan(position, text.Length - position)))

            Return instance.ToStringAndFree()
        End Function

        Private Shared Function TextSpansMatch(expected As IOrderedEnumerable(Of TextSpan), actual As IOrderedEnumerable(Of TextSpan)) As Boolean
            Debug.Assert(expected IsNot Nothing)
            Debug.Assert(actual IsNot Nothing)

            Dim enumeratorExpected As IEnumerator(Of TextSpan) = Nothing
            Dim enumeratorActual As IEnumerator(Of TextSpan) = Nothing
            Try
                enumeratorExpected = expected.GetEnumerator()
                enumeratorActual = actual.GetEnumerator()

                While True
                    Dim hasNextExpected = enumeratorExpected.MoveNext()
                    Dim hasNextActual = enumeratorActual.MoveNext()

                    If Not hasNextExpected OrElse Not hasNextActual Then
                        Return hasNextExpected = hasNextActual
                    End If

                    If Not enumeratorExpected.Current.Equals(enumeratorActual.Current) Then
                        Return False
                    End If
                End While

            Finally
                Dim asDisposable = TryCast(enumeratorExpected, IDisposable)
                If asDisposable IsNot Nothing Then
                    asDisposable.Dispose()
                End If

                asDisposable = TryCast(enumeratorActual, IDisposable)
                If asDisposable IsNot Nothing Then
                    asDisposable.Dispose()
                End If
            End Try

            Return True
        End Function

        Private Shared Function IsImplicitNamespace(referencedSymbol As ReferencedSymbol) As Boolean
            Return referencedSymbol.Definition.IsImplicitlyDeclared AndAlso
                   referencedSymbol.Definition.Kind = SymbolKind.Namespace
        End Function

        Private Shared Function IsInSource(loc As Location, uiVisibleOnly As Boolean) As Boolean
            If uiVisibleOnly Then
                Return loc.IsInSource AndAlso Not loc.SourceTree.IsHiddenPosition(loc.SourceSpan.Start)
            Else
                Return loc.IsInSource
            End If
        End Function

        Private Shared Function GetFilePathAndProjectLabel(solution As Solution, syntaxTree As SyntaxTree) As String
            Dim document = solution.GetDocument(syntaxTree)
            Return GetFilePathAndProjectLabel(document)
        End Function

        Private Shared Function GetFilePathAndProjectLabel(document As Document) As String
            Return $"{document.Project.Name}: {document.FilePath}"
        End Function

        Private Shared Function GetFilePathAndProjectLabel(hostDocument As TestHostDocument) As String
            Return $"{hostDocument.Project.Name}: {hostDocument.FilePath}"
        End Function
    End Class
End Namespace
