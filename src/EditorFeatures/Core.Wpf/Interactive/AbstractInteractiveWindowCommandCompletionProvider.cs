// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.InteractiveWindow.Commands;

namespace Microsoft.CodeAnalysis.Interactive
{
    /// <summary>
    /// Provides completion items for Interactive Window commands (such as #help, #cls, etc.) at the start of a language buffer.
    /// </summary>
    internal abstract class AbstractInteractiveWindowCommandCompletionProvider : LSPCompletionProvider
    {
        protected abstract bool ShouldDisplayCommandCompletions(SyntaxTree tree, int position, CancellationToken cancellationToken);
        protected abstract string GetCompletionString(string commandName);

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            // the provider might be invoked in non-interactive context:
            var sourceText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            if (Workspace.TryGetWorkspace(sourceText.Container, out var workspace))
            {
                if (workspace is InteractiveWindowWorkspace interactiveWorkspace)
                {
                    var window = interactiveWorkspace.Window;
                    if (window != null)
                    {
                        var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

                        if (ShouldDisplayCommandCompletions(tree, position, cancellationToken))
                        {
                            var commands = window.GetInteractiveCommands();
                            if (commands != null)
                            {
                                foreach (var command in commands.GetCommands())
                                {
                                    foreach (var commandName in command.Names)
                                    {
                                        var completion = GetCompletionString(commandName);
                                        context.AddItem(CommonCompletionItem.Create(
                                            completion, displayTextSuffix: "", CompletionItemRules.Default, description: command.Description.ToSymbolDisplayParts(), glyph: Glyph.Intrinsic));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
