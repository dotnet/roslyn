// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Commands
{
    internal enum NavigateDirection
    {
        Up = -1,
        Down = 1,
    }

    [ExcludeFromCodeCoverage]
    internal class NavigateToHighlightedReferenceCommandArgs : CommandArgs
    {
        private readonly NavigateDirection _direction;

        public NavigateToHighlightedReferenceCommandArgs(ITextView textView, ITextBuffer subjectBuffer, NavigateDirection direction)
            : base(textView, subjectBuffer)
        {
            if (!Enum.IsDefined(typeof(NavigateDirection), direction))
            {
                throw new ArgumentException("direction");
            }

            _direction = direction;
        }

        public NavigateDirection Direction => _direction;
    }
}
