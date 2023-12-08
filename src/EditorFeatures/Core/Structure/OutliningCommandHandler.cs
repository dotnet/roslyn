// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Structure
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name("Outlining Command Handler")]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class OutliningCommandHandler(IOutliningManagerService outliningManagerService) : ICommandHandler<StartAutomaticOutliningCommandArgs>
    {
        private readonly IOutliningManagerService _outliningManagerService = outliningManagerService;

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
