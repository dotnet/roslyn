// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.TypeHierarchy;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;
using TypeHierarchyRange = Roslyn.LanguageServer.Protocol.Range;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostTypeHierarchyEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task PrepareTypeHierarchy_ComponentDefinedInCSharp()
    {
        TestCode input = """
            <Sur$$veyPrompt />
            """;

        TestCode surveyPrompt = """
            using Microsoft.AspNetCore.Components;

            namespace SomeProject;

            public class {|componentDef:SurveyPrompt|} : ComponentBase
            {
            }
            """;

        var items = await GetPreparedItemsAsync(input, (FilePath("SurveyPrompt.cs"), surveyPrompt.Text));
        AssertItems(items, (FileUri("SurveyPrompt.cs"), surveyPrompt, names: null));
    }

    [Fact]
    public async Task TypeHierarchySupertypes_RoundTripsMappedItems()
    {
        TestCode input = """
            @code
            {
                interface {|topInterfaceDef:ITop|}
                {
                }

                interface {|midInterfaceDef:IMid|} : ITop
                {
                }

                class {|baseDef:Base|} : IMid
                {
                }

                class {|midDef:Mid|} : Base
                {
                }

                class {|derivedDef:Der$$ived|} : Mid
                {
                }
            }
            """;

        var document = CreateProjectAndRazorDocument(input.Text);
        var preparedItem = Assert.Single(await GetPreparedItemsAsync(document, input.Position));
        AssertItems([preparedItem], (document.GetURI().GetRequiredSystemUri(), input, ["derivedDef"]));

        var supertypes = await GetSupertypesAsync(document, preparedItem);
        AssertItems(supertypes, (document.GetURI().GetRequiredSystemUri(), input, ["midDef"]));
        var midItem = Assert.Single(supertypes);

        var midSupertypes = await GetSupertypesAsync(document, midItem);
        AssertItems(midSupertypes, (document.GetURI().GetRequiredSystemUri(), input, ["baseDef"]));
        var baseItem = Assert.Single(midSupertypes);

        var baseSupertypes = await GetSupertypesAsync(document, baseItem);
        AssertItems(baseSupertypes, (document.GetURI().GetRequiredSystemUri(), input, ["midInterfaceDef"]));
        var midInterfaceItem = Assert.Single(baseSupertypes);

        var midInterfaceSupertypes = await GetSupertypesAsync(document, midInterfaceItem);
        AssertItems(midInterfaceSupertypes, (document.GetURI().GetRequiredSystemUri(), input, ["topInterfaceDef"]));
    }

    [Fact]
    public async Task TypeHierarchySubtypes_RoundTripsMappedItems()
    {
        TestCode input = """
            @code
            {
                interface {|rootDef:IRoot|}
                {
                }

                interface {|childDef:IChild|} : IRoot
                {
                }

                interface {|grandchildDef:IGrandChild|} : IChild
                {
                }

                class {|directImplDef:DirectImplementation|} : IRoot
                {
                }

                class {|indirectImplDef:IndirectImplementation|} : IGrandChild
                {
                }

                void M(IR$$oot value)
                {
                }
            }
            """;

        var document = CreateProjectAndRazorDocument(input.Text);
        var preparedItem = Assert.Single(await GetPreparedItemsAsync(document, input.Position));
        AssertItems([preparedItem], (document.GetURI().GetRequiredSystemUri(), input, ["rootDef"]));

        var subtypes = await GetSubtypesAsync(document, preparedItem);
        AssertItems(subtypes, (document.GetURI().GetRequiredSystemUri(), input, ["childDef", "directImplDef"]));

        var childItem = Assert.Single(subtypes, item => item.Name == "IChild");
        var childSubtypes = await GetSubtypesAsync(document, childItem);
        AssertItems(childSubtypes, (document.GetURI().GetRequiredSystemUri(), input, ["grandchildDef"]));
        var grandchildItem = Assert.Single(childSubtypes);

        var grandchildSubtypes = await GetSubtypesAsync(document, grandchildItem);
        AssertItems(grandchildSubtypes, (document.GetURI().GetRequiredSystemUri(), input, ["indirectImplDef"]));
    }

    private async Task<TypeHierarchyItem[]> GetPreparedItemsAsync(
        TestCode input,
        params (string fileName, string contents)[] additionalFiles)
    {
        var document = CreateProjectAndRazorDocument(input.Text, additionalFiles: additionalFiles);
        return await GetPreparedItemsAsync(document, input.Position);
    }

    private async Task<TypeHierarchyItem[]> GetPreparedItemsAsync(TextDocument document, int absolutePosition)
    {
        var inputText = await document.GetTextAsync(DisposalToken);
        var endpoint = new CohostPrepareTypeHierarchyEndpoint(IncompatibleProjectService, RemoteServiceInvoker);

        var request = new TypeHierarchyPrepareParams
        {
            Position = inputText.GetPosition(absolutePosition),
            TextDocument = new TextDocumentIdentifier { DocumentUri = document.GetURI() },
        };

        return await endpoint.GetTestAccessor().HandleRequestAsync(request, document, DisposalToken) ?? [];
    }

    private async Task<TypeHierarchyItem[]> GetSupertypesAsync(TextDocument document, TypeHierarchyItem item)
    {
        var endpoint = new CohostTypeHierarchySupertypesEndpoint(IncompatibleProjectService, RemoteServiceInvoker);
        var roundTrippedItem = RoundTripItemData(item);

        var request = new TypeHierarchySupertypesParams
        {
            Item = roundTrippedItem,
            Position = roundTrippedItem.SelectionRange.Start,
            TextDocument = new TextDocumentIdentifier { DocumentUri = roundTrippedItem.Uri },
        };

        return await endpoint.GetTestAccessor().HandleRequestAsync(request, document, DisposalToken) ?? [];
    }

    private async Task<TypeHierarchyItem[]> GetSubtypesAsync(TextDocument document, TypeHierarchyItem item)
    {
        var endpoint = new CohostTypeHierarchySubtypesEndpoint(IncompatibleProjectService, RemoteServiceInvoker);
        var roundTrippedItem = RoundTripItemData(item);

        var request = new TypeHierarchySubtypesParams
        {
            Item = roundTrippedItem,
            Position = roundTrippedItem.SelectionRange.Start,
            TextDocument = new TextDocumentIdentifier { DocumentUri = roundTrippedItem.Uri },
        };

        return await endpoint.GetTestAccessor().HandleRequestAsync(request, document, DisposalToken) ?? [];
    }

    private static TypeHierarchyItem RoundTripItemData(TypeHierarchyItem item)
        => RazorTypeHierarchyResolveData.WithData(item, JsonSerializer.SerializeToElement(item.Data, JsonHelpers.JsonSerializerOptions));

    private static void AssertItems(TypeHierarchyItem[] items, params (Uri uri, TestCode code, string[]? names)[] expectedSources)
    {
        var expectedItems = GetExpectedItems(expectedSources);
        Assert.Equal(expectedItems.Length, items.Length);

        foreach (var expectedItem in expectedItems)
        {
            var item = Assert.Single(items, item => item.Name == expectedItem.Name);
            AssertItem(item, expectedItem);
        }
    }

    private static void AssertItem(TypeHierarchyItem item, ExpectedTypeHierarchyItem expectedItem)
    {
        Assert.Equal(expectedItem.Name, item.Name);
        Assert.Equal(expectedItem.Uri, item.Uri.ParsedUri);
        Assert.Equal(expectedItem.SelectionRange, item.SelectionRange);
        Assert.Equal(expectedItem.Uri, GetResolveDataDocumentUri(item));
    }

    private static ExpectedTypeHierarchyItem[] GetExpectedItems((Uri uri, TestCode code, string[]? names)[] expectedSources)
    {
        return
        [
            .. expectedSources.SelectMany(source =>
            {
                var expectedNames = source.names ?? [.. source.code.NamedSpans.Keys];
                return expectedNames.Select(name => GetExpectedItem(source.uri, source.code, name));
            }),
        ];
    }

    private static ExpectedTypeHierarchyItem GetExpectedItem(Uri uri, TestCode code, string name)
    {
        var span = Assert.Single(code.NamedSpans[name]);
        var sourceText = SourceText.From(code.Text);
        return new(
            sourceText.ToString(span),
            uri,
            sourceText.GetRange(span));
    }

    private static Uri GetResolveDataDocumentUri(TypeHierarchyItem item)
    {
        var data = item.Data switch
        {
            JsonElement jsonElement => jsonElement,
            { } value => JsonSerializer.SerializeToElement(value, value.GetType()),
            _ => throw new Xunit.Sdk.XunitException("Expected type hierarchy item to have resolve data."),
        };

        var textDocument = data.TryGetProperty("textDocument", out var camelCaseTextDocument)
            ? camelCaseTextDocument
            : data.GetProperty("TextDocument");
        var uriString = textDocument.TryGetProperty("uri", out var camelCaseUri)
            ? camelCaseUri.GetString()
            : textDocument.GetProperty("Uri").GetString();

        Assert.NotNull(uriString);
        return new Uri(uriString);
    }

    private sealed record ExpectedTypeHierarchyItem(string Name, Uri Uri, TypeHierarchyRange SelectionRange);
}
