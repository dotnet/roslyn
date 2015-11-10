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
        Unknown
    }

    internal static class ForegroundThreadDataInfo
    {
        private static readonly ForegroundThreadDataKind s_fallbackForegroundThreadDataKind;
        private static ForegroundThreadDataKind? s_defaultForegroundThreadDataKind;

        static ForegroundThreadDataInfo()
        {
            s_fallbackForegroundThreadDataKind = CreateDefault();
        }

        internal static ForegroundThreadDataKind CreateDefault()
        {
            ForegroundThreadDataKind kind = SynchronizationContext.Current?.GetType().FullName == "System.Windows.Threading.DispatcherSynchronizationContext"
                    ? ForegroundThreadDataKind.Wpf
                    : ForegroundThreadDataKind.Unknown;

            return kind;
        }

        internal static ForegroundThreadDataKind DefaultForegroundThreadDataKind
        {
            get { return s_defaultForegroundThreadDataKind ?? s_fallbackForegroundThreadDataKind; }
        }

        internal static void SetDefaultForegroundThreadDataKind(ForegroundThreadDataKind? kind)
        {
            s_defaultForegroundThreadDataKind = kind;
        }
    }
}