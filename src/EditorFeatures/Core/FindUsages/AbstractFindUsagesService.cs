// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    internal abstract partial class AbstractFindUsagesService : IFindUsagesService
    {
        public async Task FindImplementationsAsync(Document document, int position, IFindUsagesContext context)
        {
            var tuple = await FindUsagesHelpers.FindImplementationsAsync(
                document, position, context.CancellationToken).ConfigureAwait(false);
            if (tuple == null)
            {
                context.ReportMessage(EditorFeaturesResources.Cannot_navigate_to_the_symbol_under_the_caret);
                return;
            }

            var message = tuple.Value.message;

            if (message != null)
            {
                context.ReportMessage(message);
                return;
            }

            context.SetSearchTitle(string.Format(EditorFeaturesResources._0_implementations,
                FindUsagesHelpers.GetDisplayName(tuple.Value.symbol)));

            var project = tuple.Value.project;
            foreach (var implementation in tuple.Value.implementations)
            {
                var definitionItem = implementation.ToDefinitionItem(
                    project.Solution, includeHiddenLocations: false);
                await context.OnDefinitionFoundAsync(definitionItem).ConfigureAwait(false);
            }
        }

        public async Task FindReferencesAsync(
            Document document, int position, IFindUsagesContext context)
        {
            // NOTE: All ConFigureAwaits in this method need to pass 'true' so that
            // we return to the caller's context.  that's so the call to 
            // CallThirdPartyExtensionsAsync will happen on the UI thread.  We need
            // this to maintain the threading guarantee we had around that method
            // from pre-Roslyn days.
            var cancellationToken = context.CancellationToken;
            cancellationToken.ThrowIfCancellationRequested();

            // First, see if we're on a literal.  If so search for literals in the solution with
            // the same value.
            var found = await TryFindLiteralReferencesAsync(document, position, context).ConfigureAwait(true);
            if (found)
            {
                return;
            }

            var findReferencesProgress = await FindSymbolReferencesAsync(
                document, position, context).ConfigureAwait(true);
            if (findReferencesProgress == null)
            {
                return;
            }

            // After the FAR engine is done call into any third party extensions to see
            // if they want to add results.
            await findReferencesProgress.CallThirdPartyExtensionsAsync(
                context.CancellationToken).ConfigureAwait(true);
        }

        private async Task<FindReferencesProgressAdapter> FindSymbolReferencesAsync(
            Document document, int position, IFindUsagesContext context)
        {
            var cancellationToken = context.CancellationToken;
            cancellationToken.ThrowIfCancellationRequested();

            // Find the symbol we want to search and the solution we want to search in.
            var symbolAndProject = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(
                document, position, cancellationToken).ConfigureAwait(false);
            if (symbolAndProject == null)
            {
                return null;
            }

            var symbol = symbolAndProject?.symbol;
            var project = symbolAndProject?.project;

            context.SetSearchTitle(string.Format(EditorFeaturesResources._0_references,
                FindUsagesHelpers.GetDisplayName(symbol)));

            var progressAdapter = new FindReferencesProgressAdapter(project.Solution, context);

            // Now call into the underlying FAR engine to find reference.  The FAR
            // engine will push results into the 'progress' instance passed into it.
            // We'll take those results, massage them, and forward them along to the 
            // FindReferencesContext instance we were given.
            await SymbolFinder.FindReferencesAsync(
                SymbolAndProjectId.Create(symbol, project.Id),
                project.Solution,
                progressAdapter,
                documents: null,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return progressAdapter;
        }

        private async Task<bool> TryFindLiteralReferencesAsync(
            Document document, int position, IFindUsagesContext context)
        {
            var cancellationToken = context.CancellationToken;
            cancellationToken.ThrowIfCancellationRequested();

            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

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
            {
                return false;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbol = semanticModel.GetSymbolInfo(token.Parent).Symbol ?? semanticModel.GetDeclaredSymbol(token.Parent);

            // Numeric labels are available in VB.  In that case we want the normal FAR engine to
            // do the searching.  For these literals we want to find symbolic results and not 
            // numeric matches.
            if (symbol is ILabelSymbol)
            {
                return false;
            }

            // Use the literal to make the title.  Trim literal if it's too long.
            var title = syntaxFacts.ConvertToSingleLine(token.Parent).ToString();
            if (title.Length >= 10)
            {
                title = title.Substring(0, 10) + "...";
            }

            var searchTitle = string.Format(EditorFeaturesResources._0_references, title);
            context.SetSearchTitle(searchTitle);

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
                tokenValue, solution, progressAdapter, cancellationToken).ConfigureAwait(false);

            return true;
        }
    }
}