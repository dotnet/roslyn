// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Editor.InteractiveWindow;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    [Export]
    [ExportCommandHandler(PredefinedCommandHandlerNames.Completion, InteractiveContentTypeNames.InteractiveCommandContentType)]
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
