// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.InteractiveWindow.Commands;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    [ExportCommandHandler(PredefinedCommandHandlerNames.Completion, PredefinedInteractiveCommandsContentTypes.InteractiveCommandContentTypeName)]
    [Order(After = PredefinedCommandHandlerNames.SignatureHelp)]
    internal sealed class InteractiveCompletionCommandHandler : AbstractCompletionCommandHandler
    {
        [ImportingConstructor]
        public InteractiveCompletionCommandHandler(IAsyncCompletionService completionService)
            : base(completionService)
        {
        }
    }
}
