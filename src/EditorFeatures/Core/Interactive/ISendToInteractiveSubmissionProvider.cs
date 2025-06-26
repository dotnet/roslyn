// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;

namespace Microsoft.CodeAnalysis.Interactive;

internal interface ISendToInteractiveSubmissionProvider
{
    string GetSelectedText(IEditorOptions editorOptions, EditorCommandArgs args, CancellationToken cancellationToken);
}
