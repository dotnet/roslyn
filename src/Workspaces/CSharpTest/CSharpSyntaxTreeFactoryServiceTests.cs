// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

[UseExportProvider]
public sealed class CSharpSyntaxTreeFactoryServiceTests
{
    [Fact]
    public void LanguageServiceFactoryCreatesSyntaxTreesWithoutCache()
    {
        using var workspace = new AdhocWorkspace();
        var service = workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService<ISyntaxTreeFactoryService>();
        var text = SourceText.From("class C { }");

        var tree = service.ParseSyntaxTree("test.cs", CSharpParseOptions.Default, text, CancellationToken.None);

        Assert.Equal("test.cs", tree.FilePath);
        Assert.Same(text, tree.GetText());
        Assert.IsType<Syntax.CompilationUnitSyntax>(tree.GetRoot());
    }
}
