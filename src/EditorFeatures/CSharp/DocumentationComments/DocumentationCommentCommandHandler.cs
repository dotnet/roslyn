// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.DocumentationComments
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(PredefinedCommandHandlerNames.DocumentationComments)]
    [Order(After = PredefinedCommandHandlerNames.Rename)]
    [Order(After = PredefinedCompletionNames.CompletionCommandHandler)]
    internal sealed class DocumentationCommentCommandHandler
        : AbstractDocumentationCommentCommandHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DocumentationCommentCommandHandler(
            IUIThreadOperationExecutor uiThreadOperationExecutor,
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService,
            IGlobalOptionService globalOptions)
            : base(uiThreadOperationExecutor, undoHistoryRegistry, editorOperationsFactoryService, globalOptions)
        {
        }

        protected override string ExteriorTriviaText => "///";
    }
}
