using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host.Esent
{
    internal partial class EsentKeyValueStorage
    {
        /// <summary>
        /// Wraps error code returned by Esent and provides convenient way to check it without introducing extension methods:
        /// NativeMethod.MethodName(...).EnsureSucceeded() instead of EnsureSucceeded(NativeMethod.MethodName(...)).
        /// </summary>
        [StructLayout(layoutKind: LayoutKind.Sequential, Pack = 1)]
        private struct EsentResult
        {
            public EsentReturnCode ReturnCode;
            public void EnsureSucceeded()
            {
                EsentKeyValueStorage.EnsureSucceeded(ReturnCode);
            }
        }

        private enum EsentReturnCode
        {
            Success = 0,
            TermInProgress = -1000,
            InvalidSesid = -1104,
            TooManyActiveUsers = -1059,
            RecordNotFound = -1601,
            KeyDuplicate = -1605,
            WriteConflict = -1102,
            ObjectNotFound = -1305,
            TableLocked = -1302,
            TableDuplicate = -1303,
            NoCurrentRecord = -1603,
            FileNotFound = -1811,
            BufferTruncated = 1006,
            FileAccessDenied = -1032,
            DatabaseInUse = -1202,
            AlreadyInitialized = -1030
        }

        [BestFitMapping(false, ThrowOnUnmappableChar = true)]
        private static class NativeMethods
        {
            private const string Esent = "esent.dll";
            internal const int MAX_PATH = 260;

            [DllImport(Esent, CharSet = CharSet.Unicode, ExactSpelling = true)]
            public static extern EsentResult JetCreateInstance2W(
                out IntPtr instance, 
                string szInstanceName, 
                string szDisplayName, 
                uint grbit);

            [DllImport(Esent, ExactSpelling = true)]
            public static extern EsentResult JetInit(ref IntPtr instance);

            [DllImport(Esent, ExactSpelling = true)]
            public static extern EsentResult JetTerm2(IntPtr instance, TermGrbit grbit);

            [DllImport(Esent, CharSet = CharSet.Unicode, ExactSpelling = true)]
            public static unsafe extern EsentResult JetSetSystemParameterW(
                IntPtr* instancePtr,
                IntPtr sesid,
                uint paramid,
                IntPtr lParam,
                string szParam);

            [DllImport(Esent, CharSet = CharSet.Unicode, ExactSpelling = true)]
            public static extern EsentResult JetCreateDatabaseW(
                IntPtr sesid,
                string szFilename,
                string szConnect,
                out uint dbid,
                uint grbit);

            [DllImport(Esent, CharSet = CharSet.Unicode, ExactSpelling = true)]
            public static extern EsentResult JetAttachDatabaseW(IntPtr sesid, string szFilename, uint grbit);

            [DllImport(Esent, CharSet = CharSet.Unicode, ExactSpelling = true)]
            public static extern EsentResult JetOpenDatabaseW(
                IntPtr sesid,
                string database,
                string szConnect,
                out uint dbid,
                uint grbit);

            [DllImport(Esent, CharSet = CharSet.Unicode, ExactSpelling = true)]
            public static extern EsentResult JetGetSystemParameterW(
                IntPtr instance,
                IntPtr sesid,
                uint paramid,
                ref IntPtr plParam,
                [Out] StringBuilder szParam,
                uint cbMax);

            [DllImport(Esent, CharSet = CharSet.Ansi, ExactSpelling = true)]
            public static extern EsentResult JetBeginSession(
                IntPtr instance,
                out IntPtr session,
                string username,
                string password);

            [DllImport(Esent, ExactSpelling = true)]
            public static extern EsentResult JetEndSession(IntPtr sesid, uint grbit);

            [DllImport(Esent, ExactSpelling = true)]
            public static extern EsentResult JetCommitTransaction(IntPtr sesid, uint grbit);

            [DllImport(Esent, ExactSpelling = true)]
            public static extern EsentResult JetRollback(IntPtr sesid, uint grbit);

            [DllImport(Esent, ExactSpelling = true)]
            public static extern EsentResult JetBeginTransaction(IntPtr sesid);

            [DllImport(Esent, CharSet = CharSet.Unicode, ExactSpelling = true)]
            public static extern EsentResult JetCreateTableColumnIndex3W(
                IntPtr sesid,
                uint dbid,
                ref NativeTableCreate3 tablecreate3);

            [DllImport(Esent, ExactSpelling = true)]
            public static extern EsentResult JetPrepareUpdate(
                IntPtr sesid,
                IntPtr tableid,
                uint prep);

            [DllImport(Esent, ExactSpelling = true)]
            public static extern EsentResult JetSetColumn(
                IntPtr sesid,
                IntPtr tableid,
                uint columnid,
                IntPtr pvData,
                uint cbData,
                uint grbit,
                IntPtr psetinfo);

            [DllImport(Esent, ExactSpelling = true)]
            public static extern EsentResult JetSetColumn(
                IntPtr sesid,
                IntPtr tableid,
                uint columnid,
                IntPtr pvData,
                uint cbData,
                uint grbit,
                [In] ref NativeSetInfo psetinfo);

            [DllImport(Esent, ExactSpelling = true)]
            public static extern EsentResult JetUpdate(
                IntPtr sesid,
                IntPtr tableid,
                [Out] byte[] pvBookmark,
                uint cbBookmark,
                out uint cbActual);

            [DllImport(Esent, ExactSpelling = true)]
            public static extern EsentResult JetMakeKey(
                IntPtr sesid,
                IntPtr tableid,
                IntPtr pvData,
                uint cbData,
                uint grbit);

            [DllImport(Esent, ExactSpelling = true)]
            public static extern EsentResult JetSeek(
                IntPtr sesid,
                IntPtr tableid,
                uint grbit);

            [DllImport(Esent, CharSet = CharSet.Ansi, ExactSpelling = true)]
            public static extern EsentResult JetSetCurrentIndex(IntPtr sesid, IntPtr tableid, string szIndexName);

            [DllImport(Esent, ExactSpelling = true)]
            public static extern EsentResult JetRetrieveColumn(
                IntPtr sesid,
                IntPtr tableid,
                uint columnid,
                IntPtr pvData,
                uint cbData,
                out uint cbActual,
                uint grbit,
                [In, Out] ref NativeRetInfo pretinfo);

            [DllImport(Esent, ExactSpelling = true)]
            public static extern EsentResult JetRetrieveColumn(
                IntPtr sesid,
                IntPtr tableid,
                uint columnid,
                IntPtr pvData,
                uint cbData,
                out uint cbActual,
                uint grbit,
                IntPtr pretinfo);

            [DllImport(Esent, CharSet = CharSet.Unicode, ExactSpelling = true)]
            public static extern EsentResult JetOpenTableW(
                IntPtr sesid,
                uint dbid,
                string tablename,
                byte[] pvParameters,
                uint cbParameters,
                uint grbit,
                out IntPtr tableid);

            [DllImport(Esent, ExactSpelling = true)]
            public static extern EsentResult JetCloseTable(IntPtr sesid, IntPtr tableid);

            [DllImport(Esent, CharSet = CharSet.Ansi, ExactSpelling = true)]
            public static extern EsentResult JetGetColumnInfo(
                IntPtr sesid,
                uint dbid,
                string szTableName,
                string szColumnName,
                ref NativeColumnDef columndef,
                uint cbMax,
                uint infoLevel);

            [DllImport(Esent, ExactSpelling = true)]
            public static extern EsentResult JetCloseDatabase(IntPtr sesid, uint dbid, uint grbit);

            internal struct NativeColumnDef
            {
                /// <summary>
                /// The size of a NativeColumnDef structure.
                /// </summary>
                public static unsafe readonly uint Size = (uint)sizeof(NativeColumnDef);

                /// <summary>
                /// Size of the structure.
                /// </summary>
                public uint cbStruct;

                /// <summary>
                /// Column ID.
                /// </summary>
                public uint columnid;

                /// <summary>
                /// Type of the column.
                /// </summary>
                public uint coltyp;

                /// <summary>
                /// Reserved. Should be 0.
                /// </summary>
                [Obsolete("Reserved")]
                public ushort wCountry;

                /// <summary>
                /// Obsolete. Should be 0.
                /// </summary>
                [Obsolete("Use cp")]
                public ushort langid;

                /// <summary>
                /// Code page for text columns.
                /// </summary>
                public ushort cp;

                /// <summary>
                /// Reserved. Should be 0.
                /// </summary>
                [Obsolete("Reserved")]
                public ushort wCollate;

                /// <summary>
                /// Maximum length of the column.
                /// </summary>
                public uint cbMax;

                /// <summary>
                /// Column options.
                /// </summary>
                public uint grbit;
            }

            internal struct NativeRetInfo
            {
                /// <summary>
                /// The size of a NativeRetInfo structure.
                /// </summary>
                public static unsafe readonly uint Size = (uint)sizeof(NativeRetInfo);

                /// <summary>
                /// Size of this structure.
                /// </summary>
                public uint cbStruct;

                /// <summary>
                /// Offset of the long value to retrieve.
                /// </summary>
                public uint ibLongValue;

                /// <summary>
                /// Itag sequence to retrieve.
                /// </summary>
                public uint itagSequence;

                /// <summary>
                /// Returns the columnid of the next tagged column.
                /// </summary>
                public uint columnidNextTagged;
            }

            internal struct NativeSetInfo
            {
                /// <summary>
                /// The size of a NativeSetInfo structure.
                /// </summary>
                public static unsafe readonly uint Size = (uint)sizeof(NativeRetInfo);

                /// <summary>
                /// Size of the structure.
                /// </summary>
                public uint cbStruct;

                /// <summary>
                /// Offset to the first byte to be set in a column of type JET_coltypLongBinary or JET_coltypLongText.
                /// </summary>
                public uint ibLongValue;

                /// <summary>
                /// The sequence number of value in a multi-valued column to be set.
                /// </summary>
                public uint itagSequence;
            }

            internal struct NativeIndexCreate2
            {
                /// <summary>
                /// The size of a NativeIndexCreate2 structure.
                /// </summary>
                public static unsafe readonly uint Size = (uint)sizeof(NativeIndexCreate2);

                /// <summary>
                /// Nested NativeIndexCreate1 structure.
                /// </summary>
                public NativeIndexCreate1 indexcreate1;

                /// <summary>
                /// A NativeSpaceHints pointer.
                /// </summary>
                public IntPtr pSpaceHints;
            }

            internal struct NativeIndexCreate
            {
                /// <summary>
                /// The size of a NativeIndexCreate structure.
                /// </summary>
                public static unsafe readonly uint Size = (uint)sizeof(NativeIndexCreate);

                /// <summary>
                /// Size of the structure.
                /// </summary>
                public uint cbStruct;

                /// <summary>
                /// Name of the index.
                /// </summary>
                public IntPtr szIndexName;

                /// <summary>
                /// Index key description.
                /// </summary>
                public IntPtr szKey;

                /// <summary>
                /// Size of index key description.
                /// </summary>
                public uint cbKey;

                /// <summary>
                /// Index options.
                /// </summary>
                public uint grbit;

                /// <summary>
                /// Index density.
                /// </summary>
                public uint ulDensity;

                /// <summary>
                /// Pointer to unicode sort options.
                /// </summary>
                public IntPtr pidxUnicode;

                /// <summary>
                /// Maximum size of column data to index. This can also be
                /// a pointer to a JET_TUPLELIMITS structure.
                /// </summary>
                public IntPtr cbVarSegMac;

                /// <summary>
                /// Pointer to array of conditional columns.
                /// </summary>
                public IntPtr rgconditionalcolumn;

                /// <summary>
                /// Count of conditional columns.
                /// </summary>
                public uint cConditionalColumn;

                /// <summary>
                /// Returned error from index creation.
                /// </summary>
                public int err;

                public NativeIndexCreate(uint cbStruct, IntPtr szIndexName, IntPtr szKey, uint cbKey, uint grbit, uint ulDensity) : this()
                {
                    this.cbStruct = cbStruct;
                    this.szIndexName = szIndexName;
                    this.szKey = szKey;
                    this.cbKey = cbKey;
                    this.grbit = grbit;
                    this.ulDensity = ulDensity;

                    this.pidxUnicode = default(IntPtr);
                    this.cbVarSegMac = default(IntPtr);
                    this.rgconditionalcolumn = default(IntPtr);
                    this.cConditionalColumn = default(uint);
                    this.err = default(int);
                }
            }

            internal struct NativeIndexCreate1
            {
                /// <summary>
                /// The size of a NativeIndexCreate1 structure.
                /// </summary>
                public static unsafe readonly uint Size = (uint)sizeof(NativeIndexCreate1);

                /// <summary>
                /// Nested NativeIndexCreate structure.
                /// </summary>
                public NativeIndexCreate indexcreate;

                /// <summary>
                /// Maximum size of the key.
                /// </summary>
                public uint cbKeyMost;
            }

            internal struct NativeColumnCreate
            {
                /// <summary>
                /// The size of a NativeColumnCreate structure.
                /// </summary>
                public static unsafe readonly uint Size = (uint)sizeof(NativeColumnCreate);

                /// <summary>
                /// Size of the structure.
                /// </summary>
                public uint cbStruct;

                /// <summary>
                /// Name of the column.
                /// </summary>
                public IntPtr szColumnName;

                /// <summary>
                /// Type of the column.
                /// </summary>
                public uint coltyp;

                /// <summary>
                /// The maximum length of this column (only relevant for binary and text columns).
                /// </summary>
                public uint cbMax;

                /// <summary>
                /// Column options.
                /// </summary>
                public uint grbit;

                /// <summary>
                /// Default value (NULL if none).
                /// </summary>
                public IntPtr pvDefault;

                /// <summary>
                /// Size of the default value.
                /// </summary>
                public uint cbDefault;

                /// <summary>
                /// Code page (for text columns only).
                /// </summary>
                public uint cp;

                /// <summary>
                /// The returned column id.
                /// </summary>
                public uint columnid;

                /// <summary>
                /// The returned error code.
                /// </summary>
                public int err;

                public NativeColumnCreate(uint cbStruct, IntPtr szColumnName, uint coltyp, uint cbMax, uint grbit) : this()
                {
                    this.cbStruct = cbStruct;
                    this.szColumnName = szColumnName;
                    this.coltyp = coltyp;
                    this.cbMax = cbMax;
                    this.grbit = grbit;
                    this.pvDefault = default(IntPtr);
                    this.cbDefault = default(uint);
                    this.cp = default(uint);
                    this.columnid = default(uint);
                    this.err = default(int);
                }
            }

            internal struct NativeTableCreate3
            {
                /// <summary>
                /// The size of a NativeTableCreate3 structure. Cannot use sizeof operator here since structure contains strings
                /// </summary>
                public static readonly uint Size = (uint)Marshal.SizeOf(typeof(NativeTableCreate3));

                /// <summary>
                /// Size of the structure.
                /// </summary>
                public uint cbStruct;

                /// <summary>
                /// Name of the table to create.
                /// </summary>
                [MarshalAs(UnmanagedType.LPWStr)]
                public string szTableName;

                /// <summary>
                /// Name of the table from which to inherit base DDL.
                /// </summary>
                [MarshalAs(UnmanagedType.LPWStr)]
                public string szTemplateTableName;

                /// <summary>
                /// Initial pages to allocate for table.
                /// </summary>
                public uint ulPages;

                /// <summary>
                /// Table density.
                /// </summary>
                public uint ulDensity;

                /// <summary>
                /// Array of column creation info.
                /// </summary>
                public IntPtr rgcolumncreate;

                /// <summary>
                /// Number of columns to create.
                /// </summary>
                public uint cColumns;

                /// <summary>
                /// Array of indices to create, pointer to <see cref="NativeIndexCreate2"/>.
                /// </summary>
                public IntPtr rgindexcreate;

                /// <summary>
                /// Number of indices to create.
                /// </summary>
                public uint cIndexes;

                /// <summary>
                /// Callback function to use for the table.
                /// </summary>
                [MarshalAs(UnmanagedType.LPWStr)]
                public string szCallback;

                /// <summary>
                /// Type of the callback function (JET_cbTyp)
                /// </summary>
                public int cbtyp;

                /// <summary>
                /// Table options.
                /// </summary>
                public uint grbit;

                /// <summary>
                /// Space allocation, maintenance, and usage hints for default sequential index.
                /// </summary>
                public IntPtr pSeqSpacehints;

                /// <summary>
                /// Space allocation, maintenance, and usage hints for Separated LV tree.
                /// </summary>
                public IntPtr pLVSpacehints;

                /// <summary>
                /// Heuristic size to separate a intrinsic LV from the primary record.
                /// </summary>
                public uint cbSeparateLV;

                /// <summary>
                /// Returned table id.
                /// </summary>
                public IntPtr tableid;

                /// <summary>
                /// Count of objects created (columns+table+indexes+callbacks).
                /// </summary>
                public uint cCreated;
            }
        }
    }
}
