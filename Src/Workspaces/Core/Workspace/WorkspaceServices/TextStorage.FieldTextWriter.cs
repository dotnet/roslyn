using System.Data.SqlServerCe;
using System.IO;
using System.Text;

namespace Roslyn.Services.Host
{
    internal partial class TextStorage
    {
        private class FieldTextWriter : TextWriter
        {
            private readonly SqlCeResultSet resultSet;
            private readonly int fieldIndex;
            private long dataIndex;

            public FieldTextWriter(SqlCeResultSet resultSet, int fieldIndex, long startingOffset = 0)
            {
                this.resultSet = resultSet;
                this.fieldIndex = fieldIndex;
                this.dataIndex = startingOffset;
            }

            public override Encoding Encoding
            {
                get
                {
                    return Encoding.Unicode;
                }
            }

            public override void Write(char[] buffer, int index, int count)
            {
                this.resultSet.SetChars(this.fieldIndex, this.dataIndex, buffer, index, count);
                this.dataIndex += count;
            }
        }
    }
}