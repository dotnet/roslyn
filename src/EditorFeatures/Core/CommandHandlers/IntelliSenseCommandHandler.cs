// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.IntelliSense)]
    internal sealed class IntelliSenseCommandHandler : AbstractIntelliSenseCommandHandler
    {
        [ImportingConstructor]
        public IntelliSenseCommandHandler(
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
