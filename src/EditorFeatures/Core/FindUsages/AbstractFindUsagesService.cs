// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolMonikers;
using Roslyn.Utilities;
using VS.IntelliNav.Contracts;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    internal abstract partial class AbstractFindUsagesService : IFindUsagesService
    {
        private readonly IThreadingContext _threadingContext;
        private readonly ICodeIndexProvider? _codeIndexProvider;

        protected AbstractFindUsagesService(
            IThreadingContext threadingContext,
            ICodeIndexProvider? codeIndexProvider)
        {
            _threadingContext = threadingContext;
            _codeIndexProvider = codeIndexProvider;
        }

        public async Task FindImplementationsAsync(
            Document document, int position, IFindUsagesContext context)
        {
            var cancellationToken = context.CancellationToken;
            var tuple = await FindUsagesHelpers.FindImplementationsAsync(
                document, position, cancellationToken).ConfigureAwait(false);
            if (tuple == null)
            {
                await context.ReportMessageAsync(
                    EditorFeaturesResources.Cannot_navigate_to_the_symbol_under_the_caret).ConfigureAwait(false);
                return;
            }

            var message = tuple.Value.message;

            if (message != null)
            {
                await context.ReportMessageAsync(message).ConfigureAwait(false);
                return;
            }

            await context.SetSearchTitleAsync(
                string.Format(EditorFeaturesResources._0_implementations,
                FindUsagesHelpers.GetDisplayName(tuple.Value.symbol))).ConfigureAwait(false);

            var project = tuple.Value.project;
            foreach (var implementation in tuple.Value.implementations)
            {
                var definitionItem = await implementation.ToClassifiedDefinitionItemAsync(
                    project, includeHiddenLocations: false,
                    FindReferencesSearchOptions.Default, cancellationToken: cancellationToken).ConfigureAwait(false);
                await context.OnDefinitionFoundAsync(definitionItem).ConfigureAwait(false);
            }
        }

        public async Task FindReferencesAsync(
            Document document, int position, IFindUsagesContext context)
        {
            var definitionTrackingContext = new DefinitionTrackingContext(context);

            // Need ConfigureAwait(true) here so we get back to the UI thread before calling 
            // GetThirdPartyDefinitions.  We need to call that on the UI thread to match behavior
            // of how the language service always worked in the past.
            //
            // Any async calls before GetThirdPartyDefinitions must be ConfigureAwait(true).
            await FindLiteralOrSymbolReferencesAsync(
                document, position, definitionTrackingContext).ConfigureAwait(true);

            // After the FAR engine is done call into any third party extensions to see
            // if they want to add results.
            var thirdPartyDefinitions = GetThirdPartyDefinitions(
                document.Project.Solution, definitionTrackingContext.GetDefinitions(), context.CancellationToken);

            // From this point on we can do ConfigureAwait(false) as we're not calling back 
            // into third parties anymore.

            foreach (var definition in thirdPartyDefinitions)
            {
                // Don't need ConfigureAwait(true) here 
                await context.OnDefinitionFoundAsync(definition).ConfigureAwait(false);
            }
        }

        private async Task FindLiteralOrSymbolReferencesAsync(
            Document document, int position, IFindUsagesContext context)
        {
            // First, see if we're on a literal.  If so search for literals in the solution with
            // the same value.
            var found = await TryFindLiteralReferencesAsync(
                document, position, context).ConfigureAwait(false);
            if (found)
            {
                return;
            }

            // Wasn't a literal.  Try again as a symbol.
            await FindSymbolReferencesAsync(
                document, position, context).ConfigureAwait(false);
        }

        private ImmutableArray<DefinitionItem> GetThirdPartyDefinitions(
            Solution solution,
            ImmutableArray<DefinitionItem> definitions,
            CancellationToken cancellationToken)
        {
            var factory = solution.Workspace.Services.GetRequiredService<IDefinitionsAndReferencesFactory>();
            return definitions.Select(d => factory.GetThirdPartyDefinitionItem(solution, d, cancellationToken))
                              .WhereNotNull()
                              .ToImmutableArray();
        }

        private async Task FindSymbolReferencesAsync(
            Document document, int position, IFindUsagesContext context)
        {
            var cancellationToken = context.CancellationToken;
            cancellationToken.ThrowIfCancellationRequested();

            // Find the symbol we want to search and the solution we want to search in.
            var symbolAndProject = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(
                document, position, cancellationToken).ConfigureAwait(false);
            if (symbolAndProject == null)
                return;

            await FindSymbolReferencesAsync(
                _threadingContext, _codeIndexProvider, context,
                symbolAndProject.Value.symbol, symbolAndProject.Value.project, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Public helper that we use from features like ObjectBrowser which start with a symbol
        /// and want to push all the references to it into the Streaming-Find-References window.
        /// </summary>
        public static async Task FindSymbolReferencesAsync(
            IThreadingContext threadingContext, ICodeIndexProvider? codeIndexProvider,
            IFindUsagesContext context, ISymbol symbol, Project project, CancellationToken cancellationToken)
        {
            await context.SetSearchTitleAsync(string.Format(EditorFeaturesResources._0_references,
                FindUsagesHelpers.GetDisplayName(symbol))).ConfigureAwait(false);

            var options = FindReferencesSearchOptions.GetFeatureOptionsForStartingSymbol(symbol);
            var progressAdapter = new FindReferencesProgressAdapter(threadingContext, project.Solution, context, options);

            // Now call into the underlying FAR engine to find reference.  The FAR
            // engine will push results into the 'progress' instance passed into it.
            // We'll take those results, massage them, and forward them along to the 
            // FindReferencesContext instance we were given.
            var normalFindReferencesTask = SymbolFinder.FindReferencesAsync(
                SymbolAndProjectId.Create(symbol, project.Id),
                project.Solution,
                progressAdapter,
                documents: null,
                options,
                cancellationToken);

            // Kick off work to search the online code index system in parallel
            var codeIndexReferencesTask = FindCodeIndexReferencesAsync(
                codeIndexProvider,
                symbol,
                project.Solution,
                progressAdapter,
                options,
                cancellationToken);

            await Task.WhenAll(normalFindReferencesTask, codeIndexReferencesTask).ConfigureAwait(false);
        }

        private async Task FindCodeIndexReferencesAsync(
            ICodeIndexProvider? codeIndexProvider,
            ISymbol symbol, Solution solution,
            FindReferencesProgressAdapter progressAdapter,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            if (codeIndexProvider == null)
                return;

            var moniker = SymbolMoniker.TryCreate(symbol);
            if (moniker == null)
                return;

            var monikers = SpecializedCollections.SingletonEnumerable(moniker);
            var currentPage = 0;
            while (true)
            {
                var results = await codeIndexProvider.FindReferencesByMonikerAsync(
                    monikers, includeDecleration: true, pageIndex: currentPage, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (results == null || results.Count == 0)
                    break;

                currentPage++;
                await ProcessCodeIndexResultsAsync(
                    codeIndexProvider, results, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<bool> TryFindLiteralReferencesAsync(
            Document document, int position, IFindUsagesContext context)
        {
            var cancellationToken = context.CancellationToken;
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
            if (tokenValue == null || tokenValue is decimal)
                return false;

            if (token.Parent is null)
                return false;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbol = semanticModel.GetSymbolInfo(token.Parent).Symbol ?? semanticModel.GetDeclaredSymbol(token.Parent);

            // Numeric labels are available in VB.  In that case we want the normal FAR engine to
            // do the searching.  For these literals we want to find symbolic results and not 
            // numeric matches.
            if (symbol is ILabelSymbol)
                return false;

            // Use the literal to make the title.  Trim literal if it's too long.
            var title = syntaxFacts.ConvertToSingleLine(token.Parent).ToString();
            if (title.Length >= 10)
            {
                title = title.Substring(0, 10) + "...";
            }

            var searchTitle = string.Format(EditorFeaturesResources._0_references, title);
            await context.SetSearchTitleAsync(searchTitle).ConfigureAwait(false);

            var solution = document.Project.Solution;

            // There will only be one 'definition' that all matching literal reference.
            // So just create it now and report to the context what it is.
            var definition = DefinitionItem.CreateNonNavigableItem(
                ImmutableArray.Create(TextTags.StringLiteral),
                ImmutableArray.Create(new TaggedText(TextTags.Text, searchTitle)));

            await context.OnDefinitionFoundAsync(definition).ConfigureAwait(false);

            var progressAdapter = new FindLiteralsProgressAdapter(context, definition);

            // Now call into the underlying FAR engine to find reference.  The FAR
            // engine will push results into the 'progress' instance passed into it.
            // We'll take those results, massage them, and forward them along to the 
            // FindUsagesContext instance we were given.
            await SymbolFinder.FindLiteralReferencesAsync(
                tokenValue, Type.GetTypeCode(tokenValue.GetType()), solution, progressAdapter, cancellationToken).ConfigureAwait(false);

            return true;
        }
    }
}
