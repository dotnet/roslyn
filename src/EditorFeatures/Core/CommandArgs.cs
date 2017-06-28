// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor
{
    /// <summary>
    /// The base class of all command argument types used by ICommandHandler.
    /// </summary>
    internal abstract class CommandArgs
    {
        /// <summary>
        /// The text buffer of where the caret is when the command happens.
        /// </summary>
        public ITextBuffer SubjectBuffer { get; }

        /// <summary>
        /// The text view that originated this command.
        /// </summary>
        public ITextView TextView { get; }

        public CommandArgs(ITextView textView, ITextBuffer subjectBuffer)
        {
            this.TextView = textView ?? throw new ArgumentNullException(nameof(textView));
            this.SubjectBuffer = subjectBuffer ?? throw new ArgumentNullException(nameof(subjectBuffer));
        }
    }
}
