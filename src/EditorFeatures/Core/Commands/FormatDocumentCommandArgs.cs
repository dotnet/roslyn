// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Commands
{
    /// <summary>
    /// Arguments for the Format Document command being invoked.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class FormatDocumentCommandArgs : CommandArgs
    {
        public FormatDocumentCommandArgs(ITextView textView, ITextBuffer subjectBuffer)
            : base(textView, subjectBuffer)
        {
        }
    }
}
