using System.Data.SqlServerCe;
using System.IO;

namespace Roslyn.Services.Host
{
    internal partial class TextStorage
    {
        private class FieldTextReader : TextReader
        {
            private readonly SqlCeDataReader dataReader;
            private readonly int fieldIndex;
            private long dataIndex;

            public FieldTextReader(SqlCeDataReader dataReader, int fieldIndex)
            {
                this.dataReader = dataReader;
                this.fieldIndex = fieldIndex;
            }

            public override int Read(char[] buffer, int index, int count)
            {
                int charsRead = (int)this.dataReader.GetChars(this.fieldIndex, this.dataIndex, buffer, index, count);
                this.dataIndex += charsRead;
                return charsRead;
            }
        }
    }
}