// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.CompilerServices;
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
