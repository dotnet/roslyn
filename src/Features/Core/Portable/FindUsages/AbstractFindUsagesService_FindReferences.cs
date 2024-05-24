// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindUsages;

internal abstract partial class AbstractFindUsagesService
{
    async Task IFindUsagesService.FindReferencesAsync(
        IFindUsagesContext context, Document document, int position, OptionsProvider<ClassificationOptions> classificationOptions, CancellationToken cancellationToken)
    {
        var definitionTrackingContext = new DefinitionTrackingContext(context);

        await FindLiteralOrSymbolReferencesAsync(
            definitionTrackingContext, document, position, classificationOptions, cancellationToken).ConfigureAwait(false);

        // After the FAR engine is done call into any third party extensions to see
        // if they want to add results.
        var thirdPartyDefinitions = await GetThirdPartyDefinitionsAsync(
            document.Project.Solution, definitionTrackingContext.GetDefinitions(), cancellationToken).ConfigureAwait(false);

        foreach (var definition in thirdPartyDefinitions)
            await context.OnDefinitionFoundAsync(definition, cancellationToken).ConfigureAwait(false);
    }

    Task IFindUsagesLSPService.FindReferencesAsync(
        IFindUsagesContext context, Document document, int position, OptionsProvider<ClassificationOptions> classificationOptions, CancellationToken cancellationToken)
    {
        // We don't need to get third party definitions when finding references in LSP.
        // Currently, 3rd party definitions = XAML definitions, and XAML will provide
        // references via LSP instead of hooking into Roslyn.
        // This also means that we don't need to be on the UI thread.
        return FindLiteralOrSymbolReferencesAsync(
            new DefinitionTrackingContext(context), document, position, classificationOptions, cancellationToken);
    }

    private static async Task FindLiteralOrSymbolReferencesAsync(
        IFindUsagesContext context, Document document, int position, OptionsProvider<ClassificationOptions> classificationOptions, CancellationToken cancellationToken)
    {
        // First, see if we're on a literal.  If so search for literals in the solution with
        // the same value.
        var found = await TryFindLiteralReferencesAsync(
            context, document, position, classificationOptions, cancellationToken).ConfigureAwait(false);
        if (found)
        {
            return;
        }

        // Wasn't a literal.  Try again as a symbol.
        await FindSymbolReferencesAsync(
            context, document, position, classificationOptions, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ImmutableArray<DefinitionItem>> GetThirdPartyDefinitionsAsync(
        Solution solution,
        ImmutableArray<DefinitionItem> definitions,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<DefinitionItem>.GetInstance(out var result);

        var provider = solution.Services.GetRequiredService<IExternalDefinitionItemProvider>();

        foreach (var definition in definitions)
        {
            var thirdParty = await provider.GetThirdPartyDefinitionItemAsync(solution, definition, cancellationToken).ConfigureAwait(false);
            result.AddIfNotNull(thirdParty);
        }

        return result.ToImmutableAndClear();
    }

    private static async Task FindSymbolReferencesAsync(
        IFindUsagesContext context, Document document, int position, OptionsProvider<ClassificationOptions> classificationOptions, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // If this is a symbol from a metadata-as-source project, then map that symbol back to a symbol in the primary workspace.
        var symbolAndProject = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(
            document, position, cancellationToken).ConfigureAwait(false);
        if (symbolAndProject == null)
        {
            await context.ReportNoResultsAsync(FeaturesResources.Find_All_References_not_invoked_on_applicable_symbol, cancellationToken).ConfigureAwait(false);
            return;
        }

        var (symbol, project) = symbolAndProject.Value;

        await FindSymbolReferencesAsync(
            context, symbol, project, classificationOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Public helper that we use from features like ObjectBrowser which start with a symbol
    /// and want to push all the references to it into the Streaming-Find-References window.
    /// </summary>
    public static async Task FindSymbolReferencesAsync(
        IFindUsagesContext context, ISymbol symbol, Project project, OptionsProvider<ClassificationOptions> classificationOptions, CancellationToken cancellationToken)
    {
        await context.SetSearchTitleAsync(
            string.Format(FeaturesResources._0_references,
            FindUsagesHelpers.GetDisplayName(symbol)),
            cancellationToken).ConfigureAwait(false);

        var searchOptions = FindReferencesSearchOptions.GetFeatureOptionsForStartingSymbol(symbol);

        // Now call into the underlying FAR engine to find reference.  The FAR
        // engine will push results into the 'progress' instance passed into it.
        // We'll take those results, massage them, and forward them along to the 
        // FindReferencesContext instance we were given.
        await FindReferencesAsync(context, symbol, project, searchOptions, classificationOptions, cancellationToken).ConfigureAwait(false);
    }

    public static async Task FindReferencesAsync(
        IFindUsagesContext context,
        ISymbol symbol,
        Project project,
        FindReferencesSearchOptions searchOptions,
        OptionsProvider<ClassificationOptions> classificationOptions,
        CancellationToken cancellationToken)
    {
        var solution = project.Solution;
        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            // Create a callback that we can pass to the server process to hear about the 
            // results as it finds them.  When we hear about results we'll forward them to
            // the 'progress' parameter which will then update the UI.
            var serverCallback = new FindUsagesServerCallback(solution, context, classificationOptions);
            var symbolAndProjectId = SerializableSymbolAndProjectId.Create(symbol, project, cancellationToken);

            _ = await client.TryInvokeAsync<IRemoteFindUsagesService>(
                solution,
                (service, solutionInfo, callbackId, cancellationToken) => service.FindReferencesAsync(solutionInfo, callbackId, symbolAndProjectId, searchOptions, cancellationToken),
                serverCallback,
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Couldn't effectively search in OOP. Perform the search in-process.
            await FindReferencesInCurrentProcessAsync(
                context, symbol, project, searchOptions, classificationOptions, cancellationToken).ConfigureAwait(false);
        }
    }

    private static Task FindReferencesInCurrentProcessAsync(
        IFindUsagesContext context,
        ISymbol symbol,
        Project project,
        FindReferencesSearchOptions searchOptions,
        OptionsProvider<ClassificationOptions> classificationOptions,
        CancellationToken cancellationToken)
    {
        var progress = new FindReferencesProgressAdapter(project.Solution, context, searchOptions, classificationOptions);
        return SymbolFinder.FindReferencesAsync(
            symbol, project.Solution, progress, documents: null, searchOptions, cancellationToken);
    }

    private static async Task<bool> TryFindLiteralReferencesAsync(
        IFindUsagesContext context, Document document, int position, OptionsProvider<ClassificationOptions> classificationOptions, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        // Currently we only support FAR for numbers, strings and characters.  We don't
        // bother with true/false/null as those are likely to have way too many results
        // to be useful.
        var token = await syntaxTree.GetTouchingTokenAsync(
            position,
            t => syntaxFacts.IsNumericLiteral(t) ||
                 syntaxFacts.IsCharacterLiteral(t) ||
                 syntaxFacts.IsStringLiteral(t),
            cancellationToken).ConfigureAwait(false);

        if (token.RawKind == 0)
        {
            return false;
        }

        // Searching for decimals not supported currently.  Our index can only store 64bits
        // for numeric values, and a decimal won't fit within that.
        var tokenValue = token.Value;
        if (tokenValue is null or decimal)
            return false;

        if (token.Parent is null)
            return false;

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var symbol = semanticModel.GetSymbolInfo(token.Parent, cancellationToken).Symbol ?? semanticModel.GetDeclaredSymbol(token.Parent, cancellationToken);

        // Numeric labels are available in VB.  In that case we want the normal FAR engine to
        // do the searching.  For these literals we want to find symbolic results and not 
        // numeric matches.
        if (symbol is ILabelSymbol)
            return false;

        // Use the literal to make the title.  Trim literal if it's too long.
        var title = syntaxFacts.ConvertToSingleLine(token.Parent).ToString();
        if (title.Length >= 10)
        {
            title = title[..10] + "...";
        }

        var searchTitle = string.Format(FeaturesResources._0_references, title);
        await context.SetSearchTitleAsync(searchTitle, cancellationToken).ConfigureAwait(false);

        var solution = document.Project.Solution;

        // There will only be one 'definition' that all matching literal reference.
        // So just create it now and report to the context what it is.
        var definition = DefinitionItem.CreateNonNavigableItem(
            tags: [TextTags.StringLiteral],
            displayParts: [new TaggedText(TextTags.Text, searchTitle)]);

        await context.OnDefinitionFoundAsync(definition, cancellationToken).ConfigureAwait(false);

        var progressAdapter = new FindLiteralsProgressAdapter(context, classificationOptions, definition);

        // Now call into the underlying FAR engine to find reference.  The FAR
        // engine will push results into the 'progress' instance passed into it.
        // We'll take those results, massage them, and forward them along to the 
        // FindUsagesContext instance we were given.
        await SymbolFinder.FindLiteralReferencesAsync(
            tokenValue, Type.GetTypeCode(tokenValue.GetType()), solution, progressAdapter, cancellationToken).ConfigureAwait(false);

        return true;
    }
}
