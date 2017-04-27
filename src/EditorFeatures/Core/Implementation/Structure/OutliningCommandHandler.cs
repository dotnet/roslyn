// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;
using VSC = Microsoft.VisualStudio.Text.UI.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Structure
{
    [VSC.ExportCommandHandler("Outlining Command Handler", ContentTypeNames.RoslynContentType)]
    internal sealed class OutliningCommandHandler : VSC.ICommandHandler<StartAutomaticOutliningCommandArgs>
    {
        private readonly IOutliningManagerService _outliningManagerService;

        [ImportingConstructor]
        public OutliningCommandHandler(IOutliningManagerService outliningManagerService)
        {
            _outliningManagerService = outliningManagerService;
        }

        public bool InterestedInReadOnlyBuffer => true;

        public bool ExecuteCommand(StartAutomaticOutliningCommandArgs args)
        {
            // The editor actually handles this command, we just have to make sure it is enabled.
            return false;
        }

        public VSC.CommandState GetCommandState(StartAutomaticOutliningCommandArgs args)
        {
            var outliningManager = _outliningManagerService.GetOutliningManager(args.TextView);
            var enabled = false;
            if (outliningManager != null)
            {
                enabled = outliningManager.Enabled;
            }

            return new VSC.CommandState(isAvailable: !enabled);
        }
    }
}
