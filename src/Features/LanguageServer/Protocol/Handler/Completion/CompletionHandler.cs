﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Handle a completion request.
    /// </summary>
    [Shared]
    [ExportLspMethod(LSP.Methods.TextDocumentCompletionName)]
    internal class CompletionHandler : AbstractRequestHandler<LSP.CompletionParams, LSP.CompletionItem[]>
    {
        private readonly ImmutableHashSet<string> _csTriggerCharacters;
        private readonly ImmutableHashSet<string> _vbTriggerCharacters;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CompletionHandler(
            ILspSolutionProvider solutionProvider,
            [ImportMany] IEnumerable<Lazy<CompletionProvider, CompletionProviderMetadata>> completionProviders)
            : base(solutionProvider)
        {
            _csTriggerCharacters = completionProviders.Where(lz => lz.Metadata.Language == LanguageNames.CSharp).SelectMany(
                lz => GetTriggerCharacters(lz.Value)).Select(c => c.ToString()).ToImmutableHashSet();
            _vbTriggerCharacters = completionProviders.Where(lz => lz.Metadata.Language == LanguageNames.VisualBasic).SelectMany(
                lz => GetTriggerCharacters(lz.Value)).Select(c => c.ToString()).ToImmutableHashSet();
        }

        public override async Task<LSP.CompletionItem[]> HandleRequestAsync(LSP.CompletionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = SolutionProvider.GetDocument(request.TextDocument, context.ClientName);
            if (document == null)
            {
                return Array.Empty<LSP.CompletionItem>();
            }

            // C# and VB share the same LSP language server, and thus share the same default trigger characters.
            // We need to ensure the trigger character is valid in the document's language. For example, the '{'
            // character, while a trigger character in VB, is not a trigger character in C#.
            var triggerCharacter = char.Parse(request.Context.TriggerCharacter);
            if (request.Context.TriggerKind == LSP.CompletionTriggerKind.TriggerCharacter && !char.IsLetterOrDigit(triggerCharacter) &&
                !IsValidTriggerCharacterForDocument(document, request.Context.TriggerCharacter))
            {
                return Array.Empty<LSP.CompletionItem>();
            }

            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

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
                .WithChangedOption(CompletionServiceOptions.IsExpandedCompletion, false)
                .WithChangedOption(CompletionServiceOptions.DisallowAddingImports, true);

            var completionService = document.Project.LanguageServices.GetRequiredService<CompletionService>();

            // TO-DO: More LSP.CompletionTriggerKind mappings are required to properly map to Roslyn CompletionTriggerKinds.
            // https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1178726
            var triggerKind = ProtocolConversions.LSPToRoslynCompletionTriggerKind(request.Context.TriggerKind);
            var completionTrigger = new CompletionTrigger(triggerKind, triggerCharacter);

            var list = await completionService.GetCompletionsAsync(document, position, completionTrigger, options: completionOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (list == null)
            {
                return Array.Empty<LSP.CompletionItem>();
            }

            var lspVSClientCapability = context.ClientCapabilities?.HasVisualStudioLspCapability() == true;

            return list.Items.Select(item => CreateLSPCompletionItem(request, item, lspVSClientCapability, completionTrigger)).ToArray();

            // Local functions
            bool IsValidTriggerCharacterForDocument(Document document, string triggerCharacter)
            {
                if (document.Project.Language == LanguageNames.CSharp)
                {
                    return _csTriggerCharacters.Contains(triggerCharacter);
                }
                else if (document.Project.Language == LanguageNames.VisualBasic)
                {
                    return _vbTriggerCharacters.Contains(triggerCharacter);
                }

                return true;
            }

            static LSP.CompletionItem CreateLSPCompletionItem(
                LSP.CompletionParams request,
                CompletionItem item,
                bool useVSCompletionItem,
                CompletionTrigger completionTrigger)
            {
                if (useVSCompletionItem)
                {
                    var vsCompletionItem = CreateCompletionItem<LSP.VSCompletionItem>(request, item, completionTrigger);
                    vsCompletionItem.Icon = new ImageElement(item.Tags.GetFirstGlyph().GetImageId());
                    return vsCompletionItem;
                }
                else
                {
                    var roslynCompletionItem = CreateCompletionItem<LSP.CompletionItem>(request, item, completionTrigger);
                    return roslynCompletionItem;
                }
            }

            static TCompletionItem CreateCompletionItem<TCompletionItem>(
                LSP.CompletionParams request,
                CompletionItem item,
                CompletionTrigger completionTrigger) where TCompletionItem : LSP.CompletionItem, new()
            {
                var completeDisplayText = item.DisplayTextPrefix + item.DisplayText + item.DisplayTextSuffix;
                var completionItem = new TCompletionItem
                {
                    Label = completeDisplayText,
                    InsertText = item.Properties.ContainsKey("InsertionText") ? item.Properties["InsertionText"] : completeDisplayText,
                    SortText = item.SortText,
                    FilterText = item.FilterText,
                    Kind = GetCompletionKind(item.Tags),
                    Data = new CompletionResolveData
                    {
                        TextDocument = request.TextDocument,
                        Position = request.Position,
                        DisplayText = item.DisplayText,
                        CompletionTrigger = completionTrigger,
                    },
                    Preselect = item.Rules.SelectionBehavior == CompletionItemSelectionBehavior.HardSelection,
                    CommitCharacters = GetCommitCharacters(item)
                };

                return completionItem;
            }

            static string[]? GetCommitCharacters(CompletionItem item)
            {
                var commitCharacterRules = item.Rules.CommitCharacterRules;

                // If the item doesn't have any special rules, just use the default commit characters.
                if (commitCharacterRules.IsEmpty)
                {
                    return null;
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

                return commitCharacters.Select(c => c.ToString()).ToArray();
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
    }
}
