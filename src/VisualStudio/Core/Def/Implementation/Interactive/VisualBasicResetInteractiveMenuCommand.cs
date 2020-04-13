// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Design;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Interactive
{
    internal sealed class VisualBasicResetInteractiveMenuCommand
        : AbstractResetInteractiveMenuCommand
    {
        public VisualBasicResetInteractiveMenuCommand(
            OleMenuCommandService menuCommandService,
            IVsMonitorSelection monitorSelection,
            IComponentModel componentModel)
            : base(ContentTypeNames.VisualBasicContentType, menuCommandService, monitorSelection, componentModel)
        {
        }

        protected override string ProjectKind => VSLangProj.PrjKind.prjKindVBProject;

        protected override CommandID GetResetInteractiveFromProjectCommandID()
            => new CommandID(ID.InteractiveCommands.VisualBasicInteractiveCommandSetId, ID.InteractiveCommands.ResetInteractiveFromProject);
    }
}
