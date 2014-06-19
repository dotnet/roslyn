using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyModuleReference : IModuleReference
    {
        #region IUnitReference Members

        public string Name
        {
            get { return Dummy.Name; }
        }

        #endregion

        #region IModuleReference Members

        public IAssemblyReference/*?*/ ContainingAssembly
        {
            get { return null; }
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

        #region IModuleReference Members

        #endregion

        #region IUnitReference Members

        public void Dispatch(IMetadataVisitor visitor)
        {
        }

        #endregion
    }
}