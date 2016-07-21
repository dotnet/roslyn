using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal partial class AsyncFindReferencesPresenter
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