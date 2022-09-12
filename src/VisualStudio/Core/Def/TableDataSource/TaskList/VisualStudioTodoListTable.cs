// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.TaskList;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [ExportEventListener(WellKnownEventListeners.TaskListProvider, WorkspaceKind.Host), Shared]
    internal class VisualStudioTodoListTableWorkspaceEventListener : IEventListener<ITaskListProvider>
    {
        internal const string IdentifierString = nameof(VisualStudioTodoListTable);

        private readonly IThreadingContext _threadingContext;
        private readonly ITableManagerProvider _tableManagerProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioTodoListTableWorkspaceEventListener(IThreadingContext threadingContext, ITableManagerProvider tableManagerProvider)
        {
            _threadingContext = threadingContext;
            _tableManagerProvider = tableManagerProvider;
        }

        public void StartListening(Workspace workspace, ITaskListProvider service)
            => _ = new VisualStudioTodoListTable(workspace, _threadingContext, service, _tableManagerProvider);

        internal class VisualStudioTodoListTable : VisualStudioBaseTodoListTable
        {
            // internal for testing
            internal VisualStudioTodoListTable(Workspace workspace, IThreadingContext threadingContext, ITaskListProvider todoListProvider, ITableManagerProvider provider)
                : base(workspace, threadingContext, todoListProvider, IdentifierString, provider)
            {
                ConnectWorkspaceEvents();
            }
        }
    }
}
