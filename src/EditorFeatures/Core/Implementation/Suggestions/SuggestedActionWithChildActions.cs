// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal class SuggestedActionWithChildActions : SuggestedAction
    {
        public SuggestedActionWithChildActions(
            Workspace workspace,
            ITextBuffer subjectBuffer,
            ICodeActionEditHandlerService editHandler,
            IWaitIndicator waitIndicator,
            CodeAction codeAction,
            object provider,
            IAsynchronousOperationListener operationListener,
            SuggestedActionSet childActions)
            : base(workspace, subjectBuffer, editHandler, waitIndicator,
                  codeAction, provider, operationListener,
                  SpecializedCollections.SingletonEnumerable(childActions))
        {
        }
    }
}