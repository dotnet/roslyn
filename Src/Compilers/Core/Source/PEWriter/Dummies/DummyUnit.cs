using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyUnit : IUnit
    {
        #region IUnit Members

        public string Name
        {
            get { return Dummy.Name; }
        }

        #endregion

        #region IDoubleDispatcher Members

        public void Dispatch(IMetadataVisitor visitor)
        {
        }

        #endregion

        #region IUnitReference Members

        #endregion

        #region IReference Members

        public IEnumerable<ICustomAttribute> Attributes
        {
            get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
        }

        public IEnumerable<ILocation> Locations
        {
            get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
        }

        #endregion
    }
}