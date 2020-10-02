// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
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
    internal abstract partial class AbstractFindUsagesService
    {
        async Task IFindUsagesService.FindReferencesAsync(
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

        Task IFindUsagesLSPService.FindReferencesAsync(
            Document document, int position, IFindUsagesContext context)
        {
            // We don't need to get third party definitions when finding references in LSP.
            // Currently, 3rd party definitions = XAML definitions, and XAML will provide
            // references via LSP instead of hooking into Roslyn.
            // This also means that we don't need to be on the UI thread.
            return FindLiteralOrSymbolReferencesAsync(document, position, new DefinitionTrackingContext(context));
        }

        private static async Task FindLiteralOrSymbolReferencesAsync(
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

        private static ImmutableArray<DefinitionItem> GetThirdPartyDefinitions(
            Solution solution,
            ImmutableArray<DefinitionItem> definitions,
            CancellationToken cancellationToken)
        {
            var factory = solution.Workspace.Services.GetRequiredService<IDefinitionsAndReferencesFactory>();
            return definitions.Select(d => factory.GetThirdPartyDefinitionItem(solution, d, cancellationToken))
                              .WhereNotNull()
                              .ToImmutableArray();
        }

        private static async Task FindSymbolReferencesAsync(
            Document document, int position, IFindUsagesContext context)
        {
            var cancellationToken = context.CancellationToken;
            cancellationToken.ThrowIfCancellationRequested();

            // If this is a symbol from a metadata-as-source project, then map that symbol back to a symbol in the primary workspace.
            var symbolAndProjectOpt = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(
                document, position, cancellationToken).ConfigureAwait(false);
            if (symbolAndProjectOpt == null)
                return;

            var (symbol, project) = symbolAndProjectOpt.Value;

            await FindSymbolReferencesAsync(
                context, symbol, project).ConfigureAwait(false);
        }

        /// <summary>
        /// Public helper that we use from features like ObjectBrowser which start with a symbol
        /// and want to push all the references to it into the Streaming-Find-References window.
        /// </summary>
        public static async Task FindSymbolReferencesAsync(
            IFindUsagesContext context, ISymbol symbol, Project project)
        {
            await context.SetSearchTitleAsync(string.Format(EditorFeaturesResources._0_references,
                FindUsagesHelpers.GetDisplayName(symbol))).ConfigureAwait(false);

            var options = FindReferencesSearchOptions.GetFeatureOptionsForStartingSymbol(symbol);

            // Now call into the underlying FAR engine to find reference.  The FAR
            // engine will push results into the 'progress' instance passed into it.
            // We'll take those results, massage them, and forward them along to the 
            // FindReferencesContext instance we were given.
            await FindReferencesAsync(context, symbol, project, options).ConfigureAwait(false);
        }

        public static async Task FindReferencesAsync(
            IFindUsagesContext context,
            ISymbol symbol,
            Project project,
            FindReferencesSearchOptions options)
        {
            var cancellationToken = context.CancellationToken;
            var solution = project.Solution;
            var client = await RemoteHostClient.TryGetClientAsync(solution.Workspace, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                // Create a callback that we can pass to the server process to hear about the 
                // results as it finds them.  When we hear about results we'll forward them to
                // the 'progress' parameter which will then update the UI.
                var serverCallback = new FindUsagesServerCallback(solution, context);

                await client.RunRemoteAsync(
                    WellKnownServiceHubService.CodeAnalysis,
                    nameof(IRemoteFindUsagesService.FindReferencesAsync),
                    solution,
                    new object[]
                    {
                        SerializableSymbolAndProjectId.Create(symbol, project, cancellationToken),
                        SerializableFindReferencesSearchOptions.Dehydrate(options),
                    },
                    serverCallback,
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Couldn't effectively search in OOP. Perform the search in-process.
                await FindReferencesInCurrentProcessAsync(
                    context, symbol, project, options).ConfigureAwait(false);
            }
        }

        private static Task FindReferencesInCurrentProcessAsync(
            IFindUsagesContext context,
            ISymbol symbol,
            Project project,
            FindReferencesSearchOptions options)
        {
            var progress = new FindReferencesProgressAdapter(project.Solution, context, options);
            return SymbolFinder.FindReferencesAsync(
                symbol, project.Solution, progress, documents: null, options, context.CancellationToken);
        }

        private static async Task<bool> TryFindLiteralReferencesAsync(
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
