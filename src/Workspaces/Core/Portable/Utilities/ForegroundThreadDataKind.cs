// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using static Microsoft.CodeAnalysis.Utilities.ForegroundThreadDataKind;

namespace Microsoft.CodeAnalysis.Utilities
{
    internal enum ForegroundThreadDataKind
    {
        Wpf,
        WinForms,
        StaUnitTest,
        JoinableTask,
        ForcedByPackageInitialize,
        MonoDevelopGtk,
        MonoDevelopXwt,
        Unknown
    }

    internal static class ForegroundThreadDataInfo
    {
        internal static ForegroundThreadDataKind CreateDefault(ForegroundThreadDataKind defaultKind)
        {
            var syncContextTypeName = SynchronizationContext.Current?.GetType().FullName;

            switch (syncContextTypeName)
            {
                case "System.Windows.Threading.DispatcherSynchronizationContext":

                    return Wpf;

                case "Microsoft.VisualStudio.Threading.JoinableTask+JoinableTaskSynchronizationContext":

                    return JoinableTask;

                case "System.Windows.Forms.WindowsFormsSynchronizationContext":

                    return WinForms;

                case "MonoDevelop.Ide.DispatchService+GtkSynchronizationContext":

                    return MonoDevelopGtk;

                case "Xwt.XwtSynchronizationContext":

                    return MonoDevelopXwt;

                default:

                    return defaultKind;
            }
        }
    }
}
