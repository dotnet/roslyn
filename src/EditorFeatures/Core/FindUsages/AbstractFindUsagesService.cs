﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    internal abstract partial class AbstractFindUsagesService : IFindUsagesService
    {
        public async Task FindReferencesAsync(
            Document document, int position, IFindUsagesContext context)
        {
            if (await TryFindLiteralReferencesAsync(document, position, context).ConfigureAwait(false))
            {
                return;
            }

            await FindSymbolReferencesAsync(document, position, context).ConfigureAwait(false);
        }

        #region Find Symbol References

        private static async Task FindSymbolReferencesAsync(
            Document document, int position, IFindUsagesContext context)
        {
            var definitionTrackingContext = new DefinitionTrackingContext(context);

            // Need ConfigureAwait(true) here so we get back to the UI thread before calling 
            // GetThirdPartyDefinitions.  We need to call that on the UI thread to match behavior
            // of how the language service always worked in the past.
            //
            // Any async calls before GetThirdPartyDefinitions must be ConfigureAwait(true).
            await FindSymbolReferencesInRemoteOrLocalProcessAsync(
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

        private static ImmutableArray<DefinitionItem> GetThirdPartyDefinitions(
            Solution solution,
            ImmutableArray<DefinitionItem> definitions,
            CancellationToken cancellationToken)
        {
            var factory = solution.Workspace.Services.GetService<IDefinitionsAndReferencesFactory>();
            return definitions.Select(d => factory.GetThirdPartyDefinitionItem(solution, d, cancellationToken))
                              .WhereNotNull()
                              .ToImmutableArray();
        }

        private static async Task FindSymbolReferencesInRemoteOrLocalProcessAsync(
            Document document, int position, IFindUsagesContext context)
        {
            var cancellationToken = context.CancellationToken;
            cancellationToken.ThrowIfCancellationRequested();

            // Find the symbol we want to search and the solution we want to search in.  This is needed
            // to do things like map from a metadata-as-source symbol to an appropriate symbol in the
            // project the user is currently in.
            //
            // We need to do this in the VS process so that we can map from appropriately between workspaces.
            // Once we've mapped *then* we can remote over to the OOP server to do the expensive work of
            // actually finding all the references in the main workspace.
            var symbolAndProject = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(
                document, position, cancellationToken).ConfigureAwait(false);

            if (symbolAndProject == null ||
                symbolAndProject.Value.symbol == null ||
                symbolAndProject.Value.project == null)
            {
                return;
            }

            if (await TryFindSymbolReferencesInRemoteProcessAsync(
                    symbolAndProject.Value.symbol, symbolAndProject.Value.project, context).ConfigureAwait(false))
            {
                return;
            }

            await FindSymbolReferencesInCurrentProcessAsync(
                context, symbolAndProject?.symbol, symbolAndProject?.project).ConfigureAwait(false);
        }

        private static async Task<bool> TryFindSymbolReferencesInRemoteProcessAsync(
            ISymbol symbol, Project project, IFindUsagesContext context)
        {
            var solution = project.Solution;
            var callback = new FindUsagesCallback(solution, context);
            using (var session = await TryGetRemoteSessionAsync(
                solution, callback, context.CancellationToken).ConfigureAwait(false))
            {
                if (session == null)
                {
                    return false;
                }

                await session.InvokeAsync(
                    nameof(IRemoteFindUsages.FindSymbolUsagesAsync),
                    SerializableSymbolAndProjectId.Dehydrate(new SymbolAndProjectId(symbol, project.Id))).ConfigureAwait(false);
                return true;
            }
        }

        /// <summary>
        /// Public helper that we use from features like ObjectBrowser which start with a symbol
        /// and want to push all the references to it into the Streaming-Find-References window.
        /// </summary>
        public static async Task FindSymbolReferencesInCurrentProcessAsync(
            IFindUsagesContext context, ISymbol symbol, Project project)
        {
            await context.SetSearchTitleAsync(string.Format(EditorFeaturesResources._0_references,
                FindUsagesHelpers.GetDisplayName(symbol))).ConfigureAwait(false);

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
                cancellationToken: context.CancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Find Literal References

        private async static Task<bool> TryFindLiteralReferencesAsync(
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
            await context.SetSearchTitleAsync(searchTitle).ConfigureAwait(false);

            var solution = document.Project.Solution;
            if (await TryFindLiteralReferencesInRemoteProcessAsync(
                    solution, searchTitle, context, tokenValue).ConfigureAwait(false))
            {
                return true;
            }

            await FindLiteralReferencesInCurrentProcessAsync(
                solution, searchTitle, context, tokenValue).ConfigureAwait(false);
            return true;
        }

        private static async Task<bool> TryFindLiteralReferencesInRemoteProcessAsync(
            Solution solution, string searchTitle, IFindUsagesContext context, object tokenValue)
        {
            var callback = new FindUsagesCallback(solution, context);
            using (var session = await TryGetRemoteSessionAsync(
                    solution, callback, context.CancellationToken).ConfigureAwait(false))
            {
                if (session == null)
                {
                    return false;
                }

                await session.InvokeAsync(
                    nameof(IRemoteFindUsages.FindLiteralUsagesAsync),
                    searchTitle, tokenValue).ConfigureAwait(false);
                return true;
            }
        }

        internal static async Task FindLiteralReferencesInCurrentProcessAsync(
            Solution solution, string searchTitle, IFindUsagesContext context, object tokenValue)
        {
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
                tokenValue, solution, progressAdapter, context.CancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}