// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text.Editor;
using System.Threading;

namespace Microsoft.CodeAnalysis.Editor.Interactive
{
    internal interface ISendToInteractiveSubmissionProvider
    {
        string GetSelectedText(IEditorOptions editorOptions, CommandArgs args, CancellationToken cancellationToken);
    }
}
