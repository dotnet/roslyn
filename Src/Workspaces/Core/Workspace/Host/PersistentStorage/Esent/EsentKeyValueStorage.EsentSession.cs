using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.Host.Esent
{
    internal partial class EsentKeyValueStorage
    {
        internal class TableInfo
        {
            public IntPtr TableId { get; private set; }
            public uint KeyColumnId { get; private set; }
            public uint ValueColumnId { get; private set; }

            public TableInfo(IntPtr tableId, uint keyColumnId, uint valueColumnId)
            {
                TableId = tableId;
                KeyColumnId = keyColumnId;
                ValueColumnId = valueColumnId;
            }
        }

        /// <summary>
        /// A session in Esent is the transaction context of the database engine. 
        /// It can be used to begin, commit, or abort transactions that affect the visibility and durability of changes that are made by this or other sessions.
        /// Each session has thread affinity - Esent tracks that transaction associated with session should be started and ended by the same thread .
        /// </summary>
        internal class EsentSession : IDisposable
        {
            private const string SanitizedNames = "SanitizedNames";

            private const string KeyColumnName = "Key";
            private const string ValueColumnName = "Value";

            private readonly string databaseFile;

            private readonly CancellationToken cancellationToken;
            private readonly CancellationTokenRegistration cancellationTokenRegistration;

            private readonly EsentKeyValueStorage storage;
            public IntPtr Session { get; private set; }
            public uint Database { get; private set; }

            private readonly Dictionary<string, TableInfo> cachedTableInfo;
            private readonly TableInfo sanitizedNamesTable;

            private int pendingLazyTransactionsCount;
            private const int FlushThreshold = 10;

            internal EsentSession(EsentKeyValueStorage storage, string databaseFile, CancellationToken cancellationToken, bool isFirstSession)
            {
                this.storage = storage;
                this.databaseFile = databaseFile;
                this.cachedTableInfo = new Dictionary<string, TableInfo>();
                this.cancellationToken = cancellationToken;
                this.cancellationTokenRegistration = cancellationToken.Register(() => Dispose(), useSynchronizationContext: false);

                IntPtr session;
                NativeMethods.JetBeginSession(
                    instance: storage.esentInstanceHandle.DangerousGetHandle(),
                    session: out session,
                    username: null,
                    password: null).EnsureSucceeded();

                Session = session;

                if (!TryOpenExistingDatabase(isFirstSession))
                {
                    if (isFirstSession)
                    {
                        CreateNewDatabase();
                    }
                    else
                    {
                        throw new InvalidOperationException("Database file disappeared?!");
                    }
                }

                // pre-create table that will map invalid table names supplied by user to valid ones
                sanitizedNamesTable = GetOrCreateTable(SanitizedNames);
            }

            internal TableAccessor GetTableAccessor(string tableName)
            {
                var table = GetOrCreateTable(tableName);
                return new TableAccessor(storage, this, table);
            }

            internal bool LogLazyFlushTransaction()
            {
                pendingLazyTransactionsCount++;
                if (pendingLazyTransactionsCount < FlushThreshold)
                {
                    return false;
                }
                else
                {
                    pendingLazyTransactionsCount = 0;
                    return true;
                }
            }

            public void Dispose()
            {
                cancellationTokenRegistration.Dispose();
                if (Database != 0)
                {
                    var d = Database;                    
                    var retCode = NativeMethods.JetCloseDatabase(
                        sesid: Session,
                        dbid: d,
                        grbit: 0);

                    if (retCode.ReturnCode == EsentReturnCode.Success)
                    {
                        Database = 0;
                    }
                }

                if (Session != IntPtr.Zero)
                {
                    var s = Session;                    
                    var retCode = NativeMethods.JetEndSession(
                        sesid: s,
                        grbit: 0);

                    if (retCode.ReturnCode == EsentReturnCode.Success)
                    {
                        Session = IntPtr.Zero;
                    }
                }
            }

            /// <summary>
            /// Checks if given string is a valid table name from Esent perspective.
            /// Valid name should be made of the following set of characters: 0 through 9, A through Z, a through z, and all other punctuation except for "!" (exclamation point), 
            /// "," (comma), "[" (opening bracket), and "]" (closing bracket) - that is, ASCII characters 0x20, 0x22 through 0x2d, 0x2f through 0x5a, 0x5c, 0x5d through 0x7f.
            ///  Also the length should be less than 64 and not begin with a space.
            /// </summary>
            /// <param name="tableName"></param>
            private bool IsValidTableName(string tableName)
            {
                if (tableName.Length >= 64 || tableName.StartsWith(" "))
                {
                    return false;
                }

                return tableName.All(c => c == 0x20 || (c >= 0x22 && c <= 0x2d) || (c >= 0x2f && c <= 0x5a) || c == 0x5c || (c >= 0x5d && c <= 0x7f));
            }

            private TableInfo GetOrCreateTable(string tableName)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var originalTableName = tableName;
                TableInfo tableInfo;

                if (cachedTableInfo.TryGetValue(tableName, out tableInfo))
                {
                    return tableInfo;
                }

                if (!IsValidTableName(tableName))
                {
                    // supplied table name is not valid - instead of using it directly we need to pick a correct generated name from a 'SanitizedNames' table
                    tableName = TableAccessor.GetSanitizedTableName(this, sanitizedNamesTable, tableName);
                }

                IntPtr table;
                int retryAttempts = 10;

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using (var tx = new Transaction(Session))
                    {
                        var result =
                            NativeMethods.JetOpenTableW(
                                sesid: Session,
                                dbid: Database,
                                tablename: tableName,
                                pvParameters: null,
                                cbParameters: 0,
                                grbit: (uint)OpenTableGrbit.Updatable,
                                tableid: out table);

                        if (result.ReturnCode == EsentReturnCode.ObjectNotFound)
                        {
                            CreateNewTable(tableName, JetColumnType.LongBinary, ColumndefGrbit.ColumnCompressed);

                            // commit transactions so this table will be available on next iteration
                            tx.Commit();
                        }
                        else if (result.ReturnCode == EsentReturnCode.TableLocked)
                        {
                            // non-existing table was created by another thread and Esent puts a exclusive lock on this table (so user can create indexes etc).
                            // after creation owner thread will release the lock and re-open table in a non-exclusive mode - yield and retry -
                            // one of subsequent attempts should be successful
                            Thread.Yield();
                            retryAttempts--;
                            if (retryAttempts == 0)
                            {
                                result.EnsureSucceeded();
                            }
                        }
                        else
                        {
                            result.EnsureSucceeded();
                            tableInfo = new TableInfo(
                                tableId: table,
                                keyColumnId: GetColumnId(tableName, KeyColumnName),
                                valueColumnId: GetColumnId(tableName, ValueColumnName));

                            tx.Commit();
                            break;
                        }
                    }
                }

                // save session specific table information so it can be reused within the same session
                cachedTableInfo[originalTableName] = tableInfo;

                return tableInfo;
            }

            /// <summary>
            /// Creates a table with a name specified by <paramref name="tableName"/>.
            /// Table structure: key column with type LongBinary, value column with type <paramref name="valueColumnType"/>.
            /// Additionally table with include index over the key column
            /// </summary>
            private void CreateNewTable(string tableName, JetColumnType valueColumnType, ColumndefGrbit valueColumnGrbit)
            {
                // http://blogs.msdn.com/b/martinc/archive/2013/10/08/cbkeymost-when-indexing-needs-additional-overhead.aspx
                const int MaxKeySize = 1 + 9 * ((NativeMethods.MAX_PATH * sizeof(char) + 7) / 8);

                unsafe
                {
                    const string IndexName = "IndexByKey";

                    // sort items in 'KeyColumnName' in ascending order
                    var indexKey = "+" + KeyColumnName + "\0\0";

                    fixed (char* keyColumnNamePtr = KeyColumnName)
                    fixed (char* valueColumnNamePtr = ValueColumnName)
                    fixed (char* indexNamePtr = IndexName)
                    fixed (char* indexKeyPtr = indexKey)
                    {
                        var columns = new NativeMethods.NativeColumnCreate[]
                        {
                            new NativeMethods.NativeColumnCreate(
                                cbStruct: NativeMethods.NativeColumnCreate.Size,
                                szColumnName: new IntPtr(keyColumnNamePtr),
                                coltyp: (uint)JetColumnType.LongBinary,
                                cbMax: MaxKeySize,
                                grbit: (uint)(ColumndefGrbit.ColumnNotNULL | ColumndefGrbit.ColumnCompressed)),
                            new NativeMethods.NativeColumnCreate(
                                cbStruct: NativeMethods.NativeColumnCreate.Size,
                                szColumnName: new IntPtr(valueColumnNamePtr),
                                coltyp: (uint)valueColumnType,
                                cbMax: default(uint),
                                grbit: (uint)valueColumnGrbit)
                        };

                        var createIndexGrbit =
                            CreateIndexGrbit.IndexPrimary
                            | CreateIndexGrbit.IndexDisallowTruncation
                            | CreateIndexGrbit.IndexKeyMost;

                        var indexes = new NativeMethods.NativeIndexCreate2[]
                        {
                            new NativeMethods.NativeIndexCreate2
                            {
                                indexcreate1 = new NativeMethods.NativeIndexCreate1
                                {
                                    indexcreate = new NativeMethods.NativeIndexCreate(
                                        cbStruct: NativeMethods.NativeIndexCreate2.Size,
                                        szIndexName: new IntPtr(indexNamePtr),
                                        szKey: new IntPtr(indexKeyPtr),
                                        cbKey: (uint)indexKey.Length * sizeof(char),
                                        grbit: (uint)createIndexGrbit,
                                        ulDensity: 80),
                                    cbKeyMost = MaxKeySize
                                },
                                pSpaceHints = default(IntPtr)
                            }
                        };

                        fixed (NativeMethods.NativeColumnCreate* columnsPtr = columns)
                        fixed (NativeMethods.NativeIndexCreate2* indexesPtr = indexes)
                        {
                            var tableCreate3 = new NativeMethods.NativeTableCreate3
                            {
                                cbStruct = NativeMethods.NativeTableCreate3.Size,
                                szTableName = tableName,
                                ulPages = 16,
                                ulDensity = 80,
                                rgcolumncreate = new IntPtr(columnsPtr),
                                cColumns = (uint)columns.Length,
                                rgindexcreate = new IntPtr(indexesPtr),
                                cIndexes = (uint)indexes.Length,
                            };

                            var result = NativeMethods.JetCreateTableColumnIndex3W(
                                sesid: Session,
                                dbid: Database,
                                tablecreate3: ref tableCreate3);

                            if (result.ReturnCode != EsentReturnCode.TableDuplicate)
                            {
                                result.EnsureSucceeded();
                                NativeMethods.JetCloseTable(
                                    sesid: Session,
                                    tableid: tableCreate3.tableid).EnsureSucceeded();
                            }
                        }
                    }
                }
            }

            private uint GetColumnId(string tableName, string columnName)
            {
                var keyColumnDef = default(NativeMethods.NativeColumnDef);
                NativeMethods.JetGetColumnInfo(
                    sesid: Session,
                    dbid: Database,
                    szTableName: tableName,
                    szColumnName: columnName,
                    columndef: ref keyColumnDef,
                    cbMax: NativeMethods.NativeColumnDef.Size,
                    infoLevel: (uint)JetColumnInfo.Default).EnsureSucceeded();

                return keyColumnDef.columnid;
            }

            private bool TryOpenExistingDatabase(bool firstOpenCall)
            {
                if (firstOpenCall)
                {
                    var retCode = NativeMethods.JetAttachDatabaseW(
                        sesid: Session,
                        szFilename: databaseFile,
                        grbit: (uint)AttachDatabaseGrbit.None);

                    if (retCode.ReturnCode == EsentReturnCode.FileNotFound)
                    {
                        return false;
                    }
                    else
                    {
                        retCode.EnsureSucceeded();
                    }
                }

                uint database;
                NativeMethods.JetOpenDatabaseW(
                    sesid: Session,
                    database: databaseFile,
                    szConnect: null,
                    dbid: out database,
                    grbit: (uint)OpenDatabaseGrbit.None).EnsureSucceeded();

                Database = database;
                return true;
            }

            private void CreateNewDatabase()
            {
                uint database;
                NativeMethods.JetCreateDatabaseW(
                    sesid: Session,
                    szFilename: databaseFile,
                    szConnect: null,
                    dbid: out database,
                    grbit: (uint)CreateDatabaseGrbit.None).EnsureSucceeded();

                Database = database;

                // create a predefined table that will store mapping from non-sanitized project names to valid table names (generated by autoincremented field)
                CreateNewTable(SanitizedNames, JetColumnType.Long, ColumndefGrbit.ColumnAutoincrement);
            }
        }
    }
}
