// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    internal partial class VisualStudioSymbolSearchService
    {
        private class LogService : ForegroundThreadAffinitizedObject, ISymbolSearchLogService
        {
            private static readonly LinkedList<string> s_log = new();

            private readonly IVsActivityLog _activityLog;

            public LogService(IThreadingContext threadingContext, IVsActivityLog activityLog)
                : base(threadingContext)
            {
                _activityLog = activityLog;
            }

            public ValueTask LogInfoAsync(string text, CancellationToken cancellationToken)
                => LogAsync(text, __ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION);

            public ValueTask LogExceptionAsync(string exception, string text, CancellationToken cancellationToken)
                => LogAsync(text + ". " + exception, __ACTIVITYLOG_ENTRYTYPE.ALE_ERROR);

            private ValueTask LogAsync(string text, __ACTIVITYLOG_ENTRYTYPE type)
            {
                Log(text, type);
                return default;
            }

            private void Log(string text, __ACTIVITYLOG_ENTRYTYPE type)
            {
                if (!IsForeground())
                {
                    InvokeBelowInputPriorityAsync(() => Log(text, type));
                    return;
                }

                AssertIsForeground();
                _activityLog?.LogEntry((uint)type, SymbolSearchUpdateEngine.HostId, text);

                // Keep a running in memory log as well for debugging purposes.
                s_log.AddLast(text);
                while (s_log.Count > 100)
                {
                    s_log.RemoveFirst();
                }
            }
        }
    }
}
