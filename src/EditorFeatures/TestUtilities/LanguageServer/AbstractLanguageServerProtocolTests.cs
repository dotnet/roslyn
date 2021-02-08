// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Test;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text.Adornments;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Roslyn.Test.Utilities
{
    [UseExportProvider]
    public abstract class AbstractLanguageServerProtocolTests
    {
        // TODO: remove WPF dependency (IEditorInlineRenameService)
        private static readonly TestComposition s_composition = EditorTestCompositions.LanguageServerProtocolWpf
            .AddParts(typeof(TestLspWorkspaceRegistrationService))
            .AddParts(typeof(TestDocumentTrackingService))
            .RemoveParts(typeof(MockWorkspaceEventListenerProvider));

        [Export(typeof(ILspWorkspaceRegistrationService)), PartNotDiscoverable]
        internal class TestLspWorkspaceRegistrationService : ILspWorkspaceRegistrationService
        {
            private Workspace? _workspace;

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public TestLspWorkspaceRegistrationService()
            {
            }

            public ImmutableArray<Workspace> GetAllRegistrations()
            {
                Contract.ThrowIfNull(_workspace, "No workspace has been registered");

                return ImmutableArray.Create(_workspace);
            }

            public void Register(Workspace workspace)
            {
                Contract.ThrowIfTrue(_workspace != null);

                _workspace = workspace;
            }
        }

        private class TestSpanMapperProvider : IDocumentServiceProvider
        {
            TService IDocumentServiceProvider.GetService<TService>()
                => (TService)(object)new TestSpanMapper();
        }

        internal class TestSpanMapper : ISpanMappingService
        {
            private static readonly LinePositionSpan s_mappedLinePosition = new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 5));
            private static readonly string s_mappedFilePath = "c:\\MappedFile.cs";

            internal static readonly string GeneratedFileName = "GeneratedFile.cs";

            internal static readonly LSP.Location MappedFileLocation = new LSP.Location
            {
                Range = ProtocolConversions.LinePositionToRange(s_mappedLinePosition),
                Uri = new Uri(s_mappedFilePath)
            };

            /// <summary>
            /// LSP tests are simulating the new razor system which does support mapping import directives.
            /// </summary>
            public bool SupportsMappingImportDirectives => true;

            public Task<ImmutableArray<MappedSpanResult>> MapSpansAsync(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken)
            {
                ImmutableArray<MappedSpanResult> mappedResult = default;
                if (document.Name == GeneratedFileName)
                {
                    mappedResult = spans.Select(span => new MappedSpanResult(s_mappedFilePath, s_mappedLinePosition, new TextSpan(0, 5))).ToImmutableArray();
                }

                return Task.FromResult(mappedResult);
            }
        }

        protected class OrderLocations : Comparer<LSP.Location>
        {
            public override int Compare(LSP.Location x, LSP.Location y) => CompareLocations(x, y);
        }

        protected virtual TestComposition Composition => s_composition;

        /// <summary>
        /// Asserts two objects are equivalent by converting to JSON and ignoring whitespace.
        /// </summary>
        /// <typeparam name="T">the JSON object type.</typeparam>
        /// <param name="expected">the expected object to be converted to JSON.</param>
        /// <param name="actual">the actual object to be converted to JSON.</param>
        public static void AssertJsonEquals<T>(T expected, T actual)
        {
            var expectedStr = JsonConvert.SerializeObject(expected);
            var actualStr = JsonConvert.SerializeObject(actual);
            AssertEqualIgnoringWhitespace(expectedStr, actualStr);
        }

        protected static void AssertEqualIgnoringWhitespace(string expected, string actual)
        {
            var expectedWithoutWhitespace = Regex.Replace(expected, @"\s+", string.Empty);
            var actualWithoutWhitespace = Regex.Replace(actual, @"\s+", string.Empty);
            Assert.Equal(expectedWithoutWhitespace, actualWithoutWhitespace);
        }

        /// <summary>
        /// Assert that two location lists are equivalent.
        /// Locations are not always returned in a consistent order so they must be sorted.
        /// </summary>
        protected static void AssertLocationsEqual(IEnumerable<LSP.Location> expectedLocations, IEnumerable<LSP.Location> actualLocations)
        {
            var orderedActualLocations = actualLocations.OrderBy(CompareLocations);
            var orderedExpectedLocations = expectedLocations.OrderBy(CompareLocations);

            AssertJsonEquals(orderedExpectedLocations, orderedActualLocations);
        }

        protected static int CompareLocations(LSP.Location l1, LSP.Location l2)
        {
            var compareDocument = l1.Uri.OriginalString.CompareTo(l2.Uri.OriginalString);
            var compareRange = CompareRange(l1.Range, l2.Range);
            return compareDocument != 0 ? compareDocument : compareRange;
        }

        protected static int CompareRange(LSP.Range r1, LSP.Range r2)
        {
            var compareLine = r1.Start.Line.CompareTo(r2.Start.Line);
            var compareChar = r1.Start.Character.CompareTo(r2.Start.Character);
            return compareLine != 0 ? compareLine : compareChar;
        }

        protected static string ApplyTextEdits(LSP.TextEdit[] edits, SourceText originalMarkup)
        {
            var text = originalMarkup;
            foreach (var edit in edits)
            {
                var lines = text.Lines;
                var startPosition = ProtocolConversions.PositionToLinePosition(edit.Range.Start);
                var endPosition = ProtocolConversions.PositionToLinePosition(edit.Range.End);
                var textSpan = lines.GetTextSpan(new LinePositionSpan(startPosition, endPosition));
                text = text.Replace(textSpan, edit.NewText);
            }

            return text.ToString();
        }

        internal static LSP.SymbolInformation CreateSymbolInformation(LSP.SymbolKind kind, string name, LSP.Location location, Glyph glyph, string? containerName = null)
        {
            var info = new LSP.VSSymbolInformation()
            {
                Kind = kind,
                Name = name,
                Location = location,
                Icon = new ImageElement(glyph.GetImageId()),
            };

            if (containerName != null)
            {
                info.ContainerName = containerName;
            }

            return info;
        }

        protected static LSP.TextDocumentIdentifier CreateTextDocumentIdentifier(Uri uri, ProjectId? projectContext = null)
        {
            var documentIdentifier = new LSP.VSTextDocumentIdentifier { Uri = uri };

            if (projectContext != null)
            {
                documentIdentifier.ProjectContext =
                    new LSP.ProjectContext { Id = ProtocolConversions.ProjectIdToProjectContextId(projectContext) };
            }

            return documentIdentifier;
        }

        protected static LSP.TextDocumentPositionParams CreateTextDocumentPositionParams(LSP.Location caret, ProjectId? projectContext = null)
            => new LSP.TextDocumentPositionParams()
            {
                TextDocument = CreateTextDocumentIdentifier(caret.Uri, projectContext),
                Position = caret.Range.Start
            };

        protected static LSP.MarkupContent CreateMarkupContent(LSP.MarkupKind kind, string value)
            => new LSP.MarkupContent()
            {
                Kind = kind,
                Value = value
            };

        protected static LSP.CompletionParams CreateCompletionParams(LSP.Location caret, string triggerCharacter, LSP.CompletionTriggerKind triggerKind)
            => new LSP.CompletionParams()
            {
                TextDocument = CreateTextDocumentIdentifier(caret.Uri),
                Position = caret.Range.Start,
                Context = new LSP.CompletionContext()
                {
                    TriggerCharacter = triggerCharacter,
                    TriggerKind = triggerKind
                }
            };

        protected static LSP.VSCompletionItem CreateCompletionItem(
            string insertText,
            LSP.CompletionItemKind kind,
            string[] tags,
            LSP.CompletionParams requestParameters,
            bool preselect = false,
            ImmutableArray<char>? commitCharacters = null,
            string? sortText = null,
            int resultId = 0)
        {
            var item = new LSP.VSCompletionItem()
            {
                FilterText = insertText,
                InsertText = insertText,
                Label = insertText,
                SortText = sortText ?? insertText,
                InsertTextFormat = LSP.InsertTextFormat.Plaintext,
                Kind = kind,
                Data = JObject.FromObject(new CompletionResolveData()
                {
                    DisplayText = insertText,
                    TextDocument = requestParameters.TextDocument,
                    Position = requestParameters.Position,
                    CompletionTrigger = ProtocolConversions.LSPToRoslynCompletionTrigger(requestParameters.Context),
                    ResultId = resultId,
                }),
                Preselect = preselect
            };

            if (tags != null)
                item.Icon = tags.ToImmutableArray().GetFirstGlyph().GetImageElement();

            if (commitCharacters != null)
                item.CommitCharacters = commitCharacters.Value.Select(c => c.ToString()).ToArray();

            return item;
        }

        private protected static CodeActionResolveData CreateCodeActionResolveData(string uniqueIdentifier, LSP.Location location)
            => new CodeActionResolveData(uniqueIdentifier, location.Range, CreateTextDocumentIdentifier(location.Uri));

        /// <summary>
        /// Creates a solution with a document.
        /// </summary>
        /// <returns>the solution and the annotated ranges in the document.</returns>
        protected TestWorkspace CreateTestWorkspace(string markup, out Dictionary<string, IList<LSP.Location>> locations)
            => CreateTestWorkspace(new string[] { markup }, out locations, LanguageNames.CSharp);

        protected TestWorkspace CreateVisualBasicTestWorkspace(string markup, out Dictionary<string, IList<LSP.Location>> locations)
            => CreateTestWorkspace(new string[] { markup }, out locations, LanguageNames.VisualBasic);

        /// <summary>
        /// Create a solution with multiple documents.
        /// </summary>
        /// <returns>
        /// the solution with the documents plus a list for each document of all annotated ranges in the document.
        /// </returns>
        protected TestWorkspace CreateTestWorkspace(string[] markups, out Dictionary<string, IList<LSP.Location>> locations)
            => CreateTestWorkspace(markups, out locations, LanguageNames.CSharp);

        private TestWorkspace CreateTestWorkspace(string[] markups, out Dictionary<string, IList<LSP.Location>> locations, string languageName)
        {
            var workspace = languageName switch
            {
                LanguageNames.CSharp => TestWorkspace.CreateCSharp(markups, composition: Composition),
                LanguageNames.VisualBasic => TestWorkspace.CreateVisualBasic(markups, composition: Composition),
                _ => throw new ArgumentException($"language name {languageName} is not valid for a test workspace"),
            };

            RegisterWorkspaceForLsp(workspace);
            var solution = workspace.CurrentSolution;

            foreach (var document in workspace.Documents)
            {
                solution = solution.WithDocumentFilePath(document.Id, GetDocumentFilePathFromName(document.Name));
            }

            workspace.ChangeSolution(solution);

            locations = GetAnnotatedLocations(workspace, solution);

            return workspace;
        }

        protected TestWorkspace CreateXmlTestWorkspace(string xmlContent, out Dictionary<string, IList<LSP.Location>> locations)
        {
            var workspace = TestWorkspace.Create(xmlContent, composition: Composition);
            RegisterWorkspaceForLsp(workspace);
            locations = GetAnnotatedLocations(workspace, workspace.CurrentSolution);
            return workspace;
        }

        protected static void AddMappedDocument(Workspace workspace, string markup)
        {
            var generatedDocumentId = DocumentId.CreateNewId(workspace.CurrentSolution.ProjectIds.First());
            var version = VersionStamp.Create();
            var loader = TextLoader.From(TextAndVersion.Create(SourceText.From(markup), version, TestSpanMapper.GeneratedFileName));
            var generatedDocumentInfo = DocumentInfo.Create(generatedDocumentId, TestSpanMapper.GeneratedFileName, SpecializedCollections.EmptyReadOnlyList<string>(),
                SourceCodeKind.Regular, loader, $"C:\\{TestSpanMapper.GeneratedFileName}", isGenerated: true, designTimeOnly: false, new TestSpanMapperProvider());
            var newSolution = workspace.CurrentSolution.AddDocument(generatedDocumentInfo);
            workspace.TryApplyChanges(newSolution);
        }

        private protected static void RegisterWorkspaceForLsp(TestWorkspace workspace)
        {
            var provider = workspace.ExportProvider.GetExportedValue<ILspWorkspaceRegistrationService>();
            provider.Register(workspace);
        }

        public static Dictionary<string, IList<LSP.Location>> GetAnnotatedLocations(TestWorkspace workspace, Solution solution)
        {
            var locations = new Dictionary<string, IList<LSP.Location>>();
            foreach (var testDocument in workspace.Documents)
            {
                var document = solution.GetRequiredDocument(testDocument.Id);
                var text = document.GetTextSynchronously(CancellationToken.None);
                foreach (var (name, spans) in testDocument.AnnotatedSpans)
                {
                    var locationsForName = locations.GetValueOrDefault(name, new List<LSP.Location>());
                    locationsForName.AddRange(spans.Select(span => ConvertTextSpanWithTextToLocation(span, text, new Uri(document.FilePath))));

                    // Linked files will return duplicate annotated Locations for each document that links to the same file.
                    // Since the test output only cares about the actual file, make sure we de-dupe before returning.
                    locations[name] = locationsForName.Distinct().ToList();
                }
            }

            return locations;

            static LSP.Location ConvertTextSpanWithTextToLocation(TextSpan span, SourceText text, Uri documentUri)
            {
                var location = new LSP.Location
                {
                    Uri = documentUri,
                    Range = ProtocolConversions.TextSpanToRange(span, text),
                };

                return location;
            }
        }

        // Private protected because LanguageServerProtocol is internal
        private protected static LanguageServerProtocol GetLanguageServer(Solution solution)
        {
            var workspace = (TestWorkspace)solution.Workspace;
            return workspace.ExportProvider.GetExportedValue<LanguageServerProtocol>();
        }

        private protected static RequestExecutionQueue CreateRequestQueue(Solution solution)
        {
            var workspace = (TestWorkspace)solution.Workspace;
            var registrationService = workspace.ExportProvider.GetExportedValue<ILspWorkspaceRegistrationService>();
            return new RequestExecutionQueue(registrationService, "Tests", "TestClient");
        }

        private static string GetDocumentFilePathFromName(string documentName)
            => "C:\\" + documentName;
    }
}
