// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    /// <summary>
    /// Event processor that we export so we can control how navigation works in the streaming
    /// FAR window.  We need this because the FAR window has no way to know how to do things like
    /// navigate to definitions that are from metadata.  We take control here and handle navigation
    /// ourselves so that we can do things like navigate to MetadataAsSource.
    /// </summary>
    [Export(typeof(ITableControlEventProcessorProvider))]
    [DataSourceType(StreamingFindUsagesPresenter.RoslynFindUsagesTableDataSourceSourceTypeIdentifier)]
    [DataSource(StreamingFindUsagesPresenter.RoslynFindUsagesTableDataSourceIdentifier)]
    [Name(nameof(FindUsagesTableControlEventProcessorProvider))]
    [Order(Before = Priority.Default)]
    internal class FindUsagesTableControlEventProcessorProvider : ITableControlEventProcessorProvider
    {
        [ImportingConstructor]
        public FindUsagesTableControlEventProcessorProvider()
        {
        }

        public ITableControlEventProcessor GetAssociatedEventProcessor(
            IWpfTableControl tableControl)
        {
            return new TableControlEventProcessor();
        }

        private class TableControlEventProcessor : TableControlEventProcessorBase
        {
            public override void PreprocessNavigate(ITableEntryHandle entry, TableEntryNavigateEventArgs e)
            {
                if (entry.Identity is ISupportsNavigation supportsNavigation)
                {
                    if (supportsNavigation.TryNavigateTo(e.IsPreview))
                    {
                        e.Handled = true;
                        return;
                    }
                }

                if (entry.TryGetValue(StreamingFindUsagesPresenter.SelfKeyName, out var item) && item is ISupportsNavigation itemSupportsNavigation)
                {
                    if (itemSupportsNavigation.TryNavigateTo(e.IsPreview))
                    {
                        e.Handled = true;
                        return;
                    }
                }

                base.PreprocessNavigate(entry, e);
            }
        }
    }
}
