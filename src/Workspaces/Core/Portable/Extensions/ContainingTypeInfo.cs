using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal class ContainingTypeInfo
    {
        public string typeInfo;

        public static readonly ContainingTypeInfo None = new ContainingTypeInfo();

        public ContainingTypeInfo()
        {
        }
        public ContainingTypeInfo(string typeInfo)
        {
            this.typeInfo = typeInfo;
        }
    }
}
