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
        public enum Location
        {
            OnDisk,
            Embedded
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

        private static async Task TestAsync(Location pdbLocation, Location sourceLocation, string metadataSource, Func<Compilation, ISymbol> symbolMatcher)
        {
            var path = Path.Combine(Path.GetTempPath(), nameof(PdbSourceDocumentTests));

            try
            {
                Directory.CreateDirectory(path);

                await TestAsync(path, pdbLocation, sourceLocation, metadataSource, symbolMatcher);
            }
            finally
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
            }
        }

        private static async Task TestAsync(string path, Location pdbLocation, Location sourceLocation, string metadataSource, Func<Compilation, ISymbol> symbolMatcher)
        {
            var assemblyName = "ReferencedAssembly";
            var sourceCodePath = Path.Combine(path, "source.cs");
            var dllFilePath = Path.Combine(path, "reference.dll");
            var pdbFilePath = Path.Combine(path, "reference.pdb");

            MarkupTestFile.GetSpan(metadataSource, out var input, out var expectedSpan);

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

            MemoryStream? pdbStream;
            IEnumerable<EmbeddedText>? embeddedTexts;
            DebugInformationFormat debugInformationFormat;
            if (sourceLocation == Location.OnDisk)
            {
                embeddedTexts = null;
                File.WriteAllText(sourceCodePath, input);
            }
            else
            {
                embeddedTexts = new[] { EmbeddedText.FromSource(sourceCodePath, compilation.SyntaxTrees.First().GetText()) };
            }

            if (pdbLocation == Location.OnDisk)
            {
                pdbStream = new MemoryStream();
                debugInformationFormat = DebugInformationFormat.PortablePdb;
            }
            else
            {
                pdbStream = null;
                debugInformationFormat = DebugInformationFormat.Embedded;
            }

            var peBlob = compilation.EmitToArray(new EmitOptions(debugInformationFormat: debugInformationFormat), pdbStream: pdbStream, embeddedTexts: embeddedTexts);

            File.WriteAllBytes(dllFilePath, peBlob.ToArray());
            if (pdbStream is not null)
            {
                File.WriteAllBytes(pdbFilePath, pdbStream.ToArray());
            }

            project = project.AddMetadataReference(MetadataReference.CreateFromFile(dllFilePath));

            var mainCompilation = await project.GetRequiredCompilationAsync(CancellationToken.None);

            var symbol = symbolMatcher(mainCompilation);

            AssertEx.NotNull(symbol, $"Couldn't find symbol to go-to-def for.");

            var service = workspace.Services.GetRequiredService<IPdbSourceDocumentNavigationService>();
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
    }
}
