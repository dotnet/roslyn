// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        {
            // TODO: Revert to the code below pending https://github.com/dotnet/roslyn/issues/8927 .
            // return new CommandID(ID.InteractiveCommands.VisualBasicInteractiveCommandSetId, ID.InteractiveCommands.ResetInteractiveFromProject);
            return new CommandID(Guids.VisualBasicInteractiveCommandSetId, ID.InteractiveCommands.ResetInteractiveFromProject);
        }
    }
}
