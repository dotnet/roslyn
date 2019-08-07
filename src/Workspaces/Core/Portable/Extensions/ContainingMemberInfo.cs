using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal class ContainingMemberInfo
    {
        public string memberInfo;

        public static readonly ContainingMemberInfo None = new ContainingMemberInfo();

        public ContainingMemberInfo()
        {
        }
        public ContainingMemberInfo(string typeInfo)
        {
            this.memberInfo = typeInfo;
        }
    }
}
