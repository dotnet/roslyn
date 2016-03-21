// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VsHub.Services;

namespace Microsoft.CodeAnalysis.HubServices.SymbolSearch
{
    public partial class SymbolSearchController
    {
        private class LogService : ISymbolSearchLogService
        {
            private readonly ILogger _activityLog;

            public LogService(ILogger activityLog)
            {
                _activityLog = activityLog;
            }

            public void LogInfo(string text)
            {
                _activityLog.LogInfo(WellKnownHubServiceNames.SymbolSearch, text);
                Log(text);
            }

            public void LogException(Exception e, string text)
            {
                var message = text + ". " + e.ToString();
                _activityLog.LogError(WellKnownHubServiceNames.SymbolSearch, message);
                Log(message);
            }

            private void Log(string text)
            {
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
