// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Commanding.Commands
{
    /// <summary>
    /// Arguments for Go To Implementation.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal sealed class GoToBaseCommandArgs : EditorCommandArgs
    {
        public GoToBaseCommandArgs(ITextView textView, ITextBuffer subjectBuffer)
            : base(textView, subjectBuffer)
        {
        }
    }
}
