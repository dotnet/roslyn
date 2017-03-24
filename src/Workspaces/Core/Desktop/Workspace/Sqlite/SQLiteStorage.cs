// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Data.SQLite;
using System.IO;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLiteStorage
    {
        private readonly string _databaseFile;
        private readonly CancellationTokenSource _shutdownCancellationTokenSource;

        private SQLiteConnection _instance;

        public SQLiteStorage(string databaseFile)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(databaseFile));

            _databaseFile = databaseFile;

            _shutdownCancellationTokenSource = new CancellationTokenSource();
        }

        public void Initialize()
        {
            _instance = CreateSQLiteInstance();

            InitializeDatabaseAndTables();
        }

        public bool IsClosed
        {
            get { return _instance == null; }
        }

        public void Close()
        {
            if (_instance != null)
            {
                var handle = _instance;
                _instance = null;

                // try to free all allocated session - if succeeded we can try to do a clean shutdown
                _shutdownCancellationTokenSource.Cancel();

                try
                {
                    // just close the instance - all associated objects will be closed as well
                    handle.Dispose();
                }
                catch
                {
                    // ignore exception if whatever reason esent throws an exception.
                }

                _shutdownCancellationTokenSource.Dispose();
            }
        }

#if false
        public int GetUniqueId(string value)
        {
            return GetUniqueId(value, TableKinds.Name);
        }

        public int GetUniqueIdentifierId(string value)
        {
            return GetUniqueId(value, TableKinds.Identifier);
        }

        private int GetUniqueId(string value, TableKinds tableKind)
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(value));

            using (var accessor = (StringNameTableAccessor)GetTableAccessor(tableKind))
            {
                return accessor.GetUniqueId(value);
            }
        }

        public SolutionTableAccessor GetSolutionTableAccessor()
        {
            return (SolutionTableAccessor)GetTableAccessor(TableKinds.Solution);
        }

        public ProjectTableAccessor GetProjectTableAccessor()
        {
            return (ProjectTableAccessor)GetTableAccessor(TableKinds.Project);
        }

        public DocumentTableAccessor GetDocumentTableAccessor()
        {
            return (DocumentTableAccessor)GetTableAccessor(TableKinds.Document);
        }

        public IdentifierLocationTableAccessor GetIdentifierLocationTableAccessor()
        {
            return (IdentifierLocationTableAccessor)GetTableAccessor(TableKinds.IdentifierLocations);
        }

        private AbstractTableAccessor GetTableAccessor(TableKinds tableKind)
        {
            return _tables[tableKind].GetTableAccessor(GetOpenSession());
        }

        private OpenSession GetOpenSession()
        {
            if (_sessionCache.TryPop(out var session))
            {
                return session;
            }

            return new OpenSession(this, _databaseFile, _shutdownCancellationTokenSource.Token);
        }

        private void CloseSession(OpenSession session)
        {
            if (_shutdownCancellationTokenSource.IsCancellationRequested)
            {
                session.Close();
                return;
            }

            if (_sessionCache.Count > 5)
            {
                session.Close();
                return;
            }

            _sessionCache.Push(session);
        }
#endif
        private SQLiteConnection CreateSQLiteInstance()
        {
            var instanceDataFolder = Path.GetDirectoryName(_databaseFile);

            SQLiteConnection.CreateFile(_databaseFile);

            var instance = new SQLiteConnection($"Data Source={_databaseFile};Version=3;");
            instance.Open();

            return instance;
        }

        private void InitializeDatabaseAndTables()
        {
            var command = new SQLiteCommand(
                "CREATE TABLE IF NOT EXISTS KeyValueStorage(k BLOB PRIMARY KEY, v BLOB)",
                _instance);
            command.ExecuteNonQuery();
        }
    }
}