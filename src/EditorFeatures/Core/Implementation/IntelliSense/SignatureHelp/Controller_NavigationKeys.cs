// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp
{
    internal partial class Controller
    {
        internal bool TryHandleUpKey()
        {
            AssertIsForeground();
            return ChangeSelection(() => sessionOpt.PresenterSession.SelectPreviousItem());
        }

        internal bool TryHandleDownKey()
        {
            AssertIsForeground();
            return ChangeSelection(() => sessionOpt.PresenterSession.SelectNextItem());
        }

        private bool ChangeSelection(Action computationAction)
        {
            AssertIsForeground();

            if (!IsSessionActive)
            {
                // No computation running, so just let the editor handle this.
                return false;
            }

            // If we haven't started our editor session yet, just abort.
            // The user hasn't seen a SigHelp presentation yet, so they're
            // probably not trying to change the currently visible overload.
            if (!sessionOpt.PresenterSession.EditorSessionIsActive)
            {
                DismissSessionIfActive();
                return false;
            }

            // If we've finished computing the items then use the navigation commands to change the
            // selected item.  Otherwise, the user was just typing and is now moving through the
            // file.  In this case stop everything we're doing.
            var model = sessionOpt.InitialUnfilteredModel != null ? WaitForController() : null;

            // Check if completion is still active.  Then update the computation appropriately.
            //
            // Also, if we only computed one item, then the user doesn't want to select anything
            // else.  Just stop and let the editor handle the nav character.
            if (model != null && model.Items.Count > 1)
            {
                computationAction();
                return true;
            }
            else
            {
                // Dismiss ourselves and actually allow the editor to navigate.
                DismissSessionIfActive();
                return false;
            }
        }
    }
}
