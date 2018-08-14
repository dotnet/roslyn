// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Structure
{
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name("Outlining Command Handler")]
    internal sealed class OutliningCommandHandler : VSCommanding.ICommandHandler<StartAutomaticOutliningCommandArgs>
    {
        private readonly IOutliningManagerService _outliningManagerService;

        [ImportingConstructor]
        public OutliningCommandHandler(IOutliningManagerService outliningManagerService)
        {
            _outliningManagerService = outliningManagerService;
        }

        public string DisplayName => EditorFeaturesResources.Outlining;

        public bool ExecuteCommand(StartAutomaticOutliningCommandArgs args, CommandExecutionContext context)
        {
            // The editor actually handles this command, we just have to make sure it is enabled.
            return false;
        }

        public VSCommanding.CommandState GetCommandState(StartAutomaticOutliningCommandArgs args)
        {
            var outliningManager = _outliningManagerService.GetOutliningManager(args.TextView);
            var enabled = false;
            if (outliningManager != null)
            {
                enabled = outliningManager.Enabled;
            }

            return new VSCommanding.CommandState(isAvailable: !enabled);
        }
    }
}
