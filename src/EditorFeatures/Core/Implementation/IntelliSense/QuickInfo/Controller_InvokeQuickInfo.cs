// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    internal partial class Controller
    {
        CommandState ICommandHandler<InvokeQuickInfoCommandArgs>.GetCommandState(InvokeQuickInfoCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        void ICommandHandler<InvokeQuickInfoCommandArgs>.ExecuteCommand(InvokeQuickInfoCommandArgs args, Action nextHandler)
        {
            var caretPoint = args.TextView.GetCaretPoint(args.SubjectBuffer);
            if (caretPoint.HasValue)
            {
                // Invoking QuickInfo from the command, so there's no session yet.
                InvokeQuickInfo(caretPoint.Value.Position, trackMouse: false, augmentSession: null);
            }
        }

        public void InvokeQuickInfo(int position, bool trackMouse, IQuickInfoSession augmentSession)
        {
            AssertIsForeground();
            DismissSessionIfActive();
            StartSession(position, trackMouse, augmentSession);
        }
    }
}
