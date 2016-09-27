// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.VisualStudio.LanguageServices.FindReferences
{
    /// <summary>
    /// Event processor that we export so we can control how navigation works in the streaming
    /// FAR window.  We need this because the FAR window has no way to know how to do things like
    /// navigate to definitions that are from metadata.  We take control here and handle navigation
    /// ourselves so that we can do things like navigate to MetadataAsSource.
    /// </summary>
    [Export(typeof(ITableControlEventProcessorProvider))]
    [DataSourceType(StreamingFindReferencesPresenter.RoslynFindReferencesTableDataSourceSourceTypeIdentifier)]
    [DataSource(StreamingFindReferencesPresenter.RoslynFindReferencesTableDataSourceIdentifier)]
    [Name(nameof(FindReferencesTableControlEventProcessorProvider))]
    [Order(Before = Priority.Default)]
    internal class FindReferencesTableControlEventProcessorProvider : ITableControlEventProcessorProvider
    {
        public ITableControlEventProcessor GetAssociatedEventProcessor(
            IWpfTableControl tableControl)
        {
            return new TableControlEventProcessor();
        }

        private class TableControlEventProcessor : TableControlEventProcessorBase
        {
            public override void PreprocessNavigate(ITableEntryHandle entry, TableEntryNavigateEventArgs e)
            {
                var supportsNavigation = entry.Identity as ISupportsNavigation;
                if (supportsNavigation != null)
                {
                    if (supportsNavigation.TryNavigateTo())
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