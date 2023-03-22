// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal abstract partial class AbstractRenameCommandHandler :
        IChainedCommandHandler<TypeCharCommandArgs>
    {
        public CommandState GetCommandState(TypeCharCommandArgs args, Func<CommandState> nextHandler)
            => GetCommandState(nextHandler);

        public void ExecuteCommand(TypeCharCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            HandlePossibleTypingCommand(args, nextHandler, (activeSession, span) =>
            {
                var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document == null)
                {
                    nextHandler();
                    return;
                }

                var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();

                // We are inside the region we can edit, so let's forward only if it's a valid
                // character
                if (syntaxFactsService == null ||
                    syntaxFactsService.IsIdentifierStartCharacter(args.TypedChar) ||
                    syntaxFactsService.IsIdentifierPartCharacter(args.TypedChar) ||
                    syntaxFactsService.IsStartOfUnicodeEscapeSequence(args.TypedChar))
                {
                    nextHandler();
                }
            });
        }
    }
}
