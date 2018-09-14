// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [Export(typeof(MiscellaneousTodoListTable))]
    internal class MiscellaneousTodoListTable : VisualStudioBaseTodoListTable
    {
        internal const string IdentifierString = nameof(MiscellaneousTodoListTable);

        [ImportingConstructor]
        public MiscellaneousTodoListTable(MiscellaneousFilesWorkspace workspace, ITodoListProvider todoListProvider, ITableManagerProvider provider) :
            base(workspace, todoListProvider, IdentifierString, provider)
        {
            ConnectWorkspaceEvents();
        }

        // only for test
        public MiscellaneousTodoListTable(Microsoft.CodeAnalysis.Workspace workspace, ITodoListProvider todoListProvider, ITableManagerProvider provider) :
            base(workspace, todoListProvider, IdentifierString, provider)
        {
        }
    }
}
