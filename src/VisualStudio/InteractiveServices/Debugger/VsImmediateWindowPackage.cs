// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Interactive;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;
using Microsoft.VisualStudio.InteractiveWindow.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Debugging
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideInteractiveWindow(
        IdString,
        Orientation = ToolWindowOrientation.Bottom,
        Style = VsDockStyle.Tabbed,
        Window = CommonVsUtils.OutputWindowId)]
    internal partial class VsImmediateWindowPackage : Package, IVsToolWindowFactory
    {
        private const string IdString = "30939CD6-F4F0-4B7F-8F30-9D5B639D2E0B";
        internal static readonly Guid Id = new Guid(IdString);

        internal readonly VsImmediateWindowProvider Provider;

        public VsImmediateWindowPackage()
        {
            var componentModel = (IComponentModel)GetService(typeof(SComponentModel));
            this.Provider = componentModel.DefaultExportProvider.GetExportedValue<VsImmediateWindowProvider>();
        }

        /// <summary>
        /// When a VSPackage supports multi-instance tool windows, each window uses the same rguidPersistenceSlot.
        /// The dwToolWindowId parameter is used to differentiate between the various instances of the tool window.
        /// </summary>
        int IVsToolWindowFactory.CreateToolWindow(ref Guid rguidPersistenceSlot, uint instanceId)
        {
            if (rguidPersistenceSlot == Id && instanceId == 0)
            {
                var result = Provider.Create();
                return (result != null) ? VSConstants.S_OK : VSConstants.E_FAIL;
            }

            return VSConstants.E_FAIL;
        }
    }
}
