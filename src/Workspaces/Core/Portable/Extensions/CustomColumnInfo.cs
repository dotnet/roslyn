using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal class CustomColumnInfo
    {
        public (string label, string value) columnInfo;

        public static readonly CustomColumnInfo None = new CustomColumnInfo();

        public CustomColumnInfo()
        {
        }

        public CustomColumnInfo(string label, string value)
        {
            this.columnInfo.label = label;
            this.columnInfo.value = value;
        }
    }
}
