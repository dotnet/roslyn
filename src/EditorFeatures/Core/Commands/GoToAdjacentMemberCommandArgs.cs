﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Commands
{
    [ExcludeFromCodeCoverage]
    internal class GoToAdjacentMemberCommandArgs : CommandArgs
    {
        public GoToAdjacentMemberCommandArgs(ITextView textView, ITextBuffer subjectBuffer, NavigateDirection direction)
            : base(textView, subjectBuffer)
        {
            Direction = direction;
        }

        public NavigateDirection Direction { get; }
    }
}
