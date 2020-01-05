// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal abstract class AbstractTableControlEventProcessorProvider<TItem> : ITableControlEventProcessorProvider
        where TItem : TableItem
    {
        public ITableControlEventProcessor GetAssociatedEventProcessor(IWpfTableControl tableControl)
        {
            return CreateEventProcessor();
        }

        protected virtual EventProcessor CreateEventProcessor()
        {
            return new EventProcessor();
        }

        protected class EventProcessor : TableControlEventProcessorBase
        {
            protected static AbstractTableEntriesSnapshot<TItem> GetEntriesSnapshot(ITableEntryHandle entryHandle)
            {
                return GetEntriesSnapshot(entryHandle, out var index);
            }

            protected static AbstractTableEntriesSnapshot<TItem> GetEntriesSnapshot(ITableEntryHandle entryHandle, out int index)
            {
                if (!entryHandle.TryGetSnapshot(out var snapshot, out index))
                {
                    return null;
                }

                return snapshot as AbstractTableEntriesSnapshot<TItem>;
            }

            public override void PreprocessNavigate(ITableEntryHandle entryHandle, TableEntryNavigateEventArgs e)
            {
                var roslynSnapshot = GetEntriesSnapshot(entryHandle, out var index);
                if (roslynSnapshot == null)
                {
                    return;
                }

                // don't be too strict on navigation on our item. if we can't handle the item,
                // let default handler to handle it at least.
                // we might fail to navigate if we don't see the document in our solution anymore.
                // that can happen if error is staled build error or user used #line pragma in C#
                // to point to some random file in error or more.
                e.Handled = roslynSnapshot.TryNavigateTo(index, e.IsPreview);
            }
        }
    }
}
