﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal abstract class AbstractTableControlEventProcessorProvider<TData> : ITableControlEventProcessorProvider
    {
        public ITableControlEventProcessor GetAssociatedEventProcessor(IWpfTableControl tableControl)
        {
            return new EventProcessor();
        }

        private class EventProcessor : TableControlEventProcessorBase
        {
            public override void PreprocessNavigate(ITableEntryHandle entryHanle, TableEntryNavigateEventArgs e)
            {
                int index;
                ITableEntriesSnapshot snapshot;
                if (!entryHanle.TryGetSnapshot(out snapshot, out index))
                {
                    return;
                }

                var roslynSnapshot = snapshot as AbstractTableEntriesSnapshot<TData>;
                if (roslynSnapshot == null)
                {
                    return;
                }

                // we always mark it as handled if entry is ours
                e.Handled = true;

                // REVIEW: 
                // turning off one click navigation.
                // unlike FindAllReference which don't lose focus even after navigation, 
                // error list loses focus once navigation happens. I checked our find all reference implementation, and it uses
                // same mechanism as error list, so it must be the find all reference window doing something to not lose focus or it must
                // taking focus back once navigation happened. we need to implement same thing in error list. until then, I am disabling one
                // click navigation.
                if (!e.IsPreview)
                {
                    roslynSnapshot.TryNavigateTo(index, e.IsPreview);
                }
            }
        }
    }
}
