﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.DocumentationComments;
using Microsoft.CodeAnalysis.Host.Mef;
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
    internal class DocumentationCommentCommandHandler
        : AbstractDocumentationCommentCommandHandler<DocumentationCommentTriviaSyntax, MemberDeclarationSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DocumentationCommentCommandHandler(
            IWaitIndicator waitIndicator,
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
            : base(waitIndicator, undoHistoryRegistry, editorOperationsFactoryService)
        {
        }

        protected override string ExteriorTriviaText => "///";
    }
}
