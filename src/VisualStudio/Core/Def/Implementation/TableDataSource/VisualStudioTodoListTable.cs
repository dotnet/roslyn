// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.TableControl;
using Microsoft.VisualStudio.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [Export(typeof(VisualStudioTodoListTable))]
    internal class VisualStudioTodoListTable : VisualStudioBaseTodoListTable
    {
        internal const string IdentifierString = "{036B243C-81E5-4360-8F9D-D105A64BF04C}";
        internal static readonly Guid Identifier = new Guid(IdentifierString);

        [ImportingConstructor]
        public VisualStudioTodoListTable(VisualStudioWorkspace workspace, ITodoListProvider todoListProvider, ITableManagerProvider provider) :
            base(workspace, todoListProvider, Identifier, provider)
        {
            ConnectWorkspaceEvents();
        }

        // only for test
        public VisualStudioTodoListTable(Workspace workspace, ITodoListProvider todoListProvider, ITableManagerProvider provider) :
            base(workspace, todoListProvider, Identifier, provider)
        {
        }
    }
}
