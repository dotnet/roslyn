// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    internal partial class VisualStudioSymbolSearchService
    {
        private class LogService : ForegroundThreadAffinitizedObject, ISymbolSearchLogService
        {
            private static readonly LinkedList<string> s_log = new LinkedList<string>();

            private readonly IVsActivityLog _activityLog;

            public LogService(IThreadingContext threadingContext, IVsActivityLog activityLog)
                : base(threadingContext)
            {
                _activityLog = activityLog;
            }

            public Task LogInfoAsync(string text)
                => LogAsync(text, __ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION);

            public Task LogExceptionAsync(string exception, string text)
                => LogAsync(text + ". " + exception, __ACTIVITYLOG_ENTRYTYPE.ALE_ERROR);

            private Task LogAsync(string text, __ACTIVITYLOG_ENTRYTYPE type)
            {
                Log(text, type);
                return Task.CompletedTask;
            }

            private void Log(string text, __ACTIVITYLOG_ENTRYTYPE type)
            {
                if (!this.IsForeground())
                {
                    this.InvokeBelowInputPriorityAsync(() => Log(text, type));
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
