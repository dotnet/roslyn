using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host.Esent
{
    internal partial class EsentKeyValueStorage
    {
        /// <summary>
        /// Allows to read\writer from Esent binary columns
        /// </summary>
        private class EsentStream : Stream
        {
            private const int MaxColumnSize = 0x7fffffff;
            private const int ITag = 1;

            private readonly EsentSession session;
            private readonly TableInfo table;

            private int offset;
            private int sizeOfValueColumn;
            private const RetrieveColumnGrbit ReadGrbit = RetrieveColumnGrbit.None;

            public EsentStream(EsentSession session, TableInfo table)
            {
                this.session = session;
                this.table = table;
                sizeOfValueColumn = GetSizeOfValueColumn();
            }

            public override bool CanRead
            {
                get { return true; }
            }

            public override bool CanSeek
            {
                get { return false; }
            }

            public override bool CanWrite
            {
                get { return true; }
            }

            public override long Length
            {
                get { return GetSizeOfValueColumn(); }
            }

            public override long Position
            {
                get { return offset; }
                set { throw new NotSupportedException(); }
            }

            public override void Flush()
            {
                // if size of new data is smaller than the old one - trim the blob size to 'offset'
                if (offset < sizeOfValueColumn)
                {
                    var setinfo = new NativeMethods.NativeSetInfo { cbStruct = NativeMethods.NativeRetInfo.Size, itagSequence = ITag };
                    NativeMethods.JetSetColumn(
                        sesid: session.Session,
                        tableid: table.TableId,
                        columnid: table.ValueColumnId,
                        pvData: IntPtr.Zero,
                        cbData: (uint)offset,
                        grbit: (uint)SetColumnGrbit.SizeLV,
                        psetinfo: ref setinfo).EnsureSucceeded();
                }
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (this.offset > this.sizeOfValueColumn)
                {
                    return 0;
                }

                var retInfo = new NativeMethods.NativeRetInfo { cbStruct = NativeMethods.NativeRetInfo.Size, itagSequence = ITag, ibLongValue = (uint)this.offset };
                unsafe
                {
                    fixed (byte* p = buffer)
                    {
                        uint actual;
                        NativeMethods.JetRetrieveColumn(
                            sesid: session.Session,
                            tableid: table.TableId,
                            columnid: table.ValueColumnId,
                            pvData: new IntPtr(p + offset),
                            cbData: (uint)count,
                            cbActual: out actual,
                            grbit: (uint)ReadGrbit,
                            pretinfo: ref retInfo);

                        var read = Math.Min(count, (int)actual);
                        this.offset += read;
                        return read;
                    }
                }
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                int newOffset = offset + count;
                SetColumnGrbit setColumnGrbit = SetColumnGrbit.IntrinsicLV;

                if (this.offset == sizeOfValueColumn)
                {
                    setColumnGrbit |= SetColumnGrbit.AppendLV;
                }
                else if (newOffset >= sizeOfValueColumn)
                {
                    setColumnGrbit |= SetColumnGrbit.OverwriteLV | SetColumnGrbit.SizeLV;
                }
                else
                {
                    setColumnGrbit |= SetColumnGrbit.OverwriteLV;
                }

                var setInfo = new NativeMethods.NativeSetInfo
                {
                    cbStruct = NativeMethods.NativeSetInfo.Size,
                    itagSequence = ITag,
                    ibLongValue = (uint)this.offset
                };

                unsafe
                {
                    fixed (byte* p = buffer)
                    {
                        NativeMethods.JetSetColumn(
                            sesid: session.Session,
                            tableid: table.TableId,
                            columnid: table.ValueColumnId,
                            pvData: new IntPtr(p + offset),
                            cbData: (uint)count,
                            grbit: (uint)setColumnGrbit,
                            psetinfo: ref setInfo).EnsureSucceeded();
                    }
                }

                this.offset += count;
                if (this.offset > this.sizeOfValueColumn)
                {
                    this.sizeOfValueColumn = this.offset;
                }
            }

            private int GetSizeOfValueColumn()
            {
                uint size;
                var retInfo = new NativeMethods.NativeRetInfo { cbStruct = NativeMethods.NativeRetInfo.Size, itagSequence = ITag, ibLongValue = 0 };
                var retCode = NativeMethods.JetRetrieveColumn(
                    sesid: session.Session,
                    tableid: table.TableId,
                    columnid: table.ValueColumnId,
                    pvData: IntPtr.Zero,
                    cbData: 0,
                    cbActual: out size,
                    grbit: (uint)ReadGrbit,
                    pretinfo: ref retInfo);

                switch (retCode.ReturnCode)
                {
                    case EsentReturnCode.NoCurrentRecord:
                        
                        // new record - value in this column was not inserted yet
                        return 0;
                    case EsentReturnCode.Success:
                    case EsentReturnCode.BufferTruncated:

                        // BufferTruncated is a valid result here since we pass zero size buffer
                        return (int)size;
                    default:
                        // translate error code to exception
                        retCode.EnsureSucceeded();
                        return 0;
                }
            }
        }
    }
}
