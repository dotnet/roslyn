// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.SymbolKeyTests;

[UseExportProvider]
public sealed class SymbolKeyCrossLanguageTests
{
    [Theory]
    [InlineData("dynamic")]
    [InlineData("int*")]
    [InlineData("delegate*&lt;int, void&gt;")]
    public async Task TestUnsupportedVBTypes(string parameterType)
    {
        using var workspace = TestWorkspace.Create(
            $$"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" Name="CSProject">
                    <Document>
            public class C
            {
                public void M({{parameterType}} d) { }
            }
                    </Document>
                </Project>
                <Project Language="Visual Basic" CommonReference="true">
                    <ProjectReference>CSProject</ProjectReference>

                </Project>
            </Workspace>
            """);

        var solution = workspace.CurrentSolution;
        var csDocument = solution.Projects.Single(p => p.Language == LanguageNames.CSharp).Documents.Single();
        var semanticModel = await csDocument.GetRequiredSemanticModelAsync(CancellationToken.None);
        var tree = semanticModel.SyntaxTree;
        var root = tree.GetRoot();

        var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
        var methodSymbol = semanticModel.GetDeclaredSymbol(method);

        var vbProject = solution.Projects.Single(p => p.Language == LanguageNames.VisualBasic);
        var vbCompilation = await vbProject.GetRequiredCompilationAsync(CancellationToken.None);

        var resolved = SymbolKey.ResolveString(methodSymbol.GetSymbolKey().ToString(), vbCompilation, out var failureReason, CancellationToken.None);
        Assert.NotNull(failureReason);
        Assert.Null(resolved.GetAnySymbol());
    }
}
