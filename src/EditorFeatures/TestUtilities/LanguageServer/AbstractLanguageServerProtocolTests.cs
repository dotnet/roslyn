﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
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

        protected static LSP.CompletionParams CreateCompletionParams(
            LSP.Location caret,
            LSP.VSCompletionInvokeKind invokeKind,
            string triggerCharacter,
            LSP.CompletionTriggerKind triggerKind)
            => new LSP.CompletionParams()
            {
                TextDocument = CreateTextDocumentIdentifier(caret.Uri),
                Position = caret.Range.Start,
                Context = new LSP.VSCompletionContext()
                {
                    InvokeKind = invokeKind,
                    TriggerCharacter = triggerCharacter,
                    TriggerKind = triggerKind,
                }
            };

        protected static async Task<LSP.VSCompletionItem> CreateCompletionItemAsync(
            string label,
            LSP.CompletionItemKind kind,
            string[] tags,
            LSP.CompletionParams request,
            Document document,
            bool preselect = false,
            ImmutableArray<char>? commitCharacters = null,
            LSP.TextEdit? textEdit = null,
            string? insertText = null,
            string? sortText = null,
            string? filterText = null,
            string? displayText = null,
            int resultId = 0)
        {
            var position = await document.GetPositionFromLinePositionAsync(
                ProtocolConversions.PositionToLinePosition(request.Position), CancellationToken.None).ConfigureAwait(false);
            var completionTrigger = await ProtocolConversions.LSPToRoslynCompletionTriggerAsync(
                request.Context, document, position, CancellationToken.None).ConfigureAwait(false);

            var item = new LSP.VSCompletionItem()
            {
                TextEdit = textEdit,
                InsertText = insertText,
                FilterText = filterText ?? label,
                Label = label,
                SortText = sortText ?? label,
                InsertTextFormat = LSP.InsertTextFormat.Plaintext,
                Kind = kind,
                Data = JObject.FromObject(new CompletionResolveData()
                {
                    DisplayText = displayText ?? label,
                    TextDocument = request.TextDocument,
                    Position = request.Position,
                    CompletionTrigger = completionTrigger,
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

        protected static LSP.TextEdit GenerateTextEdit(string newText, int startLine, int startChar, int endLine, int endChar)
            => new LSP.TextEdit
            {
                NewText = newText,
                Range = new LSP.Range
                {
                    Start = new LSP.Position { Line = startLine, Character = startChar },
                    End = new LSP.Position { Line = endLine, Character = endChar }
                }
            };

        private protected static CodeActionResolveData CreateCodeActionResolveData(string uniqueIdentifier, LSP.Location location)
            => new CodeActionResolveData(uniqueIdentifier, location.Range, CreateTextDocumentIdentifier(location.Uri));

        /// <summary>
        /// Creates an LSP server backed by a workspace instance with a solution containing the markup.
        /// </summary>
        protected TestLspServer CreateTestLspServer(string markup, out Dictionary<string, IList<LSP.Location>> locations)
            => CreateTestLspServer(new string[] { markup }, out locations, LanguageNames.CSharp);

        protected TestLspServer CreateVisualBasicTestLspServer(string markup, out Dictionary<string, IList<LSP.Location>> locations)
            => CreateTestLspServer(new string[] { markup }, out locations, LanguageNames.VisualBasic);

        /// <summary>
        /// Creates an LSP server backed by a workspace instance with a solution containing the specified documents.
        /// </summary>
        protected TestLspServer CreateTestLspServer(string[] markups, out Dictionary<string, IList<LSP.Location>> locations)
            => CreateTestLspServer(markups, out locations, LanguageNames.CSharp);

        private TestLspServer CreateTestLspServer(string[] markups, out Dictionary<string, IList<LSP.Location>> locations, string languageName)
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

            return new TestLspServer(workspace);
        }

        protected TestLspServer CreateXmlTestLspServer(string xmlContent, out Dictionary<string, IList<LSP.Location>> locations)
        {
            var workspace = TestWorkspace.Create(xmlContent, composition: Composition);
            RegisterWorkspaceForLsp(workspace);
            locations = GetAnnotatedLocations(workspace, workspace.CurrentSolution);
            return new TestLspServer(workspace);
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

        private static RequestDispatcher CreateRequestDispatcher(TestWorkspace workspace)
        {
            var factory = workspace.ExportProvider.GetExportedValue<CSharpVisualBasicRequestDispatcherFactory>();
            return factory.CreateRequestDispatcher();
        }

        private static RequestExecutionQueue CreateRequestQueue(TestWorkspace workspace)
        {
            var registrationService = workspace.ExportProvider.GetExportedValue<ILspWorkspaceRegistrationService>();
            return new RequestExecutionQueue(NoOpLspLogger.Instance, registrationService, serverName: "Tests", "TestClient");
        }

        private static string GetDocumentFilePathFromName(string documentName)
            => "C:\\" + documentName;

        public sealed class TestLspServer : IDisposable
        {
            public readonly TestWorkspace TestWorkspace;
            private readonly RequestDispatcher _requestDispatcher;
            private readonly RequestExecutionQueue _executionQueue;

            internal TestLspServer(TestWorkspace testWorkspace)
            {
                TestWorkspace = testWorkspace;
                _requestDispatcher = CreateRequestDispatcher(testWorkspace);
                _executionQueue = CreateRequestQueue(testWorkspace);
            }

            public Task<ResponseType> ExecuteRequestAsync<RequestType, ResponseType>(string methodName, RequestType request, LSP.ClientCapabilities clientCapabilities,
                string? clientName, CancellationToken cancellationToken) where RequestType : class
            {
                return _requestDispatcher.ExecuteRequestAsync<RequestType, ResponseType>(
                    _executionQueue, methodName, request, clientCapabilities, clientName, cancellationToken);
            }

            public Solution GetCurrentSolution() => TestWorkspace.CurrentSolution;

            internal RequestExecutionQueue.TestAccessor GetQueueAccessor() => _executionQueue.GetTestAccessor();

            internal RequestDispatcher.TestAccessor GetDispatcherAccessor() => _requestDispatcher.GetTestAccessor();

            public void Dispose()
            {
                TestWorkspace.Dispose();
                _executionQueue.Shutdown();
            }
        }
    }
}
