// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp
{
    internal partial class Controller
    {
        internal override void OnCaretPositionChanged(object sender, EventArgs args)
        {
            AssertIsForeground();
            OnCaretPositionChanged();
        }

        private void OnCaretPositionChanged()
        {
            Retrigger(fromArrowKeys: _caretMoveFromArrowKeys);

            // Always clear this flag because when argument snippet completion is active Tab characters
            // will cause a caret move, but we don't want our special arrow key logic running.
            _caretMoveFromArrowKeys = false;
        }
    }
}
