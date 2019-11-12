// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Structure
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name("Outlining Command Handler")]
    internal sealed class OutliningCommandHandler : ICommandHandler<StartAutomaticOutliningCommandArgs>
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

        public CommandState GetCommandState(StartAutomaticOutliningCommandArgs args)
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
