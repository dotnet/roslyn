// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
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
        [Fact]
        public async Task Method()
        {
            var source = @"
public class C
{
    public void [|M|]()
    {
        // this is a comment that wouldn't appear in decompiled source
    }
}";
            await TestAsync(source, c => c.GetMember("C.M"));
        }

        [Fact]
        public async Task Parameter()
        {
            var source = @"
public class C
{
    public void M(int [|a|])
    {
        // this is a comment that wouldn't appear in decompiled source
    }
}";
            await TestAsync(source, c => c.GetMember<IMethodSymbol>("C.M").Parameters.First());
        }

        [Theory]
        [InlineData("C")]
        [InlineData("C..ctor")]
        public async Task Class_FromTypeDefinitionDocument(string symbolName)
        {
            var source = @"
public class [|C|]
{
    // this is a comment that wouldn't appear in decompiled source
}";
            await TestAsync(source, c => c.GetMember(symbolName));
        }

        [Theory]
        [InlineData("C")]
        [InlineData("C..ctor")]
        public async Task Class_FromMethodDocument(string symbolName)
        {
            var source = @"
public class [|C|]
{
    public void M()
    {
        // this is a comment that wouldn't appear in decompiled source
    }
}";
            await TestAsync(source, c => c.GetMember(symbolName));
        }

        [Fact]
        public async Task Field()
        {
            var source = @"
public class C
{
    public int [|f|];
}";
            await TestAsync(source, c => c.GetMember("C.f"));
        }

        [Fact]
        public async Task Property()
        {
            var source = @"
public class C
{
    public int [|P|] { get; set; }
}";
            await TestAsync(source, c => c.GetMember("C.P"));
        }

        [Fact]
        public async Task Property_WithBody()
        {
            var source = @"
public class C
{
    public int [|P|] { get { return 1; } }
}";
            await TestAsync(source, c => c.GetMember("C.P"));
        }

        [Fact]
        public async Task EventField()
        {
            var source = @"
public class C
{
    public event System.EventHandler [|E|];
}";
            await TestAsync(source, c => c.GetMember("C.E"));
        }

        [Fact]
        public async Task EventField_WithMethod()
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
            await TestAsync(source, c => c.GetMember("C.E"));
        }

        [Fact]
        public async Task Event()
        {
            var source = @"
public class C
{
    public event System.EventHandler [|E|] { add { } remove { } }
}";
            await TestAsync(source, c => c.GetMember("C.E"));
        }

        private static async Task TestAsync(string metadataSource, Func<Compilation, ISymbol> symbolMatcher)
        {
            MarkupTestFile.GetSpan(metadataSource, out var input, out var expectedSpan);

            var path = Path.Combine(Path.GetTempPath(), nameof(PdbSourceDocumentTests));
            var sourceCodePath = Path.Combine(path, "source.cs");
            var dllFilePath = Path.Combine(path, "reference.dll");
            var pdbFilePath = Path.Combine(path, "reference.pdb");
            var assemblyName = "ReferencedAssembly";
            try
            {
                Directory.CreateDirectory(path);

                File.WriteAllText(sourceCodePath, input);

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

                using var pdbStream = new MemoryStream();
                var peBlob = compilation.EmitToArray(new Emit.EmitOptions(debugInformationFormat: Emit.DebugInformationFormat.PortablePdb), pdbStream: pdbStream);

                File.WriteAllBytes(dllFilePath, peBlob.ToArray());
                File.WriteAllBytes(pdbFilePath, pdbStream.ToArray());

                project = project.AddMetadataReference(MetadataReference.CreateFromFile(dllFilePath));

                var mainCompilation = await project.GetRequiredCompilationAsync(CancellationToken.None);

                var symbol = symbolMatcher(mainCompilation);

                AssertEx.NotNull(symbol, $"Couldn't find symbol to go-to-def for.");

                var service = project.GetRequiredLanguageService<IPdbSourceDocumentNavigationService>();
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
                Directory.Delete(path, recursive: true);
            }
        }
    }
}
