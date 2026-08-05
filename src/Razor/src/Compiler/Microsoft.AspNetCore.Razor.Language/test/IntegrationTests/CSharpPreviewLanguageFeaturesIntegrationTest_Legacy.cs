// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// Language features not covered by tests:
// - Unsafe evolution: broad ongoing unsafe-surface work without a single stable Razor-focused scenario here.
// - ExtendedLayoutAttribute: metadata/runtime interop feature rather than Razor-authored source.
// - Runtime Async: runtime/library work rather than a distinct Razor-authored source feature.

public sealed class CSharpPreviewLanguageFeaturesIntegrationTest_Legacy : IntegrationTestBase
{
    private const string DefaultLegacyFileName = "TestView.cshtml";

    private const string LegacyTemplateBaseSource =
        """
        public abstract class LegacyTemplateBase
        {
            public virtual System.Threading.Tasks.Task ExecuteAsync()
                => System.Threading.Tasks.Task.CompletedTask;

            protected void WriteLiteral(string value)
            {
            }

            protected void Write(object value)
            {
            }

            protected TTagHelper CreateTagHelper<TTagHelper>()
                where TTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.ITagHelper
                => System.Activator.CreateInstance<TTagHelper>();

            protected void StartTagHelperWritingScope(System.Text.Encodings.Web.HtmlEncoder encoder)
            {
            }

            protected Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent EndTagHelperWritingScope()
                => throw new System.NotImplementedException();

            protected void BeginWriteTagHelperAttribute()
            {
            }

            protected string EndWriteTagHelperAttribute()
                => string.Empty;
        }
        """;

    public CSharpPreviewLanguageFeaturesIntegrationTest_Legacy()
        : base(layer: TestProject.Layer.Compiler)
    {
        AddCSharpSyntaxTree(LegacyTemplateBaseSource, filePath: "LegacyTemplateBase.cs");
    }

    public override string GetTestFileName([CallerMemberName] string? testName = null)
    {
        var fileName = $"TestFiles/IntegrationTests/{GetType().Name}/{testName}";
        var directory = Path.GetDirectoryName(fileName);
        if (directory is not null)
        {
            Directory.CreateDirectory(Path.Combine(TestProjectRoot, directory));
        }

        return fileName;
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/13188")]
    public void StringLiteralAttributeOnUnionTagHelperProperty()
    {
        AddCSharpSyntaxTree("""
            #nullable enable

            namespace System.Runtime.CompilerServices
            {
                public interface IUnion
                {
                    object? Value { get; }
                }

                public class UnionAttribute : System.Attribute
                {
                }
            }
            """);

        AddCSharpSyntaxTree("""
            using Microsoft.AspNetCore.Razor.TagHelpers;

            namespace Test
            {
                public union SlotContent(string, int);

                public class SlotTagHelper : TagHelper
                {
                    public SlotContent Content { get; set; }
                }
            }
            """);

        var projectItem = AddProjectItemFromText("""
            @inherits global::LegacyTemplateBase
            @addTagHelper *, TestAssembly

            @{
                var content = new Test.SlotContent(42);
            }

            <slot content="hello"></slot>
            <slot content="@content"></slot>
            """,
            filePath: DefaultLegacyFileName);

        var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTrees);
        var projectEngine = CreateProjectEngine(static builder => builder.RegisterDefaultTagHelperProducer());
        var tagHelpers = projectEngine.Engine.Features.OfType<ITagHelperDiscoveryService>().Single().GetTagHelpers(compilation);
        Assert.Contains(tagHelpers, static tagHelper => tagHelper.TypeName == "Test.SlotTagHelper");

        var imports = projectEngine.GetImports(projectItem, static i => i.Exists)
            .Select(static import => RazorSourceDocument.ReadFrom(import))
            .ToImmutableArray();
        var codeDocument = projectEngine.Process(RazorSourceDocument.ReadFrom(projectItem), RazorFileKind.Legacy, imports, tagHelpers);

        var references = compilation.References.Concat([compilation.ToMetadataReference()]).ToArray();
        var generated = new CompiledCSharpCode(
            CSharpCompilation.Create(compilation.AssemblyName + ".Views", references: references, options: compilation.Options),
            codeDocument);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/collection-expression-arguments.md")]
    public void CollectionExpressionArguments()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                System.Collections.Generic.List<string> values = [with(capacity: 32), "a", "b", "c"];
                _ = values.Count;
            }

            <p>@(CountValues([with(capacity: 32), "d", "e"]))</p>
            <p>@CountValues([with(capacity: 32), "f", "g"])</p>

            @functions {
                private static int CountValues(System.Collections.Generic.List<string> values)
                    => values.Count;
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

}
