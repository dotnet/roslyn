// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;
using VSShell = Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    [ExportWorkspaceServiceFactory(typeof(ISymbolSearchLogService), ServiceLayer.Host), Shared]
    internal class VisualStudioSymbolSearchLogServiceFactory : IWorkspaceServiceFactory
    {
        private static readonly LinkedList<string> s_log = new LinkedList<string>();

        private readonly VSShell.SVsServiceProvider _serviceProvider;

        [ImportingConstructor]
        public VisualStudioSymbolSearchLogServiceFactory(
            VSShell.SVsServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            var workspace = workspaceServices.Workspace as VisualStudioWorkspaceImpl;
            if (workspace == null)
            {
                return new DefaultSymbolSearchLogService();
            }


            return new LogService((IVsActivityLog)_serviceProvider.GetService(typeof(SVsActivityLog)));
        }

        private class LogService : ForegroundThreadAffinitizedObject, ISymbolSearchLogService
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
                _activityLog?.LogEntry((uint)type, DefaultSymbolSearchUpdateEngine.HostId, text);

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