// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SourceGenerators;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Roslyn.LanguageServer.Protocol;
using SumType = Roslyn.LanguageServer.Protocol.SumType<Roslyn.LanguageServer.Protocol.FullDocumentDiagnosticReport, Roslyn.LanguageServer.Protocol.UnchangedDocumentDiagnosticReport>;

namespace IdeBenchmarks.Lsp
{
    [MemoryDiagnoser]
    [GcServer(true)]
    public class LspSourceGeneratorBenchmarks : AbstractLanguageServerProtocolTests
    {
        private readonly UseExportProviderAttribute _useExportProviderAttribute = new UseExportProviderAttribute();

        private TestLspServer? _testServer;

        /// <summary>
        /// Number of typing edits to perform per benchmark iteration.
        /// </summary>
        [Params(50, 1000)]
        public int TypingIterations { get; set; }

        [Params("Automatic", "Balanced")]
        public string ExecutionPreference { get; set; } = "Automatic";

        public LspSourceGeneratorBenchmarks() : base(null)
        {
        }

        private SourceGeneratorExecutionPreference GetExecutionPreference()
            => ExecutionPreference == "Balanced"
                ? SourceGeneratorExecutionPreference.Balanced
                : SourceGeneratorExecutionPreference.Automatic;

        [GlobalSetup]
        public void GlobalSetup()
        {
        }

        [IterationSetup]
        public void IterationSetup() => LoadSolutionAsync().Wait();

        private async Task LoadSolutionAsync()
        {
            _useExportProviderAttribute.Before(null);

            // Typing extends the partial method name: partial void M() → Ma() → Maa() → ...
            // Each keystroke changes the method name, forcing the generator to update its output.
            var markup = """
                public partial class C
                {
                    public int F => _field;
                    partial void M{|typing:|}();
                }
                """;

            _testServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace: true);

            // Set the execution preference.
            var configService = _testServer.TestWorkspace.ExportProvider.GetExportedValue<TestWorkspaceConfigurationService>();
            configService.Options = new WorkspaceConfigurationOptions(SourceGeneratorExecution: GetExecutionPreference());

            var document = _testServer.GetCurrentSolution().Projects.Single().Documents.Single();

            // Add a source generator that produces a partial class with a backing field
            // when it detects "public partial class C" in the compilation.
            var generator = new BenchmarkSourceGenerator();

            _testServer.TestWorkspace.OnAnalyzerReferenceAdded(
                document.Project.Id,
                new TestGeneratorReference(generator));

            await _testServer.WaitForSourceGeneratorsAsync();

            // Open the document in LSP so edits flow through the server.
            await _testServer.OpenDocumentAsync(document.GetURI());
        }

        [Benchmark]
        public async Task TypeAndWaitForSourceGenerators()
        {
            var testServer = _testServer!;

            var typingLocation = testServer.GetLocations("typing").Single();
            var documentUri = typingLocation.DocumentUri;
            var typingLine = typingLocation.Range.Start.Line;
            var typingColumn = typingLocation.Range.Start.Character;

            // Simulate typing one character at a time and waiting for source generators after each keystroke.
            // After each wait, pull diagnostics for the document (mirrors what VS Code does on every change).
            for (var i = 0; i < TypingIterations; i++)
            {
                await testServer.InsertTextAsync(documentUri, (typingLine, typingColumn + i, "a"));
                await testServer.WaitForSourceGeneratorsAsync();

                await testServer.WaitForDiagnosticsAsync();
                await testServer.ExecuteRequestAsync<LSP.DocumentDiagnosticParams, SumType>(
                    LSP.Methods.TextDocumentDiagnosticName,
                    new LSP.DocumentDiagnosticParams
                    {
                        TextDocument = new LSP.TextDocumentIdentifier { DocumentUri = documentUri },
                    },
                    CancellationToken.None);
            }

            // After all typing, refresh source generators (simulates save / explicit refresh in balanced mode).
            await testServer.RefreshSourceGeneratorsAsync(forceRegeneration: false);

            // Request the source generated document text to verify the generator ran.
            var sourceGeneratedDocuments = await testServer.GetCurrentSolution().Projects.Single().GetSourceGeneratedDocumentsAsync();
            Assert.NotEmpty(sourceGeneratedDocuments);

            var sgIdentity = sourceGeneratedDocuments.Single().Identity;
            var sgUri = SourceGeneratedDocumentUri.Create(sgIdentity);
            var sgText = await testServer.ExecuteRequestAsync<SourceGeneratorGetTextParams, SourceGeneratedDocumentText>(
                SourceGeneratedDocumentGetTextHandler.MethodName,
                new SourceGeneratorGetTextParams(new LSP.TextDocumentIdentifier { DocumentUri = sgUri }, ResultId: null),
                CancellationToken.None);

            AssertEx.NotNull(sgText);
            Assert.Contains("public partial class C", sgText.Text);
        }

        [IterationCleanup]
        public void Cleanup()
        {
            if (_testServer is not null)
            {
                _testServer.DisposeAsync().AsTask().Wait();
            }

            _useExportProviderAttribute.After(null);
        }

        /// <summary>
        /// A source generator that looks for type "C" in the compilation, generates a backing
        /// field (<c>_field</c>), and produces implementations for any partial methods it finds.
        /// As the user types and the partial method name grows (M → Ma → Maa …), the generated
        /// output changes on every keystroke.
        /// </summary>
#pragma warning disable RS1042 // test only generator
        private sealed class BenchmarkSourceGenerator : ISourceGenerator
#pragma warning restore RS1042 // test only generator
        {
            public void Initialize(GeneratorInitializationContext context)
            {
            }

            public void Execute(GeneratorExecutionContext context)
            {
                var typeC = context.Compilation.GetTypeByMetadataName("C");
                if (typeC is null)
                    return;

                var sb = new StringBuilder();
                sb.AppendLine("public partial class C");
                sb.AppendLine("{");
                sb.AppendLine("    private readonly int _field = 1;");

                // Generate implementations for every partial method definition on C.
                foreach (var member in typeC.GetMembers())
                {
                    if (member is IMethodSymbol { IsPartialDefinition: true } method)
                    {
                        sb.AppendLine($"    partial void {method.Name}() {{ }}");
                    }
                }

                sb.AppendLine("}");

                context.AddSource("GeneratedPartial", SourceText.From(sb.ToString(), Encoding.UTF8));
            }
        }
    }
}
