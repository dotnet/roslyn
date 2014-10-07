using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host.Esent
{
    internal partial class EsentKeyValueStorage
    {
        /// <summary>
        /// Options for <see cref="NativeMethods.JetOpenDatabaseW(IntPtr, string, string, out uint, uint)"/>.
        /// </summary>
        [Flags]
        private enum OpenDatabaseGrbit
        {
            /// <summary>
            /// Default options.
            /// </summary>
            None = 0,

            /// <summary>
            /// Prevents modifications to the database.
            /// </summary>
            ReadOnly = 0x1,

            /// <summary>
            /// Allows only a single session to attach a database.
            /// Normally, several sessions can open a database.
            /// </summary>
            Exclusive = 0x2,
        }

        /// <summary>
        /// Options for JetOpenTable.
        /// </summary>
        [Flags]
        private enum OpenTableGrbit
        {
            /// <summary>
            /// Default options.
            /// </summary>
            None = 0,

            /// <summary>
            /// This table cannot be opened for write access by another session.
            /// </summary>
            DenyWrite = 0x1,

            /// <summary>
            /// This table cannot be opened for read access by another session.
            /// </summary>
            DenyRead = 0x2,

            /// <summary>
            /// Request read-only access to the table.
            /// </summary>
            ReadOnly = 0x4,

            /// <summary>
            /// Request write access to the table.
            /// </summary>
            Updatable = 0x8,

            /// <summary>
            /// Allow DDL modifications to a table flagged as FixedDDL. This option
            /// must be used with DenyRead.
            /// </summary>
            PermitDDL = 0x10,

            /// <summary>
            /// Do not cache pages for this table.
            /// </summary>
            NoCache = 0x20,

            /// <summary>
            /// Provides a hint that the table is probably not in the buffer cache, and
            /// that pre-reading may be beneficial to performance.
            /// </summary>
            Preread = 0x40,

            /// <summary>
            /// Assume a sequential access pattern and prefetch database pages.
            /// </summary>
            Sequential = 0x8000,

            /// <summary>
            /// Table belongs to stats class 1.
            /// </summary>
            TableClass1 = 0x00010000,

            /// <summary>
            /// Table belongs to stats class 2.
            /// </summary>
            TableClass2 = 0x00020000,

            /// <summary>
            /// Table belongs to stats class 3.
            /// </summary>
            TableClass3 = 0x00030000,

            /// <summary>
            /// Table belongs to stats class 4.
            /// </summary>
            TableClass4 = 0x00040000,

            /// <summary>
            /// Table belongs to stats class 5.
            /// </summary>
            TableClass5 = 0x00050000,

            /// <summary>
            /// Table belongs to stats class 6.
            /// </summary>
            TableClass6 = 0x00060000,

            /// <summary>
            /// Table belongs to stats class 7.
            /// </summary>
            TableClass7 = 0x00070000,

            /// <summary>
            /// Table belongs to stats class 8.
            /// </summary>
            TableClass8 = 0x00080000,

            /// <summary>
            /// Table belongs to stats class 9.
            /// </summary>
            TableClass9 = 0x00090000,

            /// <summary>
            /// Table belongs to stats class 10.
            /// </summary>
            TableClass10 = 0x000A0000,

            /// <summary>
            /// Table belongs to stats class 11.
            /// </summary>
            TableClass11 = 0x000B0000,

            /// <summary>
            /// Table belongs to stats class 12.
            /// </summary>
            TableClass12 = 0x000C0000,

            /// <summary>
            /// Table belongs to stats class 13.
            /// </summary>
            TableClass13 = 0x000D0000,

            /// <summary>
            /// Table belongs to stats class 14.
            /// </summary>
            TableClass14 = 0x000E0000,

            /// <summary>
            /// Table belongs to stats class 15.
            /// </summary>
            TableClass15 = 0x000F0000,
        }

        /// <summary>
        /// Codepage for an ESENT column.
        /// </summary>
        private enum JetCodePage
        {
            /// <summary>
            /// Code page for non-text columns.
            /// </summary>
            None = 0,

            /// <summary>
            /// Unicode encoding.
            /// </summary>
            Unicode = 1200,

            /// <summary>
            /// ASCII encoding.
            /// </summary>
            ASCII = 1252,
        }

        /// <summary>
        /// Options for JetAttachDatabase.
        /// </summary>
        [Flags]
        private enum AttachDatabaseGrbit
        {
            /// <summary>
            /// Default options.
            /// </summary>
            None = 0,

            /// <summary>
            ///  Prevents modifications to the database.
            /// </summary>
            ReadOnly = 0x1,

            /// <summary>
            /// If JET_paramEnableIndexChecking has been set, all indexes over Unicode
            /// data will be deleted.
            /// </summary>
            DeleteCorruptIndexes = 0x10,
        }

        [Flags]
        private enum ColumndefGrbit
        {
            /// <summary>
            /// Default options.
            /// </summary>
            None = 0x0,

            /// <summary>
            /// The column will be fixed. It will always use the same amount of space in a row,
            /// regardless of how much data is being stored in the column. ColumnFixed
            /// cannot be used with ColumnTagged. This bit cannot be used with long values
            /// (that is JET_coltyp.LongText and JET_coltyp.LongBinary).
            /// </summary>
            ColumnFixed = 0x1,

            /// <summary>
            ///  The column will be tagged. Tagged columns do not take up any space in the database
            ///  if they do not contain data. This bit cannot be used with ColumnFixed.
            /// </summary>
            ColumnTagged = 0x2,

            /// <summary>
            /// The column must never be set to a NULL value. On Windows XP this can only be applied to
            /// fixed columns (bit, byte, integer, etc).
            /// </summary>
            ColumnNotNULL = 0x4,

            /// <summary>
            /// The column is a version column that specifies the version of the row. The value of
            /// this column starts at zero and will be automatically incremented for each update on
            /// the row. This option can only be applied to JET_coltyp.Long columns. This option cannot
            /// be used with ColumnAutoincrement, ColumnEscrowUpdate, or ColumnTagged.
            /// </summary>
            ColumnVersion = 0x8,

            /// <summary>
            /// The column will automatically be incremented. The number is an increasing number, and
            /// is guaranteed to be unique within a table. The numbers, however, might not be continuous.
            /// For example, if five rows are inserted into a table, the "autoincrement" column could
            /// contain the values { 1, 2, 6, 7, 8 }. This bit can only be used on columns of type
            /// JET_coltyp.Long or JET_coltyp.Currency.
            /// </summary>
            ColumnAutoincrement = 0x10,

            /// <summary>
            /// The column can be multi-valued.
            /// A multi-valued column can have zero, one, or more values
            /// associated with it. The various values in a multi-valued column are identified by a number
            /// called the itagSequence member, which belongs to various structures, including:
            /// JET_RETINFO, JET_SETINFO, JET_SETCOLUMN, JET_RETRIEVECOLUMN, and JET_ENUMCOLUMNVALUE.
            /// Multi-valued columns must be tagged columns; that is, they cannot be fixed-length or
            /// variable-length columns.
            /// </summary>
            ColumnMultiValued = 0x400,

            /// <summary>
            ///  Specifies that a column is an escrow update column. An escrow update column can be
            ///  updated concurrently by different sessions with JetEscrowUpdate and will maintain
            ///  transactional consistency. An escrow update column must also meet the following conditions:
            ///  An escrow update column can be created only when the table is empty. 
            ///  An escrow update column must be of type JET_coltypLong. 
            ///  An escrow update column must have a default value.
            ///  JET_bitColumnEscrowUpdate cannot be used in conjunction with ColumnTagged,
            ///  ColumnVersion, or ColumnAutoincrement. 
            /// </summary>
            ColumnEscrowUpdate = 0x800,

            /// <summary>
            /// The column will be created in an without version information. This means that other
            /// transactions that attempt to add a column with the same name will fail. This bit
            /// is only useful with JetAddColumn. It cannot be used within a transaction.
            /// </summary>
            ColumnUnversioned = 0x1000,

            /// <summary>
            /// In doing an outer join, the retrieve column operation might not have a match
            /// from the inner table.
            /// </summary>
            ColumnMaybeNull = 0x2000,

            /// <summary>
            /// The default value for a column will be provided by a callback function. A column that
            /// has a user-defined default must be a tagged column. Specifying JET_bitColumnUserDefinedDefault
            /// means that pvDefault must point to a JET_USERDEFINEDDEFAULT structure, and cbDefault must be
            /// set to sizeof( JET_USERDEFINEDDEFAULT ).
            /// </summary>
            ColumnUserDefinedDefault = 0x8000,

            /// <summary>
            /// The column will be a key column for the temporary table. The order
            /// of the column definitions with this option specified in the input
            /// array will determine the precedence of each key column for the
            /// temporary table. The first column definition in the array that
            /// has this option set will be the most significant key column and
            /// so on. If more key columns are requested than can be supported
            /// by the database engine then this option is ignored for the
            /// unsupportable key columns.
            /// </summary>
            TTKey = 0x40,

            /// <summary>
            /// The sort order of the key column for the temporary table should
            /// be descending rather than ascending. If this option is specified
            ///  without <see cref="TTKey"/> then this option is ignored.
            /// </summary>
            TTDescending = 0x80,

            /// <summary>
            /// Compress data in the column, if possible.
            /// </summary>
            ColumnCompressed = 0x80000,
        }

        /// <summary>
        /// ESENT column types.
        /// </summary>
        private enum JetColumnType
        {
            /// <summary>
            /// Null column type. Invalid for column creation.
            /// </summary>
            Nil = 0,

            /// <summary>
            /// True, False or NULL.
            /// </summary>
            Bit = 1,

            /// <summary>
            /// 1-byte integer, unsigned.
            /// </summary>
            UnsignedByte = 2,

            /// <summary>
            /// 2-byte integer, signed.
            /// </summary>
            Short = 3,

            /// <summary>
            /// 4-byte integer, signed.
            /// </summary>
            Long = 4,

            /// <summary>
            /// 8-byte integer, signed.
            /// </summary>
            Currency = 5,

            /// <summary>
            /// 4-byte IEEE single-precisions.
            /// </summary>
            IEEESingle = 6,

            /// <summary>
            /// 8-byte IEEE double-precision.
            /// </summary>
            IEEEDouble = 7,

            /// <summary>
            /// Integral date, fractional time.
            /// </summary>
            DateTime = 8,

            /// <summary>
            /// Binary data, up to 255 bytes.
            /// </summary>
            Binary = 9,

            /// <summary>
            /// Text data, up to 255 bytes.
            /// </summary>
            Text = 10,

            /// <summary>
            /// Binary data, up to 2GB.
            /// </summary>
            LongBinary = 11,

            /// <summary>
            /// Text data, up to 2GB.
            /// </summary>
            LongText = 12,
        }

        /// <summary>
        /// ESENT system parameters.
        /// </summary>
        private enum JetParam
        {
            /// <summary>
            /// This parameter indicates the relative or absolute file system path of the
            /// folder that will contain the checkpoint file for the instance. The path
            /// must be terminated with a backslash character, which indicates that the
            /// target path is a folder. 
            /// </summary>
            SystemPath = 0,

            /// <summary>
            /// This parameter indicates the relative or absolute file system path of
            /// the folder or file that will contain the temporary database for the instance.
            /// If the path is to a folder that will contain the temporary database then it
            /// must be terminated with a backslash character.
            /// </summary>
            TempPath = 1,

            /// <summary>
            /// This parameter indicates the relative or absolute file system path of the
            /// folder that will contain the transaction logs for the instance. The path must
            /// be terminated with a backslash character, which indicates that the target path
            /// is a folder.
            /// </summary>
            LogFilePath = 2,

            /// <summary>
            /// This parameter sets the three letter prefix used for many of the files used by
            /// the database engine. For example, the checkpoint file is called EDB.CHK by
            /// default because EDB is the default base name.
            /// </summary>
            BaseName = 3,

            /// <summary>
            /// This parameter supplies an application specific string that will be added to
            /// any event log messages that are emitted by the database engine. This allows
            /// easy correlation of event log messages with the source application. By default
            /// the host application executable name will be used.
            /// </summary>
            EventSource = 4,

            /// <summary>
            /// This parameter reserves the requested number of session resources for use by an
            /// instance. A session resource directly corresponds to a JET_SESID data type.
            /// This setting will affect how many sessions can be used at the same time.
            /// </summary>
            MaxSessions = 5,

            /// <summary>
            /// This parameter reserves the requested number of B+ Tree resources for use by
            /// an instance. This setting will affect how many tables can be used at the same time.
            /// </summary>
            MaxOpenTables = 6,

            // PreferredMaxOpenTables(7) is obsolete

            /// <summary>
            /// This parameter reserves the requested number of cursor resources for use by an
            /// instance. A cursor resource directly corresponds to a JET_TABLEID data type.
            /// This setting will affect how many cursors can be used at the same time. A cursor
            /// resource cannot be shared by different sessions so this parameter must be set to
            /// a large enough value so that each session can use as many cursors as are required.
            /// </summary>
            MaxCursors = 8,

            /// <summary>
            /// This parameter reserves the requested number of version store pages for use by an instance.
            /// </summary>
            MaxVerPages = 9,

            /// <summary>
            /// This parameter reserves the requested number of temporary table resources for use
            /// by an instance. This setting will affect how many temporary tables can be used at
            /// the same time. If this system parameter is set to zero then no temporary database
            /// will be created and any activity that requires use of the temporary database will
            /// fail. This setting can be useful to avoid the I/O required to create the temporary
            /// database if it is known that it will not be used.
            /// </summary>
            /// <remarks>
            /// The use of a temporary table also requires a cursor resource.
            /// </remarks>
            MaxTemporaryTables = 10,

            /// <summary>
            /// This parameter will configure the size of the transaction log files. Each
            /// transaction log file is a fixed size. The size is equal to the setting of
            /// this system parameter in units of 1024 bytes.
            /// </summary>
            LogFileSize = 11,

            /// <summary>
            /// This parameter will configure the amount of memory used to cache log records
            /// before they are written to the transaction log file. The unit for this
            /// parameter is the sector size of the volume that holds the transaction log files.
            /// The sector size is almost always 512 bytes, so it is safe to assume that size
            /// for the unit. This parameter has an impact on performance. When the database
            /// engine is under heavy update load, this buffer can become full very rapidly.
            /// A larger cache size for the transaction log file is critical for good update
            /// performance under such a high load condition. The default is known to be too small
            /// for this case.
            /// Do not set this parameter to a number of buffers that is larger (in bytes) than
            /// half the size of a transaction log file.
            /// </summary>
            LogBuffers = 12,

            /// <summary>
            /// This parameter configures how transaction log files are managed by the database
            /// engine. When circular logging is off, all transaction log files that are generated
            /// are retained on disk until they are no longer needed because a full backup of the
            /// database has been performed. When circular logging is on, only transaction log files
            /// that are younger than the current checkpoint are retained on disk. The benefit of
            /// this mode is that backups are not required to retire old transaction log files. 
            /// </summary>
            CircularLog = 17,

            /// <summary>
            /// This parameter controls the amount of space that is added to a database file each
            /// time it needs to grow to accommodate more data. The size is in database pages.
            /// </summary>
            DbExtensionSize = 18,

            /// <summary>
            /// This parameter controls the initial size of the temporary database. The size is in
            /// database pages. A size of zero indicates that the default size of an ordinary
            /// database should be used. It is often desirable for small applications to configure
            /// the temporary database to be as small as possible. Setting this parameter to
            /// SystemParameters.PageTempDBSmallest will achieve the smallest temporary database possible.
            /// </summary>
            PageTempDBMin = 19,

            /// <summary>
            /// This parameter configures the maximum size of the database page cache. The size
            /// is in database pages. If this parameter is left to its default value, then the
            /// maximum size of the cache will be set to the size of physical memory when JetInit
            /// is called.
            /// </summary>
            CacheSizeMax = 23,

            /// <summary>
            /// This parameter controls how aggressively database pages are flushed from the
            /// database page cache to minimize the amount of time it will take to recover from a
            /// crash. The parameter is a threshold in bytes for about how many transaction log
            /// files will need to be replayed after a crash. If circular logging is enabled using
            /// JET_param.CircularLog then this parameter will also control the approximate amount
            /// of transaction log files that will be retained on disk.
            /// </summary>
            CheckpointDepthMax = 24,

            /// <summary>
            /// This parameter controls when the database page cache begins evicting pages from the
            /// cache to make room for pages that are not cached. When the number of page buffers in the cache
            /// drops below this threshold then a background process will be started to replenish that pool
            /// of available buffers. This threshold is always relative to the maximum cache size as set by
            /// JET_paramCacheSizeMax. This threshold must also always be less than the stop threshold as
            /// set by JET_paramStopFlushThreshold.
            /// <para>
            /// The distance height of the start threshold will determine the response time that the database
            ///  page cache must have to produce available buffers before the application needs them. A high
            /// start threshold will give the background process more time to react. However, a high start
            /// threshold implies a higher stop threshold and that will reduce the effective size of the
            /// database page cache for modified pages (Windows 2000) or for all pages (Windows XP and later).
            /// </para>
            /// </summary>
            StartFlushThreshold = 31,

            /// <summary>
            /// This parameter controls when the database page cache ends evicting pages from the cache to make
            /// room for pages that are not cached. When the number of page buffers in the cache rises above
            /// this threshold then the background process that was started to replenish that pool of available
            /// buffers is stopped. This threshold is always relative to the maximum cache size as set by
            /// JET_paramCacheSizeMax. This threshold must also always be greater than the start threshold
            /// as set by JET_paramStartFlushThreshold.
            /// <para>
            /// The distance between the start threshold and the stop threshold affects the efficiency with
            /// which database pages are flushed by the background process. A larger gap will make it
            /// more likely that writes to neighboring pages may be combined. However, a high stop
            /// threshold will reduce the effective size of the database page cache for modified
            /// pages (Windows 2000) or for all pages (Windows XP and later).
            /// </para>
            /// </summary>
            StopFlushThreshold = 32,

            /// <summary>
            /// This parameter is the master switch that controls crash recovery for an instance.
            /// If this parameter is set to "On" then ARIES style recovery will be used to bring all
            /// databases in the instance to a consistent state in the event of a process or machine
            /// crash. If this parameter is set to "Off" then all databases in the instance will be
            /// managed without the benefit of crash recovery. That is to say, that if the instance
            /// is not shut down cleanly using JetTerm prior to the process exiting or machine shutdown
            /// then the contents of all databases in that instance will be corrupted.
            /// </summary>
            Recovery = 34,

            /// <summary>
            /// This parameter controls the behavior of online defragmentation when initiated using
            /// JetDefragment and JetDefragment2.
            /// </summary>
            EnableOnlineDefrag = 35,

            /// <summary>
            /// This parameter can be used to control the size of the database page cache at run time.
            /// Ordinarily, the cache will automatically tune its size as a function of database and
            /// machine activity levels. If the application sets this parameter to zero, then the cache
            /// will tune its own size in this manner. However, if the application sets this parameter
            /// to a non-zero value then the cache will adjust itself to that target size.
            /// </summary>
            CacheSize = 41,

            /// <summary>
            /// When this parameter is true, every database is checked at JetAttachDatabase time for
            /// indexes over Unicode key columns that were built using an older version of the NLS
            /// library in the operating system. This must be done because the database engine persists
            /// the sort keys generated by LCMapStringW and the value of these sort keys change from release to release.
            /// If a primary index is detected to be in this state then JetAttachDatabase will always fail with
            /// JET_err.PrimaryIndexCorrupted.
            /// If any secondary indexes are detected to be in this state then there are two possible outcomes.
            /// If AttachDatabaseGrbit.DeleteCorruptIndexes was passed to JetAttachDatabase then these indexes
            /// will be deleted and JET_wrnCorruptIndexDeleted will be returned from JetAttachDatabase. These
            /// indexes will need to be recreated by your application. If AttachDatabaseGrbit.DeleteCorruptIndexes
            /// was not passed to JetAttachDatabase then the call will fail with JET_errSecondaryIndexCorrupted.
            /// </summary>
            EnableIndexChecking = 45,

            /// <summary>
            /// This parameter can be used to control which event log the database engine uses for its event log
            /// messages. By default, all event log messages will go to the Application event log. If the registry
            /// key name for another event log is configured then the event log messages will go there instead.
            /// </summary>        
            EventSourceKey = 49,

            /// <summary>
            /// When this parameter is true, informational event log messages that would ordinarily be generated by
            /// the database engine will be suppressed.
            /// </summary>
            NoInformationEvent = 50,

            /// <summary>
            /// Configures the detail level of event log messages that are emitted
            /// to the event log by the database engine. Higher numbers will result
            /// in more detailed event log messages.
            /// </summary>
            EventLoggingLevel = 51,

            /// <summary>
            /// Delete the log files that are not matching (generation wise) during soft recovery.
            /// </summary>
            DeleteOutOfRangeLogs = 52,

            /// <summary>
            /// This parameter configures the minimum size of the database page cache. The size is in database pages.
            /// </summary>
            CacheSizeMin = 60,

            /// <summary>
            /// This parameter represents a threshold relative to <see cref="JetParam.MaxVerPages"/> that controls
            /// the discretionary use of version pages by the database engine. If the size of the version store exceeds
            /// this threshold then any information that is only used for optional background tasks, such as reclaiming
            /// deleted space in the database, is instead sacrificed to preserve room for transactional information.
            /// </summary>
            PreferredVerPages = 63,

            /// <summary>
            /// This parameter configures the page size for the database. The page
            /// size is the smallest unit of space allocation possible for a database
            /// file. The database page size is also very important because it sets
            /// the upper limit on the size of an individual record in the database. 
            /// </summary>
            /// <remarks>
            /// Only one database page size is supported per process at this time.
            /// This means that if you are in a single process that contains different
            /// applications that use the database engine then they must all agree on
            /// a database page size.
            /// </remarks>
            DatabasePageSize = 64,

            /// <summary>
            /// This parameter can be used to convert a JET_ERR into a string.
            /// This should only be used with JetGetSystemParameter.
            /// </summary>
            ErrorToString = 70,

            /// <summary>
            /// Configures the engine with a JET_CALLBACK delegate.
            /// This callback may be called for the following reasons:
            /// JET_cbtyp.FreeCursorLS, JET_cbtyp.FreeTableLS
            /// or JET_cbtyp.Null. See JetSetLS"/>
            /// for more information. This parameter cannot currently be retrieved.
            /// </summary>
            RuntimeCallback = 73,

            /// <summary>
            /// This parameter controls the outcome of JetInit when the database
            /// engine is configured to start using transaction log files on disk
            /// that are of a different size than what is configured. Normally,
            /// <see cref="NativeMethods.JetInit(ref IntPtr)"/> will successfully recover the databases
            /// but will fail with 'LogFileSizeMismatchDatabasesConsistent'
            /// to indicate that the log file size is misconfigured. However, when
            /// this parameter is set to true then the database engine will silently
            /// delete all the old log files, start a new set of transaction log files
            /// using the configured log file size. This parameter is useful when the
            /// application wishes to transparently change its transaction log file
            /// size yet still work transparently in upgrade and restore scenarios.
            /// </summary>
            CleanupMismatchedLogFiles = 77,

            /// <summary>
            /// This parameter controls what happens when an exception is thrown by the 
            /// database engine or code that is called by the database engine. When set 
            /// to JET_ExceptionMsgBox, any exception will be thrown to the Windows unhandled 
            /// exception filter. This will result in the exception being handled as an 
            /// application failure. The intent is to prevent application code from erroneously 
            /// trying to catch and ignore an exception generated by the database engine. 
            /// This cannot be allowed because database corruption could occur. If the application 
            /// wishes to properly handle these exceptions then the protection can be disabled 
            /// by setting this parameter to JET_ExceptionNone.
            /// </summary>
            ExceptionAction = 98,

            /// <summary>
            /// When this parameter is set to true then any folder that is missing in a file system path in use by
            /// the database engine will be silently created. Otherwise, the operation that uses the missing file system
            /// path will fail with JET_err.InvalidPath.
            /// </summary>
            CreatePathIfNotExist = 100,

            /// <summary>
            /// When this parameter is true then only one database is allowed to
            /// be opened using JetOpenDatabase by a given session at one time.
            /// The temporary database is excluded from this restriction. 
            /// </summary>
            OneDatabasePerSession = 102,

            /// <summary>
            /// This parameter controls the maximum number of instances that can be created in a single process.
            /// </summary>
            MaxInstances = 104,

            /// <summary>
            /// This parameter controls the number of background cleanup work items that
            /// can be queued to the database engine thread pool at any one time.
            /// </summary>
            VersionStoreTaskQueueMax = 105,
        }

        /// <summary>
        /// Options for <see cref="NativeMethods.JetCreateDatabaseW(IntPtr, string, string, out uint, uint)"/>.
        /// </summary>
        [Flags]
        private enum CreateDatabaseGrbit
        {
            /// <summary>
            /// Default options.
            /// </summary>
            None = 0,

            /// <summary>
            /// By default, if JetCreateDatabase is called and the database already exists,
            /// the Api call will fail and the original database will not be overwritten.
            /// OverwriteExisting changes this behavior, and the old database
            /// will be overwritten with a new one.
            /// </summary>
            OverwriteExisting = 0x200,

            /// <summary>
            /// Turns off logging. Setting this bit loses the ability to replay log files
            /// and recover the database to a consistent usable state after a crash.
            /// </summary>
            RecoveryOff = 0x8,
        }

        /// <summary>
        /// Options for JetCommitTransaction.
        /// </summary>
        [Flags]
        private enum CommitTransactionGrbit
        {
            /// <summary>
            /// Default options.
            /// </summary>
            None = 0,

            /// <summary>
            /// The transaction is committed normally but this Api does not wait for
            /// the transaction to be flushed to the transaction log file before returning
            /// to the caller. This drastically reduces the duration of a commit operation
            /// at the cost of durability. Any transaction that is not flushed to the log
            /// before a crash will be automatically aborted during crash recovery during
            /// the next call to JetInit. If WaitLastLevel0Commit or WaitAllLevel0Commit
            /// are specified, this option is ignored.
            /// </summary>
            LazyFlush = 0x1,

            /// <summary>
            ///  If the session has previously committed any transactions and they have not yet
            ///  been flushed to the transaction log file, they should be flushed immediately.
            ///  This Api will wait until the transactions have been flushed before returning
            ///  to the caller. This is useful if the application has previously committed several
            ///  transactions using JET_bitCommitLazyFlush and now wants to flush all of them to disk.
            /// </summary>
            /// <remarks>
            /// This option may be used even if the session is not currently in a transaction.
            /// This option cannot be used in combination with any other option.
            /// </remarks>
            WaitLastLevel0Commit = 0x2,
        }

        /// <summary>
        /// Options for JetCreateIndex.
        /// </summary>
        [Flags]
        private enum CreateIndexGrbit
        {
            /// <summary>
            /// Default options.
            /// </summary>
            None = 0x0,

            /// <summary>
            /// Duplicate index entries (keys) are disallowed. This is enforced when JetUpdate is called,
            /// not when JetSetColumn is called.
            /// </summary>
            IndexUnique = 0x1,

            /// <summary>
            /// The index is a primary (clustered) index. Every table must have exactly one primary index.
            /// If no primary index is explicitly defined over a table, then the database engine will
            /// create its own primary index.
            /// </summary>
            IndexPrimary = 0x2,

            /// <summary>
            /// None of the columns over which the index is created may contain a NULL value.
            /// </summary>
            IndexDisallowNull = 0x4,

            /// <summary>
            /// Do not add an index entry for a row if all of the columns being indexed are NULL.
            /// </summary>
            IndexIgnoreNull = 0x8,

            /// <summary>
            /// Do not add an index entry for a row if any of the columns being indexed are NULL.
            /// </summary>
            IndexIgnoreAnyNull = 0x20,

            /// <summary>
            /// Do not add an index entry for a row if the first column being indexed is NULL.
            /// </summary>
            IndexIgnoreFirstNull = 0x40,

            /// <summary>
            /// Specifies that the index operations will be logged lazily. JET_bitIndexLazyFlush does not
            /// affect the laziness of data updates. If the indexing operations is interrupted by process
            /// termination, Soft Recovery will still be able to able to get the database to a consistent
            /// state, but the index may not be present.
            /// </summary>
            IndexLazyFlush = 0x80,

            /// <summary>
            /// Do not attempt to build the index, because all entries would evaluate to NULL. grbit MUST
            /// also specify JET_bitIgnoreAnyNull when JET_bitIndexEmpty is passed. This is a performance
            /// enhancement. For example if a new column is added to a table, then an index is created over
            /// this newly added column, all of the records in the table would be scanned even though they
            /// would never get added to the index anyway. Specifying JET_bitIndexEmpty skips the scanning
            /// of the table, which could potentially take a long time.
            /// </summary>
            IndexEmpty = 0x100,

            /// <summary>
            /// Causes index creation to be visible to other transactions. Normally a session in a
            /// transaction will not be able to see an index creation operation in another session. This
            /// flag can be useful if another transaction is likely to create the same index, so that the
            /// second index-create will simply fail instead of potentially causing many unnecessary database
            /// operations. The second transaction may not be able to use the index immediately. The index
            /// creation operation needs to complete before it is usable. The session must not currently be in
            /// a transaction to create an index without version information.
            /// </summary>
            IndexUnversioned = 0x200,

            /// <summary>
            /// Specifying this flag causes NULL values to be sorted after data for all columns in the index.
            /// </summary>
            IndexSortNullsHigh = 0x400,

            /// <summary>
            /// Specifying this flag will cause any update to the index that would result in a truncated key to fail with JET_errKeyTruncated. 
            /// Otherwise, keys will be silently truncated. For more information on key truncation, see the JetMakeKey function.
            /// </summary>
            IndexDisallowTruncation = 0x00010000,

            /// <summary>
            /// Specifying this flag will cause the index to use the maximum key size specified in the cbKeyMost field in the structure. 
            /// Otherwise, the index will use JET_cbKeyMost (255) as its maximum key size.        
            /// JET_bitIndexKeyMost was introduced in Windows Vista.
            /// </summary>
            IndexKeyMost = 0x8000,
        }

        /// <summary>
        /// Update types for JetPrepareUpdate.
        /// </summary>
        private enum JetPrepareUpdateFlags
        {
            /// <summary>
            ///  This flag causes the cursor to prepare for an insert of a new record.
            ///  All the data is initialized to the default state for the record.
            ///  If the table has an auto-increment column, then a new value is
            ///  assigned to this record regardless of whether the update ultimately
            ///  succeeds, fails or is cancelled.
            /// </summary>
            Insert = 0,

            /// <summary>
            ///  This flag causes the cursor to prepare for a replace of the current
            ///  record. If the table has a version column, then the version column
            ///  is set to the next value in its sequence. If this update does not
            ///  complete, then the version value in the record will be unaffected.
            ///  An update lock is taken on the record to prevent other sessions
            ///  from updating this record before this session completes.
            /// </summary>
            Replace = 2,

            /// <summary>
            ///  This flag causes JetPrepareUpdate to cancel the update for this cursor.
            /// </summary>
            Cancel = 3,

            /// <summary>
            ///  This flag is similar to JET_prepReplace, but no lock is taken to prevent
            ///  other sessions from updating this record. Instead, this session may receive
            ///  JET_errWriteConflict when it calls JetUpdate to complete the update.
            /// </summary>
            ReplaceNoLock = 4,

            /// <summary>
            ///  This flag causes the cursor to prepare for an insert of a copy of the
            ///  existing record. There must be a current record if this option is used.
            ///  The initial state of the new record is copied from the current record.
            ///  Long values that are stored off-record are virtually copied.
            /// </summary>
            InsertCopy = 5,

            /// <summary>
            ///  This flag causes the cursor to prepare for an insert of the same record,
            ///  and a delete or the original record. It is used in cases in which the
            ///  primary key has changed.
            /// </summary>
            InsertCopyDeleteOriginal = 7,
        }

        /// <summary>
        /// Options for JetSetColumn.
        /// </summary>
        [Flags]
        private enum SetColumnGrbit
        {
            /// <summary>
            /// Default options.
            /// </summary>
            None = 0,

            /// <summary>
            /// This option is used to append data to a column of type JET_coltypLongText
            /// or JET_coltypLongBinary. The same behavior can be achieved by determining
            /// the size of the existing long value and specifying ibLongValue in psetinfo.
            /// However, its simpler to use this grbit since knowing the size of the existing
            /// column value is not necessary.
            /// </summary>
            AppendLV = 0x1,

            /// <summary>
            /// This option is used replace the existing long value with the newly provided
            /// data. When this option is used, it is as though the existing long value has
            /// been set to 0 (zero) length prior to setting the new data.
            /// </summary>
            OverwriteLV = 0x4,

            /// <summary>
            /// This option is only applicable for tagged, sparse or multi-valued columns.
            /// It causes the column to return the default column value on subsequent retrieve
            /// column operations. All existing column values are removed.
            /// </summary>
            RevertToDefaultValue = 0x200,

            /// <summary>
            /// This option is used to force a long value, columns of type JET_coltyp.LongText
            /// or JET_coltyp.LongBinary, to be stored separately from the remainder of record
            /// data. This occurs normally when the size of the long value prevents it from being 
            /// stored with remaining record data. However, this option can be used to force the
            /// long value to be stored separately. Note that long values four bytes in size
            /// of smaller cannot be forced to be separate. In such cases, the option is ignored.
            /// </summary>
            SeparateLV = 0x40,

            /// <summary>
            /// This option is used to interpret the input buffer as a integer number of bytes
            /// to set as the length of the long value described by the given columnid and if
            /// provided, the sequence number in psetinfo->itagSequence. If the size given is
            /// larger than the existing column value, the column will be extended with 0s.
            /// If the size is smaller than the existing column value then the value will be
            /// truncated.
            /// </summary>
            SizeLV = 0x8,

            /// <summary>
            /// This option is used to enforce that all values in a multi-valued column are
            /// distinct. This option compares the source column data, without any
            /// transformations, to other existing column values and an error is returned
            /// if a duplicate is found. If this option is given, then AppendLV, OverwriteLV
            /// and SizeLV cannot also be given.
            /// </summary>
            UniqueMultiValues = 0x80,

            /// <summary>
            /// This option is used to enforce that all values in a multi-valued column are
            /// distinct. This option compares the key normalized transformation of column
            /// data, to other similarly transformed existing column values and an error is
            /// returned if a duplicate is found. If this option is given, then AppendLV, 
            /// OverwriteLV and SizeLV cannot also be given.
            /// </summary>
            UniqueNormalizedMultiValues = 0x100,

            /// <summary>
            /// This option is used to set a value to zero length. Normally, a column value
            /// is set to NULL by passing a cbMax of 0 (zero). However, for some types, like
            /// JET_coltyp.Text, a column value can be 0 (zero) length instead of NULL, and
            /// this option is used to differentiate between NULL and 0 (zero) length.
            /// </summary>
            ZeroLength = 0x20,

            /// <summary>
            /// Try to store long-value columns in the record, even if they exceed the default
            /// separation size.
            /// </summary>
            IntrinsicLV = 0x400,
        }

        /// <summary>
        /// Options for JetRetrieveColumn.
        /// </summary>
        [Flags]
        private enum RetrieveColumnGrbit
        {
            /// <summary>
            /// Default options.
            /// </summary>
            None = 0,

            /// <summary>
            ///  This flag causes retrieve column to retrieve the modified value instead of
            ///  the original value. If the value has not been modified, then the original
            ///  value is retrieved. In this way, a value that has not yet been inserted or
            ///  updated may be retrieved during the operation of inserting or updating a record.
            /// </summary>
            RetrieveCopy = 0x1,

            /// <summary>
            /// This option is used to retrieve column values from the index, if possible,
            /// without accessing the record. In this way, unnecessary loading of records
            /// can be avoided when needed data is available from index entries themselves.
            /// </summary>
            RetrieveFromIndex = 0x2,

            /// <summary>
            /// This option is used to retrieve column values from the index bookmark,
            /// and may differ from the index value when a column appears both in the
            /// primary index and the current index. This option should not be specified
            /// if the current index is the clustered, or primary, index. This bit cannot
            /// be set if RetrieveFromIndex is also set. 
            /// </summary>
            RetrieveFromPrimaryBookmark = 0x4,

            /// <summary>
            /// This option is used to retrieve the sequence number of a multi-valued
            /// column value in JET_RETINFO.itagSequence. Retrieving the sequence number
            /// can be a costly operation and should only be done if necessary. 
            /// </summary>
            RetrieveTag = 0x8,

            /// <summary>
            /// This option is used to retrieve multi-valued column NULL values. If
            /// this option is not specified, multi-valued column NULL values will
            /// automatically be skipped. 
            /// </summary>
            RetrieveNull = 0x10,

            /// <summary>
            /// This option affects only multi-valued columns and causes a NULL
            /// value to be returned when the requested sequence number is 1 and
            /// there are no set values for the column in the record. 
            /// </summary>
            RetrieveIgnoreDefault = 0x20,
        }

        /// <summary>
        /// Options for JetMakeKey.
        /// </summary>
        [Flags]
        private enum MakeKeyGrbit
        {
            /// <summary>
            /// Default options.
            /// </summary>
            None = 0,

            /// <summary>
            /// A new search key should be constructed. Any previously existing search
            /// key is discarded.
            /// </summary>
            NewKey = 0x1,

            /// <summary>
            /// When this option is specified, all other options are ignored, any
            /// previously existing search key is discarded, and the contents of the
            /// input buffer are loaded as the new search key.
            /// </summary>
            NormalizedKey = 0x8,

            /// <summary>
            /// If the size of the input buffer is zero and the current key column
            /// is a variable length column, this option indicates that the input
            /// buffer contains a zero length value. Otherwise, an input buffer size
            /// of zero would indicate a NULL value.
            /// </summary>
            KeyDataZeroLength = 0x10,

            /// <summary>
            /// This option indicates that the search key should be constructed
            /// such that any key columns that come after the current key column
            /// should be considered to be wildcards.
            /// </summary>
            StrLimit = 0x2,

            /// <summary>
            /// This option indicates that the search key should be constructed
            /// such that the current key column is considered to be a prefix
            /// wildcard and that any key columns that come after the current
            /// key column should be considered to be wildcards.
            /// </summary>
            SubStrLimit = 0x4,

            /// <summary>
            /// The search key should be constructed such that any key columns
            /// that come after the current key column should be considered to
            /// be wildcards.
            /// </summary>
            FullColumnStartLimit = 0x100,

            /// <summary>
            /// The search key should be constructed in such a way that any key
            /// columns that come after the current key column are considered to
            /// be wildcards.
            /// </summary>
            FullColumnEndLimit = 0x200,

            /// <summary>
            /// The search key should be constructed such that the current key
            /// column is considered to be a prefix wildcard and that any key
            /// columns that come after the current key column should be considered
            /// to be wildcards. 
            /// </summary>
            PartialColumnStartLimit = 0x400,

            /// <summary>
            /// The search key should be constructed such that the current key
            /// column is considered to be a prefix wildcard and that any key
            /// columns that come after the current key column should be considered
            /// to be wildcards.
            /// </summary>
            PartialColumnEndLimit = 0x800,
        }

        /// <summary>
        /// Options for JetSeek.
        /// </summary>
        [Flags]
        private enum SeekGrbit
        {
            /// <summary>
            /// The cursor will be positioned at the index entry closest to the
            /// start of the index that exactly matches the search key.
            /// </summary>
            SeekEQ = 0x1,

            /// <summary>
            /// The cursor will be positioned at the index entry closest to the
            /// end of the index that is less than an index entry that would
            /// exactly match the search criteria.
            /// </summary>
            SeekLT = 0x2,

            /// <summary>
            /// The cursor will be positioned at the index entry closest to the
            /// end of the index that is less than or equal to an index entry
            /// that would exactly match the search criteria.
            /// </summary>
            SeekLE = 0x4,

            /// <summary>
            /// The cursor will be positioned at the index entry closest to the
            /// start of the index that is greater than or equal to an index
            /// entry that would exactly match the search criteria.
            /// </summary>
            SeekGE = 0x8,

            /// <summary>
            /// The cursor will be positioned at the index entry closest to the
            /// start of the index that is greater than an index entry that
            /// would exactly match the search criteria.
            /// </summary>
            SeekGT = 0x10,

            /// <summary>
            /// An index range will automatically be setup for all keys that
            /// exactly match the search key. 
            /// </summary>
            SetIndexRange = 0x20,
        }

        /// <summary>
        /// Options for JetRollbackTransaction.
        /// </summary>
        [Flags]
        private enum RollbackTransactionGrbit
        {
            /// <summary>
            /// Default options.
            /// </summary>
            None = 0,

            /// <summary>
            /// This option requests that all changes made to the state of the
            /// database during all save points be undone. As a result, the
            /// session will exit the transaction.
            /// </summary>
            RollbackAll = 0x1,
        }

        /// <summary>
        /// Info levels for retrieving column info.
        /// </summary>
        private enum JetColumnInfo
        {
            /// <summary>
            /// Default option. Retrieves a JET_COLUMNDEF.
            /// </summary>
            Default = 0,

            /// <summary>
            /// Retrieves a JET_COLUMNLIST structure, containing all the columns
            /// in the table.
            /// </summary>
            List = 1,

            /// <summary>
            /// Retrieves a JET_COLUMNBASE structure.
            /// </summary>
            Base = 4,

            /// <summary>
            /// Retrieves a JET_COLUMNDEF, the szColumnName argument is interpreted
            /// as a pointer to a columnid.
            /// </summary>
            ByColid = 6,
        }

        /// <summary>
        /// Options for <see cref="NativeMethods.JetTerm2(IntPtr, TermGrbit)"/>.
        /// </summary>
        [Flags]
        internal enum TermGrbit
        {
            /// <summary>
            /// Default options.
            /// </summary>
            None = 0,

            /// <summary>
            /// Requests that the instance be shut down cleanly. Any optional
            /// cleanup work that would ordinarily be done in the background at
            /// run time is completed immediately.
            /// </summary>
            Complete = 1,

            /// <summary>
            /// Requests that the instance be shut down as quickly as possible.
            /// Any optional work that would ordinarily be done in the
            /// background at run time is abandoned. 
            /// </summary>
            Abrupt = 2,

            /// <summary>
            /// Terminate without flushing the database cache.
            /// </summary>
            Dirty = 0x8
        }
    }
}
