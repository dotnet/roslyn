// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
            var source = """
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
                }
                """;
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C.M"), preprocessorSymbols: new[] { "SOME_DEFINED_CONSTANT" });
        }

        [Theory]
        [CombinatorialData]
        public async Task PreprocessorSymbols2(Location pdbLocation, Location sourceLocation)
        {
            var source = """
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
                }
                """;
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C.M"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Method(Location pdbLocation, Location sourceLocation)
        {
            var source = """
                public class C
                {
                    public void [|M|]()
                    {
                        // this is a comment that wouldn't appear in decompiled source
                    }
                }
                """;
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C.M"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Constructor(Location pdbLocation, Location sourceLocation)
        {
            var source = """
                public class C
                {
                    public [|C|]()
                    {
                        // this is a comment that wouldn't appear in decompiled source
                    }
                }
                """;
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C..ctor"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Parameter(Location pdbLocation, Location sourceLocation)
        {
            var source = """
                public class C
                {
                    public void M(int [|a|])
                    {
                        // this is a comment that wouldn't appear in decompiled source
                    }
                }
                """;
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember<IMethodSymbol>("C.M").Parameters.First());
        }

        [Theory]
        [CombinatorialData]
        public async Task Class_FromTypeDefinitionDocument(Location pdbLocation, Location sourceLocation)
        {
            var source = """
                public class [|C|]
                {
                    // this is a comment that wouldn't appear in decompiled source
                }
                """;

            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Constructor_FromTypeDefinitionDocument(Location pdbLocation, Location sourceLocation)
        {
            var source = """
                public class [|C|]
                {
                    // this is a comment that wouldn't appear in decompiled source
                }
                """;
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C..ctor"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NestedClass_FromTypeDefinitionDocument(Location pdbLocation, Location sourceLocation)
        {
            var source = """
                public class Outer
                {
                    public class [|C|]
                    {
                        // this is a comment that wouldn't appear in decompiled source
                    }
                }
                """;
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("Outer.C"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NestedClassConstructor_FromTypeDefinitionDocument(Location pdbLocation, Location sourceLocation)
        {
            var source = """
                public class Outer
                {
                    public class [|C|]
                    {
                        // this is a comment that wouldn't appear in decompiled source
                    }
                }
                """;
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("Outer.C..ctor"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Class_FromTypeDefinitionDocumentOfNestedClass(Location pdbLocation, Location sourceLocation)
        {
            var source = """
                public class [|Outer|]
                {
                    public class C
                    {
                        // this is a comment that wouldn't appear in decompiled source
                    }
                }
                """;
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("Outer"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Constructor_FromTypeDefinitionDocumentOfNestedClass(Location pdbLocation, Location sourceLocation)
        {
            var source = """
                public class [|Outer|]
                {
                    public class C
                    {
                        // this is a comment that wouldn't appear in decompiled source
                    }
                }
                """;
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("Outer..ctor"));

        }

        [Theory]
        [CombinatorialData]
        public async Task NestedClass_FromMethodDocument(Location pdbLocation, Location sourceLocation)
        {
            var source = """
                public class Outer
                {
                    public class [|C|]
                    {
                        public void M()
                        {
                            // this is a comment that wouldn't appear in decompiled source
                        }
                    }
                }
                """;
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("Outer.C"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NestedClassConstructor_FromMethodDocument(Location pdbLocation, Location sourceLocation)
        {
            var source = """
                public class Outer
                {
                    public class [|C|]
                    {
                        public void M()
                        {
                            // this is a comment that wouldn't appear in decompiled source
                        }
                    }
                }
                """;

            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("Outer.C..ctor"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Class_FromMethodDocumentOfNestedClass(Location pdbLocation, Location sourceLocation)
        {
            var source = """
                public class [|Outer|]
                {
                    public class C
                    {
                        public void M()
                        {
                            // this is a comment that wouldn't appear in decompiled source
                        }
                    }
                }
                """;

            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("Outer"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Constructor_FromMethodDocumentOfNestedClass(Location pdbLocation, Location sourceLocation)
        {
            var source = """
                public class [|Outer|]
                {
                    public class C
                    {
                        public void M()
                        {
                            // this is a comment that wouldn't appear in decompiled source
                        }
                    }
                }
                """;

            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("Outer..ctor"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Class_FromMethodDocument(Location pdbLocation, Location sourceLocation)
        {
            var source = """
                public class [|C|]
                {
                    public void M()
                    {
                        // this is a comment that wouldn't appear in decompiled source
                    }
                }
                """;
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Constructor_FromMethodDocument(Location pdbLocation, Location sourceLocation)
        {
            var source = """
                public class [|C|]
                {
                    public void M()
                    {
                        // this is a comment that wouldn't appear in decompiled source
                    }
                }
                """;
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C..ctor"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Field(Location pdbLocation, Location sourceLocation)
        {
            var source = """
                public class C
                {
                    public int [|f|];
                }
                """;
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C.f"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Property(Location pdbLocation, Location sourceLocation)
        {
            var source = """
                public class C
                {
                    public int [|P|] { get; set; }
                }
                """;
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C.P"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Property_WithBody(Location pdbLocation, Location sourceLocation)
        {
            var source = """
                public class C
                {
                    public int [|P|] { get { return 1; } }
                }
                """;
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C.P"));
        }

        [Theory]
        [CombinatorialData]
        public async Task EventField(Location pdbLocation, Location sourceLocation)
        {
            var source = """
                public class C
                {
                    public event System.EventHandler [|E|];
                }
                """;
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C.E"));
        }

        [Theory]
        [CombinatorialData]
        public async Task EventField_WithMethod(Location pdbLocation, Location sourceLocation)
        {
            var source = """
                public class C
                {
                    public event System.EventHandler [|E|];

                    public void M()
                    {
                        // this is a comment that wouldn't appear in decompiled source
                    }
                }
                """;
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C.E"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Event(Location pdbLocation, Location sourceLocation)
        {
            var source = """
                public class C
                {
                    public event System.EventHandler [|E|] { add { } remove { } }
                }
                """;
            await TestAsync(pdbLocation, sourceLocation, source, c => c.GetMember("C.E"));
        }

        [Fact]
        public async Task ReferenceAssembly_NullResult()
        {
            var source = """
                public class C
                {
                    public event System.EventHandler [|E|] { add { } remove { } }
                }
                """;
            // A pdb won't be emitted when building a reference assembly so the first two parameters don't actually matter
            await TestAsync(Location.OnDisk, Location.OnDisk, source, c => c.GetMember("C.E"), buildReferenceAssembly: true, expectNullResult: true);
        }

        [Fact]
        public async Task NugetPackageLayout()
        {
            var source = """
                public class C
                {
                    // A change
                    public event System.EventHandler [|E|] { add { } remove { } }
                }
                """;

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
        public async Task Net6SdkLayout()
        {
            var source = """
                public class C
                {
                    // A change
                    public event System.EventHandler [|E|] { add { } remove { } }
                }
                """;

            await RunTestAsync(async path =>
            {
                MarkupTestFile.GetSpan(source, out var metadataSource, out var expectedSpan);

                var packDir = Directory.CreateDirectory(Path.Combine(path, "packs", "MyPack.Ref", "1.0", "ref", "net6.0")).FullName;
                var dataDir = Directory.CreateDirectory(Path.Combine(path, "packs", "MyPack.Ref", "1.0", "data")).FullName;
                var sharedDir = Directory.CreateDirectory(Path.Combine(path, "shared", "MyPack", "1.0")).FullName;

                // Compile reference assembly
                var sourceText = SourceText.From(metadataSource, encoding: Encoding.UTF8);
                var (project, symbol) = await CompileAndFindSymbolAsync(packDir, Location.Embedded, Location.Embedded, sourceText, c => c.GetMember("C.E"), buildReferenceAssembly: true);

                // Compile implementation assembly
                CompileTestSource(sharedDir, sourceText, project, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

                // Create FrameworkList.xml
                File.WriteAllText(Path.Combine(dataDir, "FrameworkList.xml"), """
                    <FileList FrameworkName="MyPack">
                    </FileList>
                    """);

                await GenerateFileAndVerifyAsync(project, symbol, Location.Embedded, metadataSource.ToString(), expectedSpan, expectNullResult: false);
            });
        }

        [Fact]
        public async Task Net6SdkLayout_WithOtherReferences()
        {
            var source = """
                public class C
                {
                    public void [|M|](string d)
                    {
                    }
                }
                """;

            await RunTestAsync(async path =>
            {
                MarkupTestFile.GetSpan(source, out var metadataSource, out var expectedSpan);

                var packDir = Directory.CreateDirectory(Path.Combine(path, "packs", "MyPack.Ref", "1.0", "ref", "net6.0")).FullName;
                var dataDir = Directory.CreateDirectory(Path.Combine(path, "packs", "MyPack.Ref", "1.0", "data")).FullName;
                var sharedDir = Directory.CreateDirectory(Path.Combine(path, "shared", "MyPack", "1.0")).FullName;

                var sourceText = SourceText.From(metadataSource, Encoding.UTF8);
                var (project, symbol) = await CompileAndFindSymbolAsync(packDir, Location.Embedded, Location.Embedded, sourceText, c => c.GetMember("C.M"), buildReferenceAssembly: true);

                var workspace = TestWorkspace.Create(@$"
<Workspace>
    <Project Language=""{LanguageNames.CSharp}"" CommonReferences=""true"" ReferencesOnDisk=""true"">
    </Project>
</Workspace>", composition: GetTestComposition());

                var implProject = workspace.CurrentSolution.Projects.First();

                // Compile implementation assembly
                CompileTestSource(sharedDir, sourceText, project, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

                // Create FrameworkList.xml
                File.WriteAllText(Path.Combine(dataDir, "FrameworkList.xml"), """
                    <FileList FrameworkName="MyPack">
                    </FileList>
                    """);

                await GenerateFileAndVerifyAsync(project, symbol, Location.Embedded, metadataSource.ToString(), expectedSpan, expectNullResult: false);
            });
        }

        [Fact]
        public async Task Net6SdkLayout_TypeForward()
        {
            var source = """
                public class [|C|]
                {
                    public void M(string d)
                    {
                    }
                }
                """;
            var typeForwardSource = """
                [assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(C))]
                """;

            await RunTestAsync(async path =>
            {
                MarkupTestFile.GetSpan(source, out var metadataSource, out var expectedSpan);

                var packDir = Directory.CreateDirectory(Path.Combine(path, "packs", "MyPack.Ref", "1.0", "ref", "net6.0")).FullName;
                var dataDir = Directory.CreateDirectory(Path.Combine(path, "packs", "MyPack.Ref", "1.0", "data")).FullName;
                var sharedDir = Directory.CreateDirectory(Path.Combine(path, "shared", "MyPack", "1.0")).FullName;

                var sourceText = SourceText.From(metadataSource, Encoding.UTF8);
                var (project, symbol) = await CompileAndFindSymbolAsync(packDir, Location.Embedded, Location.Embedded, sourceText, c => c.GetMember("C"), buildReferenceAssembly: true);

                var workspace = TestWorkspace.Create(@$"
<Workspace>
    <Project Language=""{LanguageNames.CSharp}"" CommonReferences=""true"" ReferencesOnDisk=""true"">
    </Project>
</Workspace>", composition: GetTestComposition());

                var implProject = workspace.CurrentSolution.Projects.First();

                // Compile implementation assembly
                var implementationDllFilePath = Path.Combine(sharedDir, "implementation.dll");
                var sourceCodePath = Path.Combine(sharedDir, "implementation.cs");
                var pdbFilePath = Path.Combine(sharedDir, "implementation.pdb");
                var assemblyName = "implementation";

                CompileTestSource(implementationDllFilePath, sourceCodePath, pdbFilePath, assemblyName, sourceText, project, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

                // Compile type forwarding implementation DLL, that looks like reference.dll
                var typeForwardDllFilePath = Path.Combine(sharedDir, "reference.dll");
                sourceCodePath = Path.Combine(sharedDir, "reference.cs");
                pdbFilePath = Path.Combine(sharedDir, "reference.pdb");
                assemblyName = "reference";

                implProject = implProject.AddMetadataReference(MetadataReference.CreateFromFile(implementationDllFilePath));
                sourceText = SourceText.From(typeForwardSource, Encoding.UTF8);
                CompileTestSource(typeForwardDllFilePath, sourceCodePath, pdbFilePath, assemblyName, sourceText, implProject, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

                // Create FrameworkList.xml
                File.WriteAllText(Path.Combine(dataDir, "FrameworkList.xml"), """
                    <FileList FrameworkName="MyPack">
                    </FileList>
                    """);

                await GenerateFileAndVerifyAsync(project, symbol, Location.Embedded, metadataSource.ToString(), expectedSpan, expectNullResult: false);
            });
        }

        [Fact]
        public async Task NoPdb_NullResult()
        {
            var source = """
                public class C
                {
                    public event System.EventHandler [|E|] { add { } remove { } }
                }
                """;

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
            var source = """
                public class C
                {
                    public event System.EventHandler [|E|] { add { } remove { } }
                }
                """;

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
            var source = """
                public class C
                {
                    public event System.EventHandler [|E|] { add { } remove { } }
                }
                """;
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
            var source = """
                public class C
                {
                    public event System.EventHandler [|E|] { add { } remove { } }
                }
                """;
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
            var source = """
                public class C
                {
                    public event System.EventHandler [|E|] { add { } remove { } }
                }
                """;

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
            var source = """
                public class C
                {
                    public event System.EventHandler [|E|] { add { } remove { } }
                }
                """;

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
            var source1 = """
                public class C
                {
                    public event System.EventHandler [|E|] { add { } remove { } }
                }
                """;
            var source2 = """
                public class C
                {
                    // A change
                    public event System.EventHandler E { add { } remove { } }
                }
                """;

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
            var source1 = """
                public class C
                {
                    public event System.EventHandler [|E|] { add { } remove { } }
                }
                """;
            var source2 = """
                public class C
                {
                    // A change
                    public event System.EventHandler E { add { } remove { } }
                }
                """;

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
            var source = """
                public class C
                {
                    public event System.EventHandler E { add { } remove { } }
                }
                """;

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
            var source = """
                public class C
                {
                    // ワ
                    public event System.EventHandler E { add { } remove { } }
                }
                """;

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
            var source = """
                public class C
                {
                    // ワ
                    public event System.EventHandler E { add { } remove { } }
                }
                """;

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

        [Fact]
        public async Task OptionTurnedOff_NullResult()
        {
            var source = """
                public class C
                {
                    public event System.EventHandler E { add { } remove { } }
                }
                """;

            await RunTestAsync(async path =>
            {
                var sourceText = SourceText.From(source, Encoding.UTF8);
                var (project, symbol) = await CompileAndFindSymbolAsync(path, Location.Embedded, Location.Embedded, sourceText, c => c.GetMember("C.E"));

                using var workspace = (TestWorkspace)project.Solution.Workspace;

                var service = workspace.GetService<IMetadataAsSourceFileService>();
                try
                {
                    var options = MetadataAsSourceOptions.GetDefault(project.Services) with
                    {
                        NavigateToSourceLinkAndEmbeddedSources = false
                    };
                    var file = await service.GetGeneratedFileAsync(workspace, project, symbol, signaturesOnly: false, options, CancellationToken.None).ConfigureAwait(false);

                    Assert.Same(NullResultMetadataAsSourceFileProvider.NullResult, file);
                }
                finally
                {
                    service.CleanupGeneratedFiles();
                    service.TryGetWorkspace()?.Dispose();
                }
            });
        }

        [Fact]
        public async Task MethodInPartialType_NavigateToCorrectFile()
        {
            var source1 = """
                public partial class C
                {
                    public void M1()
                    {
                    }
                }
                """;
            var source2 = """
                using System.Threading.Tasks;

                public partial class C
                {
                    public static async Task [|M2|]() => await M3();

                    private static async Task M3()
                    {
                    }
                }
                """;

            await RunTestAsync(async path =>
            {
                MarkupTestFile.GetSpan(source2, out source2, out var expectedSpan);

                var sourceText1 = SourceText.From(source1, Encoding.UTF8);
                var sourceText2 = SourceText.From(source2, Encoding.UTF8);

                var workspace = TestWorkspace.Create(@$"
<Workspace>
    <Project Language=""{LanguageNames.CSharp}"" CommonReferences=""true"" ReferencesOnDisk=""true"">
    </Project>
</Workspace>", composition: GetTestComposition());

                var project = workspace.CurrentSolution.Projects.First();

                var dllFilePath = GetDllPath(path);
                var sourceCodePath = GetSourceFilePath(path);
                var pdbFilePath = GetPdbPath(path);
                CompileTestSource(dllFilePath, new[] { Path.Combine(path, "source1.cs"), Path.Combine(path, "source2.cs") }, pdbFilePath, "reference", new[] { sourceText1, sourceText2 }, project, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

                project = project.AddMetadataReference(MetadataReference.CreateFromFile(GetDllPath(path)));

                var mainCompilation = await project.GetRequiredCompilationAsync(CancellationToken.None).ConfigureAwait(false);

                var symbol = mainCompilation.GetMember("C.M2");

                AssertEx.NotNull(symbol, $"Couldn't find symbol to go-to-def for.");

                await GenerateFileAndVerifyAsync(project, symbol, Location.Embedded, source2.ToString(), expectedSpan, expectNullResult: false);
            });
        }
    }
}
