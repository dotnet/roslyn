// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal class PreviewChangesSuggestedAction : SuggestedAction
    {
        internal PreviewChangesSuggestedAction(
            Workspace workspace,
            ITextBuffer subjectBuffer,
            ICodeActionEditHandlerService editHandler,
            PreviewChangesCodeAction codeAction,
            object provider)
            : base(workspace, subjectBuffer, editHandler, codeAction, provider)
        {
        }

        public override object GetPreview(CancellationToken cancellationToken)
        {
            // Since PreviewChangesSuggestedAction will always be presented as a
            // 'flavored' action, code in the VS editor / lightbulb layer should
            // never call GetPreview() on it. We override and return null here
            // regardless so that nothing blows up if this ends up getting called.
            return null;
        }
    }
}
