// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [ExportWorkspaceEventListener(WorkspaceKind.Host), Shared]
    internal class VisualStudioTodoListTableWorkspaceEventListener : IWorkspaceEventListener
    {
        internal const string IdentifierString = nameof(VisualStudioTodoListTable);

        private readonly ITodoListProvider _todoListProvider;
        private readonly ITableManagerProvider _tableManagerProvider;

        [ImportingConstructor]
        public VisualStudioTodoListTableWorkspaceEventListener(
            ITodoListProvider todoListProvider, ITableManagerProvider tableManagerProvider)
        {
            _todoListProvider = todoListProvider;
            _tableManagerProvider = tableManagerProvider;
        }

        public void Listen(Workspace workspace)
        {
            new VisualStudioTodoListTable(workspace, _todoListProvider, _tableManagerProvider);
        }

        public void Stop(Workspace workspace)
        {
            // nothing to do
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
