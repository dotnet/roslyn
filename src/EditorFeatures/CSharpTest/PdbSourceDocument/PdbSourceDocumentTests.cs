// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.PdbSourceDocument
{
    public partial class PdbSourceDocumentTests : AbstractPdbSourceDocumentTests
    {
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
        public async Task ReferenceAssembly_WithImplementation()
        {
            var source = @"
public class C
{
    // A change
    public event System.EventHandler [|E|] { add { } remove { } }
}";

            await RunTestAsync(async path =>
            {
                MarkupTestFile.GetSpan(source, out var metadataSource, out var expectedSpan);

                // Laziest. Nuget package directory layout. Ever.
                Directory.CreateDirectory(Path.Combine(path, "ref"));
                Directory.CreateDirectory(Path.Combine(path, "lib"));

                // Compile reference assembly
                var sourceText = SourceText.From(metadataSource, encoding: Encoding.UTF8);
                var (project, symbol) = await CompileAndFindSymbolAsync(Path.Combine(path, "ref"), Location.Embedded, Location.OnDisk, sourceText, c => c.GetMember("C.E"), buildReferenceAssembly: true);

                // Compile implementation assembly
                CompileTestSource(Path.Combine(path, "lib"), sourceText, project, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

                await GenerateFileAndVerifyAsync(project, symbol, Location.Embedded, metadataSource.ToString(), expectedSpan, expectNullResult: false);
            });
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

                await GenerateFileAndVerifyAsync(project, symbol, Location.OnDisk, source, expectedSpan, expectNullResult: true);
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

                await GenerateFileAndVerifyAsync(project, symbol, Location.OnDisk, source, expectedSpan, expectNullResult: true);
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

                await GenerateFileAndVerifyAsync(project, symbol, Location.OnDisk, source, expectedSpan, expectNullResult: true);
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
                await GenerateFileAndVerifyAsync(project, symbol, Location.OnDisk, source, expectedSpan, expectNullResult: true);
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

                await GenerateFileAndVerifyAsync(project, symbol, Location.OnDisk, source, expectedSpan, expectNullResult: true);
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

                await GenerateFileAndVerifyAsync(project, symbol, Location.OnDisk, source, expectedSpan, expectNullResult: true);
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

                await GenerateFileAndVerifyAsync(project, symbol, Location.OnDisk, source1, expectedSpan, expectNullResult: true);
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

                await GenerateFileAndVerifyAsync(project, symbol, Location.OnDisk, metadataSource, expectedSpan, expectNullResult: true);
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

                var (actualText, _) = await GetGeneratedSourceTextAsync(project, symbol, Location.Embedded, expectNullResult: false);

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

                var (actualText, _) = await GetGeneratedSourceTextAsync(project, symbol, Location.Embedded, expectNullResult: false);

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

                var (actualText, _) = await GetGeneratedSourceTextAsync(project, symbol, Location.Embedded, expectNullResult: false);

                AssertEx.NotNull(actualText);
                AssertEx.NotNull(actualText.Encoding);
                AssertEx.Equal(encoding.WebName, actualText.Encoding.WebName);
                AssertEx.EqualOrDiff(source, actualText.ToString());
            });
        }
    }
}
