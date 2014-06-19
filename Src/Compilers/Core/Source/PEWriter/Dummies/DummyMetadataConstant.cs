using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyMetadataConstant : IMetadataConstant
    {
        #region IMetadataConstant Members

        public object/*?*/ Value
        {
            get { return null; }
        }

        #endregion

        #region IMetadataExpression Members

        public void Dispatch(IMetadataVisitor visitor)
        {
        }

        public IEnumerable<ILocation> Locations
        {
            get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
        }

        public ITypeReference Type
        {
            get { return Dummy.TypeReference; }
        }

        #endregion
    }
}