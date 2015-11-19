﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [Export(typeof(ITableControlEventProcessorProvider))]
    [DataSourceType(StandardTableDataSources.CommentTableDataSource)]
    [DataSource(VisualStudioTodoListTable.IdentifierString)]
    [Name(Name)]
    [Order(Before = "default")]
    internal class TodoListTableControlEventProcessorProvider : AbstractTableControlEventProcessorProvider<TodoItem>
    {
        internal const string Name = "C#/VB Todo List Table Event Processor";
    }
}
