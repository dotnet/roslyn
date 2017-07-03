// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.InteractiveWindow.Commands;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    [ExportCommandHandler(PredefinedCommandHandlerNames.IntelliSense, PredefinedInteractiveCommandsContentTypes.InteractiveCommandContentTypeName)]
    internal sealed class InteractiveIntelliSenseCommandHandler : AbstractIntelliSenseCommandHandler
    {
        [ImportingConstructor]
        public InteractiveIntelliSenseCommandHandler(
               CompletionCommandHandler completionCommandHandler,
               SignatureHelpCommandHandler signatureHelpCommandHandler,
               QuickInfoCommandHandlerAndSourceProvider quickInfoCommandHandler)
            : base(completionCommandHandler,
                   signatureHelpCommandHandler,
                   quickInfoCommandHandler)
        {
        }
    }
}
