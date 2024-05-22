// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.PdbSourceDocument;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.PdbSourceDocument
{
    [UseExportProvider]
    public abstract class AbstractPdbSourceDocumentTests
    {
        public enum Location
        {
            OnDisk,
            Embedded
        }

        protected static Task TestAsync(
            Location pdbLocation,
            Location sourceLocation,
            string metadataSource,
            Func<Compilation, ISymbol> symbolMatcher,
            string[]? preprocessorSymbols = null,
            bool buildReferenceAssembly = false,
            bool expectNullResult = false)
        {
            return RunTestAsync(path => TestAsync(
                path,
                pdbLocation,
                sourceLocation,
                metadataSource,
                symbolMatcher,
                preprocessorSymbols,
                buildReferenceAssembly,
                expectNullResult));
        }

        protected static async Task RunTestAsync(Func<string, Task> testRunner)
        {
            var path = Path.Combine(Path.GetTempPath(), nameof(PdbSourceDocumentTests));

            try
            {
                Directory.CreateDirectory(path);

                await testRunner(path);
            }
            finally
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
            }
        }

        protected static async Task TestAsync(
            string path,
            Location pdbLocation,
            Location sourceLocation,
            string metadataSource,
            Func<Compilation, ISymbol> symbolMatcher,
            string[]? preprocessorSymbols,
            bool buildReferenceAssembly,
            bool expectNullResult)
        {
            MarkupTestFile.GetSpan(metadataSource, out var source, out var expectedSpan);

            var (project, symbol) = await CompileAndFindSymbolAsync(
                path,
                pdbLocation,
                sourceLocation,
                source,
                symbolMatcher,
                preprocessorSymbols,
                buildReferenceAssembly,
                windowsPdb: false);

            await GenerateFileAndVerifyAsync(project, symbol, sourceLocation, source, expectedSpan, expectNullResult);
        }

        protected static async Task GenerateFileAndVerifyAsync(
            Project project,
            ISymbol symbol,
            Location sourceLocation,
            string expected,
            Text.TextSpan expectedSpan,
            bool expectNullResult)
        {
            var (actual, actualSpan) = await GetGeneratedSourceTextAsync(project, symbol, sourceLocation, expectNullResult);

            if (actual is null)
                return;

            // Compare exact texts and verify that the location returned is exactly that
            // indicated by expected
            AssertEx.EqualOrDiff(expected, actual.ToString());
            Assert.Equal(expectedSpan.Start, actualSpan.Start);
            Assert.Equal(expectedSpan.End, actualSpan.End);
        }

        protected static async Task<(SourceText?, TextSpan)> GetGeneratedSourceTextAsync(
            Project project,
            ISymbol symbol,
            Location sourceLocation,
            bool expectNullResult)
        {
            using var workspace = (EditorTestWorkspace)project.Solution.Workspace;

            var service = workspace.GetService<IMetadataAsSourceFileService>();
            try
            {
                // Using default settings here because none of the tests exercise any of the settings
                var file = await service.GetGeneratedFileAsync(workspace, project, symbol, signaturesOnly: false, MetadataAsSourceOptions.GetDefault(project.Services), CancellationToken.None).ConfigureAwait(false);

                if (expectNullResult)
                {
                    Assert.Same(NullResultMetadataAsSourceFileProvider.NullResult, file);
                    return (null, default);
                }
                else
                {
                    Assert.NotSame(NullResultMetadataAsSourceFileProvider.NullResult, file);
                }

                if (sourceLocation == Location.OnDisk)
                {
                    Assert.True(file.DocumentTitle.Contains($"[{FeaturesResources.external}]"));
                }
                else
                {
                    Assert.True(file.DocumentTitle.Contains($"[{FeaturesResources.embedded}]"));
                }

                AssertEx.NotNull(file, $"No source document was found in the pdb for the symbol.");

                var masWorkspace = service.TryGetWorkspace();

                var document = masWorkspace!.CurrentSolution.Projects.First().Documents.First(d => d.FilePath == file.FilePath);

                // Mapping the project from the generated document should map back to the original project
                var provider = workspace.ExportProvider.GetExportedValues<IMetadataAsSourceFileProvider>().OfType<PdbSourceDocumentMetadataAsSourceFileProvider>().Single();
                var mappedProject = provider.MapDocument(document);
                Assert.NotNull(mappedProject);
                Assert.Equal(project.Id, mappedProject!.Id);

                var actual = await document.GetTextAsync();
                var actualSpan = file!.IdentifierLocation.SourceSpan;

                return (actual, actualSpan);
            }
            finally
            {
                service.CleanupGeneratedFiles();
                service.TryGetWorkspace()?.Dispose();
            }
        }

        protected static Task<(Project, ISymbol)> CompileAndFindSymbolAsync(
            string path,
            Location pdbLocation,
            Location sourceLocation,
            string source,
            Func<Compilation, ISymbol> symbolMatcher,
            string[]? preprocessorSymbols = null,
            bool buildReferenceAssembly = false,
            bool windowsPdb = false,
            Encoding? encoding = null)
        {
            var sourceText = SourceText.From(source, encoding: encoding ?? Encoding.UTF8);
            return CompileAndFindSymbolAsync(path, pdbLocation, sourceLocation, sourceText, symbolMatcher, preprocessorSymbols, buildReferenceAssembly, windowsPdb);
        }

        protected static async Task<(Project, ISymbol)> CompileAndFindSymbolAsync(
            string path,
            Location pdbLocation,
            Location sourceLocation,
            SourceText source,
            Func<Compilation, ISymbol> symbolMatcher,
            string[]? preprocessorSymbols = null,
            bool buildReferenceAssembly = false,
            bool windowsPdb = false,
            Encoding? fallbackEncoding = null)
        {
            var preprocessorSymbolsAttribute = preprocessorSymbols?.Length > 0
                ? $"PreprocessorSymbols=\"{string.Join(";", preprocessorSymbols)}\""
                : "";

            var workspace = EditorTestWorkspace.Create(@$"
<Workspace>
    <Project Language=""{LanguageNames.CSharp}"" CommonReferences=""true"" ReferencesOnDisk=""true"" {preprocessorSymbolsAttribute}>
    </Project>
</Workspace>", composition: GetTestComposition());

            var project = workspace.CurrentSolution.Projects.First();

            CompileTestSource(path, source, project, pdbLocation, sourceLocation, buildReferenceAssembly, windowsPdb, fallbackEncoding);

            project = project.AddMetadataReference(MetadataReference.CreateFromFile(GetDllPath(path)));

            var mainCompilation = await project.GetRequiredCompilationAsync(CancellationToken.None).ConfigureAwait(false);

            var symbol = symbolMatcher(mainCompilation);

            AssertEx.NotNull(symbol, $"Couldn't find symbol to go-to-def for.");

            return (project, symbol);
        }

        protected static TestComposition GetTestComposition()
        {
            // We construct our own composition here because we only want the decompilation metadata as source provider
            // to be available.

            return EditorTestCompositions.EditorFeatures
                .WithExcludedPartTypes(ImmutableHashSet.Create(typeof(IMetadataAsSourceFileProvider)))
                .AddParts(typeof(PdbSourceDocumentMetadataAsSourceFileProvider), typeof(NullResultMetadataAsSourceFileProvider));
        }

        protected static void CompileTestSource(string path, SourceText source, Project project, Location pdbLocation, Location sourceLocation, bool buildReferenceAssembly, bool windowsPdb, Encoding? fallbackEncoding = null)
        {
            var dllFilePath = GetDllPath(path);
            var sourceCodePath = GetSourceFilePath(path);
            var pdbFilePath = GetPdbPath(path);
            var assemblyName = "reference";

            CompileTestSource(dllFilePath, sourceCodePath, pdbFilePath, assemblyName, source, project, pdbLocation, sourceLocation, buildReferenceAssembly, windowsPdb, fallbackEncoding);
        }

        protected static void CompileTestSource(string dllFilePath, string sourceCodePath, string? pdbFilePath, string assemblyName, SourceText source, Project project, Location pdbLocation, Location sourceLocation, bool buildReferenceAssembly, bool windowsPdb, Encoding? fallbackEncoding = null)
        {
            CompileTestSource(dllFilePath, [sourceCodePath], pdbFilePath, assemblyName, [source], project, pdbLocation, sourceLocation, buildReferenceAssembly, windowsPdb, fallbackEncoding);
        }

        protected static void CompileTestSource(string dllFilePath, string[] sourceCodePaths, string? pdbFilePath, string assemblyName, SourceText[] sources, Project project, Location pdbLocation, Location sourceLocation, bool buildReferenceAssembly, bool windowsPdb, Encoding? fallbackEncoding = null)
        {
            var compilationFactory = project.Solution.Services.GetRequiredLanguageService<ICompilationFactoryService>(LanguageNames.CSharp);
            var options = compilationFactory.GetDefaultCompilationOptions().WithOutputKind(OutputKind.DynamicallyLinkedLibrary);
            var parseOptions = project.ParseOptions;

            var compilation = compilationFactory
                .CreateCompilation(assemblyName, options)
                .AddSyntaxTrees(sources.Select((s, i) => SyntaxFactory.ParseSyntaxTree(s, options: parseOptions, path: sourceCodePaths[i])))
                .AddReferences(project.MetadataReferences);

            IEnumerable<EmbeddedText>? embeddedTexts;
            if (buildReferenceAssembly)
            {
                embeddedTexts = null;
            }
            else if (sourceLocation == Location.OnDisk)
            {
                embeddedTexts = null;
                for (var i = 0; i < sources.Length; i++)
                {
                    File.WriteAllText(sourceCodePaths[i], sources[i].ToString(), sources[i].Encoding);
                }
            }
            else
            {
                embeddedTexts = sources.Select((s, i) => EmbeddedText.FromSource(sourceCodePaths[i], s)).ToArray();
            }

            EmitOptions emitOptions;
            if (buildReferenceAssembly)
            {
                pdbFilePath = null;
                emitOptions = new EmitOptions(metadataOnly: true, includePrivateMembers: false);
            }
            else if (pdbLocation == Location.OnDisk)
            {
                emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb, pdbFilePath: pdbFilePath);
            }
            else
            {
                pdbFilePath = null;
                emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded);
            }

            // TODO: When supported, move this to pdbLocation
            if (windowsPdb)
            {
                emitOptions = emitOptions.WithDebugInformationFormat(DebugInformationFormat.Pdb);
            }

            if (fallbackEncoding is null)
            {
                emitOptions = emitOptions.WithDefaultSourceFileEncoding(sources[0].Encoding);
            }
            else
            {
                emitOptions = emitOptions.WithFallbackSourceFileEncoding(fallbackEncoding);
            }

            using (var dllStream = FileUtilities.CreateFileStreamChecked(File.Create, dllFilePath, nameof(dllFilePath)))
            using (var pdbStream = (pdbFilePath == null ? null : FileUtilities.CreateFileStreamChecked(File.Create, pdbFilePath, nameof(pdbFilePath))))
            {
                var result = compilation.Emit(dllStream, pdbStream, options: emitOptions, embeddedTexts: embeddedTexts);
                Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            }
        }

        protected static string GetDllPath(string path)
        {
            return Path.Combine(path, "reference.dll");
        }

        protected static string GetSourceFilePath(string path)
        {
            return Path.Combine(path, "source.cs");
        }

        protected static string GetPdbPath(string path)
        {
            return Path.Combine(path, "reference.pdb");
        }
    }
}
