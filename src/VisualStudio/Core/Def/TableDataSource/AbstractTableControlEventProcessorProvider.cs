// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal abstract class AbstractTableControlEventProcessorProvider<TItem> : ITableControlEventProcessorProvider
        where TItem : TableItem
    {
        public ITableControlEventProcessor GetAssociatedEventProcessor(IWpfTableControl tableControl)
            => CreateEventProcessor();

        protected virtual EventProcessor CreateEventProcessor()
            => new();

        protected class EventProcessor : TableControlEventProcessorBase
        {
            protected static AbstractTableEntriesSnapshot<TItem>? GetEntriesSnapshot(ITableEntryHandle entryHandle)
                => GetEntriesSnapshot(entryHandle, out _);

            protected static AbstractTableEntriesSnapshot<TItem>? GetEntriesSnapshot(ITableEntryHandle entryHandle, out int index)
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

                // TODO: Use a threaded-wait-dialog here so we can cancel navigation.
                var options = new NavigationOptions(PreferProvisionalTab: e.IsPreview, ActivateTab: e.ShouldActivate);
                e.Handled = roslynSnapshot.TryNavigateTo(index, options, CancellationToken.None);
            }
        }
    }
}
