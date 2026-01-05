// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.Composition;
using Nerdbank.Streams;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using StreamJsonRpc;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Roslyn.Test.Utilities;

[UseExportProvider]
public abstract partial class AbstractLanguageServerProtocolTests
{
    protected static readonly JsonSerializerOptions JsonSerializerOptions = RoslynLanguageServer.CreateJsonMessageFormatter().JsonSerializerOptions;

    private protected readonly AbstractLspLogger TestOutputLspLogger;
    protected AbstractLanguageServerProtocolTests(ITestOutputHelper? testOutputHelper)
    {
        TestOutputLspLogger = testOutputHelper != null ? new TestOutputLspLogger(testOutputHelper) : NoOpLspLogger.Instance;
    }

    protected static readonly TestComposition FeaturesLspComposition = LspTestCompositions.LanguageServerProtocol
        .AddParts(typeof(TestDocumentTrackingService))
        .AddParts(typeof(TestWorkspaceRegistrationService));

    private sealed class TestSpanMapperProvider : IDocumentServiceProvider
    {
        TService? IDocumentServiceProvider.GetService<TService>() where TService : class
            => typeof(TService) == typeof(ISpanMappingService) ? (TService)(object)new TestSpanMapper() : null;
    }

    internal sealed class TestSpanMapper : ISpanMappingService
    {
        private static readonly LinePositionSpan s_mappedLinePosition = new(new LinePosition(0, 0), new LinePosition(0, 5));
        private static readonly string s_mappedFilePath = "c:\\MappedFile_\ue25b\ud86d\udeac.cs";

        internal static readonly string GeneratedFileName = "GeneratedFile_\ue25b\ud86d\udeac.cs";

        internal static readonly LSP.Location MappedFileLocation = new()
        {
            Range = ProtocolConversions.LinePositionToRange(s_mappedLinePosition),
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(s_mappedFilePath)
        };

        /// <summary>
        /// LSP tests are simulating the new razor system which does support mapping import directives.
        /// </summary>
        public bool SupportsMappingImportDirectives => true;

        public async Task<ImmutableArray<MappedSpanResult>> MapSpansAsync(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken)
        {
            ImmutableArray<MappedSpanResult> mappedResult = default;
            if (document.Name == GeneratedFileName)
            {
                mappedResult = [.. spans.Select(span => new MappedSpanResult(s_mappedFilePath, s_mappedLinePosition, new TextSpan(0, 5)))];
            }

            return mappedResult;
        }

        public Task<ImmutableArray<(string mappedFilePath, TextChange mappedTextChange)>> GetMappedTextChangesAsync(
            Document oldDocument,
            Document newDocument,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private protected sealed class OrderLocations : Comparer<LSP.Location?>
    {
        public override int Compare(LSP.Location? x, LSP.Location? y) => CompareLocations(x, y);
    }

    protected virtual async ValueTask<ExportProvider> CreateExportProviderAsync() => Composition.ExportProviderFactory.CreateExportProvider();
    protected virtual TestComposition Composition => FeaturesLspComposition;

    private protected virtual TestAnalyzerReferenceByLanguage CreateTestAnalyzersReference()
        => new(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap());

    private protected static LSP.ClientCapabilities CapabilitiesWithVSExtensions => new LSP.VSInternalClientCapabilities { SupportsVisualStudioExtensions = true };

    private protected static LSP.ClientCapabilities GetCapabilities(bool isVS)
        => isVS ? CapabilitiesWithVSExtensions : new LSP.ClientCapabilities();

    /// <summary>
    /// Asserts two objects are equivalent by converting to JSON and ignoring whitespace.
    /// </summary>
    /// <param name="expected">the expected object to be converted to JSON.</param>
    /// <param name="actual">the actual object to be converted to JSON.</param>
    public static void AssertJsonEquals<T1, T2>(T1 expected, T2 actual)
    {
        var expectedStr = JsonSerializer.Serialize(expected, JsonSerializerOptions);
        var actualStr = JsonSerializer.Serialize(actual, JsonSerializerOptions);
        AssertEqualIgnoringWhitespace(expectedStr, actualStr);
    }

    protected static void AssertEqualIgnoringWhitespace(string expected, string actual)
    {
        var expectedWithoutWhitespace = Regex.Replace(expected, @"\s+", string.Empty);
        var actualWithoutWhitespace = Regex.Replace(actual, @"\s+", string.Empty);
        AssertEx.Equal(expectedWithoutWhitespace, actualWithoutWhitespace);
    }

    /// <summary>
    /// Assert that two location lists are equivalent.
    /// Locations are not always returned in a consistent order so they must be sorted.
    /// </summary>
    private protected static void AssertLocationsEqual(IEnumerable<LSP.Location> expectedLocations, IEnumerable<LSP.Location> actualLocations)
    {
        var orderedActualLocations = actualLocations.OrderBy(CompareLocations);
        var orderedExpectedLocations = expectedLocations.OrderBy(CompareLocations);

        AssertJsonEquals(orderedExpectedLocations, orderedActualLocations);
    }

    private protected static int CompareLocations(LSP.Location? l1, LSP.Location? l2)
    {
        if (ReferenceEquals(l1, l2))
            return 0;

        if (l1 is null)
            return -1;

        if (l2 is null)
            return 1;

        var compareDocument = l1.DocumentUri.UriString.CompareTo(l2.DocumentUri.UriString);
        var compareRange = CompareRange(l1.Range, l2.Range);
        return compareDocument != 0 ? compareDocument : compareRange;
    }

    private protected static int CompareRange(LSP.Range r1, LSP.Range r2)
    {
        var compareLine = r1.Start.Line.CompareTo(r2.Start.Line);
        var compareChar = r1.Start.Character.CompareTo(r2.Start.Character);
        return compareLine != 0 ? compareLine : compareChar;
    }

    private protected static string ApplyTextEdits(LSP.TextEdit[]? edits, SourceText originalMarkup)
    {
        var changes = Array.ConvertAll(edits ?? [], edit => ProtocolConversions.TextEditToTextChange(edit, originalMarkup));
        return originalMarkup.WithChanges(changes).ToString();
    }

    internal static LSP.SymbolInformation CreateSymbolInformation(LSP.SymbolKind kind, string name, LSP.Location location, Glyph glyph, string? containerName = null)
    {
        var (guid, id) = glyph.GetVsImageData();

#pragma warning disable CS0618 // SymbolInformation is obsolete, need to switch to DocumentSymbol/WorkspaceSymbol
        var info = new LSP.VSSymbolInformation()
        {
            Kind = kind,
            Name = name,
            Location = location,
            Icon = new LSP.VSImageId { Guid = guid, Id = id },
        };

        if (containerName != null)
            info.ContainerName = containerName;
#pragma warning restore CS0618

        return info;
    }

    private protected static LSP.TextDocumentIdentifier CreateTextDocumentIdentifier(DocumentUri uri, ProjectId? projectContext = null)
    {
        var documentIdentifier = new LSP.VSTextDocumentIdentifier { DocumentUri = uri };

        if (projectContext != null)
        {
            documentIdentifier.ProjectContext =
                new LSP.VSProjectContext { Id = ProtocolConversions.ProjectIdToProjectContextId(projectContext), Label = projectContext.DebugName!, Kind = LSP.VSProjectKind.CSharp };
        }

        return documentIdentifier;
    }

    private protected static LSP.TextDocumentPositionParams CreateTextDocumentPositionParams(LSP.Location caret, ProjectId? projectContext = null)
        => new()
        {
            TextDocument = CreateTextDocumentIdentifier(caret.DocumentUri, projectContext),
            Position = caret.Range.Start
        };

    private protected static LSP.MarkupContent CreateMarkupContent(LSP.MarkupKind kind, string value)
        => new()
        {
            Kind = kind,
            Value = value
        };

    private protected static LSP.CompletionParams CreateCompletionParams(
        LSP.Location caret,
        LSP.VSInternalCompletionInvokeKind invokeKind,
        string triggerCharacter,
        LSP.CompletionTriggerKind triggerKind)
        => new()
        {
            TextDocument = CreateTextDocumentIdentifier(caret.DocumentUri),
            Position = caret.Range.Start,
            Context = new LSP.VSInternalCompletionContext()
            {
                InvokeKind = invokeKind,
                TriggerCharacter = triggerCharacter,
                TriggerKind = triggerKind,
            }
        };

    private protected static async Task<LSP.VSInternalCompletionItem> CreateCompletionItemAsync(
        string label,
        LSP.CompletionItemKind kind,
        string[] tags,
        LSP.CompletionParams request,
        Document document,
        bool preselect = false,
        ImmutableArray<char>? commitCharacters = null,
        LSP.TextEdit? textEdit = null,
        string? textEditText = null,
        string? sortText = null,
        string? filterText = null,
        long resultId = 0,
        bool vsResolveTextEditOnCommit = false,
        LSP.CompletionItemLabelDetails? labelDetails = null)
    {
        var position = await document.GetPositionFromLinePositionAsync(
            ProtocolConversions.PositionToLinePosition(request.Position), CancellationToken.None).ConfigureAwait(false);
        var completionTrigger = await ProtocolConversions.LSPToRoslynCompletionTriggerAsync(
            request.Context, document, position, CancellationToken.None).ConfigureAwait(false);

        var item = new LSP.VSInternalCompletionItem()
        {
            TextEdit = textEdit,
            TextEditText = textEditText,
            FilterText = filterText,
            Label = label,
            SortText = sortText,
            InsertTextFormat = LSP.InsertTextFormat.Plaintext,
            Kind = kind,
            Data = JsonSerializer.SerializeToElement(new CompletionResolveData(resultId, ProtocolConversions.DocumentToTextDocumentIdentifier(document)), JsonSerializerOptions),
            Preselect = preselect,
            VsResolveTextEditOnCommit = vsResolveTextEditOnCommit,
            LabelDetails = labelDetails
        };

        if (tags != null)
            item.Icon = new(tags.ToImmutableArray().GetFirstGlyph().ToLSPImageId());

        if (commitCharacters != null)
            item.CommitCharacters = [.. commitCharacters.Value.Select(c => c.ToString())];

        return item;
    }

    private protected static LSP.TextEdit GenerateTextEdit(string newText, int startLine, int startChar, int endLine, int endChar)
        => new()
        {
            NewText = newText,
            Range = new LSP.Range
            {
                Start = new LSP.Position { Line = startLine, Character = startChar },
                End = new LSP.Position { Line = endLine, Character = endChar }
            }
        };

    private protected static CodeActionResolveData CreateCodeActionResolveData(string uniqueIdentifier, LSP.Location location, string[] codeActionPath, IEnumerable<string>? customTags = null)
        => new(uniqueIdentifier, customTags.ToImmutableArrayOrEmpty(), location.Range, CreateTextDocumentIdentifier(location.DocumentUri), fixAllFlavors: null, nestedCodeActions: null, codeActionPath: codeActionPath);

    private protected Task<TestLspServer> CreateTestLspServerAsync([StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup, bool mutatingLspWorkspace, LSP.ClientCapabilities clientCapabilities, bool callInitialized = true)
        => CreateTestLspServerAsync([markup], LanguageNames.CSharp, mutatingLspWorkspace, new InitializationOptions { ClientCapabilities = clientCapabilities, CallInitialized = callInitialized });

    private protected Task<TestLspServer> CreateTestLspServerAsync([StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup, bool mutatingLspWorkspace, InitializationOptions? initializationOptions = null, TestComposition? composition = null)
        => CreateTestLspServerAsync([markup], LanguageNames.CSharp, mutatingLspWorkspace, initializationOptions, composition);

    private protected Task<TestLspServer> CreateTestLspServerAsync([StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string[] markups, bool mutatingLspWorkspace, InitializationOptions? initializationOptions = null, TestComposition? composition = null)
        => CreateTestLspServerAsync(markups, LanguageNames.CSharp, mutatingLspWorkspace, initializationOptions, composition);

    private protected Task<TestLspServer> CreateVisualBasicTestLspServerAsync(string markup, bool mutatingLspWorkspace, InitializationOptions? initializationOptions = null, TestComposition? composition = null)
        => CreateTestLspServerAsync([markup], LanguageNames.VisualBasic, mutatingLspWorkspace, initializationOptions, composition);

    private protected async Task<TestLspServer> CreateTestLspServerAsync(
        string[] markups, string languageName, bool mutatingLspWorkspace, InitializationOptions? initializationOptions, TestComposition? composition = null, bool commonReferences = true)
    {
        var lspOptions = initializationOptions ?? new InitializationOptions();

        var workspace = await CreateWorkspaceAsync(lspOptions, workspaceKind: null, mutatingLspWorkspace, composition);

        workspace.InitializeDocuments(
            LspTestWorkspace.CreateWorkspaceElement(
                languageName,
                parseOptions: lspOptions.ParseOptions,
                files: markups,
                fileContainingFolders: lspOptions.DocumentFileContainingFolders,
                sourceGeneratedFiles: lspOptions.SourceGeneratedMarkups,
                commonReferences: commonReferences),
            openDocuments: false);

        return await CreateTestLspServerAsync(workspace, lspOptions, languageName);
    }

    private protected async Task<TestLspServer> CreateTestLspServerAsync(LspTestWorkspace workspace, InitializationOptions initializationOptions, string languageName)
    {
        var solution = workspace.CurrentSolution;

        var analyzerReferencesByLanguage = CreateTestAnalyzersReference();
        if (initializationOptions.AdditionalAnalyzers != null)
            analyzerReferencesByLanguage = analyzerReferencesByLanguage.WithAdditionalAnalyzers(languageName, initializationOptions.AdditionalAnalyzers);

        solution = solution.WithAnalyzerReferences([analyzerReferencesByLanguage]);
        await workspace.ChangeSolutionAsync(solution);

        return await TestLspServer.CreateAsync(workspace, initializationOptions, TestOutputLspLogger);
    }

    private protected async Task<TestLspServer> CreateXmlTestLspServerAsync(
        string xmlContent,
        bool mutatingLspWorkspace,
        string? workspaceKind = null,
        InitializationOptions? initializationOptions = null)
    {
        var lspOptions = initializationOptions ?? new InitializationOptions();

        var workspace = await CreateWorkspaceAsync(lspOptions, workspaceKind, mutatingLspWorkspace);

        workspace.InitializeDocuments(XElement.Parse(xmlContent), openDocuments: false);
        workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences([CreateTestAnalyzersReference()]));

        return await TestLspServer.CreateAsync(workspace, lspOptions, TestOutputLspLogger);
    }

    private void CheckForCompositionErrors(TestComposition composition)
    {
        // The test compositions tend to have a bunch of errors.
        // We only want to fail the test if we're seeing errors in relevant parts to the language server.
        // This isn't foolproof, but helps catch issues early.

        var config = composition.GetCompositionConfiguration();
        var hasLanguageServerErrors = config.CompositionErrors.Flatten().Any(error => error.Parts.Any(IsRelevantPartError));

        if (hasLanguageServerErrors)
        {
            try
            {
                config.ThrowOnErrors();
            }
            catch (CompositionFailedException ex)
            {
                // The ToString for the composition failed exception doesn't output a nice set of errors by default, so log it separately
                this.TestOutputLspLogger.LogError($"Encountered errors in the MEF composition: {ex.Message}{Environment.NewLine}{ex.ErrorsAsString}");
                throw;
            }
        }

        bool IsRelevantPartError(ComposedPart part)
        {
            return part.Definition.Type.FullName?.Contains("Microsoft.CodeAnalysis.LanguageServer") == true;
        }
    }

    internal async Task<LspTestWorkspace> CreateWorkspaceAsync(
        InitializationOptions? options, string? workspaceKind, bool mutatingLspWorkspace, TestComposition? composition = null)
    {
        CheckForCompositionErrors(composition ?? Composition);
        var workspace = new LspTestWorkspace(
            composition?.ExportProviderFactory.CreateExportProvider() ?? await CreateExportProviderAsync(),
            workspaceKind,
            supportsLspMutation: mutatingLspWorkspace);
        options?.OptionUpdater?.Invoke(workspace.GetService<IGlobalOptionService>());

        // By default in most MEF containers, workspace event listeners are disabled in tests.  Explicitly enable the LSP workspace registration event listener
        // to ensure that the lsp workspace registration service sees all workspaces. If we're running tests against our full LSP server
        // composition, we don't expect the mock to exist and the real thing is running.
        var listenerProvider = workspace.ExportProvider.GetExportedValues<MockWorkspaceEventListenerProvider>().SingleOrDefault();
        if (listenerProvider is not null)
        {
            var lspWorkspaceRegistrationListener = (LspWorkspaceRegistrationEventListener)workspace.ExportProvider.GetExports<IEventListener>().Single(e => e.Value is LspWorkspaceRegistrationEventListener).Value;
            listenerProvider.EventListeners = [lspWorkspaceRegistrationListener];
        }

        return workspace;
    }

    /// <summary>
    /// Waits for the async operations on the workspace to complete.
    /// This ensures that events like workspace registration / workspace changes are processed by the time we exit this method.
    /// </summary>
    protected static async Task WaitForWorkspaceOperationsAsync<TDocument, TProject, TSolution>(TestWorkspace<TDocument, TProject, TSolution> workspace)
        where TDocument : TestHostDocument
        where TProject : TestHostProject<TDocument>
        where TSolution : TestHostSolution<TDocument>
    {
        var workspaceWaiter = GetWorkspaceWaiter(workspace);
        await workspaceWaiter.ExpeditedWaitAsync();
    }

    private static IAsynchronousOperationWaiter GetWorkspaceWaiter<TDocument, TProject, TSolution>(TestWorkspace<TDocument, TProject, TSolution> workspace)
        where TDocument : TestHostDocument
        where TProject : TestHostProject<TDocument>
        where TSolution : TestHostSolution<TDocument>
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

    protected static async Task<AnalyzerReference> AddGeneratorAsync(ISourceGenerator generator, LspTestWorkspace workspace)
    {
        var analyzerReference = new TestGeneratorReference(generator);

        var solution = workspace.CurrentSolution
            .Projects.Single()
            .AddAnalyzerReference(analyzerReference)
            .Solution;

        await workspace.ChangeSolutionAsync(solution);
        await WaitForWorkspaceOperationsAsync(workspace);
        return analyzerReference;
    }

    protected static async Task RemoveGeneratorAsync(AnalyzerReference reference, LspTestWorkspace workspace)
    {
        var solution = workspace.CurrentSolution
            .Projects.Single()
            .RemoveAnalyzerReference(reference)
            .Solution;

        await workspace.ChangeSolutionAsync(solution);
        await WaitForWorkspaceOperationsAsync(workspace);
    }

    internal static async Task<Dictionary<string, IList<LSP.Location>>> GetAnnotatedLocationsAsync<TDocument, TProject, TSolution>(TestWorkspace<TDocument, TProject, TSolution> workspace, Solution solution)
        where TDocument : TestHostDocument
        where TProject : TestHostProject<TDocument>
        where TSolution : TestHostSolution<TDocument>
    {
        var locations = new Dictionary<string, IList<LSP.Location>>();
        foreach (var testDocument in workspace.Documents)
        {
            var document = await solution.GetRequiredDocumentAsync(testDocument.Id, includeSourceGenerated: true, CancellationToken.None);
            var text = await document.GetTextAsync(CancellationToken.None);
            foreach (var (name, spans) in testDocument.AnnotatedSpans)
            {
                Contract.ThrowIfNull(document.FilePath);

                var locationsForName = locations.GetValueOrDefault(name, []);
                locationsForName.AddRange(spans.Select(span => ConvertTextSpanWithTextToLocation(span, text, document.GetURI())));

                // Linked files will return duplicate annotated Locations for each document that links to the same file.
                // Since the test output only cares about the actual file, make sure we de-dupe before returning.
                locations[name] = [.. locationsForName.Distinct()];
            }
        }

        return locations;

        static LSP.Location ConvertTextSpanWithTextToLocation(TextSpan span, SourceText text, DocumentUri documentUri)
        {
            var location = new LSP.Location
            {
                DocumentUri = documentUri,
                Range = ProtocolConversions.TextSpanToRange(span, text),
            };

            return location;
        }
    }

    private protected static LSP.Location GetLocationPlusOne(LSP.Location originalLocation)
    {
        var newPosition = new LSP.Position { Character = originalLocation.Range.Start.Character + 1, Line = originalLocation.Range.Start.Line };
        return new LSP.Location
        {
            DocumentUri = originalLocation.DocumentUri,
            Range = new LSP.Range { Start = newPosition, End = newPosition }
        };
    }

    private static LSP.DidChangeTextDocumentParams CreateDidChangeTextDocumentParams(
        DocumentUri documentUri,
        ImmutableArray<(LSP.Range Range, string Text)> changes,
        int version = 0)
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
                DocumentUri = documentUri,
                Version = version
            },
            ContentChanges = changeEvents
        };
    }

    private static LSP.DidOpenTextDocumentParams CreateDidOpenTextDocumentParams(DocumentUri uri, string source, string languageId = "", int version = 0)
        => new()
        {
            TextDocument = new LSP.TextDocumentItem
            {
                Text = source,
                DocumentUri = uri,
                LanguageId = languageId,
                Version = version
            }
        };

    private static LSP.DidCloseTextDocumentParams CreateDidCloseTextDocumentParams(DocumentUri uri)
       => new()
       {
           TextDocument = new LSP.TextDocumentIdentifier
           {
               DocumentUri = uri
           }
       };

    /// <summary>
    /// Implementation of <see cref="AbstractTestLspServer{TWorkspace, TDocument, TProject, TSolution}"/>
    /// using the <see cref="LspTestWorkspace"/> workspace.
    /// </summary>
    internal sealed class TestLspServer : AbstractTestLspServer<LspTestWorkspace, TestHostDocument, TestHostProject, TestHostSolution>
    {
        public TestLspServer(LspTestWorkspace testWorkspace, Dictionary<string, IList<LSP.Location>> locations, InitializationOptions initializationOptions, AbstractLspLogger logger)
            : base(testWorkspace, locations, initializationOptions, logger)
        {
        }

        public static async Task<TestLspServer> CreateAsync(LspTestWorkspace testWorkspace, InitializationOptions initializationOptions, AbstractLspLogger logger)
        {
            var locations = await GetAnnotatedLocationsAsync(testWorkspace, testWorkspace.CurrentSolution);
            var server = new TestLspServer(testWorkspace, locations, initializationOptions, logger);
            await server.InitializeAsync();
            return server;
        }
    }

    internal abstract class AbstractTestLspServer<TWorkspace, TDocument, TProject, TSolution> : IAsyncDisposable
        where TDocument : TestHostDocument
        where TProject : TestHostProject<TDocument>
        where TSolution : TestHostSolution<TDocument>
        where TWorkspace : TestWorkspace<TDocument, TProject, TSolution>
    {
        public readonly TWorkspace TestWorkspace;
        private readonly JsonRpc _clientRpc;
        private readonly Dictionary<string, IList<LSP.Location>> _locations;
        private readonly ICodeAnalysisDiagnosticAnalyzerService _codeAnalysisService;
        private readonly InitializationOptions _initializationOptions;
        private readonly Lazy<RoslynLanguageServer> _languageServer;

        private LSP.InitializeResult? _initializeResult;

        public LSP.ClientCapabilities ClientCapabilities { get; }

        public AbstractTestLspServer(
            TWorkspace testWorkspace,
            Dictionary<string, IList<LSP.Location>> locations,
            InitializationOptions initializationOptions,
            AbstractLspLogger logger)
        {
            TestWorkspace = testWorkspace;
            _initializationOptions = initializationOptions;
            _locations = locations;
            _codeAnalysisService = testWorkspace.Services.GetRequiredService<ICodeAnalysisDiagnosticAnalyzerService>();

            ClientCapabilities = initializationOptions.ClientCapabilities;

            var clientMessageFormatter = initializationOptions.ClientMessageFormatter ?? RoslynLanguageServer.CreateJsonMessageFormatter();

            var (clientStream, serverStream) = FullDuplexStream.CreatePair();

            _clientRpc = new JsonRpc(new HeaderDelimitedMessageHandler(clientStream, clientStream, clientMessageFormatter), initializationOptions.ClientTarget)
            {
                ExceptionStrategy = ExceptionProcessing.ISerializable,
            };

            _languageServer = new(() =>
            {
                var server = CreateLanguageServer(serverStream, serverStream, _initializationOptions.ServerKind, logger);

                InitializeClientRpc();
                return server;
            });

        }

        private void InitializeClientRpc()
        {
            _clientRpc.StartListening();

            var workspaceWaiter = GetWorkspaceWaiter(TestWorkspace);
            Assert.False(workspaceWaiter.HasPendingWork);
        }

        internal async Task InitializeAsync()
        {
            // Important: We must wait for workspace creation operations to finish.
            // Otherwise we could have a race where workspace change events triggered by creation are changing the state
            // created by the initial test steps. This can interfere with the expected test state.
            await WaitForWorkspaceOperationsAsync(TestWorkspace);

            // Initialize the language server
            _ = _languageServer.Value;

            if (_initializationOptions.CallInitialize)
            {
                _initializeResult = await this.ExecuteRequestAsync<LSP.InitializeParams, LSP.InitializeResult>(LSP.Methods.InitializeName, new LSP.InitializeParams
                {
                    Capabilities = _initializationOptions.ClientCapabilities,
                    Locale = _initializationOptions.Locale,
                }, CancellationToken.None);
            }

            if (_initializationOptions.CallInitialized)
            {
                await this.ExecuteRequestAsync<LSP.InitializedParams, object?>(LSP.Methods.InitializedName, new LSP.InitializedParams { }, CancellationToken.None);
            }
        }

        protected virtual RoslynLanguageServer CreateLanguageServer(Stream inputStream, Stream outputStream, WellKnownLspServerKinds serverKind, AbstractLspLogger logger)
        {
            var capabilitiesProvider = TestWorkspace.ExportProvider.GetExportedValue<ExperimentalCapabilitiesProvider>();
            var factory = TestWorkspace.ExportProvider.GetExportedValue<ILanguageServerFactory>();

            var jsonMessageFormatter = RoslynLanguageServer.CreateJsonMessageFormatter();
            var jsonRpc = new JsonRpc(new HeaderDelimitedMessageHandler(outputStream, inputStream, jsonMessageFormatter))
            {
                ExceptionStrategy = ExceptionProcessing.ISerializable,
            };

            var languageServer = (RoslynLanguageServer)factory.Create(jsonRpc, jsonMessageFormatter.JsonSerializerOptions, capabilitiesProvider, serverKind, logger, TestWorkspace.Services.HostServices);

            jsonRpc.StartListening();
            return languageServer;
        }

        public async Task<Document> GetDocumentAsync(DocumentUri uri)
        {
            var textDocument = await GetTextDocumentAsync(uri).ConfigureAwait(false);
            if (textDocument is not Document document)
            {
                throw new InvalidOperationException($"Found TextDocument with {uri} in solution, but it is not a Document");
            }

            return document;
        }

        public async Task<TextDocument> GetTextDocumentAsync(DocumentUri uri)
        {
            var document = await GetCurrentSolution().GetTextDocumentAsync(new LSP.TextDocumentIdentifier { DocumentUri = uri }, CancellationToken.None).ConfigureAwait(false);
            Contract.ThrowIfNull(document, $"Unable to find document with {uri} in solution");
            return document;
        }

        public async Task<SourceText> GetDocumentTextAsync(DocumentUri uri)
        {
            var document = await GetTextDocumentAsync(uri).ConfigureAwait(false);
            return await document.GetTextAsync(CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<ResponseType?> ExecuteRequestAsync<RequestType, ResponseType>(string methodName, RequestType request, CancellationToken cancellationToken) where RequestType : class
        {
            // If creating the LanguageServer threw we might timeout without this.
            var result = await _clientRpc.InvokeWithParameterObjectAsync<ResponseType>(methodName, request, cancellationToken: cancellationToken).ConfigureAwait(false);
            return result;
        }

        public async Task<ResponseType?> ExecuteRequest0Async<ResponseType>(string methodName, CancellationToken cancellationToken)
        {
            // If creating the LanguageServer threw we might timeout without this.
            var result = await _clientRpc.InvokeWithParameterObjectAsync<ResponseType>(methodName, cancellationToken: cancellationToken).ConfigureAwait(false);
            return result;
        }

        public Task ExecuteNotificationAsync<RequestType>(string methodName, RequestType request) where RequestType : class
        {
            return _clientRpc.NotifyWithParameterObjectAsync(methodName, request);
        }

        public Task ExecuteNotification0Async(string methodName)
        {
            return _clientRpc.NotifyWithParameterObjectAsync(methodName);
        }

        public Task ExecutePreSerializedRequestAsync(string methodName, JsonDocument serializedRequest)
        {
            return _clientRpc.InvokeWithParameterObjectAsync(methodName, serializedRequest);
        }

        public async Task OpenDocumentAsync(DocumentUri documentUri, string? text = null, string languageId = "", int version = 0)
        {
            if (text == null)
            {
                // LSP open files don't care about the project context, just the file contents with the URI.
                // So pick any of the linked documents to get the text from.
                var sourceText = await GetDocumentTextAsync(documentUri).ConfigureAwait(false);
                text = sourceText.ToString();
            }

            var didOpenParams = CreateDidOpenTextDocumentParams(documentUri, text.ToString(), languageId, version);
            await ExecuteRequestAsync<LSP.DidOpenTextDocumentParams, object>(LSP.Methods.TextDocumentDidOpenName, didOpenParams, CancellationToken.None);
        }

        /// <summary>
        /// Opens a document in the workspace only, and waits for workspace operations.
        /// Use <see cref="OpenDocumentAsync(DocumentUri, string, string, int)"/> if the document should be opened in LSP"/>
        /// </summary>
        public async Task OpenDocumentInWorkspaceAsync(DocumentId documentId, bool openAllLinkedDocuments, SourceText? text = null)
        {
            var document = TestWorkspace.CurrentSolution.GetDocument(documentId);
            Contract.ThrowIfNull(document);

            text ??= await TestWorkspace.CurrentSolution.GetDocument(documentId)!.GetTextAsync(CancellationToken.None);

            List<DocumentId> linkedDocuments = [documentId];
            if (openAllLinkedDocuments)
            {
                linkedDocuments.AddRange(document.GetLinkedDocumentIds());
            }

            var container = new TestStaticSourceTextContainer(text);

            foreach (var documentIdToOpen in linkedDocuments)
            {
                TestWorkspace.OnDocumentOpened(documentIdToOpen, container);
            }

            await WaitForWorkspaceOperationsAsync(TestWorkspace);
        }

        public Task ReplaceTextAsync(DocumentUri documentUri, int version, params (LSP.Range Range, string Text)[] changes)
        {
            var didChangeParams = CreateDidChangeTextDocumentParams(
                documentUri,
                [.. changes],
                version);
            return ExecuteRequestAsync<LSP.DidChangeTextDocumentParams, object>(LSP.Methods.TextDocumentDidChangeName, didChangeParams, CancellationToken.None);
        }

        public Task ReplaceTextAsync(DocumentUri documentUri, params (LSP.Range Range, string Text)[] changes)
        {
            return ReplaceTextAsync(documentUri, version: 0, changes);
        }

        public Task InsertTextAsync(DocumentUri documentUri, int version, params (int Line, int Column, string Text)[] changes)
        {
            return ReplaceTextAsync(documentUri, version, [.. changes.Select(change => (new LSP.Range
            {
                Start = new LSP.Position { Line = change.Line, Character = change.Column },
                End = new LSP.Position { Line = change.Line, Character = change.Column }
            }, change.Text))]);
        }

        public Task InsertTextAsync(DocumentUri documentUri, params (int Line, int Column, string Text)[] changes)
        {
            return InsertTextAsync(documentUri, version: 0, changes);
        }

        public Task DeleteTextAsync(DocumentUri documentUri, params (int StartLine, int StartColumn, int EndLine, int EndColumn)[] changes)
        {
            return ReplaceTextAsync(documentUri, [.. changes.Select(change => (new LSP.Range
            {
                Start = new LSP.Position { Line = change.StartLine, Character = change.StartColumn },
                End = new LSP.Position { Line = change.EndLine, Character = change.EndColumn }
            }, string.Empty))]);
        }

        public Task CloseDocumentAsync(DocumentUri documentUri)
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
            await _languageServer.Value.WaitForExitAsync().ConfigureAwait(false);
        }

        public IList<LSP.Location> GetLocations(string locationName) => _locations[locationName];

        public Dictionary<string, IList<LSP.Location>> GetLocations() => _locations;

        public Solution GetCurrentSolution() => TestWorkspace.CurrentSolution;

        public LSP.ServerCapabilities GetServerCapabilities()
        {
            Contract.ThrowIfNull(_initializeResult, "Initialize has not been called");
            return _initializeResult.Capabilities;
        }

        public async Task AssertServerShuttingDownAsync()
        {
            var queueAccessor = GetQueueAccessor()!.Value;
            await queueAccessor.WaitForProcessingToStopAsync().ConfigureAwait(false);

            var shutdownTask = GetServerAccessor().GetShutdownTaskAsync();
            AssertEx.NotNull(shutdownTask, "Unexpected shutdown not started");

            // Shutdown task will close the queue, so we need to wait for it to complete.
            await shutdownTask.ConfigureAwait(false);
            Assert.True(queueAccessor.IsComplete(), "Unexpected queue not complete");
        }

        internal async Task WaitForDiagnosticsAsync()
        {
            var listenerProvider = TestWorkspace.GetService<IAsynchronousOperationListenerProvider>();

            await listenerProvider.GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();
            await listenerProvider.GetWaiter(FeatureAttribute.SolutionCrawlerLegacy).ExpeditedWaitAsync();
            await listenerProvider.GetWaiter(FeatureAttribute.DiagnosticService).ExpeditedWaitAsync();
        }

        internal async Task WaitForSourceGeneratorsAsync()
        {
            var operations = TestWorkspace.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
            await operations.WaitAllAsync(TestWorkspace, [FeatureAttribute.Workspace, FeatureAttribute.SourceGenerators]);
        }

        internal RequestExecutionQueue<RequestContext>.TestAccessor? GetQueueAccessor() => _languageServer.Value.GetTestAccessor().GetQueueAccessor();

        internal LspWorkspaceManager.TestAccessor GetManagerAccessor() => GetRequiredLspService<LspWorkspaceManager>().GetTestAccessor();

        internal LspWorkspaceManager GetManager() => GetRequiredLspService<LspWorkspaceManager>();

        internal AbstractLanguageServer<RequestContext>.TestAccessor GetServerAccessor() => _languageServer.Value.GetTestAccessor();

        internal T GetRequiredLspService<T>() where T : class, ILspService => _languageServer.Value.GetTestAccessor().GetRequiredLspService<T>();

        internal ImmutableArray<SourceText> GetTrackedTexts() => [.. GetManager().GetTrackedLspText().Values.Select(v => v.SourceText)];

        internal async ValueTask RunCodeAnalysisAsync(ProjectId? projectId)
        {
            var solution = GetCurrentSolution();
            if (projectId is null)
            {
                foreach (var project in solution.Projects)
                    await _codeAnalysisService.RunAnalysisAsync(project, CancellationToken.None);
            }
            else
            {
                await _codeAnalysisService.RunAnalysisAsync(solution.GetRequiredProject(projectId), CancellationToken.None);
            }
        }

        public async ValueTask DisposeAsync()
        {
            // Some tests will manually call shutdown and exit, so attempting to call this during dispose
            // will fail as the server's jsonrpc instance will be disposed of.
            if (!_languageServer.Value.GetTestAccessor().HasShutdownStarted())
            {
                await ShutdownTestServerAsync();
                await ExitTestServerAsync();
            }

            // Wait for all the exit notifications to run to completion.
            await _languageServer.Value.WaitForExitAsync();

            TestWorkspace.Dispose();
            _clientRpc.Dispose();
        }
    }
}
