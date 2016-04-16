// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Isam.Esent.Interop;
using Microsoft.Isam.Esent.Interop.Vista;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Esent
{
    internal partial class EsentStorage
    {
        public abstract class AbstractTable
        {
            protected const string PrimaryIndexName = "PrimaryIndex";

            public abstract void Create(JET_SESID sessionId, JET_DBID databaseId);
            public abstract void Initialize(JET_SESID sessionId, JET_DBID databaseId);
            public abstract AbstractTableAccessor GetTableAccessor(OpenSession openSession);

            protected JET_COLUMNCREATE CreateAutoIncrementIdColumn(string columnName)
            {
                return new JET_COLUMNCREATE()
                {
                    szColumnName = columnName,
                    coltyp = JET_coltyp.Long,
                    grbit = ColumndefGrbit.ColumnAutoincrement | ColumndefGrbit.ColumnNotNULL
                };
            }

            protected JET_COLUMNCREATE CreateIdColumn(string columnName)
            {
                return new JET_COLUMNCREATE()
                {
                    szColumnName = columnName,
                    coltyp = JET_coltyp.Long,
                    grbit = ColumndefGrbit.ColumnNotNULL
                };
            }

            protected JET_COLUMNCREATE CreateBinaryColumn(string columnName)
            {
                return new JET_COLUMNCREATE()
                {
                    szColumnName = columnName,
                    coltyp = JET_coltyp.LongBinary,
                    grbit = ColumndefGrbit.None
                };
            }

            protected JET_COLUMNCREATE CreateTextColumn(string columnName)
            {
                return new JET_COLUMNCREATE()
                {
                    szColumnName = columnName,
                    coltyp = JET_coltyp.LongText,
                    cp = JET_CP.Unicode,
                    grbit = ColumndefGrbit.ColumnNotNULL
                };
            }

            protected string CreateIndexKey(params string[] indexNames)
            {
                return "+" + string.Join("\0+", indexNames) + "\0\0";
            }

            protected JET_INDEXCREATE CreatePrimaryIndex(string indexKey)
            {
                return new JET_INDEXCREATE
                {
                    szIndexName = PrimaryIndexName,
                    szKey = indexKey,
                    cbKey = indexKey.Length,
                    grbit = CreateIndexGrbit.IndexPrimary | CreateIndexGrbit.IndexUnique | CreateIndexGrbit.IndexDisallowNull,
                    ulDensity = 80
                };
            }

            protected JET_INDEXCREATE CreateIndex(string name, string indexKey)
            {
                return new JET_INDEXCREATE
                {
                    szIndexName = name,
                    szKey = indexKey,
                    cbKey = indexKey.Length,
                    grbit = CreateIndexGrbit.IndexDisallowNull,
                    ulDensity = 80
                };
            }

            protected JET_INDEXCREATE CreateUniqueTextIndex(string name, string indexKey)
            {
                return new JET_INDEXCREATE
                {
                    szIndexName = name,
                    szKey = indexKey,
                    cbKey = indexKey.Length,
                    grbit = CreateIndexGrbit.IndexUnique | CreateIndexGrbit.IndexDisallowNull | VistaGrbits.IndexDisallowTruncation,
                    ulDensity = 80,
                    // this should be 2000 bytes Max after vista
                    cbKeyMost = SystemParameters.KeyMost
                };
            }

            protected JET_TABLECREATE CreateTable(string tableName, JET_COLUMNCREATE[] columns, JET_INDEXCREATE[] indexes)
            {
                return new JET_TABLECREATE()
                {
                    szTableName = tableName,
                    ulPages = 16,
                    ulDensity = 80,
                    rgcolumncreate = columns,
                    cColumns = columns.Length,
                    rgindexcreate = indexes,
                    cIndexes = indexes.Length
                };
            }
        }
    }
}
