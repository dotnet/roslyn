// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Microsoft.VisualStudio.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(PredefinedInteractiveCommandsContentTypes.InteractiveCommandContentTypeName)]
    [Name(PredefinedCommandHandlerNames.Completion)]
    [Order(After = PredefinedCommandHandlerNames.SignatureHelpBeforeCompletion)]
    internal sealed class InteractiveCompletionCommandHandler : AbstractCompletionCommandHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InteractiveCompletionCommandHandler(IThreadingContext threadingContext, IAsyncCompletionService completionService)
            : base(threadingContext, completionService)
        {
        }
    }
}
