// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Implementation.Interactive;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Completion.CompletionProviders
{
    [ExportCompletionProvider("ReplCommandCompletionProvider", LanguageNames.CSharp)]
    [TextViewRole(PredefinedInteractiveTextViewRoles.InteractiveTextViewRole)]
    [Order(Before = PredefinedCompletionProviderNames.Keyword)]
    internal abstract class ReplCompletionProvider : CompletionListProvider
    {
        protected abstract Task<TextSpan> GetTextChangeSpanAsync(Document document, int position, CancellationToken cancellationToken);
        protected abstract bool ShouldDisplayCommandCompletions(SyntaxTree tree, int position, CancellationToken cancellationToken);

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

                    if (ShouldDisplayCommandCompletions(tree, position, cancellationToken))
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
