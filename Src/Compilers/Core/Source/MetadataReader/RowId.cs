using System;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.CodeAnalysis.MetadataReader.PEFile
{
    // TODO (tomat): remove

    /// <summary>
    /// Pass-through struct to uint. Used to represent a row in a
    /// PE metadata table (e.g., a typedef table).
    /// </summary>
    internal struct RowId : IComparable<RowId>
    {
        private readonly uint value;

        public RowId(uint value)
        {
            this.value = value;
        }

        public static implicit operator RowId(uint value)
        {
            return new RowId(value);
        }

        public static implicit operator uint(RowId rid)
        {
            return rid.value;
        }

        public MetadataToken TokenFromRid(TokenType tokenType)
        {
            return (MetadataToken)(this.value | (uint)tokenType);
        }

        public bool IsNil()
        {
            return this.value == 0;
        }

        public static bool operator <=(RowId left, RowId right)
        {
            return left.value <= right.value;
        }

        public static bool operator >=(RowId left, RowId right)
        {
            return left.value >= right.value;
        }

        public static bool operator <(RowId left, RowId right)
        {
            return left.value < right.value;
        }

        public static bool operator >(RowId left, RowId right)
        {
            return left.value > right.value;
        }

        public int CompareTo(RowId other)
        {
            return this.value.CompareTo(other.value);
        }

        public static bool operator ==(RowId left, RowId right)
        {
            return left.value == right.value;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            return obj is RowId && ((RowId)obj).value == this.value;
        }

        public bool Equals(RowId other)
        {
            return other.value == this.value;
        }

        public override int GetHashCode()
        {
            return this.value.GetHashCode();
        }

        public static bool operator !=(RowId left, RowId right)
        {
            return left.value != right.value;
        }

        public static RowId operator +(RowId left, uint right)
        {
            return new RowId(left.value + right);
        }
    }
}
