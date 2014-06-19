using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummySecurityAttribute : ISecurityAttribute
    {
        #region ISecurityAttribute Members

        public SecurityAction Action
        {
            get { return SecurityAction.LinkDemand; }
        }

        public IEnumerable<ICustomAttribute> Attributes
        {
            get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
        }

        #endregion
    }
}