// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PdbSourceDocument;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.PdbSourceDocument
{
    [UseExportProvider]
    public class PdbSourceDocumentTests
    {
        public enum SourceLocation
        {
            OnDisk,
            Embedded
        }

        [Theory]
        [CombinatorialData]
        public async Task Method(SourceLocation location)
        {
            var source = @"
public class C
{
    public void [|M|]()
    {
        // this is a comment that wouldn't appear in decompiled source
    }
}";
            await TestAsync(location, source, c => c.GetMember("C.M"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Constructor(SourceLocation location)
        {
            var source = @"
public class C
{
    public [|C|]()
    {
        // this is a comment that wouldn't appear in decompiled source
    }
}";
            await TestAsync(location, source, c => c.GetMember("C..ctor"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Parameter(SourceLocation location)
        {
            var source = @"
public class C
{
    public void M(int [|a|])
    {
        // this is a comment that wouldn't appear in decompiled source
    }
}";
            await TestAsync(location, source, c => c.GetMember<IMethodSymbol>("C.M").Parameters.First());
        }

        [Theory]
        [CombinatorialData]
        public async Task Class_FromTypeDefinitionDocument(SourceLocation location)
        {
            var source = @"
public class [|C|]
{
    // this is a comment that wouldn't appear in decompiled source
}";

            await TestAsync(location, source, c => c.GetMember("C"));

            await TestAsync(location, source, c => c.GetMember("C..ctor"));
        }

        [Theory]
        [CombinatorialData]
        public async Task NestedClass_FromTypeDefinitionDocument(SourceLocation location)
        {
            var source = @"
public class Outer
{
    public class [|C|]
    {
        // this is a comment that wouldn't appear in decompiled source
    }
}";
            await TestAsync(location, source, c => c.GetMember("Outer.C"));

            await TestAsync(location, source, c => c.GetMember("Outer.C..ctor"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Class_FromTypeDefinitionDocumentOfNestedClass(SourceLocation location)
        {
            var source = @"
public class [|Outer|]
{
    public class C
    {
        // this is a comment that wouldn't appear in decompiled source
    }
}";
            await TestAsync(location, source, c => c.GetMember("Outer"));

            await TestAsync(location, source, c => c.GetMember("Outer..ctor"));

        }

        [Theory]
        [CombinatorialData]
        public async Task NestedClass_FromMethodDocument(SourceLocation location)
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
            await TestAsync(location, source, c => c.GetMember("Outer.C"));

            await TestAsync(location, source, c => c.GetMember("Outer.C..ctor"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Class_FromMethodDocumentOfNestedClass(SourceLocation location)
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
            await TestAsync(location, source, c => c.GetMember("Outer"));

            await TestAsync(location, source, c => c.GetMember("Outer..ctor"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Class_FromMethodDocument(SourceLocation location)
        {
            var source = @"
public class [|C|]
{
    public void M()
    {
        // this is a comment that wouldn't appear in decompiled source
    }
}";
            await TestAsync(location, source, c => c.GetMember("C"));

            await TestAsync(location, source, c => c.GetMember("C..ctor"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Field(SourceLocation location)
        {
            var source = @"
public class C
{
    public int [|f|];
}";
            await TestAsync(location, source, c => c.GetMember("C.f"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Property(SourceLocation location)
        {
            var source = @"
public class C
{
    public int [|P|] { get; set; }
}";
            await TestAsync(location, source, c => c.GetMember("C.P"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Property_WithBody(SourceLocation location)
        {
            var source = @"
public class C
{
    public int [|P|] { get { return 1; } }
}";
            await TestAsync(location, source, c => c.GetMember("C.P"));
        }

        [Theory]
        [CombinatorialData]
        public async Task EventField(SourceLocation location)
        {
            var source = @"
public class C
{
    public event System.EventHandler [|E|];
}";
            await TestAsync(location, source, c => c.GetMember("C.E"));
        }

        [Theory]
        [CombinatorialData]
        public async Task EventField_WithMethod(SourceLocation location)
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
            await TestAsync(location, source, c => c.GetMember("C.E"));
        }

        [Theory]
        [CombinatorialData]
        public async Task Event(SourceLocation location)
        {
            var source = @"
public class C
{
    public event System.EventHandler [|E|] { add { } remove { } }
}";
            await TestAsync(location, source, c => c.GetMember("C.E"));
        }

        private static async Task TestAsync(SourceLocation location, string metadataSource, Func<Compilation, ISymbol> symbolMatcher)
        {
            MarkupTestFile.GetSpan(metadataSource, out var input, out var expectedSpan);

            var assemblyName = "ReferencedAssembly";
            var path = Path.Combine(Path.GetTempPath(), nameof(PdbSourceDocumentTests));
            var sourceCodePath = Path.Combine(path, "source.cs");

            var workspace = TestWorkspace.Create(@$"
<Workspace>
    <Project Language=""{LanguageNames.CSharp}"" CommonReferences=""true"" ReferencesOnDisk=""true"">
    </Project>
</Workspace>");
            var project = workspace.CurrentSolution.Projects.First();

            var languageServices = workspace.Services.GetLanguageServices(LanguageNames.CSharp);
            var compilationFactory = languageServices.GetRequiredService<ICompilationFactoryService>();
            var options = compilationFactory.GetDefaultCompilationOptions().WithOutputKind(OutputKind.DynamicallyLinkedLibrary);
            var parseOptions = project.ParseOptions;

            var compilation = compilationFactory
                .CreateCompilation(assemblyName, options)
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(input, options: parseOptions, path: sourceCodePath, encoding: Encoding.UTF8))
                .AddReferences(project.MetadataReferences);

            try
            {
                if (location == SourceLocation.OnDisk)
                {
                    using var pdbStream = new MemoryStream();
                    var peBlob = compilation.EmitToArray(new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb), pdbStream: pdbStream);

                    var dllFilePath = Path.Combine(path, "reference.dll");
                    var pdbFilePath = Path.Combine(path, "reference.pdb");
                    Directory.CreateDirectory(path);

                    File.WriteAllText(sourceCodePath, input);

                    File.WriteAllBytes(dllFilePath, peBlob.ToArray());
                    File.WriteAllBytes(pdbFilePath, pdbStream.ToArray());

                    project = project.AddMetadataReference(MetadataReference.CreateFromFile(dllFilePath));
                }
                else
                {
                    var embeddedTexts = ImmutableArray<EmbeddedText>.Empty;
                    if (location == SourceLocation.Embedded)
                    {
                        embeddedTexts = embeddedTexts.Add(EmbeddedText.FromSource(sourceCodePath, compilation.SyntaxTrees.First().GetText()));
                    }

                    var peBlob = compilation.EmitToArray(new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded), embeddedTexts: embeddedTexts);
                    project = project.AddMetadataReference(MetadataReference.CreateFromImage(peBlob));
                }

                var mainCompilation = await project.GetRequiredCompilationAsync(CancellationToken.None);

                var symbol = symbolMatcher(mainCompilation);

                AssertEx.NotNull(symbol, $"Couldn't find symbol to go-to-def for.");

                var service = workspace.GetService<IPdbSourceDocumentNavigationService>();
                var file = await service.GetPdbSourceDocumentAsync(project, symbol, CancellationToken.None);

                AssertEx.NotNull(file, $"No source document was found in the pdb for the symbol.");

                var actual = File.ReadAllText(file!.FilePath);
                var actualSpan = file.IdentifierLocation.SourceSpan;

                // Compare exact texts and verify that the location returned is exactly that
                // indicated by expected
                AssertEx.EqualOrDiff(input, actual);
                Assert.Equal(expectedSpan.Start, actualSpan.Start);
                Assert.Equal(expectedSpan.End, actualSpan.End);
            }
            finally
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
            }
        }
    }
}
