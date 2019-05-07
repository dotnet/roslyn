// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal sealed class MiscellaneousTodoListTable : VisualStudioBaseTodoListTable
    {
        internal const string IdentifierString = nameof(MiscellaneousTodoListTable);

        public static void Register(MiscellaneousFilesWorkspace workspace, ITodoListProvider todoListProvider, ITableManagerProvider provider)
        {
            new MiscellaneousTodoListTable(workspace, todoListProvider, provider);
        }

        private MiscellaneousTodoListTable(MiscellaneousFilesWorkspace workspace, ITodoListProvider todoListProvider, ITableManagerProvider provider) :
            base(workspace, todoListProvider, IdentifierString, provider)
        {
            ConnectWorkspaceEvents();
        }
    }
}
