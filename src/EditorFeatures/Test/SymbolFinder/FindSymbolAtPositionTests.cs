// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;

[UseExportProvider]
public class FindSymbolAtPositionTests
{
    private static Task<ISymbol> FindSymbolAtPositionAsync(TestWorkspace workspace)
    {
        var position = workspace.Documents.Single(d => d.CursorPosition.HasValue).CursorPosition!.Value;
        var document = workspace.CurrentSolution.GetRequiredDocument(workspace.Documents.Single().Id);
        return SymbolFinder.FindSymbolAtPositionAsync(document, position);
    }

    [Fact]
    public async Task PositionOnLeadingTrivia()
    {
        using var workspace = TestWorkspace.CreateCSharp(
            @"using System;
                class Program
                {
                    static void Main()
                    {
                        $$#pragma warning disable 612
                        Goo();
                        #pragma warning restore 612
                    }
                }");
        var symbol = await FindSymbolAtPositionAsync(workspace);
        Assert.Null(symbol);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53269")]
    public async Task PositionInCaseLabel()
    {
        using var workspace = TestWorkspace.CreateCSharp(
            @"using System;
                enum E { A, B }
                class Program
                {
                    static void Main()
                    {
                        E e = default;
                        switch (e)
                        {
                            case E.$$A: break;
                        }
                    }
                }");

        var fieldSymbol = Assert.IsAssignableFrom<IFieldSymbol>(await FindSymbolAtPositionAsync(workspace));
        Assert.Equal(TypeKind.Enum, fieldSymbol.ContainingType.TypeKind);
    }
}
