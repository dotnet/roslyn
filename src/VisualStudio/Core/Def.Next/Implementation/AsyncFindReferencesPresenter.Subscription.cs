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
            private readonly CancellationTokenSource _tokenSource;
            public readonly ITableDataSink TableDataSink;

            public Subscription(CancellationTokenSource tokenSource, ITableDataSink sink)
            {
                _tokenSource = tokenSource;
                TableDataSink = sink;
            }

            public void Dispose()
            {
                _tokenSource.Cancel();
            }
        }
    }
}