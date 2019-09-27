// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.ExtractMethod;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.CSharp.ExtractMethod
{
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(PredefinedCommandHandlerNames.ExtractMethod)]
    [Order(After = PredefinedCommandHandlerNames.DocumentationComments)]
    internal class ExtractMethodCommandHandler :
        AbstractExtractMethodCommandHandler
    {
        [ImportingConstructor]
        public ExtractMethodCommandHandler(
            IThreadingContext threadingContext,
            ITextBufferUndoManagerProvider undoManager,
            IInlineRenameService renameService)
            : base(threadingContext, undoManager, renameService)
        {
        }
    }
}
