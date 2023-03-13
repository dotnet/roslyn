// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Xunit;

namespace Microsoft.CodeAnalysis.ExternalAccess.RazorCompiler.UnitTests
{
    public class HostOutputsTests : CSharpTestBase
    {
        [Fact]
        public void Added()
        {
            var source = """
                class C { }
                """;
            var parseOptions = TestOptions.Regular;
            var compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var generator = new PipelineCallbackGenerator(ctx =>
            {
                var syntaxProvider = ctx.SyntaxProvider.CreateSyntaxProvider((n, _) => n.IsKind(SyntaxKind.ClassDeclaration), (c, _) => c.Node);

                ctx.RegisterHostOutput(syntaxProvider, static (hpc, node, _) =>
                {
                    hpc.AddOutput("test", node.ToFullString());
                });
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator.AsSourceGenerator() }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);

            var result = driver.GetRunResult().Results.Single();
            Assert.Empty(result.Diagnostics);

            var hostOutputs = result.GetHostOutputs();
            Assert.Equal(1, hostOutputs.Length);
            Assert.Equal("test", hostOutputs[0].Key);
            Assert.Equal(source, hostOutputs[0].Value);
        }
    }
}
