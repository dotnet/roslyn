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
    internal sealed class CSharpResetInteractiveMenuCommand
        : AbstractResetInteractiveMenuCommand
    {
        public CSharpResetInteractiveMenuCommand(
            OleMenuCommandService menuCommandService,
            IVsMonitorSelection monitorSelection,
            IComponentModel componentModel)
            : base(ContentTypeNames.CSharpContentType, menuCommandService, monitorSelection, componentModel)
        {
        }

        protected override string ProjectKind => VSLangProj.PrjKind.prjKindCSharpProject;

        protected override CommandID GetResetInteractiveFromProjectCommandID()
            => new CommandID(ID.InteractiveCommands.CSharpInteractiveCommandSetId, ID.InteractiveCommands.ResetInteractiveFromProject);
    }
}
