// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities.Snippets;

[UseExportProvider]
public abstract class AbstractSnippetProviderTests
{
    protected abstract string SnippetIdentifier { get; }

    protected abstract string LanguageName { get; }

    protected async Task VerifySnippetAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markupBeforeCommit,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markupAfterCommit,
        string? editorconfig = null,
        ReferenceAssemblies? referenceAssemblies = null)
    {
        using var workspace = new TestWorkspace(FeaturesTestCompositions.Features);

        referenceAssemblies ??= ReferenceAssemblies.Default;
        var metadataReferences = await referenceAssemblies.ResolveAsync(LanguageName, CancellationToken.None);
        var project = workspace.CurrentSolution.
            AddProject("TestProject", "TestAssembly", LanguageName)
            .WithMetadataReferences(metadataReferences);

        TestFileMarkupParser.GetPosition(markupBeforeCommit, out markupBeforeCommit, out var snippetRequestPosition);
        var document = project.AddDocument("TestDocument", markupBeforeCommit, filePath: "/TestDocument");

        if (editorconfig is not null)
        {
            var editorConfigDoc = document.Project.AddAnalyzerConfigDocument(".editorconfig", SourceText.From(editorconfig), filePath: "/.editorconfig");
            document = editorConfigDoc.Project.GetDocument(document.Id)!;
        }

        var snippetServiceInterface = document.GetRequiredLanguageService<ISnippetService>();
        var snippetService = Assert.IsAssignableFrom<AbstractSnippetService>(snippetServiceInterface);

        snippetService.EnsureSnippetsLoaded(LanguageName);
        var snippetProvider = snippetService.GetSnippetProvider(SnippetIdentifier);

        var syntaxContextService = document.GetRequiredLanguageService<ISyntaxContextService>();
        var semanticModel = await document.GetRequiredSemanticModelAsync(CancellationToken.None);
        var syntaxContext = syntaxContextService.CreateContext(document, semanticModel, snippetRequestPosition, CancellationToken.None);

        var snippetContext = new SnippetContext(syntaxContext);
        var isValidSnippetLocation = snippetProvider.IsValidSnippetLocation(snippetContext, CancellationToken.None);
        Assert.True(isValidSnippetLocation, "Snippet is unexpectedly invalid on a given position");

        var snippetChange = await snippetProvider.GetSnippetChangeAsync(document, snippetRequestPosition, CancellationToken.None);
        var documentSourceText = await document.GetTextAsync();
        var documentTextAfterSnippet = documentSourceText.WithChanges(snippetChange.TextChanges);

        TestFileMarkupParser.GetPositionAndSpans(markupAfterCommit, out markupAfterCommit, out int finalCaretPosition, out ImmutableDictionary<string, ImmutableArray<TextSpan>> placeholderLocations);
        Assert.Equal(markupAfterCommit, documentTextAfterSnippet.ToString());

        var placeholderLocationsArray = new ImmutableArray<TextSpan>[placeholderLocations.Count];
        var snippetPlaceholders = snippetChange.Placeholders;
        Assert.Equal(placeholderLocationsArray.Length, snippetPlaceholders.Length);

        foreach (var placeholderLocationPair in placeholderLocations)
        {
            if (!int.TryParse(placeholderLocationPair.Key, out var locationIndex))
            {
                Assert.True(false, "Expected placeholder locations contains span with invalid annotation");
                return;
            }

            placeholderLocationsArray[locationIndex] = placeholderLocationPair.Value;
        }

        for (var i = 0; i < placeholderLocationsArray.Length; i++)
        {
            if (placeholderLocationsArray[i].IsDefault)
            {
                Assert.True(false, $"Placeholder location for index {i} was not specified");
            }
        }

        for (var i = 0; i < snippetPlaceholders.Length; i++)
        {
            var expectedSpans = placeholderLocationsArray[i];
            var (placeholderText, placeholderPositions) = snippetPlaceholders[i];

            Assert.Equal(expectedSpans.Length, placeholderPositions.Length);

            for (var j = 0; j < placeholderPositions.Length; j++)
            {
                var expectedSpan = expectedSpans[j];
                Assert.Contains(expectedSpan.Start, placeholderPositions);
                Assert.Equal(documentTextAfterSnippet.ToString(expectedSpan), placeholderText);
            }
        }

        Assert.Equal(finalCaretPosition, snippetChange.FinalCaretPosition);
    }

    protected async Task VerifySnippetIsAbsentAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup,
        ReferenceAssemblies? referenceAssemblies = null)
    {
        using var workspace = new TestWorkspace(FeaturesTestCompositions.Features);

        referenceAssemblies ??= ReferenceAssemblies.Default;
        var metadataReferences = await referenceAssemblies.ResolveAsync(LanguageName, CancellationToken.None);
        var project = workspace.CurrentSolution.
            AddProject("TestProject", "TestAssembly", LanguageName)
            .WithMetadataReferences(metadataReferences);

        TestFileMarkupParser.GetPosition(markup, out markup, out var snippetRequestPosition);
        var document = project.AddDocument("TestDocument", markup);

        var snippetServiceInterface = document.GetRequiredLanguageService<ISnippetService>();
        var snippetService = Assert.IsAssignableFrom<AbstractSnippetService>(snippetServiceInterface);

        snippetService.EnsureSnippetsLoaded(LanguageName);
        var snippetProvider = snippetService.GetSnippetProvider(SnippetIdentifier);

        var syntaxContextService = document.GetRequiredLanguageService<ISyntaxContextService>();
        var semanticModel = await document.GetRequiredSemanticModelAsync(CancellationToken.None);
        var syntaxContext = syntaxContextService.CreateContext(document, semanticModel, snippetRequestPosition, CancellationToken.None);

        var snippetContext = new SnippetContext(syntaxContext);
        var isValidSnippetLocation = snippetProvider.IsValidSnippetLocation(snippetContext, CancellationToken.None);
        Assert.False(isValidSnippetLocation, "Snippet is unexpectedly valid on a given position");
    }
}
