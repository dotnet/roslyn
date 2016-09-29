// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Roslyn.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    internal partial class SymbolSearchService
    {
        private class LogService : ForegroundThreadAffinitizedObject, ILogService
        {
            private readonly IVsActivityLog _activityLog;

            public LogService(IVsActivityLog activityLog)
            {
                _activityLog = activityLog;
            }

            public Task LogInfoAsync(string text)
            {
                return LogAsync(text, __ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION);
            }

            public Task LogExceptionAsync(Exception e, string text)
            {
                return LogAsync(text + ". " + e.ToString(), __ACTIVITYLOG_ENTRYTYPE.ALE_ERROR);
            }

            private Task LogAsync(string text, __ACTIVITYLOG_ENTRYTYPE type)
            {
                Log(text, type);
                return SpecializedTasks.EmptyTask;
            }

            private void Log(string text, __ACTIVITYLOG_ENTRYTYPE type)
            { 
                if (!this.IsForeground())
                {
                    this.InvokeBelowInputPriority(() => Log(text, type));
                    return;
                }

                AssertIsForeground();
                _activityLog?.LogEntry((uint)type, HostId, text);

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
