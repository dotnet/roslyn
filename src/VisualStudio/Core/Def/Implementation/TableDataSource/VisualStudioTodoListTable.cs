// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [ExportEventListener(WellKnownEventListeners.TodoListProvider, WorkspaceKind.Host), Shared]
    internal class VisualStudioTodoListTableWorkspaceEventListener : IEventListener<ITodoListProvider>
    {
        internal const string IdentifierString = nameof(VisualStudioTodoListTable);

        private readonly ITableManagerProvider _tableManagerProvider;

        [ImportingConstructor]
        public VisualStudioTodoListTableWorkspaceEventListener(ITableManagerProvider tableManagerProvider)
        {
            _tableManagerProvider = tableManagerProvider;
        }

        public void StartListening(Workspace workspace, ITodoListProvider service)
        {
            new VisualStudioTodoListTable(workspace, service, _tableManagerProvider);
        }

        internal class VisualStudioTodoListTable : VisualStudioBaseTodoListTable
        {
            // internal for testing
            internal VisualStudioTodoListTable(Workspace workspace, ITodoListProvider todoListProvider, ITableManagerProvider provider) :
                base(workspace, todoListProvider, IdentifierString, provider)
            {
                ConnectWorkspaceEvents();
            }
        }
    }
}
