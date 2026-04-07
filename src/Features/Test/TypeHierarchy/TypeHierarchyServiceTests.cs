// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.TypeHierarchy;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.TypeHierarchy;

[UseExportProvider]
public sealed class TypeHierarchyServiceTests
{
    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task GetBaseTypesAndInterfacesAsync_ReturnsExpectedBaseTypes(string language)
    {
        using var workspace = CreateWorkspace(
            language,
            csharpMarkup: """
                interface IRoot { }
                class Base { }
                class $$Derived : Base, IRoot { }
                """,
            vbMarkup: """
                Interface IRoot
                End Interface

                Class Base
                End Class

                Class $$Derived
                    Inherits Base
                    Implements IRoot
                End Class
                """);

        var (_, service, symbol) = await GetTypeSymbolWithServiceAsync(workspace);
        var baseTypes = service.GetBaseTypesAndInterfaces(symbol);
        var baseTypeNames = baseTypes.Select(static t => t.Name).ToImmutableArray();

        Assert.Contains("Base", baseTypeNames);
        Assert.Contains("IRoot", baseTypeNames);
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task GetDerivedTypesAndImplementationsAsync_ReturnsExpectedDerivedTypes(string language)
    {
        using var workspace = CreateWorkspace(
            language,
            csharpMarkup: """
                class $$Base { }
                class Mid : Base { }
                class Derived : Mid { }
                """,
            vbMarkup: """
                Class $$Base
                End Class

                Class Mid
                    Inherits Base
                End Class

                Class Derived
                    Inherits Mid
                End Class
                """);

        var (document, service, symbol) = await GetTypeSymbolWithServiceAsync(workspace);
        var derivedTypes = await service.GetDerivedTypesAndImplementationsAsync(
            document.Project.Solution,
            symbol,
            transitive: true,
            CancellationToken.None);

        AssertEx.SetEqual(["Mid", "Derived"], derivedTypes.Select(static t => t.Name));
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task GetBaseTypesAndInterfacesAsync_ForInterface_ReturnsExpectedBaseInterfaces(string language)
    {
        using var workspace = CreateWorkspace(
            language,
            csharpMarkup: """
                interface IRoot { }
                interface $$IChild : IRoot { }
                """,
            vbMarkup: """
                Interface IRoot
                End Interface

                Interface $$IChild
                    Inherits IRoot
                End Interface
                """);

        var (_, service, symbol) = await GetTypeSymbolWithServiceAsync(workspace);
        var baseTypes = service.GetBaseTypesAndInterfaces(symbol);
        var baseTypeNames = baseTypes.Select(static t => t.Name).ToImmutableArray();

        Assert.Contains("IRoot", baseTypeNames);
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task GetDerivedTypesAndImplementationsAsync_ForInterface_ReturnsDerivedInterfacesAndImplementations(string language)
    {
        using var workspace = CreateWorkspace(
            language,
            csharpMarkup: """
                interface $$IRoot { }
                interface IChild : IRoot { }
                class A : IRoot { }
                class B : IChild { }
                """,
            vbMarkup: """
                Interface $$IRoot
                End Interface

                Interface IChild
                    Inherits IRoot
                End Interface

                Class A
                    Implements IRoot
                End Class

                Class B
                    Implements IChild
                End Class
                """);

        var (document, service, symbol) = await GetTypeSymbolWithServiceAsync(workspace);
        var derivedTypes = await service.GetDerivedTypesAndImplementationsAsync(
            document.Project.Solution,
            symbol,
            transitive: true,
            CancellationToken.None);
        var derivedTypeNames = derivedTypes.Select(static t => t.Name).ToImmutableArray();

        Assert.Contains("IChild", derivedTypeNames);
        Assert.Contains("A", derivedTypeNames);
        Assert.Contains("B", derivedTypeNames);
    }

    private static TestWorkspace CreateWorkspace(string language, string csharpMarkup, string vbMarkup)
        => language switch
        {
            LanguageNames.CSharp => TestWorkspace.CreateCSharp(csharpMarkup),
            LanguageNames.VisualBasic => TestWorkspace.CreateVisualBasic(vbMarkup),
            _ => throw new System.ArgumentOutOfRangeException(nameof(language), language, message: null),
        };

    private static async Task<(Document Document, ITypeHierarchyService Service, INamedTypeSymbol TypeSymbol)> GetTypeSymbolWithServiceAsync(TestWorkspace workspace)
    {
        var hostDocument = workspace.DocumentWithCursor;
        var document = workspace.CurrentSolution.GetRequiredDocument(hostDocument.Id);
        var symbol = await SymbolFinder.FindSymbolAtPositionAsync(document, hostDocument.CursorPosition!.Value, cancellationToken: CancellationToken.None);
        var typeSymbol = Assert.IsAssignableFrom<INamedTypeSymbol>(symbol);

        var service = document.GetRequiredLanguageService<ITypeHierarchyService>();
        return (document, service, typeSymbol);
    }
}
