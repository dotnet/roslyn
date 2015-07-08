// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Isam.Esent.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Esent
{
    internal partial class EsentStorage
    {
        // set db page size to 8K and version store page size to 16K
        private const int DatabasePageSize = 2 * 4 * 1024;
        private const int VersionStorePageSize = 4 * 4 * 1024;

        // JET parameter consts
        private const int JET_paramIOPriority = 152;
        private const int JET_paramCheckpointIOMax = 135;
        private const int JET_paramVerPageSize = 128;
        private const int JET_paramDisablePerfmon = 107;
        private const int JET_paramPageHintCacheSize = 101;
        private const int JET_paramLogFileCreateAsynch = 69;
        private const int JET_paramOutstandingIOMax = 30;
        private const int JET_paramLRUKHistoryMax = 26;
        private const int JET_paramCommitDefault = 16;

        private readonly string _databaseFile;
        private readonly bool _enablePerformanceMonitor;
        private readonly Dictionary<TableKinds, AbstractTable> _tables;

        private readonly ConcurrentStack<OpenSession> _sessionCache;
        private readonly CancellationTokenSource _shutdownCancellationTokenSource;

        private Instance _instance;
        private Session _primarySessionId;
        private JET_DBID _primaryDatabaseId;

        public EsentStorage(string databaseFile, bool enablePerformanceMonitor = false)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(databaseFile));

            _databaseFile = databaseFile;
            _enablePerformanceMonitor = enablePerformanceMonitor;

            // order of tables are fixed. don't change it
            _tables = new Dictionary<TableKinds, AbstractTable>()
            {
                { TableKinds.Name, new NameTable() },
                { TableKinds.Solution, new SolutionTable() },
                { TableKinds.Project, new ProjectTable() },
                { TableKinds.Document, new DocumentTable() },
                { TableKinds.Identifier, new IdentifierNameTable() },
                { TableKinds.IdentifierLocations, new IdentifierLocationTable() },
            };

            _sessionCache = new ConcurrentStack<OpenSession>();
            _shutdownCancellationTokenSource = new CancellationTokenSource();
        }

        public void Initialize()
        {
            _instance = CreateEsentInstance();
            _primarySessionId = new Session(_instance);

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
                    _primarySessionId.Dispose();
                    handle.Dispose();
                }
                catch
                {
                    // ignore exception if whatever reason esent throws an exception.
                }

                _shutdownCancellationTokenSource.Dispose();
            }
        }

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
            OpenSession session;
            if (_sessionCache.TryPop(out session))
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

        private Instance CreateEsentInstance()
        {
            var instanceDataFolder = Path.GetDirectoryName(_databaseFile);

            TryInitializeGlobalParameters();

            var instance = new Instance(Path.GetFileName(_databaseFile), _databaseFile, TermGrbit.Complete);

            // create log file preemptively
            Api.JetSetSystemParameter(instance.JetInstance, JET_SESID.Nil, (JET_param)JET_paramLogFileCreateAsynch, /* true */ 1, null);

            // set default commit mode
            Api.JetSetSystemParameter(instance.JetInstance, JET_SESID.Nil, (JET_param)JET_paramCommitDefault, /* lazy */ 1, null);

            // remove transaction log file that is not needed anymore
            instance.Parameters.CircularLog = true;

            // transaction log file buffer 1M (1024 * 2 * 512 bytes)
            instance.Parameters.LogBuffers = 2 * 1024;

            // transaction log file is 2M (2 * 1024 * 1024 bytes)
            instance.Parameters.LogFileSize = 2 * 1024;

            // db directories
            instance.Parameters.LogFileDirectory = instanceDataFolder;
            instance.Parameters.SystemDirectory = instanceDataFolder;
            instance.Parameters.TempDirectory = instanceDataFolder;

            // Esent uses version pages to store intermediate non-committed data during transactions
            // smaller values may cause VersionStoreOutOfMemory error when dealing with multiple transactions\writing lot's of data in transaction or both
            // it is about 16MB - this is okay to be big since most of it is temporary memory that will be released once the last transaction goes away
            instance.Parameters.MaxVerPages = 16 * 1024 * 1024 / VersionStorePageSize;

            // set the size of max transaction log size (in bytes) that should be replayed after the crash
            // small values: smaller log files but potentially longer transaction flushes if there was a crash (6M)
            instance.Parameters.CheckpointDepthMax = 6 * 1024 * 1024;

            // how much db grows when it finds db is full (1M)
            // (less I/O as value gets bigger)
            instance.Parameters.DbExtensionSize = 1024 * 1024 / DatabasePageSize;

            // fail fast if log file is wrong. we will recover from it by creating db from scratch
            instance.Parameters.CleanupMismatchedLogFiles = true;
            instance.Parameters.EnableIndexChecking = true;

            // now, actually initialize instance
            instance.Init();

            return instance;
        }

        private void TryInitializeGlobalParameters()
        {
            int instances;
            JET_INSTANCE_INFO[] infos;
            Api.JetGetInstanceInfo(out instances, out infos);

            // already initialized nothing we can do.
            if (instances != 0)
            {
                return;
            }

            try
            {
                // use small configuration so that esent use process heap and windows file cache
                SystemParameters.Configuration = 0;

                // allow many esent instances
                SystemParameters.MaxInstances = 1024;

                // enable perf monitor if requested
                Api.JetSetSystemParameter(JET_INSTANCE.Nil, JET_SESID.Nil, (JET_param)JET_paramDisablePerfmon, _enablePerformanceMonitor ? 0 : 1, null);

                // set max IO queue (bigger value better IO perf)
                Api.JetSetSystemParameter(JET_INSTANCE.Nil, JET_SESID.Nil, (JET_param)JET_paramOutstandingIOMax, 1024, null);

                // set max current write to db
                Api.JetSetSystemParameter(JET_INSTANCE.Nil, JET_SESID.Nil, (JET_param)JET_paramCheckpointIOMax, 32, null);

                // better cache management (4M)
                Api.JetSetSystemParameter(JET_INSTANCE.Nil, JET_SESID.Nil, (JET_param)JET_paramLRUKHistoryMax, 4 * 1024 * 1024 / DatabasePageSize, null);

                // better db performance (100K bytes)
                Api.JetSetSystemParameter(JET_INSTANCE.Nil, JET_SESID.Nil, (JET_param)JET_paramPageHintCacheSize, 100 * 1024, null);

                // set version page size to normal 16K
                Api.JetSetSystemParameter(JET_INSTANCE.Nil, JET_SESID.Nil, (JET_param)JET_paramVerPageSize, VersionStorePageSize, null);

                // use windows file system cache
                SystemParameters.EnableFileCache = true;

                // don't use mapped file for database. this will waste more VM.
                SystemParameters.EnableViewCache = false;

                // this is the unit where chunks are loaded into memory/locked and etc
                SystemParameters.DatabasePageSize = DatabasePageSize;

                // set max cache size - don't use too much memory for cache (8MB)
                SystemParameters.CacheSizeMax = 8 * 1024 * 1024 / DatabasePageSize;

                // set min cache size - Esent tries to adjust this value automatically but often it is better to help him.
                // small cache sizes => more I\O during random seeks
                // currently set to 2MB
                SystemParameters.CacheSizeMin = 2 * 1024 * 1024 / DatabasePageSize;

                // set box of when cache eviction starts (1% - 2% of max cache size)
                SystemParameters.StartFlushThreshold = 20;
                SystemParameters.StopFlushThreshold = 40;
            }
            catch (EsentAlreadyInitializedException)
            {
                // can't change global status
            }
        }

        private void InitializeDatabaseAndTables()
        {
            // open database for the first time: database file will be created if necessary

            // first quick check whether file exist
            if (!File.Exists(_databaseFile))
            {
                Api.JetCreateDatabase(_primarySessionId, _databaseFile, null, out _primaryDatabaseId, CreateDatabaseGrbit.None);
                CreateTables();
                return;
            }

            // file exist, just attach the db.
            try
            {
                // if this succeed, it will lock the file.
                Api.JetAttachDatabase(_primarySessionId, _databaseFile, AttachDatabaseGrbit.None);
            }
            catch (EsentFileNotFoundException)
            {
                // if someone has deleted the file, while we are attaching.
                Api.JetCreateDatabase(_primarySessionId, _databaseFile, null, out _primaryDatabaseId, CreateDatabaseGrbit.None);
                CreateTables();
                return;
            }

            Api.JetOpenDatabase(_primarySessionId, _databaseFile, null, out _primaryDatabaseId, OpenDatabaseGrbit.None);
            InitializeTables();
        }

        private void CreateTables()
        {
            foreach (var table in _tables.Values)
            {
                table.Create(_primarySessionId, _primaryDatabaseId);
            }
        }

        private void InitializeTables()
        {
            foreach (var table in _tables.Values)
            {
                table.Initialize(_primarySessionId, _primaryDatabaseId);
            }
        }
    }
}
