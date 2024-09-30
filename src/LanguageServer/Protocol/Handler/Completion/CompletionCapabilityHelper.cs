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
            : this(supportsVSExtensions: clientCapabilities.HasVisualStudioLspCapability(),
                   completionSetting: clientCapabilities.TextDocument?.Completion)
        {
        }

        public CompletionCapabilityHelper(bool supportsVSExtensions, CompletionSetting? completionSetting)
        {
            // public LSP
            SupportSnippets = completionSetting?.CompletionItem?.SnippetSupport ?? false;
            SupportDefaultEditRange = completionSetting?.CompletionListSetting?.ItemDefaults?.Contains(EditRangePropertyName) == true;
            SupportsMarkdownDocumentation = completionSetting?.CompletionItem?.DocumentationFormat?.Contains(MarkupKind.Markdown) == true;
            SupportCompletionListData = completionSetting?.CompletionListSetting?.ItemDefaults?.Contains(DataPropertyName) == true;
            SupportDefaultCommitCharacters = completionSetting?.CompletionListSetting?.ItemDefaults?.Contains(CommitCharactersPropertyName) == true;
            SupportedItemKinds = completionSetting?.CompletionItemKind?.ValueSet?.ToSet() ?? SpecializedCollections.EmptySet<CompletionItemKind>();
            SupportedItemTags = completionSetting?.CompletionItem?.TagSupport?.ValueSet?.ToSet() ?? SpecializedCollections.EmptySet<CompletionItemTag>();

            // internal VS LSP
            if (supportsVSExtensions)
            {
                SupportVSInternalClientCapabilities = true;

                var vsCompletionSetting = completionSetting as VSInternalCompletionSetting;
                SupportVSInternalCompletionListData = vsCompletionSetting?.CompletionList?.Data == true;
                SupportVSInternalDefaultCommitCharacters = vsCompletionSetting?.CompletionList?.CommitCharacters == true;
            }
        }
    }
}
