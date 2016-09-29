// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.LanguageServices.Interactive;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.InteractiveWindow.Shell;
using LanguageServiceGuids = Microsoft.VisualStudio.LanguageServices.Guids;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Interactive
{
    [Guid(LanguageServiceGuids.CSharpReplPackageIdString)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideMenuResource("Menus.ctmenu", 17)]
    [ProvideLanguageExtension(LanguageServiceGuids.CSharpLanguageServiceIdString, ".csx")]
    [ProvideInteractiveWindow(
        IdString,
        Orientation = ToolWindowOrientation.Bottom,
        Style = VsDockStyle.Tabbed,
        Window = CommonVsUtils.OutputWindowId)]
    internal partial class CSharpVsInteractiveWindowPackage : VsInteractiveWindowPackage<CSharpVsInteractiveWindowProvider>
    {
        private const string IdString = "CA8CC5C7-0231-406A-95CD-AA5ED6AC0190";
        internal static readonly Guid Id = new Guid(IdString);

        protected override Guid ToolWindowId
        {
            get { return Id; }
        }

        protected override Guid LanguageServiceGuid
        {
            get { return LanguageServiceGuids.CSharpLanguageServiceId; }
        }

        protected override void InitializeMenuCommands(OleMenuCommandService menuCommandService)
        {
            var openInteractiveCommand = new MenuCommand(
                (sender, args) => this.InteractiveWindowProvider.Open(instanceId: 0, focus: true),
                new CommandID(CSharpInteractiveCommands.InteractiveCommandSetId, CSharpInteractiveCommands.InteractiveToolWindow));

            menuCommandService.AddCommand(openInteractiveCommand);
        }
    }
}
