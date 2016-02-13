// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        CommandState ICommandHandler<ToggleCompletionModeCommandArgs>.GetCommandState(ToggleCompletionModeCommandArgs args, System.Func<CommandState> nextHandler)
        {
            AssertIsForeground();

            var isEnabled = args.SubjectBuffer.GetOption(EditorCompletionOptions.UseSuggestionMode);
            return new CommandState(isAvailable: true, isChecked: isEnabled);
        }

        void ICommandHandler<ToggleCompletionModeCommandArgs>.ExecuteCommand(ToggleCompletionModeCommandArgs args, Action nextHandler)
        {
            Workspace workspace;

            if (Workspace.TryGetWorkspace(args.SubjectBuffer.AsTextContainer(), out workspace))
            {
                var optionService = workspace.Services.GetService<IOptionService>();
                var optionSet = optionService.GetOptions();

                var wasEnabled = optionService.GetOption(EditorCompletionOptions.UseSuggestionMode);
                optionService.SetOptions(optionSet.WithChangedOption(EditorCompletionOptions.UseSuggestionMode, !wasEnabled));

                // If we don't have a computation in progress, then we don't have to do anything here.
                if (this.sessionOpt == null)
                {
                    return;
                }

                this.sessionOpt.SetModelBuilderState(!wasEnabled);
            }
        }
    }
}
