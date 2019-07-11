// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [ExportEventListener(WellKnownEventListeners.TodoListProvider, WorkspaceKind.MiscellaneousFiles), Shared]
    internal sealed class MiscellaneousTodoListTableWorkspaceEventListener : IEventListener<ITodoListProvider>
    {
        internal const string IdentifierString = nameof(MiscellaneousTodoListTable);

        private readonly ITableManagerProvider _tableManagerProvider;

        [ImportingConstructor]
        public MiscellaneousTodoListTableWorkspaceEventListener(ITableManagerProvider tableManagerProvider)
        {
            _tableManagerProvider = tableManagerProvider;
        }

        public void StartListening(Workspace workspace, ITodoListProvider service)
        {
            new MiscellaneousTodoListTable(workspace, service, _tableManagerProvider);
        }

        private sealed class MiscellaneousTodoListTable : VisualStudioBaseTodoListTable
        {
            public MiscellaneousTodoListTable(Workspace workspace, ITodoListProvider todoListProvider, ITableManagerProvider provider) :
                base(workspace, todoListProvider, IdentifierString, provider)
            {
                ConnectWorkspaceEvents();
            }
        }
    }
}
