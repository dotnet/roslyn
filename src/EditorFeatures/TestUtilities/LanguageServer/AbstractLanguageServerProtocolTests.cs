// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Test;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.LanguageServer.UnitTests;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Nerdbank.Streams;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using StreamJsonRpc;
using Xunit;
using Xunit.Abstractions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Roslyn.Test.Utilities
{
    [UseExportProvider]
    public abstract partial class AbstractLanguageServerProtocolTests
    {
        private protected readonly ILspServiceLogger TestOutputLspLogger;
        protected AbstractLanguageServerProtocolTests(ITestOutputHelper? testOutputHelper)
        {
            TestOutputLspLogger = testOutputHelper != null ? new TestOutputLspLogger(testOutputHelper) : NoOpLspLogger.Instance;
        }

        private static readonly TestComposition s_composition = EditorTestCompositions.LanguageServerProtocolEditorFeatures
            .AddParts(typeof(TestDocumentTrackingService))
            .AddParts(typeof(TestWorkspaceRegistrationService));

        private class TestSpanMapperProvider : IDocumentServiceProvider
        {
            TService? IDocumentServiceProvider.GetService<TService>() where TService : class
                => typeof(TService) == typeof(ISpanMappingService) ? (TService)(object)new TestSpanMapper() : null;
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

            public Task<ImmutableArray<(string mappedFilePath, TextChange mappedTextChange)>> GetMappedTextChangesAsync(
                Document oldDocument,
                Document newDocument,
                CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        protected class OrderLocations : Comparer<LSP.Location>
        {
            public override int Compare(LSP.Location x, LSP.Location y) => CompareLocations(x, y);
        }

        protected virtual TestComposition Composition => s_composition;

        private protected virtual TestAnalyzerReferenceByLanguage CreateTestAnalyzersReference()
            => new(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap());

        protected static LSP.ClientCapabilities CapabilitiesWithVSExtensions => new LSP.VSInternalClientCapabilities { SupportsVisualStudioExtensions = true };

        protected static LSP.ClientCapabilities GetCapabilities(bool isVS)
            => isVS ? CapabilitiesWithVSExtensions : new LSP.ClientCapabilities();

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
            var imageId = glyph.GetImageId();

            var info = new LSP.VSSymbolInformation()
            {
                Kind = kind,
                Name = name,
                Location = location,
                Icon = new LSP.VSImageId { Guid = imageId.Guid, Id = imageId.Id },
            };

            if (containerName != null)
                info.ContainerName = containerName;

            return info;
        }

        protected static LSP.TextDocumentIdentifier CreateTextDocumentIdentifier(Uri uri, ProjectId? projectContext = null)
        {
            var documentIdentifier = new LSP.VSTextDocumentIdentifier { Uri = uri };

            if (projectContext != null)
            {
                documentIdentifier.ProjectContext =
                    new LSP.VSProjectContext { Id = ProtocolConversions.ProjectIdToProjectContextId(projectContext), Label = projectContext.DebugName!, Kind = LSP.VSProjectKind.CSharp };
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
            LSP.VSInternalCompletionInvokeKind invokeKind,
            string triggerCharacter,
            LSP.CompletionTriggerKind triggerKind)
            => new LSP.CompletionParams()
            {
                TextDocument = CreateTextDocumentIdentifier(caret.Uri),
                Position = caret.Range.Start,
                Context = new LSP.VSInternalCompletionContext()
                {
                    InvokeKind = invokeKind,
                    TriggerCharacter = triggerCharacter,
                    TriggerKind = triggerKind,
                }
            };

        protected static async Task<LSP.VSInternalCompletionItem> CreateCompletionItemAsync(
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
            long resultId = 0,
            bool vsResolveTextEditOnCommit = false)
        {
            var position = await document.GetPositionFromLinePositionAsync(
                ProtocolConversions.PositionToLinePosition(request.Position), CancellationToken.None).ConfigureAwait(false);
            var completionTrigger = await ProtocolConversions.LSPToRoslynCompletionTriggerAsync(
                request.Context, document, position, CancellationToken.None).ConfigureAwait(false);

            var item = new LSP.VSInternalCompletionItem()
            {
                TextEdit = textEdit,
                InsertText = insertText,
                FilterText = filterText,
                Label = label,
                SortText = sortText,
                InsertTextFormat = LSP.InsertTextFormat.Plaintext,
                Kind = kind,
                Data = JObject.FromObject(new CompletionResolveData()
                {
                    ResultId = resultId,
                }),
                Preselect = preselect,
                VsResolveTextEditOnCommit = vsResolveTextEditOnCommit
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

        private protected static CodeActionResolveData CreateCodeActionResolveData(string uniqueIdentifier, LSP.Location location, IEnumerable<string>? customTags = null)
            => new(uniqueIdentifier, customTags.ToImmutableArrayOrEmpty(), location.Range, CreateTextDocumentIdentifier(location.Uri));

        private protected Task<TestLspServer> CreateTestLspServerAsync(string markup, bool mutatingLspWorkspace, LSP.ClientCapabilities clientCapabilities, bool callInitialized = true)
            => CreateTestLspServerAsync(new string[] { markup }, LanguageNames.CSharp, mutatingLspWorkspace, new InitializationOptions { ClientCapabilities = clientCapabilities, CallInitialized = callInitialized });

        private protected Task<TestLspServer> CreateTestLspServerAsync(string markup, bool mutatingLspWorkspace, InitializationOptions? initializationOptions = null)
            => CreateTestLspServerAsync(new string[] { markup }, LanguageNames.CSharp, mutatingLspWorkspace, initializationOptions);

        private protected Task<TestLspServer> CreateTestLspServerAsync(string[] markups, bool mutatingLspWorkspace, InitializationOptions? initializationOptions = null)
            => CreateTestLspServerAsync(markups, LanguageNames.CSharp, mutatingLspWorkspace, initializationOptions);

        private protected Task<TestLspServer> CreateVisualBasicTestLspServerAsync(string markup, bool mutatingLspWorkspace, InitializationOptions? initializationOptions = null)
            => CreateTestLspServerAsync(new string[] { markup }, LanguageNames.VisualBasic, mutatingLspWorkspace, initializationOptions);

        private Task<TestLspServer> CreateTestLspServerAsync(
            string[] markups, string languageName, bool mutatingLspWorkspace, InitializationOptions? initializationOptions)
        {
            var lspOptions = initializationOptions ?? new InitializationOptions();

            var workspace = CreateWorkspace(lspOptions, workspaceKind: null, mutatingLspWorkspace);

            workspace.InitializeDocuments(TestWorkspace.CreateWorkspaceElement(languageName, files: markups, sourceGeneratedFiles: lspOptions.SourceGeneratedMarkups), openDocuments: false);

            return CreateTestLspServerAsync(workspace, lspOptions);
        }

        private async Task<TestLspServer> CreateTestLspServerAsync(TestWorkspace workspace, InitializationOptions initializationOptions)
        {
            var solution = workspace.CurrentSolution;

            foreach (var document in workspace.Documents)
            {
                if (document.IsSourceGenerated)
                    continue;

                solution = solution.WithDocumentFilePath(document.Id, GetDocumentFilePathFromName(document.Name));

                var documentText = await solution.GetRequiredDocument(document.Id).GetTextAsync(CancellationToken.None);
                solution = solution.WithDocumentText(document.Id, SourceText.From(documentText.ToString(), System.Text.Encoding.UTF8, SourceHashAlgorithms.Default));
            }

            foreach (var project in workspace.Projects)
            {
                // Ensure all the projects have a valid file path.
                solution = solution.WithProjectFilePath(project.Id, GetDocumentFilePathFromName(project.FilePath));
            }

            solution = solution.WithAnalyzerReferences(new[] { CreateTestAnalyzersReference() });
            await workspace.ChangeSolutionAsync(solution);

            // Important: We must wait for workspace creation operations to finish.
            // Otherwise we could have a race where workspace change events triggered by creation are changing the state
            // created by the initial test steps. This can interfere with the expected test state.
            await WaitForWorkspaceOperationsAsync(workspace);

            return await TestLspServer.CreateAsync(workspace, initializationOptions, TestOutputLspLogger);
        }

        private protected async Task<TestLspServer> CreateXmlTestLspServerAsync(
            string xmlContent,
            bool mutatingLspWorkspace,
            string? workspaceKind = null,
            InitializationOptions? initializationOptions = null)
        {
            var lspOptions = initializationOptions ?? new InitializationOptions();

            var workspace = CreateWorkspace(lspOptions, workspaceKind, mutatingLspWorkspace);

            workspace.InitializeDocuments(XElement.Parse(xmlContent), openDocuments: false);
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { CreateTestAnalyzersReference() }));

            // Important: We must wait for workspace creation operations to finish.
            // Otherwise we could have a race where workspace change events triggered by creation are changing the state
            // created by the initial test steps. This can interfere with the expected test state.
            await WaitForWorkspaceOperationsAsync(workspace);
            return await TestLspServer.CreateAsync(workspace, lspOptions, TestOutputLspLogger);
        }

        internal TestWorkspace CreateWorkspace(
            InitializationOptions? options, string? workspaceKind, bool mutatingLspWorkspace)
        {
            var workspace = new TestWorkspace(
                Composition, workspaceKind, configurationOptions: new WorkspaceConfigurationOptions(EnableOpeningSourceGeneratedFiles: true), supportsLspMutation: mutatingLspWorkspace);
            options?.OptionUpdater?.Invoke(workspace.GetService<IGlobalOptionService>());

            workspace.GetService<LspWorkspaceRegistrationService>().Register(workspace);

            // solution crawler is currently required in order to create incremental analyzer that provides diagnostics
            var solutionCrawlerRegistrationService = (SolutionCrawlerRegistrationService)workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
            solutionCrawlerRegistrationService.Register(workspace);

            return workspace;
        }

        /// <summary>
        /// Waits for the async operations on the workspace to complete.
        /// This ensures that events like workspace registration / workspace changes are processed by the time we exit this method.
        /// </summary>
        protected static async Task WaitForWorkspaceOperationsAsync(TestWorkspace workspace)
        {
            var workspaceWaiter = GetWorkspaceWaiter(workspace);
            await workspaceWaiter.ExpeditedWaitAsync();
        }

        private static IAsynchronousOperationWaiter GetWorkspaceWaiter(TestWorkspace workspace)
        {
            var operations = workspace.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
            return operations.GetWaiter(FeatureAttribute.Workspace);
        }

        protected static void AddMappedDocument(Workspace workspace, string markup)
        {
            var generatedDocumentId = DocumentId.CreateNewId(workspace.CurrentSolution.ProjectIds.First());
            var version = VersionStamp.Create();
            var loader = TextLoader.From(TextAndVersion.Create(SourceText.From(markup), version, TestSpanMapper.GeneratedFileName));
            var generatedDocumentInfo = DocumentInfo.Create(
                generatedDocumentId,
                TestSpanMapper.GeneratedFileName,
                loader: loader,
                filePath: $"C:\\{TestSpanMapper.GeneratedFileName}",
                isGenerated: true)
                .WithDocumentServiceProvider(new TestSpanMapperProvider());

            var newSolution = workspace.CurrentSolution.AddDocument(generatedDocumentInfo);
            workspace.TryApplyChanges(newSolution);
        }

        public static async Task<Dictionary<string, IList<LSP.Location>>> GetAnnotatedLocationsAsync(TestWorkspace workspace, Solution solution)
        {
            var locations = new Dictionary<string, IList<LSP.Location>>();
            foreach (var testDocument in workspace.Documents)
            {
                var document = await solution.GetRequiredDocumentAsync(testDocument.Id, includeSourceGenerated: true, CancellationToken.None);
                var text = await document.GetTextAsync(CancellationToken.None);
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

        private static string GetDocumentFilePathFromName(string documentName)
            => "C:\\" + documentName;

        private static LSP.DidChangeTextDocumentParams CreateDidChangeTextDocumentParams(
            Uri documentUri,
            ImmutableArray<(LSP.Range Range, string Text)> changes)
        {
            var changeEvents = changes.Select(change => new LSP.TextDocumentContentChangeEvent
            {
                Text = change.Text,
                Range = change.Range,
            }).ToArray();

            return new LSP.DidChangeTextDocumentParams()
            {
                TextDocument = new LSP.VersionedTextDocumentIdentifier
                {
                    Uri = documentUri
                },
                ContentChanges = changeEvents
            };
        }

        private static LSP.DidOpenTextDocumentParams CreateDidOpenTextDocumentParams(Uri uri, string source, string languageId = "")
            => new LSP.DidOpenTextDocumentParams
            {
                TextDocument = new LSP.TextDocumentItem
                {
                    Text = source,
                    Uri = uri,
                    LanguageId = languageId
                }
            };

        private static LSP.DidCloseTextDocumentParams CreateDidCloseTextDocumentParams(Uri uri)
           => new LSP.DidCloseTextDocumentParams()
           {
               TextDocument = new LSP.TextDocumentIdentifier
               {
                   Uri = uri
               }
           };

        internal sealed class TestLspServer : IAsyncDisposable
        {
            public readonly TestWorkspace TestWorkspace;
            private readonly Dictionary<string, IList<LSP.Location>> _locations;
            private readonly JsonRpc _clientRpc;

            private readonly RoslynLanguageServer LanguageServer;

            public LSP.ClientCapabilities ClientCapabilities { get; }

            private TestLspServer(
                TestWorkspace testWorkspace,
                Dictionary<string, IList<LSP.Location>> locations,
                LSP.ClientCapabilities clientCapabilities,
                RoslynLanguageServer target,
                Stream clientStream,
                object? clientTarget = null)
            {
                TestWorkspace = testWorkspace;
                ClientCapabilities = clientCapabilities;
                _locations = locations;

                LanguageServer = target;

                _clientRpc = new JsonRpc(new HeaderDelimitedMessageHandler(clientStream, clientStream, CreateJsonMessageFormatter()), clientTarget)
                {
                    ExceptionStrategy = ExceptionProcessing.ISerializable,
                };

                // Workspace listener events do not run in tests, so we manually register the lsp misc workspace.
                TestWorkspace.GetService<LspWorkspaceRegistrationService>().Register(GetManagerAccessor().GetLspMiscellaneousFilesWorkspace());

                InitializeClientRpc();
            }

            private void InitializeClientRpc()
            {
                _clientRpc.StartListening();

                var workspaceWaiter = GetWorkspaceWaiter(TestWorkspace);
                Assert.False(workspaceWaiter.HasPendingWork);
            }

            private static JsonMessageFormatter CreateJsonMessageFormatter()
            {
                var messageFormatter = new JsonMessageFormatter();
                LSP.VSInternalExtensionUtilities.AddVSInternalExtensionConverters(messageFormatter.JsonSerializer);
                return messageFormatter;
            }

            internal static async Task<TestLspServer> CreateAsync(TestWorkspace testWorkspace, InitializationOptions initializationOptions, ILspServiceLogger logger)
            {
                var locations = await GetAnnotatedLocationsAsync(testWorkspace, testWorkspace.CurrentSolution);

                var (clientStream, serverStream) = FullDuplexStream.CreatePair();
                var languageServer = CreateLanguageServer(serverStream, serverStream, testWorkspace, initializationOptions.ServerKind, logger);

                var server = new TestLspServer(testWorkspace, locations, initializationOptions.ClientCapabilities, languageServer, clientStream, initializationOptions.ClientTarget);

                await server.ExecuteRequestAsync<LSP.InitializeParams, LSP.InitializeResult>(LSP.Methods.InitializeName, new LSP.InitializeParams
                {
                    Capabilities = initializationOptions.ClientCapabilities,
                }, CancellationToken.None);

                if (initializationOptions.CallInitialized)
                {
                    await server.ExecuteRequestAsync<LSP.InitializedParams, object?>(LSP.Methods.InitializedName, new LSP.InitializedParams { }, CancellationToken.None);
                }

                return server;
            }

            internal static async Task<TestLspServer> CreateAsync(TestWorkspace testWorkspace, LSP.ClientCapabilities clientCapabilities, RoslynLanguageServer target, Stream clientStream)
            {
                var locations = await GetAnnotatedLocationsAsync(testWorkspace, testWorkspace.CurrentSolution);
                var server = new TestLspServer(testWorkspace, locations, clientCapabilities, target, clientStream);

                await server.ExecuteRequestAsync<LSP.InitializeParams, LSP.InitializeResult>(LSP.Methods.InitializeName, new LSP.InitializeParams
                {
                    Capabilities = clientCapabilities,
                }, CancellationToken.None);

                await server.ExecuteRequestAsync<LSP.InitializedParams, object?>(LSP.Methods.InitializedName, new LSP.InitializedParams { }, CancellationToken.None);

                return server;
            }

            private static RoslynLanguageServer CreateLanguageServer(Stream inputStream, Stream outputStream, TestWorkspace workspace, WellKnownLspServerKinds serverKind, ILspServiceLogger logger)
            {
                var capabilitiesProvider = workspace.ExportProvider.GetExportedValue<ExperimentalCapabilitiesProvider>();
                var servicesProvider = workspace.ExportProvider.GetExportedValue<CSharpVisualBasicLspServiceProvider>();

                var jsonRpc = new JsonRpc(new HeaderDelimitedMessageHandler(outputStream, inputStream, CreateJsonMessageFormatter()))
                {
                    ExceptionStrategy = ExceptionProcessing.ISerializable,
                };

                var languageServer = new RoslynLanguageServer(
                    servicesProvider, jsonRpc,
                    capabilitiesProvider,
                    logger,
                    workspace.Services.HostServices,
                    ProtocolConstants.RoslynLspLanguages,
                    serverKind);

                jsonRpc.StartListening();
                return languageServer;
            }

            public async Task<ResponseType?> ExecuteRequestAsync<RequestType, ResponseType>(string methodName, RequestType request, CancellationToken cancellationToken) where RequestType : class
            {
                // If creating the LanguageServer threw we might timeout without this.
                var result = await _clientRpc.InvokeWithParameterObjectAsync<ResponseType>(methodName, request, cancellationToken: cancellationToken).ConfigureAwait(false);
                return result;
            }

            public async Task OpenDocumentAsync(Uri documentUri, string? text = null, string languageId = "")
            {
                if (text == null)
                {
                    // LSP open files don't care about the project context, just the file contents with the URI.
                    // So pick any of the linked documents to get the text from.
                    var sourceText = await TestWorkspace.CurrentSolution.GetDocuments(documentUri).First().GetTextAsync(CancellationToken.None).ConfigureAwait(false);
                    text = sourceText.ToString();
                }

                var didOpenParams = CreateDidOpenTextDocumentParams(documentUri, text.ToString(), languageId);
                await ExecuteRequestAsync<LSP.DidOpenTextDocumentParams, object>(LSP.Methods.TextDocumentDidOpenName, didOpenParams, CancellationToken.None);
            }

            public Task InsertTextAsync(Uri documentUri, params (int Line, int Column, string Text)[] changes)
            {
                return ReplaceTextAsync(documentUri, changes.Select(change => (new LSP.Range
                {
                    Start = new LSP.Position { Line = change.Line, Character = change.Column },
                    End = new LSP.Position { Line = change.Line, Character = change.Column }
                }, change.Text)).ToArray());
            }

            public Task ReplaceTextAsync(Uri documentUri, params (LSP.Range Range, string Text)[] changes)
            {
                var didChangeParams = CreateDidChangeTextDocumentParams(
                    documentUri,
                    changes.Select(change => (change.Range, change.Text)).ToImmutableArray());
                return ExecuteRequestAsync<LSP.DidChangeTextDocumentParams, object>(LSP.Methods.TextDocumentDidChangeName, didChangeParams, CancellationToken.None);
            }

            public Task DeleteTextAsync(Uri documentUri, params (int StartLine, int StartColumn, int EndLine, int EndColumn)[] changes)
            {
                return ReplaceTextAsync(documentUri, changes.Select(change => (new LSP.Range
                {
                    Start = new LSP.Position { Line = change.StartLine, Character = change.StartColumn },
                    End = new LSP.Position { Line = change.EndLine, Character = change.EndColumn }
                }, string.Empty)).ToArray());
            }

            public Task CloseDocumentAsync(Uri documentUri)
            {
                var didCloseParams = CreateDidCloseTextDocumentParams(documentUri);
                return ExecuteRequestAsync<LSP.DidCloseTextDocumentParams, object>(LSP.Methods.TextDocumentDidCloseName, didCloseParams, CancellationToken.None);
            }

            public async Task ShutdownTestServerAsync()
            {
                await _clientRpc.InvokeAsync(LSP.Methods.ShutdownName).ConfigureAwait(false);
            }

            public async Task ExitTestServerAsync()
            {
                // Since exit is a notification that disposes of the json rpc stream we cannot wait on the result
                // of the request itself since it will throw a ConnectionLostException.
                // Instead we wait for the server's exit task to be completed.
                await _clientRpc.NotifyAsync(LSP.Methods.ExitName).ConfigureAwait(false);
                await LanguageServer.WaitForExitAsync().ConfigureAwait(false);
            }

            public IList<LSP.Location> GetLocations(string locationName) => _locations[locationName];

            public Solution GetCurrentSolution() => TestWorkspace.CurrentSolution;

            internal async Task WaitForDiagnosticsAsync()
            {
                var listenerProvider = TestWorkspace.GetService<IAsynchronousOperationListenerProvider>();

                await listenerProvider.GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();
                await listenerProvider.GetWaiter(FeatureAttribute.SolutionCrawlerLegacy).ExpeditedWaitAsync();
                await listenerProvider.GetWaiter(FeatureAttribute.DiagnosticService).ExpeditedWaitAsync();
            }

            internal RequestExecutionQueue<RequestContext>.TestAccessor? GetQueueAccessor() => LanguageServer.GetTestAccessor().GetQueueAccessor();

            internal LspWorkspaceManager.TestAccessor GetManagerAccessor() => GetRequiredLspService<LspWorkspaceManager>().GetTestAccessor();

            internal LspWorkspaceManager GetManager() => GetRequiredLspService<LspWorkspaceManager>();

            internal AbstractLanguageServer<RequestContext>.TestAccessor GetServerAccessor() => LanguageServer.GetTestAccessor();

            internal T GetRequiredLspService<T>() where T : class, ILspService => LanguageServer.GetTestAccessor().GetRequiredLspService<T>();

            internal ImmutableArray<SourceText> GetTrackedTexts() => GetManager().GetTrackedLspText().Values.Select(v => v.Text).ToImmutableArray();

            public async ValueTask DisposeAsync()
            {
                TestWorkspace.GetService<LspWorkspaceRegistrationService>().Deregister(TestWorkspace);
                TestWorkspace.GetService<LspWorkspaceRegistrationService>().Deregister(GetManagerAccessor().GetLspMiscellaneousFilesWorkspace());

                var solutionCrawlerRegistrationService = (SolutionCrawlerRegistrationService)TestWorkspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
                solutionCrawlerRegistrationService.Unregister(TestWorkspace);

                // Some tests will manually call shutdown and exit, so attempting to call this during dispose
                // will fail as the server's jsonrpc instance will be disposed of.
                if (!LanguageServer.GetTestAccessor().HasShutdownStarted())
                {
                    await ShutdownTestServerAsync();
                    await ExitTestServerAsync();
                }

                // Wait for all the exit notifications to run to completion.
                await LanguageServer.WaitForExitAsync();

                TestWorkspace.Dispose();
                _clientRpc.Dispose();
            }
        }
    }
}
