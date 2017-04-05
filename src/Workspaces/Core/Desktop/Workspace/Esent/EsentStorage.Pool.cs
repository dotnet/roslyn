// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Isam.Esent.Interop;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Esent
{
    internal partial class EsentStorage
    {
        private static class Pool
        {
            private static readonly ObjectPool<ColumnValue[]>[] s_columnValuePool = new[]
            {
                new ObjectPool<ColumnValue[]>(() => new ColumnValue[3], 20),
                new ObjectPool<ColumnValue[]>(() => new ColumnValue[4], 20)
            };

            public static ColumnValue[] GetInt32Columns(JET_COLUMNID columnId1, int value1, JET_COLUMNID columnId2, int value2, JET_COLUMNID columnId3, int value3)
            {
                var array = s_columnValuePool[0].Allocate();

                array[0] = GetInt32Column(columnId1, value1);
                array[1] = GetInt32Column(columnId2, value2);
                array[2] = GetInt32Column(columnId3, value3);

                return array;
            }

            public static ColumnValue[] GetInt32Columns(JET_COLUMNID columnId1, int value1, JET_COLUMNID columnId2, int value2, JET_COLUMNID columnId3, int value3, JET_COLUMNID columnId4, int value4)
            {
                var array = s_columnValuePool[1].Allocate();

                array[0] = GetInt32Column(columnId1, value1);
                array[1] = GetInt32Column(columnId2, value2);
                array[2] = GetInt32Column(columnId3, value3);
                array[3] = GetInt32Column(columnId4, value4);

                return array;
            }

            public static Int32ColumnValue GetInt32Column(JET_COLUMNID columnId, int value)
            {
                var column = SharedPools.Default<Int32ColumnValue>().Allocate();
                column.Columnid = columnId;
                column.Value = value;

                return column;
            }

            public static void Free(ColumnValue[] values)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    Free(values[i]);
                }

                s_columnValuePool[values.Length - 3].Free(values);
            }

            public static void Free(ColumnValue value)
            {
                var intValue = value as Int32ColumnValue;
                if (intValue != null)
                {
                    SharedPools.Default<Int32ColumnValue>().Free(intValue);
                }
            }
        }
    }
}
