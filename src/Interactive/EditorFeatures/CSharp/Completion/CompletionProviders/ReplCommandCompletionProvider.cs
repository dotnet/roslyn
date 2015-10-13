// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Editor.Implementation.Interactive;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Completion.CompletionProviders
{
    [ExportCompletionProvider("ReplCommandCompletionProvider", LanguageNames.CSharp)]
    [TextViewRole(PredefinedInteractiveTextViewRoles.InteractiveTextViewRole)]
    [Order(Before = PredefinedCompletionProviderNames.Keyword)]
    internal class ReplCommandCompletionProvider : CompletionListProvider
    {
        private async Task<TextSpan> GetTextChangeSpanAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return CompletionUtilities.GetTextChangeSpan(text, position);
        }

        // TODO (tomat): REPL commands should have their own providers:
        private static readonly Regex s_referenceRegex = new Regex(@"\s*#\s*r\s+", RegexOptions.Compiled);
        private static readonly Regex s_loadCommandRegex = new Regex(@"#load\s+", RegexOptions.Compiled);

        private bool IsReplCommandLocation(SnapshotPoint characterPoint)
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

        public override bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, characterPosition, options);
        }

        public override async Task ProduceCompletionListAsync(CompletionListContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            // the provider might be invoked in non-interactive context:
            Workspace ws;
            if (Workspace.TryGetWorkspace(document.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken).Container, out ws))
            {
                var workspace = ws as InteractiveWorkspace;
                if (workspace != null)
                {
                    var window = workspace.Engine.CurrentWindow;
                    var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

                    if (tree.IsBeforeFirstToken(position, cancellationToken) &&
                        tree.IsPreProcessorKeywordContext(position, cancellationToken))
                    {
                        var filterSpan = await this.GetTextChangeSpanAsync(document, position, cancellationToken).ConfigureAwait(false);

                        IInteractiveWindowCommands commands = window.GetInteractiveCommands();
                        if (commands != null)
                        {
                            foreach (var command in commands.GetCommands())
                            {
                                foreach (var commandName in command.Names)
                                {
                                    context.AddItem(new CompletionItem(
                                        this, commandName, filterSpan, c => Task.FromResult(command.Description.ToSymbolDisplayParts()), glyph: Glyph.Intrinsic));
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
