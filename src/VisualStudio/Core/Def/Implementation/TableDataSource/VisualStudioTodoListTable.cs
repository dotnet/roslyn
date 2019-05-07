// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal class VisualStudioTodoListTable : VisualStudioBaseTodoListTable
    {
        internal const string IdentifierString = nameof(VisualStudioTodoListTable);

        public static void Register(Workspace workspace, ITodoListProvider todoListProvider, ITableManagerProvider provider)
        {
            new VisualStudioTodoListTable(workspace, todoListProvider, provider);
        }

        // internal for testing
        internal VisualStudioTodoListTable(Workspace workspace, ITodoListProvider todoListProvider, ITableManagerProvider provider) :
            base(workspace, todoListProvider, IdentifierString, provider)
        {
            ConnectWorkspaceEvents();
        }
    }
}
