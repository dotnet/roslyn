// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;
using VSC = Microsoft.VisualStudio.Text.UI.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler :
        VSC.ICommandHandler<TypeCharCommandArgs>
    {
        public VSC.CommandState GetCommandState(TypeCharCommandArgs args)
        {
            return GetCommandState();
        }

        public bool ExecuteCommand(TypeCharCommandArgs args)
        {
            return HandlePossibleTypingCommand(args, span =>
            {
                var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document == null)
                {
                    return false;
                }

                var syntaxFactsService = document.Project.LanguageServices.GetService<ISyntaxFactsService>();

                // We are inside the region we can edit, so let's forward only if it's a valid
                // character
                if (syntaxFactsService == null ||
                    syntaxFactsService.IsIdentifierStartCharacter(args.TypedChar) ||
                    syntaxFactsService.IsIdentifierPartCharacter(args.TypedChar) ||
                    syntaxFactsService.IsStartOfUnicodeEscapeSequence(args.TypedChar))
                {
                    return false;
                }

                return true;
            });
        }
    }
}
