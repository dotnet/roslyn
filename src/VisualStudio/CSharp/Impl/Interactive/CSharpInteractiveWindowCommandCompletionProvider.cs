// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    /// <summary>
    /// Provides completion items for Interactive Window commands (such as #help, #cls, etc.) at the start of a C# language buffer.
    /// </summary>
    [ExportCompletionProviderMef1("ReplCommandCompletionProvider", LanguageNames.CSharp)]
    [TextViewRole(PredefinedInteractiveTextViewRoles.InteractiveTextViewRole)]
    [Order(Before = PredefinedCompletionProviderNames.Keyword)]
    internal sealed class CSharpInteractiveWindowCommandCompletionProvider : AbstractInteractiveWindowCommandCompletionProvider
    {
        internal override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.SpaceTriggerCharacter;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpInteractiveWindowCommandCompletionProvider()
        {
        }

        protected override string GetCompletionString(string commandName)
        {
            return commandName;
        }

        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, characterPosition, options);
        }


        protected override async Task<bool> ShouldDisplayCommandCompletionsAsync(SyntaxTree tree, int position, CancellationToken cancellationToken)
        {
            return await tree.IsBeforeFirstTokenAsync(position, cancellationToken).ConfigureAwait(false) &&
                   tree.IsPreProcessorKeywordContext(position, cancellationToken);
        }
    }
}
