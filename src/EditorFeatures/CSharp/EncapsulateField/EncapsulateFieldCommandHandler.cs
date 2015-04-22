// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.EncapsulateField;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EncapsulateField
{
    [ExportCommandHandler(PredefinedCommandHandlerNames.EncapsulateField, ContentTypeNames.CSharpContentType)]
    [Order(After = PredefinedCommandHandlerNames.DocumentationComments)]
    internal class EncapsulateFieldCommandHandler : AbstractEncapsulateFieldCommandHandler
    {
        [ImportingConstructor]
        public EncapsulateFieldCommandHandler(
            IWaitIndicator waitIndicator,
            ITextBufferUndoManagerProvider undoManager)
            : base(waitIndicator, undoManager)
        {
        }
    }
}
