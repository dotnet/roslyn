// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal abstract class AbstractTableControlEventProcessorProvider<TData> : ITableControlEventProcessorProvider
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
            protected static AbstractTableEntriesSnapshot<TData> GetEntriesSnapshot(ITableEntryHandle entryHandle)
            {
                int index;
                return GetEntriesSnapshot(entryHandle, out index);
            }

            protected static AbstractTableEntriesSnapshot<TData> GetEntriesSnapshot(ITableEntryHandle entryHandle, out int index)
            {
                ITableEntriesSnapshot snapshot;
                if (!entryHandle.TryGetSnapshot(out snapshot, out index))
                {
                    return null;
                }

                return snapshot as AbstractTableEntriesSnapshot<TData>;
            }

            public override void PreprocessNavigate(ITableEntryHandle entryHandle, TableEntryNavigateEventArgs e)
            {
                int index;
                var roslynSnapshot = GetEntriesSnapshot(entryHandle, out index);
                if (roslynSnapshot == null)
                {
                    return;
                }

                // we always mark it as handled if entry is ours
                e.Handled = true;

                roslynSnapshot.TryNavigateTo(index, e.IsPreview);
            }
        }
    }
}
