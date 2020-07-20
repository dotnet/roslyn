// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Editor.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Editor.CSharp.Completion.FileSystem;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Completion.CompletionProviders
{
    [ExportCompletionProvider(
        nameof(CSharpReplCommandCompletionProvider),
        LanguageNames.CSharp,
        Roles = new[] { PredefinedInteractiveTextViewRoles.InteractiveTextViewRole })]
    [ExtensionOrder(After = nameof(ReferenceDirectiveCompletionProvider))]
    [ExtensionOrder(Before = nameof(LastBuiltInCompletionProvider))]
    [Shared]
    internal class CSharpReplCommandCompletionProvider : ReplCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpReplCommandCompletionProvider()
        {
        }

        protected override string GetCompletionString(string commandName)
        {
            return commandName;
        }

        // TODO (tomat): REPL commands should have their own providers:
        private static readonly Regex s_referenceRegex = new Regex(@"\s*#\s*r\s+", RegexOptions.Compiled);
        private static readonly Regex s_loadCommandRegex = new Regex(@"#load\s+", RegexOptions.Compiled);

        private static bool IsReplCommandLocation(SnapshotPoint characterPoint)
        {
            // TODO(cyrusn): We don't need to do this textually.  We could just defer this to
            // IsTriggerCharacter and just check the syntax tree.
            var line = characterPoint.GetContainingLine();
            var text = characterPoint.Snapshot.GetText(line.Start.Position, characterPoint.Position - line.Start.Position);

            // TODO (tomat): REPL commands should have their own handlers:
            if (characterPoint.Snapshot.ContentType.IsOfType(ContentTypeNames.CSharpContentType))
            {
                if (s_referenceRegex.IsMatch(text))
                {
                    return true;
                }
            }

            return false;
        }

        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, characterPosition, options);
        }

        internal override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.SpaceTriggerCharacter;

        protected override async Task<bool> ShouldDisplayCommandCompletionsAsync(SyntaxTree tree, int position, CancellationToken cancellationToken)
        {
            return await tree.IsBeforeFirstTokenAsync(position, cancellationToken).ConfigureAwait(false) &&
                   tree.IsPreProcessorKeywordContext(position, cancellationToken);
        }
    }
}
