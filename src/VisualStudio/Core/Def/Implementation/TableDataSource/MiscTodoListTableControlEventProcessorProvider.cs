// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.TableControl;
using Microsoft.VisualStudio.TableManager;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [Export(typeof(ITableControlEventProcessorProvider))]
    [DataSourceType(StandardTableDataSources.CommentTableDataSourceString)]
    [DataSource(MiscellaneousTodoListTable.IdentifierString)]
    [Name(Name)]
    [Order(Before = "default")]
    internal class MiscTodoListTableControlEventProcessorProvider : AbstractTableControlEventProcessorProvider<ITaskItem>
    {
        internal const string Name = "Misc C#/VB Todo List Table Event Processor";
    }
}
