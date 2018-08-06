// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using VSCommanding = Microsoft.VisualStudio.Commanding;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler :
        IChainedCommandHandler<TypeCharCommandArgs>
    {
        public VSCommanding.CommandState GetCommandState(TypeCharCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            return GetCommandState(nextHandler);
        }

        public void ExecuteCommand(TypeCharCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            HandlePossibleTypingCommand(args, nextHandler, span =>
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
