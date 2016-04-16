// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    /// <summary>
    /// Provide information to create a ITableEntriesSnapshot
    /// 
    /// This works on data that belong to logically same source of items such as one particular analyzer or todo list analyzer.
    /// </summary>
    internal abstract class AbstractTableEntriesSource<TData>
    {
        public abstract object Key { get; }
        public abstract ImmutableArray<TableItem<TData>> GetItems();
        public abstract ImmutableArray<ITrackingPoint> GetTrackingPoints(ImmutableArray<TableItem<TData>> items);
    }
}
