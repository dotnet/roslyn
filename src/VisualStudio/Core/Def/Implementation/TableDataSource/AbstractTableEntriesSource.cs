// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    /// <summary>
    /// Provide information to create a ITableEntriesSnapshot
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
