// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    internal partial class PackageSearchService
    {
        private class LogService : ForegroundThreadAffinitizedObject, IPackageSearchLogService
        {
            private readonly IVsActivityLog _activityLog;

            public LogService(IVsActivityLog activityLog)
            {
                _activityLog = activityLog;
            }

            public void LogInfo(string text)
            {
                Log(text, __ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION);
            }

            public void LogException(Exception e, string text)
            {
                Log(text + ". " + e.ToString(), __ACTIVITYLOG_ENTRYTYPE.ALE_ERROR);
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
