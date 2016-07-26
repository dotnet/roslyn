using System;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.FindReferences
{
    internal partial class StreamingFindReferencesPresenter
    {
        private class Subscription : IDisposable
        {
            private readonly TableDataSourceFindReferencesContext _dataSource;
            public readonly ITableDataSink TableDataSink;

            public Subscription(TableDataSourceFindReferencesContext dataSource, ITableDataSink sink)
            {
                _dataSource = dataSource;
                TableDataSink = sink;
            }

            public void Dispose()
            {
                _dataSource.OnSubscriptionDisposed();
            }
        }
    }
}