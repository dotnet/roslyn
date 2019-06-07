// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    /// <summary>
    /// Provide information to create a <see cref="ITableEntriesSnapshot"/>.
    /// 
    /// This works on data that belong to logically same source of items such as one particular analyzer or todo list analyzer.
    /// </summary>
    internal abstract class AbstractTableEntriesSource<TItem> where TItem : TableItem
    {
        public abstract object Key { get; }
        public abstract ImmutableArray<TItem> GetItems();
        public abstract ImmutableArray<ITrackingPoint> GetTrackingPoints(ImmutableArray<TItem> items);
    }
}
