using System;
using System.Threading;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindReferences
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
