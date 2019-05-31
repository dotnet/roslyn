// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [ExportWorkspaceEventListener(WorkspaceKind.MiscellaneousFiles), Shared]
    internal sealed class MiscellaneousTodoListTableWorkspaceEventListener : IWorkspaceEventListener
    {
        internal const string IdentifierString = nameof(MiscellaneousTodoListTable);

        private readonly ITodoListProvider _todoListProvider;
        private readonly ITableManagerProvider _tableManagerProvider;

        [ImportingConstructor]
        public MiscellaneousTodoListTableWorkspaceEventListener(
            ITodoListProvider todoListProvider, ITableManagerProvider tableManagerProvider)
        {
            _todoListProvider = todoListProvider;
            _tableManagerProvider = tableManagerProvider;
        }

        public void Listen(Workspace workspace)
        {
            new MiscellaneousTodoListTable(workspace, _todoListProvider, _tableManagerProvider);
        }

        public void Stop(Workspace workspace)
        {
            // nothing to do
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
