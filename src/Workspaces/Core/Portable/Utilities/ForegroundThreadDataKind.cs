﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        Unknown
    }

    internal static class ForegroundThreadDataInfo
    {
        private static readonly ForegroundThreadDataKind s_fallbackForegroundThreadDataKind;
        private static ForegroundThreadDataKind? s_currentForegroundThreadDataKind;

        static ForegroundThreadDataInfo()
        {
            s_fallbackForegroundThreadDataKind = CreateDefault(Unknown);
        }

        internal static ForegroundThreadDataKind CreateDefault(ForegroundThreadDataKind defaultKind)
        {
            var syncConextTypeName = SynchronizationContext.Current?.GetType().FullName;

            switch (syncConextTypeName)
            {
                case "System.Windows.Threading.DispatcherSynchronizationContext":

                    return Wpf;

                case "Microsoft.VisualStudio.Threading.JoinableTask+JoinableTaskSynchronizationContext":

                    return JoinableTask;

                case "System.Windows.Forms.WindowsFormsSynchronizationContext":

                    return WinForms;

                default:

                    return defaultKind;
            }
        }

        internal static ForegroundThreadDataKind CurrentForegroundThreadDataKind
        {
            get { return s_currentForegroundThreadDataKind ?? s_fallbackForegroundThreadDataKind; }
        }

        internal static void SetCurrentForegroundThreadDataKind(ForegroundThreadDataKind? kind)
        {
            s_currentForegroundThreadDataKind = kind;
        }
    }
}
