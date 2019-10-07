// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.EncapsulateField;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EncapsulateField
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(PredefinedCommandHandlerNames.EncapsulateField)]
    [Order(After = PredefinedCommandHandlerNames.DocumentationComments)]
    internal class EncapsulateFieldCommandHandler : AbstractEncapsulateFieldCommandHandler
    {
        [ImportingConstructor]
        public EncapsulateFieldCommandHandler(
            IThreadingContext threadingContext,
            ITextBufferUndoManagerProvider undoManager,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext, undoManager, listenerProvider)
        {
        }
    }
}
