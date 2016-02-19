// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        {
            // TODO: Revert to the code below pending https://github.com/dotnet/roslyn/issues/8927 .
            // return new CommandID(ID.InteractiveCommands.CSharpInteractiveCommandSetId, ID.InteractiveCommands.ResetInteractiveFromProject);
            return new CommandID(Guids.CSharpInteractiveCommandSetId, ID.InteractiveCommands.ResetInteractiveFromProject);
        }
    }
}
