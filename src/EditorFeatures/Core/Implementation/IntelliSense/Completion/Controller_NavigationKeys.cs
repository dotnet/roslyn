// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        CommandState ICommandHandler<PageUpKeyCommandArgs>.GetCommandState(PageUpKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        CommandState ICommandHandler<PageDownKeyCommandArgs>.GetCommandState(PageDownKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

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

        void ICommandHandler<PageUpKeyCommandArgs>.ExecuteCommand(PageUpKeyCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            if (!ChangeSelection(() => sessionOpt.PresenterSession.SelectPreviousPageItem()))
            {
                nextHandler();
            }
        }

        void ICommandHandler<PageDownKeyCommandArgs>.ExecuteCommand(PageDownKeyCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            if (!ChangeSelection(() => sessionOpt.PresenterSession.SelectNextPageItem()))
            {
                nextHandler();
            }
        }

        private bool ChangeSelection(Action computationAction)
        {
            AssertIsForeground();

            if (!IsSessionActive)
            {
                // No computation running, so just let the editor handle this.
                return false;
            }

            // If we've finished computing the completions then use the navigation commands to
            // change the selected item.  Otherwise, the user was just typing and is now moving
            // through the file.  In this case stop everything we're doing.
            var model = sessionOpt.Computation.InitialUnfilteredModel != null ? sessionOpt.Computation.WaitForController() : null;

            // Check if completion is still active.  Then update the computation appropriately.
            if (model != null)
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
