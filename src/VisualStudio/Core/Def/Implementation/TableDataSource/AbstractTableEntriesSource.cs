// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal abstract class AbstractTableEntriesSource<TData>
    {
        public AbstractTableEntriesSource()
        {
        }

        public abstract object Key { get; }
        public abstract ImmutableArray<TableItem<TData>> GetItems();
        public abstract ImmutableArray<ITrackingPoint> GetTrackingPoints(ImmutableArray<TableItem<TData>> items);
        public abstract AbstractTableEntriesSnapshot<TData> CreateSnapshot(int version, ImmutableArray<TableItem<TData>> items, ImmutableArray<ITrackingPoint> trackingPoints);
    }
}
