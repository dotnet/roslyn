// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Completion
{
    internal sealed class CompletionCapabilityHelper
    {
        public const string CommitCharactersPropertyName = "commitCharacters";
        public const string DataPropertyName = "data";
        public const string EditRangePropertyName = "editRange";

        private readonly CompletionSetting? _completionSetting;
        private readonly VSInternalCompletionSetting? _vsCompletionSetting;

        public bool SupportVSInternalClientCapabilities { get; }
        public bool SupportDefaultEditRange { get; }
        public bool SupportCompletionListData { get; }
        public bool SupportVSInternalCompletionListData { get; }
        public bool SupportDefaultCommitCharacters { get; }
        public bool SupportVSInternalDefaultCommitCharacters { get; }
        public bool SupportSnippets { get; }
        public bool SupportsMarkdownDocumentation { get; }
        public ISet<CompletionItemKind> SupportedItemKinds { get; }
        public ISet<CompletionItemTag> SupportedItemTags { get; }

        public CompletionCapabilityHelper(ClientCapabilities clientCapabilities)
        {
            // public LSP
            _completionSetting = clientCapabilities.TextDocument?.Completion;

            SupportSnippets = _completionSetting?.CompletionItem?.SnippetSupport ?? false;
            SupportDefaultEditRange = _completionSetting?.CompletionListSetting?.ItemDefaults?.Contains(EditRangePropertyName) == true;
            SupportsMarkdownDocumentation = _completionSetting?.CompletionItem?.DocumentationFormat?.Contains(MarkupKind.Markdown) == true;
            SupportCompletionListData = _completionSetting?.CompletionListSetting?.ItemDefaults?.Contains(DataPropertyName) == true;
            SupportDefaultCommitCharacters = _completionSetting?.CompletionListSetting?.ItemDefaults?.Contains(CommitCharactersPropertyName) == true;
            SupportedItemKinds = _completionSetting?.CompletionItemKind?.ValueSet?.ToSet() ?? SpecializedCollections.EmptySet<CompletionItemKind>();
            SupportedItemTags = _completionSetting?.CompletionItem?.TagSupport?.ValueSet?.ToSet() ?? SpecializedCollections.EmptySet<CompletionItemTag>();

            // internal VS LSP
            if (clientCapabilities.HasVisualStudioLspCapability())
            {
                SupportVSInternalClientCapabilities = true;
                _vsCompletionSetting = ((VSInternalClientCapabilities)clientCapabilities).TextDocument?.Completion as VSInternalCompletionSetting;
            }
            else
            {
                SupportVSInternalClientCapabilities = false;
                _vsCompletionSetting = null;
            }

            SupportVSInternalCompletionListData = SupportVSInternalClientCapabilities && _vsCompletionSetting?.CompletionList?.Data == true;
            SupportVSInternalDefaultCommitCharacters = SupportVSInternalClientCapabilities && _vsCompletionSetting?.CompletionList?.CommitCharacters == true;
        }
    }
}
