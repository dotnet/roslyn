// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Utilities
{
    internal enum ForegroundThreadDataKind
    {
        Wpf,
        StaUnitTest,
        JoinableTask,
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

        internal static ForegroundThreadDataKind CreateDefault()
        {
            var syncConextTypeName = SynchronizationContext.Current?.GetType().FullName;

            switch (syncConextTypeName)
            {
                case "System.Windows.Threading.DispatcherSynchronizationContext":

                    return ForegroundThreadDataKind.Wpf;

                case "Microsoft.VisualStudio.Threading.JoinableTask+JoinableTaskSynchronizationContext":

                    return ForegroundThreadDataKind.JoinableTask;

                default:

                    return ForegroundThreadDataKind.Unknown;

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