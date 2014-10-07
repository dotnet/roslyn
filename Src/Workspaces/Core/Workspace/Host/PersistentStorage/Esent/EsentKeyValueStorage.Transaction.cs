using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host.Esent
{
    internal partial class EsentKeyValueStorage
    {
        private class Transaction : IDisposable
        {
            private IntPtr session;
            private CommitTransactionGrbit? commitGrbit;
            public Transaction(IntPtr session)
            {
                this.session = session;
                NativeMethods.JetBeginTransaction(sesid: session).EnsureSucceeded();
            }

            public void Commit(CommitTransactionGrbit commitGrbit = CommitTransactionGrbit.None)
            {
                this.commitGrbit = commitGrbit;
            }

            public bool IsCommitted
            {
                get { return commitGrbit.HasValue; }
            }

            public void Dispose()
            {
                if (IsCommitted)
                {
                    NativeMethods.JetCommitTransaction(
                        sesid: session,
                        grbit: (uint)commitGrbit.Value).EnsureSucceeded();
                }
                else
                {
                    NativeMethods.JetRollback(
                        sesid: session,
                        grbit: 0).EnsureSucceeded();
                }
            }
        }
    }
}
