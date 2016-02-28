// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.Utilities.ForegroundThreadDataKind;

namespace Microsoft.CodeAnalysis.Utilities
{
    internal enum ForegroundThreadDataKind
    {
        Wpf,
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
            s_fallbackForegroundThreadDataKind = CreateDefault();
        }

        internal static ForegroundThreadDataKind CreateDefault(ForegroundThreadDataKind? defaultKind = null)
        {
            var syncConextTypeName = SynchronizationContext.Current?.GetType().FullName;

            switch (syncConextTypeName)
            {
                case "System.Windows.Threading.DispatcherSynchronizationContext":

                    return Wpf;

                case "Microsoft.VisualStudio.Threading.JoinableTask+JoinableTaskSynchronizationContext":

                    return JoinableTask;

                default:

                    return defaultKind ?? Unknown;
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