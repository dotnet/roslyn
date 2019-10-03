// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Editing
{
    [UseExportProvider]
    public class AddImportsTests
    {
        private Document GetDocument(string code)
        {
            var ws = new AdhocWorkspace();
            var emptyProject = ws.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Default,
                    "test",
                    "test.dll",
                    LanguageNames.CSharp,
                    metadataReferences: new[] { TestReferences.NetFx.v4_0_30319.mscorlib }));

            return emptyProject.AddDocument("test.cs", code);
        }

        private async Task TestAsync(string initialText, string importsAddedText, string simplifiedText, Func<OptionSet, OptionSet> optionsTransform = null)
        {
            var doc = GetDocument(initialText);
            OptionSet options = await doc.GetOptionsAsync();
            if (optionsTransform != null)
            {
                options = optionsTransform(options);
            }

            var imported = await ImportAdder.AddImportsAsync(doc, options);

            if (importsAddedText != null)
            {
                var formatted = await Formatter.FormatAsync(imported, SyntaxAnnotation.ElasticAnnotation, options);
                var actualText = (await formatted.GetTextAsync()).ToString();
                Assert.Equal(importsAddedText, actualText);
            }

            if (simplifiedText != null)
            {
                var reduced = await Simplifier.ReduceAsync(imported, options);
                var formatted = await Formatter.FormatAsync(reduced, SyntaxAnnotation.ElasticAnnotation, options);

                var actualText = (await formatted.GetTextAsync()).ToString();
                Assert.Equal(simplifiedText, actualText);
            }
        }

        [Fact]
        public async Task TestAddImport()
        {
            await TestAsync(
@"class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using System.Collections.Generic;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using System.Collections.Generic;

class C
{
    public List<int> F;
}");
        }

        [Fact]
        public async Task TestAddSystemImportFirst()
        {
            await TestAsync(
@"using N;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using System.Collections.Generic;
using N;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using System.Collections.Generic;
using N;

class C
{
    public List<int> F;
}");
        }

        [Fact]
        public async Task TestDontAddSystemImportFirst()
        {
            await TestAsync(
@"using N;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using N;
using System.Collections.Generic;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using N;
using System.Collections.Generic;

class C
{
    public List<int> F;
}",
                options => options.WithChangedOption(GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.CSharp, false)
);
        }

        [Fact]
        public async Task TestAddImportsInOrder()
        {
            await TestAsync(
@"using System.Collections;
using System.Diagnostics;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

class C
{
    public List<int> F;
}");
        }

        [Fact]
        public async Task TestAddMultipleImportsInOrder()
        {
            await TestAsync(
@"class C
{
    public System.Collections.Generic.List<int> F;
    public System.EventHandler Handler;
}",

@"using System;
using System.Collections.Generic;

class C
{
    public System.Collections.Generic.List<int> F;
    public System.EventHandler Handler;
}",

@"using System;
using System.Collections.Generic;

class C
{
    public List<int> F;
    public EventHandler Handler;
}");
        }

        [Fact]
        public async Task TestImportNotRedundantlyAdded()
        {
            await TestAsync(
@"using System.Collections.Generic;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using System.Collections.Generic;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using System.Collections.Generic;

class C
{
    public List<int> F;
}");
        }

        [Fact]
        public async Task TestUnusedAddedImportIsRemovedBySimplifier()
        {
            await TestAsync(
@"class C
{
    public System.Int32 F;
}",

@"using System;

class C
{
    public System.Int32 F;
}",

@"class C
{
    public int F;
}");
        }

        [Fact]
        public async Task TestImportNotAddedForNamespaceDeclarations()
        {
            await TestAsync(
@"namespace N
{
}",

@"namespace N
{
}",

@"namespace N
{
}");
        }

        [Fact]
        public async Task TestImportAddedAndRemovedForReferencesInsideNamespaceDeclarations()
        {
            await TestAsync(
@"namespace N
{
    class C
    {
        private N.C c;
    }
}",

@"using N;

namespace N
{
    class C
    {
        private N.C c;
    }
}",

@"namespace N
{
    class C
    {
        private C c;
    }
}");
        }

        [Fact]
        public async Task TestImportAddedAndRemovedForReferencesMatchingNestedImports()
        {
            await TestAsync(
@"namespace N
{
    using System.Collections.Generic;

    class C
    {
        private System.Collections.Generic.List<int> F;
    }
}",

@"using System.Collections.Generic;

namespace N
{
    using System.Collections.Generic;

    class C
    {
        private System.Collections.Generic.List<int> F;
    }
}",

@"namespace N
{
    using System.Collections.Generic;

    class C
    {
        private List<int> F;
    }
}");
        }

        [Fact]
        public async Task TestImportRemovedIfItMakesReferenceAmbiguous()
        {
            // this is not really an artifact of the AddImports feature, it is due
            // to Simplifier not reducing the namespace reference because it would 
            // become ambiguous, thus leaving an unused using directive
            await TestAsync(
@"namespace N
{
    class C
    {
    }
}

class C
{
    public N.C F;
}",

@"using N;

namespace N
{
    class C
    {
    }
}

class C
{
    public N.C F;
}",

@"namespace N
{
    class C
    {
    }
}

class C
{
    public N.C F;
}");
        }

        [Fact]
        [WorkItem(8797, "https://github.com/dotnet/roslyn/issues/8797")]
        public async Task TestBannerTextRemainsAtTopOfDocumentWithoutExistingImports()
        {
            await TestAsync(
@"// --------------------------------------------------------------------------------------------------------------------
// <copyright file=""File.cs"" company=""MyOrgnaization"">
// Copyright (C) MyOrgnaization 2016
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
class C
{
    public System.Collections.Generic.List<int> F;
}",

@"// --------------------------------------------------------------------------------------------------------------------
// <copyright file=""File.cs"" company=""MyOrgnaization"">
// Copyright (C) MyOrgnaization 2016
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"// --------------------------------------------------------------------------------------------------------------------
// <copyright file=""File.cs"" company=""MyOrgnaization"">
// Copyright (C) MyOrgnaization 2016
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;

class C
{
    public List<int> F;
}");
        }

        [Fact]
        [WorkItem(8797, "https://github.com/dotnet/roslyn/issues/8797")]
        public async Task TestBannerTextRemainsAtTopOfDocumentWithExistingImports()
        {
            await TestAsync(
@"// --------------------------------------------------------------------------------------------------------------------
// <copyright file=""File.cs"" company=""MyOrgnaization"">
// Copyright (C) MyOrgnaization 2016
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using ZZZ;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"// --------------------------------------------------------------------------------------------------------------------
// <copyright file=""File.cs"" company=""MyOrgnaization"">
// Copyright (C) MyOrgnaization 2016
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using ZZZ;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"// --------------------------------------------------------------------------------------------------------------------
// <copyright file=""File.cs"" company=""MyOrgnaization"">
// Copyright (C) MyOrgnaization 2016
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using ZZZ;

class C
{
    public List<int> F;
}");
        }

        [Fact]
        [WorkItem(8797, "https://github.com/dotnet/roslyn/issues/8797")]
        public async Task TestLeadingWhitespaceLinesArePreserved()
        {
            await TestAsync(
@"class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using System.Collections.Generic;

class C
{
    public System.Collections.Generic.List<int> F;
}",

@"using System.Collections.Generic;

class C
{
    public List<int> F;
}");
        }

        [Fact]
        [WorkItem(9228, "https://github.com/dotnet/roslyn/issues/9228")]
        public async Task TestDoNotAddDuplicateImportIfNamespaceIsDefinedInSourceAndExternalAssembly()
        {
            var externalCode =
@"namespace N.M { public class A : System.Attribute { } }";

            var code =
@"using System;
using N.M;

class C
{
    public void M1(String p1) { }

    public void M2([A] String p2) { }
}";

            var otherAssemblyReference = GetInMemoryAssemblyReferenceForCode(externalCode);

            var ws = new AdhocWorkspace();
            var emptyProject = ws.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Default,
                    "test",
                    "test.dll",
                    LanguageNames.CSharp,
                    metadataReferences: new[] { TestReferences.NetFx.v4_0_30319.mscorlib }));

            var project = emptyProject
                .AddMetadataReferences(new[] { otherAssemblyReference })
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            project = project.AddDocument("duplicate.cs", externalCode).Project;
            var document = project.AddDocument("test.cs", code);

            var options = document.Project.Solution.Workspace.Options;

            var compilation = await document.Project.GetCompilationAsync(CancellationToken.None);
            var compilerDiagnostics = compilation.GetDiagnostics(CancellationToken.None);
            Assert.Empty(compilerDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

            var attribute = compilation.GetTypeByMetadataName("N.M.A");

            var syntaxRoot = await document.GetSyntaxRootAsync(CancellationToken.None).ConfigureAwait(false);
            SyntaxNode p1SyntaxNode = syntaxRoot.DescendantNodes().OfType<ParameterSyntax>().FirstOrDefault();

            // Add N.M.A attribute to p1.
            var editor = await DocumentEditor.CreateAsync(document, CancellationToken.None).ConfigureAwait(false);
            var attributeSyntax = editor.Generator.Attribute(editor.Generator.TypeExpression(attribute));

            editor.AddAttribute(p1SyntaxNode, attributeSyntax);
            var documentWithAttribute = editor.GetChangedDocument();

            // Add namespace import.
            var imported = await ImportAdder.AddImportsAsync(documentWithAttribute, null,
                CancellationToken.None).ConfigureAwait(false);

            var formatted = await Formatter.FormatAsync(imported, options);
            var actualText = (await formatted.GetTextAsync()).ToString();

            Assert.Equal(@"using System;
using N.M;

class C
{
    public void M1([global::N.M.A] String p1) { }

    public void M2([A] String p2) { }
}", actualText);
        }

        private static MetadataReference GetInMemoryAssemblyReferenceForCode(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);

            var compilation = CSharpCompilation
                .Create("test.dll", new[] { tree })
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(TestReferences.NetFx.v4_0_30319.mscorlib);

            return compilation.ToMetadataReference();
        }
    }
}
