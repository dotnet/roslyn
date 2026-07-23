// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.TypeHierarchy;

public sealed class TypeHierarchyTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    [Theory, CombinatorialData]
    public async Task TestPrepareTypeHierarchyReturnsTypeItem(bool mutatingLspWorkspace)
    {
        var markup = """
            class {|definition:C|}
            {
                void {|caret:|}M()
                {
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var preparedItem = Assert.Single(await RunPrepareTypeHierarchyAsync(testLspServer, testLspServer.GetLocations("caret").Single()));
        var definition = testLspServer.GetLocations("definition").Single();

        AssertEqualsItem(preparedItem, "C", definition);
        Assert.NotNull(preparedItem.Data);
    }

    [Theory, CombinatorialData]
    public async Task TestPrepareTypeHierarchyReturnsTypeItemForCrossDocumentReference(bool mutatingLspWorkspace)
    {
        var markups = new[]
        {
            """
            class User
            {
                {|caret:|}C field;
            }
            """,
            """
            class {|definition:C|}
            {
            }
            """,
        };

        await using var testLspServer = await CreateTestLspServerAsync(markups, mutatingLspWorkspace);

        var preparedItem = Assert.Single(await RunPrepareTypeHierarchyAsync(testLspServer, testLspServer.GetLocations("caret").Single()));
        var definition = testLspServer.GetLocations("definition").Single();

        AssertEqualsItem(preparedItem, "C", definition);
        Assert.NotNull(preparedItem.Data);
    }

    [Theory, CombinatorialData]
    public async Task TestPrepareTypeHierarchyReturnsContainingTypeFromConstructor(bool mutatingLspWorkspace)
    {
        var markup = """
            class {|definition:C|}
            {
                {|caret:|}C()
                {
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var preparedItem = Assert.Single(await RunPrepareTypeHierarchyAsync(testLspServer, testLspServer.GetLocations("caret").Single()));
        var definition = testLspServer.GetLocations("definition").Single();

        AssertEqualsItem(preparedItem, "C", definition);
        Assert.NotNull(preparedItem.Data);
    }

    [Theory, CombinatorialData]
    public async Task TestPrepareTypeHierarchyReturnsContainingTypeFromStaticConstructor(bool mutatingLspWorkspace)
    {
        var markup = """
            class {|definition:C|}
            {
                static {|caret:|}C()
                {
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var preparedItem = Assert.Single(await RunPrepareTypeHierarchyAsync(testLspServer, testLspServer.GetLocations("caret").Single()));
        var definition = testLspServer.GetLocations("definition").Single();

        AssertEqualsItem(preparedItem, "C", definition);
        Assert.NotNull(preparedItem.Data);
    }

    [Theory, CombinatorialData]
    public async Task TestPrepareTypeHierarchyPrefersRequestDocumentForPartialType(bool mutatingLspWorkspace)
    {
        var markups = new[]
        {
            """
            partial class {|definitionInFirst:C|}
            {
            }
            """,
            """
            partial class {|definitionInSecond:C|}
            {
                void M()
                {
                    {|caret:|}C c;
                }
            }
            """,
        };

        await using var testLspServer = await CreateTestLspServerAsync(markups, mutatingLspWorkspace);

        var preparedItem = Assert.Single(await RunPrepareTypeHierarchyAsync(testLspServer, testLspServer.GetLocations("caret").Single()));
        var preferredDefinition = testLspServer.GetLocations("definitionInSecond").Single();

        AssertEqualsItem(preparedItem, "C", preferredDefinition);
        Assert.NotNull(preparedItem.Data);
    }

    [Theory, CombinatorialData]
    public async Task TestPrepareTypeHierarchyReturnsContainingTypeForLocalVariableSymbol(bool mutatingLspWorkspace)
    {
        var markup = """
            class {|definition:C|}
            {
                void M()
                {
                    int {|caret:local|} = 0;
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var preparedItem = Assert.Single(await RunPrepareTypeHierarchyAsync(testLspServer, testLspServer.GetLocations("caret").Single()));
        var definition = testLspServer.GetLocations("definition").Single();

        AssertEqualsItem(preparedItem, "C", definition);
        Assert.NotNull(preparedItem.Data);
    }

    [Theory, CombinatorialData]
    public async Task TestPrepareTypeHierarchyReturnsNullForNamespaceSymbol(bool mutatingLspWorkspace)
    {
        var markup = """
            namespace {|caret:N|}
            {
                class C
                {
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var preparedItems = await RunPrepareTypeHierarchyAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        Assert.Empty(preparedItems);
    }

    [Theory, CombinatorialData]
    public async Task TestPrepareTypeHierarchyReturnsNullForKeywordPosition(bool mutatingLspWorkspace)
    {
        var markup = """
            class C
            {
                void M()
                {
                    {|caret:if|} (true)
                    {
                    }
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var preparedItems = await RunPrepareTypeHierarchyAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        Assert.Empty(preparedItems);
    }

    [Theory, CombinatorialData]
    public async Task TestTypeHierarchySupertypesReturnsBaseAndInterface(bool mutatingLspWorkspace)
    {
        var markups = new[]
        {
            """
            interface {|interfaceDef:IRoot|}
            {
            }
            """,
            """
            class {|baseDef:Base|}
            {
            }
            """,
            """
            class {|derivedDef:Derived|} : Base, IRoot
            {
                void {|caret:|}M()
                {
                }
            }
            """,
        };

        await using var testLspServer = await CreateTestLspServerAsync(markups, mutatingLspWorkspace);

        var preparedItem = Assert.Single(await RunPrepareTypeHierarchyAsync(testLspServer, testLspServer.GetLocations("caret").Single()));
        var supertypes = await RunSupertypesAsync(testLspServer, preparedItem);

        var baseDefinition = testLspServer.GetLocations("baseDef").Single();
        var interfaceDefinition = testLspServer.GetLocations("interfaceDef").Single();

        Assert.Equal(2, supertypes.Length);
        AssertContainsItem(supertypes, "Base", baseDefinition);
        AssertContainsItem(supertypes, "IRoot", interfaceDefinition);
    }

    [Theory, CombinatorialData]
    public async Task TestTypeHierarchySubtypesReturnsDerivedInterfaceAndImplementation(bool mutatingLspWorkspace)
    {
        var markups = new[]
        {
            """
            interface {|rootDef:IRoot|}
            {
            }
            """,
            """
            interface {|childDef:IChild|} : {|caret:|}IRoot
            {
            }
            """,
            """
            class {|directImplDef:DirectImplementation|} : IRoot
            {
            }
            """,
            """
            class {|implDef:Implementation|} : IChild
            {
            }
            """,
        };

        await using var testLspServer = await CreateTestLspServerAsync(markups, mutatingLspWorkspace);

        var preparedItem = Assert.Single(await RunPrepareTypeHierarchyAsync(testLspServer, testLspServer.GetLocations("caret").Single()));
        var subtypes = await RunSubtypesAsync(testLspServer, preparedItem);

        var childDefinition = testLspServer.GetLocations("childDef").Single();
        var directImplementationDefinition = testLspServer.GetLocations("directImplDef").Single();

        Assert.Equal(2, subtypes.Length);
        AssertContainsItem(subtypes, "IChild", childDefinition);
        AssertContainsItem(subtypes, "DirectImplementation", directImplementationDefinition);
    }

    [Theory, CombinatorialData]
    public async Task TestTypeHierarchySupertypesReturnsOnlyImmediateSupertypes(bool mutatingLspWorkspace)
    {
        var markups = new[]
        {
            """
            interface {|topInterfaceDef:ITop|}
            {
            }
            """,
            """
            interface {|midInterfaceDef:IMid|} : ITop
            {
            }
            """,
            """
            class {|baseDef:Base|} : IMid
            {
            }
            """,
            """
            class {|midDef:Mid|} : Base
            {
            }
            """,
            """
            class {|derivedDef:Derived|} : Mid
            {
                void {|caret:|}M()
                {
                }
            }
            """,
        };

        await using var testLspServer = await CreateTestLspServerAsync(markups, mutatingLspWorkspace);

        var preparedItem = Assert.Single(await RunPrepareTypeHierarchyAsync(testLspServer, testLspServer.GetLocations("caret").Single()));
        var supertypes = await RunSupertypesAsync(testLspServer, preparedItem);

        var midDefinition = testLspServer.GetLocations("midDef").Single();
        var baseDefinition = testLspServer.GetLocations("baseDef").Single();
        var midInterfaceDefinition = testLspServer.GetLocations("midInterfaceDef").Single();
        var topInterfaceDefinition = testLspServer.GetLocations("topInterfaceDef").Single();

        var midItem = Assert.Single(supertypes);
        AssertEqualsItem(midItem, "Mid", midDefinition);

        var midSupertypes = await RunSupertypesAsync(testLspServer, midItem);
        var baseItem = Assert.Single(midSupertypes);
        AssertEqualsItem(baseItem, "Base", baseDefinition);

        var baseSupertypes = await RunSupertypesAsync(testLspServer, baseItem);
        var midInterfaceItem = Assert.Single(baseSupertypes);
        AssertEqualsItem(midInterfaceItem, "IMid", midInterfaceDefinition);

        var midInterfaceSupertypes = await RunSupertypesAsync(testLspServer, midInterfaceItem);
        var topInterfaceItem = Assert.Single(midInterfaceSupertypes);
        AssertEqualsItem(topInterfaceItem, "ITop", topInterfaceDefinition);
    }

    [Theory, CombinatorialData]
    public async Task TestTypeHierarchySubtypesReturnsOnlyImmediateSubtypes(bool mutatingLspWorkspace)
    {
        var markups = new[]
        {
            """
            interface {|rootDef:IRoot|}
            {
            }
            """,
            """
            interface {|childDef:IChild|} : {|caret:|}IRoot
            {
            }
            """,
            """
            interface {|grandchildDef:IGrandChild|} : IChild
            {
            }
            """,
            """
            class {|directImplDef:DirectImplementation|} : IRoot
            {
            }
            """,
            """
            class {|indirectImplDef:IndirectImplementation|} : IGrandChild
            {
            }
            """,
        };

        await using var testLspServer = await CreateTestLspServerAsync(markups, mutatingLspWorkspace);

        var preparedItem = Assert.Single(await RunPrepareTypeHierarchyAsync(testLspServer, testLspServer.GetLocations("caret").Single()));
        var subtypes = await RunSubtypesAsync(testLspServer, preparedItem);

        var childDefinition = testLspServer.GetLocations("childDef").Single();
        var directImplementationDefinition = testLspServer.GetLocations("directImplDef").Single();
        var grandchildDefinition = testLspServer.GetLocations("grandchildDef").Single();
        var indirectImplementationDefinition = testLspServer.GetLocations("indirectImplDef").Single();

        Assert.Equal(2, subtypes.Length);
        AssertContainsItem(subtypes, "IChild", childDefinition);
        AssertContainsItem(subtypes, "DirectImplementation", directImplementationDefinition);

        var childItem = Assert.Single(subtypes, static item => item.Name == "IChild");
        var childSubtypes = await RunSubtypesAsync(testLspServer, childItem);
        var grandchildItem = Assert.Single(childSubtypes);
        AssertEqualsItem(grandchildItem, "IGrandChild", grandchildDefinition);

        var grandchildSubtypes = await RunSubtypesAsync(testLspServer, grandchildItem);
        var indirectImplementationItem = Assert.Single(grandchildSubtypes);
        AssertEqualsItem(indirectImplementationItem, "IndirectImplementation", indirectImplementationDefinition);
    }

    private static void AssertContainsItem(LSP.TypeHierarchyItem[] items, string expectedName, LSP.Location expectedDefinition)
    {
        var item = Assert.Single(items, i => i.Name == expectedName);
        AssertEqualsItem(item, expectedName, expectedDefinition);
    }

    private static void AssertEqualsItem(LSP.TypeHierarchyItem item, string expectedName, LSP.Location expectedDefinition)
    {
        Assert.Equal(expectedName, item.Name);
        Assert.Equal(expectedDefinition.DocumentUri, item.Uri);
        Assert.Equal(0, CompareRange(expectedDefinition.Range, item.SelectionRange));
    }

    private static async Task<LSP.TypeHierarchyItem[]> RunPrepareTypeHierarchyAsync(TestLspServer testLspServer, LSP.Location caret)
        => await testLspServer.ExecuteRequestAsync<LSP.TypeHierarchyPrepareParams, LSP.TypeHierarchyItem[]?>(
            LSP.Methods.PrepareTypeHierarchyName,
            new LSP.TypeHierarchyPrepareParams
            {
                TextDocument = CreateTextDocumentIdentifier(caret.DocumentUri),
                Position = caret.Range.Start,
            },
            CancellationToken.None) ?? [];

    private static async Task<LSP.TypeHierarchyItem[]> RunSupertypesAsync(TestLspServer testLspServer, LSP.TypeHierarchyItem item)
        => await testLspServer.ExecuteRequestAsync<LSP.TypeHierarchySupertypesParams, LSP.TypeHierarchyItem[]?>(
            LSP.Methods.TypeHierarchySupertypesName,
            new LSP.TypeHierarchySupertypesParams
            {
                TextDocument = CreateTextDocumentIdentifier(item.Uri),
                Position = item.SelectionRange.Start,
                Item = item,
            },
            CancellationToken.None) ?? [];

    private static async Task<LSP.TypeHierarchyItem[]> RunSubtypesAsync(TestLspServer testLspServer, LSP.TypeHierarchyItem item)
        => await testLspServer.ExecuteRequestAsync<LSP.TypeHierarchySubtypesParams, LSP.TypeHierarchyItem[]?>(
            LSP.Methods.TypeHierarchySubtypesName,
            new LSP.TypeHierarchySubtypesParams
            {
                TextDocument = CreateTextDocumentIdentifier(item.Uri),
                Position = item.SelectionRange.Start,
                Item = item,
            },
            CancellationToken.None) ?? [];
}
