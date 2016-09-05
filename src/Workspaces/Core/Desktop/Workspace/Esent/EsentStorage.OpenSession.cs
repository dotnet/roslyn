// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.Isam.Esent.Interop;

namespace Microsoft.CodeAnalysis.Esent
{
    internal partial class EsentStorage
    {
        public class OpenSession : IDisposable
        {
#if false
            private static int globalId;
            private readonly int id;
#endif

            private readonly string _databaseFile;

            private readonly EsentStorage _storage;
            private readonly CancellationToken _shutdownCancellationToken;
            private readonly CancellationTokenRegistration _cancellationTokenRegistration;

            private Session _session;
            private JET_DBID _databaseId;

            public OpenSession(EsentStorage storage, string databaseFile, CancellationToken shutdownCancellationToken)
            {
                _storage = storage;

#if false
                id = Interlocked.Increment(ref globalId);
                System.Diagnostics.Trace.WriteLine("open sessionId: " + id);
#endif

                _session = new Session(storage._instance);

                _databaseFile = databaseFile;
                _databaseId = OpenExistingDatabase(_session, databaseFile);

                _shutdownCancellationToken = shutdownCancellationToken;
                _cancellationTokenRegistration = shutdownCancellationToken.Register(() => Dispose(), useSynchronizationContext: false);
            }

            public JET_SESID SessionId { get { return _session.JetSesid; } }
            public JET_DBID DatabaseId { get { return _databaseId; } }

            public void Dispose()
            {
                _storage.CloseSession(this);
            }

            public void Close()
            {
                _cancellationTokenRegistration.Dispose();

                if (_databaseId != JET_DBID.Nil)
                {
                    Api.JetCloseDatabase(_session, _databaseId, CloseDatabaseGrbit.None);
                    _databaseId = JET_DBID.Nil;
                }

                if (_session != null)
                {
#if false
                    System.Diagnostics.Trace.WriteLine("close sessionId: " + id);
#endif

                    _session.Dispose();
                    _session = null;
                }
            }

            private JET_DBID OpenExistingDatabase(JET_SESID session, string databaseFile)
            {
                JET_DBID databaseId;
                Api.JetOpenDatabase(SessionId, databaseFile, null, out databaseId, OpenDatabaseGrbit.None);

                return databaseId;
            }
        }
    }
}
