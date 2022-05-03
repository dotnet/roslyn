// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [ExportEventListener(WellKnownEventListeners.TodoListProvider, WorkspaceKind.MiscellaneousFiles), Shared]
    internal sealed class MiscellaneousTodoListTableWorkspaceEventListener : IEventListener<ITodoListProvider>
    {
        internal const string IdentifierString = nameof(MiscellaneousTodoListTable);

        private readonly IThreadingContext _threadingContext;
        private readonly ITableManagerProvider _tableManagerProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public MiscellaneousTodoListTableWorkspaceEventListener(IThreadingContext threadingContext, ITableManagerProvider tableManagerProvider)
        {
            _threadingContext = threadingContext;
            _tableManagerProvider = tableManagerProvider;
        }

        public void StartListening(Workspace workspace, ITodoListProvider service)
            => _ = new MiscellaneousTodoListTable(workspace, _threadingContext, service, _tableManagerProvider);

        private sealed class MiscellaneousTodoListTable : VisualStudioBaseTodoListTable
        {
            public MiscellaneousTodoListTable(Workspace workspace, IThreadingContext threadingContext, ITodoListProvider todoListProvider, ITableManagerProvider provider)
                : base(workspace, threadingContext, todoListProvider, IdentifierString, provider)
            {
                ConnectWorkspaceEvents();
            }
        }
    }
}
