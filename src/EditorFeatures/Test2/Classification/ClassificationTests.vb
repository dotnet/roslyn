' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Editor.Shared.Extensions
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.UnitTests
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Classification
Imports Microsoft.VisualStudio.Text.Tagging
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification
    <UseExportProvider>
    Public Class ClassificationTests
        <Fact, WorkItem("https://github.com/dotnet/roslyn/pull/66245")>
        Public Async Function TestClassificationAndHighlight1() As Task
            Using workspace = EditorTestWorkspace.Create(
                <Workspace>
                    <Project Language="C#" AssemblyName="TestAssembly" CommonReferences="true">
                        <Document>
                        using System.Text.RegularExpressions;

                        class C
                        {
                           [| Regex |]re = new Regex("()");
                        }
                        </Document>
                    </Project>
                </Workspace>)

                Dim document = workspace.CurrentSolution.Projects.Single().Documents.Single()
                Dim text = Await document.GetTextAsync()
                Dim referenceSpan = workspace.Documents.Single().SelectedSpans.Single()

                Dim spansAndHighlightSpan = Await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(
                    New DocumentSpan(document, referenceSpan),
                    ClassificationOptions.Default, CancellationToken.None)

                ' This is the classification of the line, starting at the beginning of the highlight, and going to the end of that line.
                Assert.Equal(
"(text, '<spaces>', [154..155))
(class name, 'Regex', [155..160))
(text, '<spaces>', [160..161))
(field name, 're', [161..163))
(text, '<spaces>', [163..164))
(operator, '=', [164..165))
(text, '<spaces>', [165..166))
(keyword, 'new', [166..169))
(text, '<spaces>', [169..170))
(class name, 'Regex', [170..175))
(punctuation, '(', [175..176))
(string, '""', [176..177))
(regex - grouping, '(', [177..178))
(regex - grouping, ')', [178..179))
(string, '""', [179..180))
(punctuation, ')', [180..181))
(punctuation, ';', [181..182))", String.Join(vbCrLf, spansAndHighlightSpan.ClassifiedSpans.Select(Function(s) ToTestString(text, s))))

                ' The portion of the classified spans to highlight goes from the start of the classified spans to the
                ' length of the original reference span.
                Assert.Equal(New TextSpan(0, referenceSpan.Length), spansAndHighlightSpan.HighlightSpan)
            End Using
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65926")>
        Public Async Function TestEmbeddedClassifications1() As Task
            Using workspace = EditorTestWorkspace.Create(
                <Workspace>
                    <Project Language="C#" AssemblyName="TestAssembly" CommonReferences="true">
                        <Document>
                        using System.Text.RegularExpressions;

                        class C
                        {
                            private Regex re = new Regex("()");
                        }
                        </Document>
                    </Project>
                </Workspace>)

                Dim document = workspace.CurrentSolution.Projects.Single().Documents.Single()
                Dim text = Await document.GetTextAsync()

                Dim spans = Await ClassifierHelper.GetClassifiedSpansAsync(
                    document, New TextSpan(0, text.Length), ClassificationOptions.Default, includeAdditiveSpans:=False, CancellationToken.None)

                Assert.Equal(
"(text, '<spaces>', [0..26))
(keyword, 'using', [26..31))
(text, '<spaces>', [31..32))
(namespace name, 'System', [32..38))
(operator, '.', [38..39))
(namespace name, 'Text', [39..43))
(operator, '.', [43..44))
(namespace name, 'RegularExpressions', [44..62))
(punctuation, ';', [62..63))
(text, '<spaces>', [63..91))
(keyword, 'class', [91..96))
(text, '<spaces>', [96..97))
(class name, 'C', [97..98))
(text, '<spaces>', [98..124))
(punctuation, '{', [124..125))
(text, '<spaces>', [125..155))
(keyword, 'private', [155..162))
(text, '<spaces>', [162..163))
(class name, 'Regex', [163..168))
(text, '<spaces>', [168..169))
(field name, 're', [169..171))
(text, '<spaces>', [171..172))
(operator, '=', [172..173))
(text, '<spaces>', [173..174))
(keyword, 'new', [174..177))
(text, '<spaces>', [177..178))
(class name, 'Regex', [178..183))
(punctuation, '(', [183..184))
(string, '""', [184..185))
(regex - grouping, '(', [185..186))
(regex - grouping, ')', [186..187))
(string, '""', [187..188))
(punctuation, ')', [188..189))
(punctuation, ';', [189..190))
(text, '<spaces>', [190..216))
(punctuation, '}', [216..217))", String.Join(vbCrLf, spans.Select(Function(s) ToTestString(text, s))))
            End Using
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63702")>
        Public Async Function TestEmbeddedClassifications2() As Task
            Using workspace = EditorTestWorkspace.Create(
                <Workspace>
                    <Project Language="C#" AssemblyName="TestAssembly" CommonReferences="true">
                        <Document>
                        class C
                        {
                            void B() => M("W\"X\tY\'Z");
                            void M(string s) { }
                        }
                        </Document>
                    </Project>
                </Workspace>)

                Dim document = workspace.CurrentSolution.Projects.Single().Documents.Single()
                Dim text = Await document.GetTextAsync()

                Dim spans = Await ClassifierHelper.GetClassifiedSpansAsync(
                    document, New TextSpan(0, text.Length), ClassificationOptions.Default, includeAdditiveSpans:=False, CancellationToken.None)

                Assert.Equal(
"(text, '<spaces>', [0..26))
(keyword, 'class', [26..31))
(text, '<spaces>', [31..32))
(class name, 'C', [32..33))
(text, '<spaces>', [33..59))
(punctuation, '{', [59..60))
(text, '<spaces>', [60..90))
(keyword, 'void', [90..94))
(text, '<spaces>', [94..95))
(method name, 'B', [95..96))
(punctuation, '(', [96..97))
(punctuation, ')', [97..98))
(text, '<spaces>', [98..99))
(operator, '=>', [99..101))
(text, '<spaces>', [101..102))
(method name, 'M', [102..103))
(punctuation, '(', [103..104))
(string, '""W', [104..106))
(string - escape character, '\""', [106..108))
(string, 'X', [108..109))
(string - escape character, '\t', [109..111))
(string, 'Y', [111..112))
(string - escape character, '\'', [112..114))
(string, 'Z""', [114..116))
(punctuation, ')', [116..117))
(punctuation, ';', [117..118))
(text, '<spaces>', [118..148))
(keyword, 'void', [148..152))
(text, '<spaces>', [152..153))
(method name, 'M', [153..154))
(punctuation, '(', [154..155))
(keyword, 'string', [155..161))
(text, '<spaces>', [161..162))
(parameter name, 's', [162..163))
(punctuation, ')', [163..164))
(text, '<spaces>', [164..165))
(punctuation, '{', [165..166))
(text, '<spaces>', [166..167))
(punctuation, '}', [167..168))
(text, '<spaces>', [168..194))
(punctuation, '}', [194..195))", String.Join(vbCrLf, spans.Select(Function(s) ToTestString(text, s))))
            End Using
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66507")>
        Public Async Function TestUtf8StringSuffix() As Task
            Using workspace = EditorTestWorkspace.Create(
                <Workspace>
                    <Project Language="C#" AssemblyName="TestAssembly" CommonReferences="true">
                        <Document>
                        [|var v = "goo"u8;|]
                        </Document>
                    </Project>
                </Workspace>)

                Dim document = workspace.CurrentSolution.Projects.Single().Documents.Single()
                Dim text = Await document.GetTextAsync()
                Dim referenceSpan = workspace.Documents.Single().SelectedSpans.Single()

                Dim spansAndHighlightSpan = Await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(
                    New DocumentSpan(document, referenceSpan),
                    ClassificationOptions.Default, CancellationToken.None)

                ' string classification should not overlap u8 classification.
                AssertEx.Equal(
"(keyword, 'var', [26..29))
(text, '<spaces>', [29..30))
(local name, 'v', [30..31))
(text, '<spaces>', [31..32))
(operator, '=', [32..33))
(text, '<spaces>', [33..34))
(string, '""goo""', [34..39))
(keyword, 'u8', [39..41))
(punctuation, ';', [41..42))", String.Join(vbCrLf, spansAndHighlightSpan.ClassifiedSpans.Select(Function(s) ToTestString(text, s))))
            End Using
        End Function

        Private Shared Function ToTestString(text As SourceText, span As ClassifiedSpan) As String
            Dim subText = text.ToString(span.TextSpan)
            Return $"({span.ClassificationType}, '{If(subText.Trim() = "", "<spaces>",
                subText)}', {span.TextSpan})"
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/13753")>
        Public Async Function TestSemanticClassificationWithoutSyntaxTree() As Task
            Dim workspaceDefinition =
            <Workspace>
                <Project Language="NoCompilation" AssemblyName="TestAssembly" CommonReferencesPortable="true">
                    <Document>
                        var x = {}; // e.g., TypeScript code or anything else that doesn't support compilations
                    </Document>
                </Project>
            </Workspace>

            Dim composition = EditorTestCompositions.EditorFeatures.AddParts(
                GetType(NoCompilationContentTypeDefinitions),
                GetType(NoCompilationContentTypeLanguageService),
                GetType(NoCompilationEditorClassificationService))

            Using workspace = EditorTestWorkspace.Create(workspaceDefinition, composition:=composition)
                Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)

                Dim provider = New SemanticClassificationViewTaggerProvider(
                    workspace.GetService(Of IThreadingContext),
                    workspace.GetService(Of ClassificationTypeMap),
                    workspace.GetService(Of IGlobalOptionService),
                    visibilityTracker:=Nothing,
                    listenerProvider)

                Dim buffer = workspace.Documents.First().GetTextBuffer()
                Using tagger = provider.CreateTagger(
                    workspace.Documents.First().GetTextView(),
                    buffer)

                    Using edit = buffer.CreateEdit()
                        edit.Insert(0, " ")
                        edit.Apply()
                    End Using

                    Await listenerProvider.GetWaiter(FeatureAttribute.Classification).ExpeditedWaitAsync()

                    ' Note: we don't actually care what results we get back.  We're just
                    ' verifying that we don't crash because the SemanticViewTagger ends up
                    ' calling SyntaxTree/SemanticModel code.
                    tagger.GetTags(New NormalizedSnapshotSpanCollection(
                            New SnapshotSpan(buffer.CurrentSnapshot, New Span(0, 1))))
                End Using
            End Using
        End Function

        <WpfFact>
        Public Sub TestFailOverOfMissingClassificationType()
            Dim exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider()

            Dim typeMap = exportProvider.GetExportedValue(Of ClassificationTypeMap)
            Dim formatMap = exportProvider.GetExportedValue(Of IClassificationFormatMapService).GetClassificationFormatMap("tooltip")

            Dim classifiedText = New ClassifiedText("UnknownClassificationType", "dummy")
            Dim run = classifiedText.ToRun(formatMap, typeMap)

            Assert.NotNull(run)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/13753")>
        Public Async Function TestWrongDocument() As Task
            Dim workspaceDefinition =
            <Workspace>
                <Project Language="NoCompilation" AssemblyName="NoCompilationAssembly" CommonReferencesPortable="true">
                    <Document>
                        var x = {}; // e.g., TypeScript code or anything else that doesn't support compilations
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferencesPortable="true">
                </Project>
            </Workspace>

            Dim composition = EditorTestCompositions.EditorFeatures.AddParts(
                GetType(NoCompilationContentTypeLanguageService),
                GetType(NoCompilationContentTypeDefinitions))

            Using workspace = EditorTestWorkspace.Create(workspaceDefinition, composition:=composition)
                Dim project = workspace.CurrentSolution.Projects.First(Function(p) p.Language = LanguageNames.CSharp)
                Dim classificationService = project.Services.GetService(Of IClassificationService)()

                Dim wrongDocument = workspace.CurrentSolution.Projects.First(Function(p) p.Language = "NoCompilation").Documents.First()
                Dim text = Await wrongDocument.GetTextAsync(CancellationToken.None)

                ' make sure we don't crash with wrong document
                Dim result = New SegmentedList(Of ClassifiedSpan)()
                Await classificationService.AddSyntacticClassificationsAsync(wrongDocument, New TextSpan(0, text.Length), result, CancellationToken.None)
                Await classificationService.AddSemanticClassificationsAsync(wrongDocument, New TextSpan(0, text.Length), options:=Nothing, result, CancellationToken.None)
            End Using
        End Function

        <ExportLanguageService(GetType(IClassificationService), NoCompilationConstants.LanguageName, ServiceLayer.Test), [Shared], PartNotDiscoverable>
        Private Class NoCompilationEditorClassificationService
            Implements IClassificationService

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Sub AddLexicalClassifications(text As SourceText, textSpan As TextSpan, result As SegmentedList(Of ClassifiedSpan), cancellationToken As CancellationToken) Implements IClassificationService.AddLexicalClassifications
            End Sub

            Public Sub AddSyntacticClassifications(services As SolutionServices, root As SyntaxNode, textSpans As ImmutableArray(Of TextSpan), result As SegmentedList(Of ClassifiedSpan), cancellationToken As CancellationToken) Implements IClassificationService.AddSyntacticClassifications
            End Sub

            Public Function AddSemanticClassificationsAsync(document As Document, textSpans As ImmutableArray(Of TextSpan), options As ClassificationOptions, result As SegmentedList(Of ClassifiedSpan), cancellationToken As CancellationToken) As Task Implements IClassificationService.AddSemanticClassificationsAsync
                Return Task.CompletedTask
            End Function

            Public Function AddSyntacticClassificationsAsync(document As Document, textSpans As ImmutableArray(Of TextSpan), result As SegmentedList(Of ClassifiedSpan), cancellationToken As CancellationToken) As Task Implements IClassificationService.AddSyntacticClassificationsAsync
                Return Task.CompletedTask
            End Function

            Public Function AdjustStaleClassification(text As SourceText, classifiedSpan As ClassifiedSpan) As ClassifiedSpan Implements IClassificationService.AdjustStaleClassification
            End Function

            Public Function ComputeSyntacticChangeRangeAsync(oldDocument As Document, newDocument As Document, timeout As TimeSpan, cancellationToken As CancellationToken) As ValueTask(Of TextChangeRange?) Implements IClassificationService.ComputeSyntacticChangeRangeAsync
                Return New ValueTask(Of TextChangeRange?)
            End Function

            Public Function ComputeSyntacticChangeRange(services As SolutionServices, oldRoot As SyntaxNode, newRoot As SyntaxNode, timeout As TimeSpan, cancellationToken As CancellationToken) As TextChangeRange? Implements IClassificationService.ComputeSyntacticChangeRange
                Return Nothing
            End Function

            Public Function AddEmbeddedLanguageClassificationsAsync(document As Document, textSpans As ImmutableArray(Of TextSpan), options As ClassificationOptions, result As SegmentedList(Of ClassifiedSpan), cancellationToken As CancellationToken) As Task Implements IClassificationService.AddEmbeddedLanguageClassificationsAsync
                Return Task.CompletedTask
            End Function
        End Class
    End Class
End Namespace
