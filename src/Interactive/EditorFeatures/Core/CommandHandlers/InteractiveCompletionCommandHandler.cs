// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(PredefinedInteractiveCommandsContentTypes.InteractiveCommandContentTypeName)]
    [Name(PredefinedCommandHandlerNames.Completion)]
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
