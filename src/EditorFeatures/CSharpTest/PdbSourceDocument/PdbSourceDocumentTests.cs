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
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Host;
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
    public partial class PdbSourceDocumentTests
    {
        public enum Location
        {
            OnDisk,
            Embedded
        }

        [Theory]
        [CombinatorialData]
        public async Task PreprocessorSymbols1(Location pdbLocation, Location sourceLocation)
        {
            var source = @"
public class C
{
#if SOME_DEFINED_CONSTANT
    public void [|M|]()
    {
    }
#else
    public void M()
    {
    }
#endif
}";
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C.M"), preprocessorSymbols: new[] { "SOME_DEFINED_CONSTANT" });
        }

        [Theory]
        [CombinatorialData]
        public async Task PreprocessorSymbols2(Location pdbLocation, Location sourceLocation)
        {
            var source = @"
public class C
{
#if SOME_DEFINED_CONSTANT
    public void M()
    {
    }
#else
    public void [|M|]()
    {
    }
#endif
}";
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C.M"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Method(Location pdbLocation, Location sourceLocation)
        {
            var source = @"
public class C
{
    public void [|M|]()
    {
        // this is a comment that wouldn't appear in decompiled source
    }
}";
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C.M"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Constructor(Location pdbLocation, Location sourceLocation)
        {
            var source = @"
public class C
{
    public [|C|]()
    {
        // this is a comment that wouldn't appear in decompiled source
    }
}";
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C..ctor"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Parameter(Location pdbLocation, Location sourceLocation)
        {
            var source = @"
public class C
{
    public void M(int [|a|])
    {
        // this is a comment that wouldn't appear in decompiled source
    }
}";
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember<IMethodSymbol>("C.M").Parameters.First());
        }

        [Theory]
        [CombinatorialData]
        public async Task Class_FromTypeDefinitionDocument(Location pdbLocation, Location sourceLocation)
        {
            var source = @"
public class [|C|]
{
    // this is a comment that wouldn't appear in decompiled source
}";

            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Constructor_FromTypeDefinitionDocument(Location pdbLocation, Location sourceLocation)
        {
            var source = @"
public class [|C|]
{
    // this is a comment that wouldn't appear in decompiled source
}";
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C..ctor"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NestedClass_FromTypeDefinitionDocument(Location pdbLocation, Location sourceLocation)
        {
            var source = @"
public class Outer
{
    public class [|C|]
    {
        // this is a comment that wouldn't appear in decompiled source
    }
}";
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("Outer.C"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NestedClassConstructor_FromTypeDefinitionDocument(Location pdbLocation, Location sourceLocation)
        {
            var source = @"
public class Outer
{
    public class [|C|]
    {
        // this is a comment that wouldn't appear in decompiled source
    }
}";
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("Outer.C..ctor"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Class_FromTypeDefinitionDocumentOfNestedClass(Location pdbLocation, Location sourceLocation)
        {
            var source = @"
public class [|Outer|]
{
    public class C
    {
        // this is a comment that wouldn't appear in decompiled source
    }
}";
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("Outer"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Constructor_FromTypeDefinitionDocumentOfNestedClass(Location pdbLocation, Location sourceLocation)
        {
            var source = @"
public class [|Outer|]
{
    public class C
    {
        // this is a comment that wouldn't appear in decompiled source
    }
}";
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("Outer..ctor"));

        }

        [Theory]
        [CombinatorialData]
        public async Task NestedClass_FromMethodDocument(Location pdbLocation, Location sourceLocation)
        {
            var source = @"
public class Outer
{
    public class [|C|]
    {
        public void M()
        {
            // this is a comment that wouldn't appear in decompiled source
        }
    }
}";
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("Outer.C"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NestedClassConstructor_FromMethodDocument(Location pdbLocation, Location sourceLocation)
        {
            var source = @"
public class Outer
{
    public class [|C|]
    {
        public void M()
        {
            // this is a comment that wouldn't appear in decompiled source
        }
    }
}";

            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("Outer.C..ctor"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Class_FromMethodDocumentOfNestedClass(Location pdbLocation, Location sourceLocation)
        {
            var source = @"
public class [|Outer|]
{
    public class C
    {
        public void M()
        {
            // this is a comment that wouldn't appear in decompiled source
        }
    }
}";

            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("Outer"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Constructor_FromMethodDocumentOfNestedClass(Location pdbLocation, Location sourceLocation)
        {
            var source = @"
public class [|Outer|]
{
    public class C
    {
        public void M()
        {
            // this is a comment that wouldn't appear in decompiled source
        }
    }
}";

            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("Outer..ctor"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Class_FromMethodDocument(Location pdbLocation, Location sourceLocation)
        {
            var source = @"
public class [|C|]
{
    public void M()
    {
        // this is a comment that wouldn't appear in decompiled source
    }
}";
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Constructor_FromMethodDocument(Location pdbLocation, Location sourceLocation)
        {
            var source = @"
public class [|C|]
{
    public void M()
    {
        // this is a comment that wouldn't appear in decompiled source
    }
}";
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C..ctor"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Field(Location pdbLocation, Location sourceLocation)
        {
            var source = @"
public class C
{
    public int [|f|];
}";
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C.f"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Property(Location pdbLocation, Location sourceLocation)
        {
            var source = @"
public class C
{
    public int [|P|] { get; set; }
}";
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C.P"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Property_WithBody(Location pdbLocation, Location sourceLocation)
        {
            var source = @"
public class C
{
    public int [|P|] { get { return 1; } }
}";
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C.P"));
        }

        [Theory]
        [CombinatorialData]
        public async Task EventField(Location pdbLocation, Location sourceLocation)
        {
            var source = @"
public class C
{
    public event System.EventHandler [|E|];
}";
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C.E"));
        }

        [Theory]
        [CombinatorialData]
        public async Task EventField_WithMethod(Location pdbLocation, Location sourceLocation)
        {
            var source = @"
public class C
{
    public event System.EventHandler [|E|];

    public void M()
    {
        // this is a comment that wouldn't appear in decompiled source
    }
}";
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C.E"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Event(Location pdbLocation, Location sourceLocation)
        {
            var source = @"
public class C
{
    public event System.EventHandler [|E|] { add { } remove { } }
}";
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C.E"));
        }

        [Fact]
        public async Task ReferenceAssembly_NullResult()
        {
            var source = @"
public class C
{
    public event System.EventHandler [|E|] { add { } remove { } }
}";
            // A pdb won't be emitted when building a reference assembly so the first two parameters don't actually matter
            await TestAsync(Location.OnDisk, Location.OnDisk, source, c => c.GetMember("C.E"), buildReferenceAssembly: true, expectNullResult: true);
        }

        [Fact]
        public async Task NoPdb_NullResult()
        {
            var source = @"
public class C
{
    public event System.EventHandler [|E|] { add { } remove { } }
}";

            await RunTestAsync(async path =>
            {
                MarkupTestFile.GetSpan(source, out var metadataSource, out var expectedSpan);

                var (project, symbol) = await CompileAndFindSymbolAsync(path, Location.OnDisk, Location.OnDisk, metadataSource, c => c.GetMember("C.E"));

                // Now delete the PDB
                File.Delete(GetPdbPath(path));

                await GenerateFileAndVerifyAsync(project, symbol, source, expectedSpan, expectNullResult: true);
            });
        }

        [Fact]
        public async Task NoDll_NullResult()
        {
            var source = @"
public class C
{
    public event System.EventHandler [|E|] { add { } remove { } }
}";

            await RunTestAsync(async path =>
            {
                MarkupTestFile.GetSpan(source, out var metadataSource, out var expectedSpan);

                var (project, symbol) = await CompileAndFindSymbolAsync(path, Location.OnDisk, Location.OnDisk, metadataSource, c => c.GetMember("C.E"));

                // Now delete the DLL
                File.Delete(GetDllPath(path));

                await GenerateFileAndVerifyAsync(project, symbol, source, expectedSpan, expectNullResult: true);
            });
        }

        [Fact]
        public async Task NoSource_NullResult()
        {
            var source = @"
public class C
{
    public event System.EventHandler [|E|] { add { } remove { } }
}";
            await RunTestAsync(async path =>
            {
                MarkupTestFile.GetSpan(source, out var metadataSource, out var expectedSpan);

                var (project, symbol) = await CompileAndFindSymbolAsync(path, Location.OnDisk, Location.OnDisk, metadataSource, c => c.GetMember("C.E"));

                // Now delete the source
                File.Delete(GetSourceFilePath(path));

                await GenerateFileAndVerifyAsync(project, symbol, source, expectedSpan, expectNullResult: true);
            });
        }

        [Fact]
        public async Task WindowsPdb_NullResult()
        {
            var source = @"
public class C
{
    public event System.EventHandler [|E|] { add { } remove { } }
}";
            await RunTestAsync(async path =>
            {
                MarkupTestFile.GetSpan(source, out var metadataSource, out var expectedSpan);

                var (project, symbol) = await CompileAndFindSymbolAsync(path, Location.OnDisk, Location.OnDisk, metadataSource, c => c.GetMember("C.E"), windowsPdb: true);

                //TODO: This should not be a null result: https://github.com/dotnet/roslyn/issues/55834
                await GenerateFileAndVerifyAsync(project, symbol, source, expectedSpan, expectNullResult: true);
            });
        }

        [Fact]
        public async Task EmptyPdb_NullResult()
        {
            var source = @"
public class C
{
    public event System.EventHandler [|E|] { add { } remove { } }
}";

            await RunTestAsync(async path =>
            {
                MarkupTestFile.GetSpan(source, out var metadataSource, out var expectedSpan);

                var (project, symbol) = await CompileAndFindSymbolAsync(path, Location.OnDisk, Location.OnDisk, metadataSource, c => c.GetMember("C.E"));

                // Now make the PDB a zero byte file
                File.WriteAllBytes(GetPdbPath(path), new byte[0]);

                await GenerateFileAndVerifyAsync(project, symbol, source, expectedSpan, expectNullResult: true);
            });
        }

        [Fact]
        public async Task CorruptPdb_NullResult()
        {
            var source = @"
public class C
{
    public event System.EventHandler [|E|] { add { } remove { } }
}";

            await RunTestAsync(async path =>
            {
                MarkupTestFile.GetSpan(source, out var metadataSource, out var expectedSpan);

                var (project, symbol) = await CompileAndFindSymbolAsync(path, Location.OnDisk, Location.OnDisk, metadataSource, c => c.GetMember("C.E"));

                // The first four bytes of this are BSJB so it is identified as a portable PDB.
                // The next two bytes are unimportant, they're just not valid PDB data.
                var corruptPdb = new byte[] { 66, 83, 74, 66, 68, 87 };
                File.WriteAllBytes(GetPdbPath(path), corruptPdb);

                await GenerateFileAndVerifyAsync(project, symbol, source, expectedSpan, expectNullResult: true);
            });
        }

        [Fact]
        public async Task OldPdb_NullResult()
        {
            var source1 = @"
public class C
{
    public event System.EventHandler [|E|] { add { } remove { } }
}";
            var source2 = @"
public class C
{
    // A change
    public event System.EventHandler E { add { } remove { } }
}";

            await RunTestAsync(async path =>
            {
                MarkupTestFile.GetSpan(source1, out var metadataSource, out var expectedSpan);

                var (project, symbol) = await CompileAndFindSymbolAsync(path, Location.OnDisk, Location.OnDisk, metadataSource, c => c.GetMember("C.E"));

                // Archive off the current PDB so we can restore it later
                var pdbFilePath = GetPdbPath(path);
                var archivePdbFilePath = pdbFilePath + ".old";
                File.Move(pdbFilePath, archivePdbFilePath);

                CompileTestSource(path, SourceText.From(source2, Encoding.UTF8), project, Location.OnDisk, Location.OnDisk, buildReferenceAssembly: false, windowsPdb: false);

                // Move the old file back, so the PDB is now old
                File.Delete(pdbFilePath);
                File.Move(archivePdbFilePath, pdbFilePath);

                await GenerateFileAndVerifyAsync(project, symbol, source1, expectedSpan, expectNullResult: true);
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task SourceFileChecksumIncorrect_NullResult(Location pdbLocation)
        {
            var source1 = @"
public class C
{
    public event System.EventHandler [|E|] { add { } remove { } }
}";
            var source2 = @"
public class C
{
    // A change
    public event System.EventHandler E { add { } remove { } }
}";

            await RunTestAsync(async path =>
            {
                MarkupTestFile.GetSpan(source1, out var metadataSource, out var expectedSpan);

                var (project, symbol) = await CompileAndFindSymbolAsync(path, pdbLocation, Location.OnDisk, metadataSource, c => c.GetMember("C.E"));

                File.WriteAllText(GetSourceFilePath(path), source2, Encoding.UTF8);

                await GenerateFileAndVerifyAsync(project, symbol, metadataSource, expectedSpan, expectNullResult: true);
            });
        }

        [Theory]
        [InlineData(Location.Embedded, "utf-16")]
        [InlineData(Location.Embedded, "utf-16BE")]
        [InlineData(Location.Embedded, "utf-32")]
        [InlineData(Location.Embedded, "utf-32BE")]
        [InlineData(Location.Embedded, "us-ascii")]
        [InlineData(Location.Embedded, "iso-8859-1")]
        [InlineData(Location.Embedded, "utf-8")]
        [InlineData(Location.OnDisk, "utf-16")]
        [InlineData(Location.OnDisk, "utf-16BE")]
        [InlineData(Location.OnDisk, "utf-32")]
        [InlineData(Location.OnDisk, "utf-32BE")]
        [InlineData(Location.OnDisk, "us-ascii")]
        [InlineData(Location.OnDisk, "iso-8859-1")]
        [InlineData(Location.OnDisk, "utf-8")]
        public async Task EncodedEmbeddedSource(Location pdbLocation, string encodingWebName)
        {
            var source = @"
public class C
{
    public event System.EventHandler E { add { } remove { } }
}";

            var encoding = Encoding.GetEncoding(encodingWebName);

            await RunTestAsync(async path =>
            {
                using var ms = new MemoryStream(encoding.GetBytes(source));
                var encodedSourceText = EncodedStringText.Create(ms, encoding, canBeEmbedded: true);

                var (project, symbol) = await CompileAndFindSymbolAsync(path, pdbLocation, Location.Embedded, encodedSourceText, c => c.GetMember("C.E"));

                var (actualText, _) = await GetGeneratedSourceTextAsync(project, symbol, expectNullResult: false);

                AssertEx.NotNull(actualText);
                AssertEx.NotNull(actualText.Encoding);
                AssertEx.Equal(encoding.WebName, actualText.Encoding.WebName);
                AssertEx.EqualOrDiff(source, actualText.ToString());
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task EncodedEmbeddedSource_SJIS(Location pdbLocation)
        {
            var source = @"
public class C
{
    // ワ
    public event System.EventHandler E { add { } remove { } }
}";

            var encoding = Encoding.GetEncoding("SJIS");

            await RunTestAsync(async path =>
            {
                using var ms = new MemoryStream(encoding.GetBytes(source));
                var encodedSourceText = EncodedStringText.Create(ms, encoding, canBeEmbedded: true);

                var (project, symbol) = await CompileAndFindSymbolAsync(path, pdbLocation, Location.Embedded, encodedSourceText, c => c.GetMember("C.E"));

                var (actualText, _) = await GetGeneratedSourceTextAsync(project, symbol, expectNullResult: false);

                AssertEx.NotNull(actualText);
                AssertEx.NotNull(actualText.Encoding);
                AssertEx.Equal(encoding.WebName, actualText.Encoding.WebName);
                AssertEx.EqualOrDiff(source, actualText.ToString());
            });
        }

        [Theory]
        [CombinatorialData]
        public async Task EncodedEmbeddedSource_SJIS_FallbackEncoding(Location pdbLocation)
        {
            var source = @"
public class C
{
    // ワ
    public event System.EventHandler E { add { } remove { } }
}";

            var encoding = Encoding.GetEncoding("SJIS");

            await RunTestAsync(async path =>
            {
                using var ms = new MemoryStream(encoding.GetBytes(source));
                var encodedSourceText = EncodedStringText.Create(ms, encoding, canBeEmbedded: true);

                var (project, symbol) = await CompileAndFindSymbolAsync(path, pdbLocation, Location.Embedded, encodedSourceText, c => c.GetMember("C.E"), fallbackEncoding: encoding);

                var (actualText, _) = await GetGeneratedSourceTextAsync(project, symbol, expectNullResult: false);

                AssertEx.NotNull(actualText);
                AssertEx.NotNull(actualText.Encoding);
                AssertEx.Equal(encoding.WebName, actualText.Encoding.WebName);
                AssertEx.EqualOrDiff(source, actualText.ToString());
            });
        }

        private static Task TestAsync(
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

        private static async Task RunTestAsync(Func<string, Task> testRunner)
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

        private static async Task TestAsync(
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

            await GenerateFileAndVerifyAsync(project, symbol, source, expectedSpan, expectNullResult);
        }

        private static async Task GenerateFileAndVerifyAsync(
            Project project,
            ISymbol symbol,
            string expected,
            Text.TextSpan expectedSpan,
            bool expectNullResult)
        {
            var (actual, actualSpan) = await GetGeneratedSourceTextAsync(project, symbol, expectNullResult);

            if (actual is null)
                return;

            // Compare exact texts and verify that the location returned is exactly that
            // indicated by expected
            AssertEx.EqualOrDiff(expected, actual.ToString());
            Assert.Equal(expectedSpan.Start, actualSpan.Start);
            Assert.Equal(expectedSpan.End, actualSpan.End);
        }

        private static async Task<(SourceText?, TextSpan)> GetGeneratedSourceTextAsync(
            Project project,
            ISymbol symbol,
            bool expectNullResult)
        {
            using var workspace = (TestWorkspace)project.Solution.Workspace;

            var service = workspace.GetService<IMetadataAsSourceFileService>();
            try
            {
                var file = await service.GetGeneratedFileAsync(project, symbol, signaturesOnly: false, allowDecompilation: false, CancellationToken.None).ConfigureAwait(false);

                if (expectNullResult)
                {
                    Assert.Same(NullResultMetadataAsSourceFileProvider.NullResult, file);
                    return (null, default);
                }
                else
                {
                    Assert.NotSame(NullResultMetadataAsSourceFileProvider.NullResult, file);
                }

                AssertEx.NotNull(file, $"No source document was found in the pdb for the symbol.");

                var masWorkspace = service.TryGetWorkspace();

                var document = masWorkspace!.CurrentSolution.Projects.First().Documents.First();

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

        private static Task<(Project, ISymbol)> CompileAndFindSymbolAsync(
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

        private static async Task<(Project, ISymbol)> CompileAndFindSymbolAsync(
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

            // We construct our own composition here because we only want the decompilation metadata as source provider
            // to be available.
            var composition = EditorTestCompositions.EditorFeatures
                .WithExcludedPartTypes(ImmutableHashSet.Create(typeof(IMetadataAsSourceFileProvider)))
                .AddParts(typeof(PdbSourceDocumentMetadataAsSourceFileProvider), typeof(NullResultMetadataAsSourceFileProvider));

            var workspace = TestWorkspace.Create(@$"
<Workspace>
    <Project Language=""{LanguageNames.CSharp}"" CommonReferences=""true"" ReferencesOnDisk=""true"" {preprocessorSymbolsAttribute}>
    </Project>
</Workspace>", composition: composition);

            var project = workspace.CurrentSolution.Projects.First();

            CompileTestSource(path, source, project, pdbLocation, sourceLocation, buildReferenceAssembly, windowsPdb, fallbackEncoding);

            project = project.AddMetadataReference(MetadataReference.CreateFromFile(GetDllPath(path)));

            var mainCompilation = await project.GetRequiredCompilationAsync(CancellationToken.None).ConfigureAwait(false);

            var symbol = symbolMatcher(mainCompilation);

            AssertEx.NotNull(symbol, $"Couldn't find symbol to go-to-def for.");

            return (project, symbol);
        }

        private static void CompileTestSource(string path, SourceText source, Project project, Location pdbLocation, Location sourceLocation, bool buildReferenceAssembly, bool windowsPdb, Encoding? fallbackEncoding = null)
        {
            var dllFilePath = GetDllPath(path);
            var sourceCodePath = GetSourceFilePath(path);
            var pdbFilePath = GetPdbPath(path);

            var assemblyName = "ReferencedAssembly";

            var languageServices = project.Solution.Workspace.Services.GetLanguageServices(LanguageNames.CSharp);
            var compilationFactory = languageServices.GetRequiredService<ICompilationFactoryService>();
            var options = compilationFactory.GetDefaultCompilationOptions().WithOutputKind(OutputKind.DynamicallyLinkedLibrary);
            var parseOptions = project.ParseOptions;

            var compilation = compilationFactory
                .CreateCompilation(assemblyName, options)
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(source, options: parseOptions, path: sourceCodePath))
                .AddReferences(project.MetadataReferences);

            IEnumerable<EmbeddedText>? embeddedTexts;
            if (sourceLocation == Location.OnDisk)
            {
                embeddedTexts = null;
                File.WriteAllText(sourceCodePath, source.ToString(), source.Encoding);
            }
            else
            {
                embeddedTexts = new[] { EmbeddedText.FromSource(sourceCodePath, source) };
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
                emitOptions = emitOptions.WithDefaultSourceFileEncoding(source.Encoding);
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

        private static string GetDllPath(string path)
        {
            return Path.Combine(path, "reference.dll");
        }

        private static string GetSourceFilePath(string path)
        {
            return Path.Combine(path, "source.cs");
        }

        private static string GetPdbPath(string path)
        {
            return Path.Combine(path, "reference.pdb");
        }
    }
}
