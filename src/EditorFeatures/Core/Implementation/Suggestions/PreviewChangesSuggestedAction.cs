// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal class PreviewChangesSuggestedAction : SuggestedAction
    {
        internal PreviewChangesSuggestedAction(
            Workspace workspace,
            ITextBuffer subjectBuffer,
            ICodeActionEditHandlerService editHandler,
            IWaitIndicator waitIndicator,
            PreviewChangesCodeAction codeAction,
            object provider,
            IAsynchronousOperationListener operationListener)
            : base(workspace, subjectBuffer, editHandler, waitIndicator, codeAction, provider, operationListener)
        {
        }

        public override bool HasPreview
        {
            get
            {
                // Since PreviewChangesSuggestedAction will always be presented as a
                // 'flavored' action, it will never have a preview.
                return false;
            }
        }

        public override Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            // Since PreviewChangesSuggestedAction will always be presented as a
            // 'flavored' action, code in the VS editor / lightbulb layer should
            // never call GetPreview() on it. We override and return null here
            // regardless so that nothing blows up if this ends up getting called.
            return SpecializedTasks.Default<object>();
        }
    }
}
