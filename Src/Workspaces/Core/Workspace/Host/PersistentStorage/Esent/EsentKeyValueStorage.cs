using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host.Esent
{
    internal partial class EsentKeyValueStorage
    {
        private const int MaxNumberOfSessionsInPool = 5;

        private EsentInstanceHandle esentInstanceHandle;
        private readonly ConcurrentBag<EsentSession> sessionsPool;
        private readonly CancellationTokenSource cancellationTokenSource;

        public EsentKeyValueStorage(string databaseFile)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(databaseFile));

            this.sessionsPool = new ConcurrentBag<EsentSession>();
            this.cancellationTokenSource = new CancellationTokenSource();
            DatabaseFile = databaseFile;

            var instanceDataFolder = Path.GetDirectoryName(databaseFile);

            // set smallest allocation unit for the database
            SetProperty(JetParam.DatabasePageSize, intParam: 4096);

            IntPtr instance = IntPtr.Zero;

            // use database location as instance name/display name
            NativeMethods.JetCreateInstance2W(
                instance: out instance, 
                szDisplayName: databaseFile, 
                szInstanceName: Path.GetFileName(databaseFile), 
                grbit: 0).EnsureSucceeded();

            // enable circular logging - esent will delete transaction logs older than current checkpoint
            SetProperty(JetParam.CircularLog, intParam: 1, instance: instance);

            // set the prefix for all database files
            SetProperty(JetParam.BaseName, stringParam: "ROS", instance: instance);

            // set size of the log file
            SetProperty(JetParam.LogFileSize, intParam: 2 * 1024, instance: instance);

            SetProperty(JetParam.LogFilePath, stringParam: instanceDataFolder, instance: instance);
            SetProperty(JetParam.SystemPath, stringParam: instanceDataFolder, instance: instance);
            SetProperty(JetParam.TempPath, stringParam: instanceDataFolder, instance: instance);

            // disable temporary tables
            SetProperty(JetParam.MaxTemporaryTables, intParam: 0, instance: instance);

            // Esent uses version pages to store intermediate non-committed data during transactions
            // smaller values may cause VersionStoreOutOfMemory error when dealing with multiple transactions\writing lot's of data in transaction or both
            SetProperty(JetParam.MaxVerPages, intParam: 512, instance: instance);

            // set min cache size - Esent tries to adjust this value automatically but often it is better to help him.
            // small cache sizes => more I\O during random seeks
            SetProperty(JetParam.CacheSizeMin, intParam: 4096, instance: instance);

            // set the size of max transaction log size (in bytes) that should be replayed after the crash
            // small values: smaller log files but potentially longer transaction flushes
            SetProperty(JetParam.CheckpointDepthMax, intParam: 5 * 1024 * 1024, instance: instance);

            NativeMethods.JetInit(ref instance).EnsureSucceeded();

            esentInstanceHandle = new EsentInstanceHandle(instance);
            EsentSession session = null;
            try
            {
                // open database for the first time: database file will be created if necessary
                session = GetSession(isFirstSession: true);
            }
            finally
            {
                if (session != null)
                {
                    FreeSession(session);
                }
            }
        }

        public string DatabaseFile { get; private set; }

        public bool IsClosed
        {
            get { return esentInstanceHandle == null; }
        }

        public void Close()
        {
            if (esentInstanceHandle != null)
            {
                var handle = esentInstanceHandle;
                esentInstanceHandle = null;
                try
                {
                    // try to free all allocated session - if succeeded we can try to do a clean shutdown
                    cancellationTokenSource.Cancel();
                }
                catch (Exception)
                {
                    // suppress exceptions from Cancel
                }

                // just close the instance - all associated objects will be closed as well
                handle.Dispose();

                cancellationTokenSource.Dispose();
            }
        }

        public TableAccessor GetTableAccessor(string tableName)
        {
            var session = GetSession(isFirstSession: false);
            return session.GetTableAccessor(tableName);
        }

        private EsentSession GetSession(bool isFirstSession)
        {
            EsentSession session;
            if (sessionsPool.TryTake(out session))
            {
                return session;
            }
            else
            {
                return new EsentSession(this, DatabaseFile, cancellationTokenSource.Token, isFirstSession);
            }
        }

        private void FreeSession(EsentSession session)
        {
            if (sessionsPool.Count < MaxNumberOfSessionsInPool)
            {
                sessionsPool.Add(session);
            }
            else
            {
                session.Dispose();
            }
        }

        private void SetProperty(JetParam parameter, IntPtr instance = default(IntPtr), int intParam = 0, string stringParam = null)
        {
            EsentResult retCode;
            unsafe
            {
                IntPtr* instancePtr = instance == default(IntPtr) ? null : &instance;
                retCode = NativeMethods.JetSetSystemParameterW(
                    instancePtr: instancePtr,
                    sesid: IntPtr.Zero,
                    paramid: (uint)parameter,
                    lParam: new IntPtr(intParam),
                    szParam: stringParam);
            }

            // skip 'AlreadyInitialized' for global properties
            if (retCode.ReturnCode == EsentReturnCode.AlreadyInitialized && instance == default(IntPtr))
            {
                return;
            }
            else
            {
                retCode.EnsureSucceeded();
            }
        }

        private static void EnsureSucceeded(EsentReturnCode returnCode)
        {
            if (returnCode == EsentReturnCode.Success)
            {
                return;
            }
            else
            {
                uint maxChars = 1024;
                uint bytesMax = maxChars * sizeof(char);
                var parameter = (IntPtr)returnCode;
                var sb = new StringBuilder((int)maxChars);
                NativeMethods.JetGetSystemParameterW(
                    instance: IntPtr.Zero, 
                    sesid: IntPtr.Zero, 
                    paramid: (uint)JetParam.ErrorToString, 
                    plParam: ref parameter, 
                    szParam: sb, 
                    cbMax: bytesMax);

                var errorMessage = sb.ToString();
                switch (returnCode)
                {
                    case EsentReturnCode.TermInProgress:
                    case EsentReturnCode.InvalidSesid:
                        throw new EsentInstanceShutdownException(errorMessage);
                    case EsentReturnCode.DatabaseInUse:
                    case EsentReturnCode.FileAccessDenied:
                        throw new EsentDatabaseFileLockedException(errorMessage);
                    default:
                        throw new EsentException(errorMessage);
                }
            }
        }
    }
}
