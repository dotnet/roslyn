// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Implementation.Interactive;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.InteractiveWindow.Commands;

namespace Microsoft.CodeAnalysis.Editor.Completion.CompletionProviders
{
    internal abstract class ReplCompletionProvider : CommonCompletionProvider
    {
        protected abstract Task<bool> ShouldDisplayCommandCompletionsAsync(SyntaxTree tree, int position, CancellationToken cancellationToken);
        protected abstract string GetCompletionString(string commandName);

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            // the provider might be invoked in non-interactive context:
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (Workspace.TryGetWorkspace(sourceText.Container, out var ws))
            {
                if (ws is InteractiveWorkspace workspace)
                {
                    var window = workspace.Window;
                    var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

                    if (await ShouldDisplayCommandCompletionsAsync(tree, position, cancellationToken).ConfigureAwait(false))
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
