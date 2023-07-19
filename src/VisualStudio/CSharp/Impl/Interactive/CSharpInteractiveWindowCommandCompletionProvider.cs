// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Interactive;
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
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpInteractiveWindowCommandCompletionProvider()
        {
        }

        internal override string Language
            => LanguageNames.CSharp;

        protected override string GetCompletionString(string commandName)
            => commandName;

        public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
            => CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, characterPosition, options);

        public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.SpaceTriggerCharacter;

        protected override bool ShouldDisplayCommandCompletions(SyntaxTree tree, int position, CancellationToken cancellationToken)
            => tree.IsBeforeFirstToken(position, cancellationToken) &&
               tree.IsPreProcessorKeywordContext(position, cancellationToken);
    }
}
