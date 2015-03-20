// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.TableControl;
using Microsoft.VisualStudio.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [Export(typeof(MiscellaneousTodoListTable))]
    internal class MiscellaneousTodoListTable : VisualStudioBaseTodoListTable
    {
        internal const string IdentifierString = "{A9F23FB3-51C9-46AD-85DC-1FA7669DA32C}";
        internal static readonly Guid Identifier = new Guid(IdentifierString);

        [ImportingConstructor]
        public MiscellaneousTodoListTable(MiscellaneousFilesWorkspace workspace, ITodoListProvider todoListProvider, ITableManagerProvider provider) :
            base(workspace, todoListProvider, Identifier, provider)
        {
            ConnectWorkspaceEvents();
        }

        // only for test
        public MiscellaneousTodoListTable(Workspace workspace, ITodoListProvider todoListProvider, ITableManagerProvider provider) :
            base(workspace, todoListProvider, Identifier, provider)
        {
        }
    }
}
