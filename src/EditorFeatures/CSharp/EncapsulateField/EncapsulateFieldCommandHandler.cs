// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.EncapsulateField;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EncapsulateField
{
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(PredefinedCommandHandlerNames.EncapsulateField)]
    [Order(After = PredefinedCommandHandlerNames.DocumentationComments)]
    internal class EncapsulateFieldCommandHandler : AbstractEncapsulateFieldCommandHandler
    {
#pragma warning disable RS0033 // Importing constructor should be [Obsolete]
        [ImportingConstructor]
#pragma warning restore RS0033 // Importing constructor should be [Obsolete]
        public EncapsulateFieldCommandHandler(
            ITextBufferUndoManagerProvider undoManager,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(undoManager, listenerProvider)
        {
        }
    }
}
