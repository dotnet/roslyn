// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(PredefinedCommandHandlerNames.ExtractMethod)]
    [Order(After = PredefinedCommandHandlerNames.DocumentationComments)]
    internal class ExtractMethodCommandHandler :
        AbstractExtractMethodCommandHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ExtractMethodCommandHandler(
            IThreadingContext threadingContext,
            ITextBufferUndoManagerProvider undoManager,
            IInlineRenameService renameService,
            IGlobalOptionService globalOptions)
            : base(threadingContext, undoManager, renameService, globalOptions)
        {
        }
    }
}
