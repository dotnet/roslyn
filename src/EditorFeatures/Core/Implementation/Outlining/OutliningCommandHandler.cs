// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.VisualStudio.Text.Outlining;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Outlining
{
    [ExportCommandHandler("Outlining Command Handler", ContentTypeNames.RoslynContentType)]
    internal sealed class OutliningCommandHandler : ICommandHandler<StartAutomaticOutliningCommandArgs>
    {
        private readonly IOutliningManagerService _outliningManagerService;

        [ImportingConstructor]
        public OutliningCommandHandler(IOutliningManagerService outliningManagerService)
        {
            _outliningManagerService = outliningManagerService;
        }

        public void ExecuteCommand(StartAutomaticOutliningCommandArgs args, Action nextHandler)
        {
            // The editor actually handles this command, we just have to make sure it is enabled.
            nextHandler();
        }

        public CommandState GetCommandState(StartAutomaticOutliningCommandArgs args, Func<CommandState> nextHandler)
        {
            var outliningManager = _outliningManagerService.GetOutliningManager(args.TextView);
            var enabled = false;
            if (outliningManager != null)
            {
                enabled = outliningManager.Enabled;
            }

            return new CommandState(isAvailable: !enabled);
        }
    }
}
