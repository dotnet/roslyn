using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal class CustomColumnInfo
    {
        public string columnValue;

        public static readonly CustomColumnInfo None = new CustomColumnInfo();

        public CustomColumnInfo()
        {
        }

        public CustomColumnInfo(string columnValue)
        {
            this.columnValue = columnValue;
        }
    }
}
