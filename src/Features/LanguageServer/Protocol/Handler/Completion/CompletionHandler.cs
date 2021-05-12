// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Handle a completion request.
    /// </summary>
    internal class CompletionHandler : IRequestHandler<LSP.CompletionParams, LSP.CompletionList?>
    {
        private readonly ImmutableHashSet<char> _csharpTriggerCharacters;
        private readonly ImmutableHashSet<char> _vbTriggerCharacters;

        private readonly CompletionListCache _completionListCache;

        public string Method => LSP.Methods.TextDocumentCompletionName;

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public CompletionHandler(
            IEnumerable<Lazy<CompletionProvider, CompletionProviderMetadata>> completionProviders,
            CompletionListCache completionListCache)
        {
            _csharpTriggerCharacters = completionProviders.Where(lz => lz.Metadata.Language == LanguageNames.CSharp).SelectMany(
                lz => GetTriggerCharacters(lz.Value)).ToImmutableHashSet();
            _vbTriggerCharacters = completionProviders.Where(lz => lz.Metadata.Language == LanguageNames.VisualBasic).SelectMany(
                lz => GetTriggerCharacters(lz.Value)).ToImmutableHashSet();

            _completionListCache = completionListCache;
        }

        public LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.CompletionParams request) => request.TextDocument;

        public async Task<LSP.CompletionList?> HandleRequestAsync(LSP.CompletionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            if (document == null)
            {
                return null;
            }

            // C# and VB share the same LSP language server, and thus share the same default trigger characters.
            // We need to ensure the trigger character is valid in the document's language. For example, the '{'
            // character, while a trigger character in VB, is not a trigger character in C#.
            if (request.Context != null &&
                request.Context.TriggerKind == LSP.CompletionTriggerKind.TriggerCharacter &&
                !char.TryParse(request.Context.TriggerCharacter, out var triggerCharacter) &&
                !char.IsLetterOrDigit(triggerCharacter) &&
                !IsValidTriggerCharacterForDocument(document, triggerCharacter))
            {
                return null;
            }

            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);
            var completionOptions = await GetCompletionOptionsAsync(document, cancellationToken).ConfigureAwait(false);
            var completionService = document.Project.LanguageServices.GetRequiredService<CompletionService>();

            // TO-DO: More LSP.CompletionTriggerKind mappings are required to properly map to Roslyn CompletionTriggerKinds.
            // https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1178726
            var completionTrigger = await ProtocolConversions.LSPToRoslynCompletionTriggerAsync(request.Context, document, position, cancellationToken).ConfigureAwait(false);

            var list = await completionService.GetCompletionsAsync(document, position, completionTrigger, options: completionOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (list == null || list.Items.IsEmpty || cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            var lspVSClientCapability = context.ClientCapabilities.HasVisualStudioLspCapability() == true;
            var snippetsSupported = context.ClientCapabilities.TextDocument?.Completion?.CompletionItem?.SnippetSupport ?? false;
            var commitCharactersRuleCache = new Dictionary<ImmutableArray<CharacterSetModificationRule>, ImmutableArray<string>>();

            // Cache the completion list so we can avoid recomputation in the resolve handler
            var resultId = _completionListCache.UpdateCache(request.TextDocument, list);

            // Feature flag to enable the return of TextEdits instead of InsertTexts (will increase payload size).
            // Flag is defined in VisualStudio\Core\Def\PackageRegistration.pkgdef.
            // We also check against the CompletionOption for test purposes only.
            Contract.ThrowIfNull(context.Solution);
            var featureFlagService = context.Solution.Workspace.Services.GetRequiredService<IExperimentationService>();
            var returnTextEdits = featureFlagService.IsExperimentEnabled(WellKnownExperimentNames.LSPCompletion) ||
                completionOptions.GetOption(CompletionOptions.ForceRoslynLSPCompletionExperiment, document.Project.Language);

            SourceText? documentText = null;
            TextSpan? defaultSpan = null;
            LSP.Range? defaultRange = null;
            if (returnTextEdits)
            {
                // We want to compute the document's text just once.
                documentText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                // We use the first item in the completion list as our comparison point for span
                // and range for optimization when generating the TextEdits later on.
                var completionChange = await completionService.GetChangeAsync(
                    document, list.Items.First(), cancellationToken: cancellationToken).ConfigureAwait(false);

                // If possible, we want to compute the item's span and range just once.
                // Individual items can override this range later.
                defaultSpan = completionChange.TextChange.Span;
                defaultRange = ProtocolConversions.TextSpanToRange(defaultSpan.Value, documentText);
            }

            var stringBuilder = new StringBuilder();
            using var _ = ArrayBuilder<LSP.CompletionItem>.GetInstance(out var lspCompletionItems);
            foreach (var item in list.Items)
            {
                var lspCompletionItem = await CreateLSPCompletionItemAsync(
                    request, document, item, resultId, lspVSClientCapability, completionTrigger, commitCharactersRuleCache,
                    completionService, context.ClientName, returnTextEdits, snippetsSupported, stringBuilder, documentText,
                    defaultSpan, defaultRange, cancellationToken).ConfigureAwait(false);
                lspCompletionItems.Add(lspCompletionItem);
            }

            var completionList = new LSP.VSCompletionList
            {
                Items = lspCompletionItems.ToArray(),
                SuggestionMode = list.SuggestionModeItem != null,
            };
            var optimizedCompletionList = new LSP.OptimizedVSCompletionList(completionList);
            return optimizedCompletionList;

            // Local functions
            bool IsValidTriggerCharacterForDocument(Document document, char triggerCharacter)
            {
                if (document.Project.Language == LanguageNames.CSharp)
                {
                    return _csharpTriggerCharacters.Contains(triggerCharacter);
                }
                else if (document.Project.Language == LanguageNames.VisualBasic)
                {
                    return _vbTriggerCharacters.Contains(triggerCharacter);
                }

                // Typescript still calls into this for completion.
                // Since we don't know what their trigger characters are, just return true.
                return true;
            }

            static async Task<LSP.CompletionItem> CreateLSPCompletionItemAsync(
                LSP.CompletionParams request,
                Document document,
                CompletionItem item,
                long? resultId,
                bool useVSCompletionItem,
                CompletionTrigger completionTrigger,
                Dictionary<ImmutableArray<CharacterSetModificationRule>, ImmutableArray<string>> commitCharacterRulesCache,
                CompletionService completionService,
                string? clientName,
                bool returnTextEdits,
                bool snippetsSupported,
                StringBuilder stringBuilder,
                SourceText? documentText,
                TextSpan? defaultSpan,
                LSP.Range? defaultRange,
                CancellationToken cancellationToken)
            {
                if (useVSCompletionItem)
                {
                    var vsCompletionItem = await CreateCompletionItemAsync<LSP.VSCompletionItem>(
                        request, document, item, resultId, completionTrigger, commitCharacterRulesCache,
                        completionService, clientName, returnTextEdits, snippetsSupported, stringBuilder,
                        documentText, defaultSpan, defaultRange, cancellationToken).ConfigureAwait(false);
                    vsCompletionItem.Icon = new ImageElement(item.Tags.GetFirstGlyph().GetImageId());
                    return vsCompletionItem;
                }
                else
                {
                    var roslynCompletionItem = await CreateCompletionItemAsync<LSP.CompletionItem>(
                        request, document, item, resultId, completionTrigger, commitCharacterRulesCache,
                        completionService, clientName, returnTextEdits, snippetsSupported, stringBuilder,
                        documentText, defaultSpan, defaultRange, cancellationToken).ConfigureAwait(false);
                    return roslynCompletionItem;
                }
            }

            static async Task<TCompletionItem> CreateCompletionItemAsync<TCompletionItem>(
                LSP.CompletionParams request,
                Document document,
                CompletionItem item,
                long? resultId,
                CompletionTrigger completionTrigger,
                Dictionary<ImmutableArray<CharacterSetModificationRule>, ImmutableArray<string>> commitCharacterRulesCache,
                CompletionService completionService,
                string? clientName,
                bool returnTextEdits,
                bool snippetsSupported,
                StringBuilder stringBuilder,
                SourceText? documentText,
                TextSpan? defaultSpan,
                LSP.Range? defaultRange,
                CancellationToken cancellationToken) where TCompletionItem : LSP.CompletionItem, new()
            {
                // Generate display text
                stringBuilder.Append(item.DisplayTextPrefix);
                stringBuilder.Append(item.DisplayText);
                stringBuilder.Append(item.DisplayTextSuffix);
                var completeDisplayText = stringBuilder.ToString();
                stringBuilder.Clear();

                var completionItem = new TCompletionItem
                {
                    Label = completeDisplayText,
                    SortText = item.SortText,
                    FilterText = item.FilterText,
                    Kind = GetCompletionKind(item.Tags),
                    Data = new CompletionResolveData
                    {
                        ResultId = resultId,
                    },
                    Preselect = item.Rules.SelectionBehavior == CompletionItemSelectionBehavior.HardSelection,
                };

                // Complex text edits (e.g. override and partial method completions) are always populated in the
                // resolve handler, so we leave both TextEdit and InsertText unpopulated in these cases.
                if (item.IsComplexTextEdit)
                {
                    // Razor C# is currently the only language client that supports LSP.InsertTextFormat.Snippet.
                    // We can enable it for regular C# once LSP is used for local completion.
                    if (snippetsSupported)
                    {
                        completionItem.InsertTextFormat = LSP.InsertTextFormat.Snippet;
                    }
                }
                // If the feature flag is on, always return a TextEdit.
                else if (returnTextEdits)
                {
                    var textEdit = await GenerateTextEdit(
                        document, item, completionService, documentText, defaultSpan, defaultRange, cancellationToken).ConfigureAwait(false);
                    completionItem.TextEdit = textEdit;
                }
                // If the feature flag is off, return an InsertText.
                else
                {
                    completionItem.InsertText = item.Properties.ContainsKey("InsertionText") ? item.Properties["InsertionText"] : completeDisplayText;
                }

                var commitCharacters = GetCommitCharacters(item, commitCharacterRulesCache);
                if (commitCharacters != null)
                {
                    completionItem.CommitCharacters = commitCharacters;
                }

                return completionItem;

                static async Task<LSP.TextEdit> GenerateTextEdit(
                    Document document,
                    CompletionItem item,
                    CompletionService completionService,
                    SourceText? documentText,
                    TextSpan? defaultSpan,
                    LSP.Range? defaultRange,
                    CancellationToken cancellationToken)
                {
                    Contract.ThrowIfNull(documentText);
                    Contract.ThrowIfNull(defaultSpan);
                    Contract.ThrowIfNull(defaultRange);

                    var completionChange = await completionService.GetChangeAsync(
                        document, item, cancellationToken: cancellationToken).ConfigureAwait(false);
                    var completionChangeSpan = completionChange.TextChange.Span;

                    var textEdit = new LSP.TextEdit()
                    {
                        NewText = completionChange.TextChange.NewText ?? "",
                        Range = completionChangeSpan == defaultSpan.Value
                            ? defaultRange
                            : ProtocolConversions.TextSpanToRange(completionChangeSpan, documentText),
                    };

                    return textEdit;
                }
            }

            static string[]? GetCommitCharacters(CompletionItem item, Dictionary<ImmutableArray<CharacterSetModificationRule>, ImmutableArray<string>> currentRuleCache)
            {
                var commitCharacterRules = item.Rules.CommitCharacterRules;

                // If the item doesn't have any special rules, just use the default commit characters.
                if (commitCharacterRules.IsEmpty)
                {
                    return null;
                }

                if (currentRuleCache.TryGetValue(commitCharacterRules, out var currentRules))
                {
                    return currentRules.ToArray();
                }

                using var _ = PooledHashSet<char>.GetInstance(out var commitCharacters);
                commitCharacters.AddAll(CompletionRules.Default.DefaultCommitCharacters);
                foreach (var rule in commitCharacterRules)
                {
                    switch (rule.Kind)
                    {
                        case CharacterSetModificationKind.Add:
                            commitCharacters.UnionWith(rule.Characters);
                            continue;
                        case CharacterSetModificationKind.Remove:
                            commitCharacters.ExceptWith(rule.Characters);
                            continue;
                        case CharacterSetModificationKind.Replace:
                            commitCharacters.Clear();
                            commitCharacters.AddRange(rule.Characters);
                            break;
                    }
                }

                var commitCharacterSet = commitCharacters.Select(c => c.ToString()).ToImmutableArray();
                currentRuleCache.Add(item.Rules.CommitCharacterRules, commitCharacterSet);
                return commitCharacterSet.ToArray();
            }
        }

        internal static ImmutableHashSet<char> GetTriggerCharacters(CompletionProvider provider)
        {
            if (provider is LSPCompletionProvider lspProvider)
            {
                return lspProvider.TriggerCharacters;
            }

            return ImmutableHashSet<char>.Empty;
        }

        internal static async Task<OptionSet> GetCompletionOptionsAsync(Document document, CancellationToken cancellationToken)
        {
            // Filter out snippets as they are not supported in the LSP client
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1139740
            // Filter out unimported types for now as there are two issues with providing them:
            // 1.  LSP client does not currently provide a way to provide detail text on the completion item to show the namespace.
            //     https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1076759
            // 2.  We need to figure out how to provide the text edits along with the completion item or provide them in the resolve request.
            //     https://devdiv.visualstudio.com/DevDiv/_workitems/edit/985860/
            // 3.  LSP client should support completion filters / expanders
            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var completionOptions = documentOptions
                .WithChangedOption(CompletionOptions.SnippetsBehavior, SnippetsRule.NeverInclude)
                .WithChangedOption(CompletionOptions.ShowItemsFromUnimportedNamespaces, false)
                .WithChangedOption(CompletionServiceOptions.IsExpandedCompletion, false);
            return completionOptions;
        }

        private static LSP.CompletionItemKind GetCompletionKind(ImmutableArray<string> tags)
        {
            foreach (var tag in tags)
            {
                if (ProtocolConversions.RoslynTagToCompletionItemKind.TryGetValue(tag, out var completionItemKind))
                {
                    return completionItemKind;
                }
            }

            return LSP.CompletionItemKind.Text;
        }

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly CompletionHandler _completionHandler;

            public TestAccessor(CompletionHandler completionHandler)
                => _completionHandler = completionHandler;

            public CompletionListCache GetCache()
                => _completionHandler._completionListCache;
        }
    }
}
