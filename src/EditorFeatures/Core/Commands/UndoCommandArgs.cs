// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Commands
{
    [ExcludeFromCodeCoverage]
    internal class UndoCommandArgs : CommandArgs
    {
        public readonly int Count;

        public UndoCommandArgs(ITextView textView, ITextBuffer subjectBuffer, int count = 1)
            : base(textView, subjectBuffer)
        {
            this.Count = count;
        }
    }
}
