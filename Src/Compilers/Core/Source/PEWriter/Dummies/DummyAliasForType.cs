using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyAliasForType : IAliasForType
    {
        #region IAliasForType Members

        public ITypeReference AliasedType
        {
            get { return Dummy.TypeReference; }
        }

        #endregion

        #region IDefinition Members

        public IEnumerable<ICustomAttribute> Attributes
        {
            get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
        }

        public void Dispatch(IMetadataVisitor visitor)
        {
        }

        public IEnumerable<ILocation> Locations
        {
            get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
        }

        #endregion
    }
}