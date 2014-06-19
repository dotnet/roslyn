using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyAssemblyReference : IAssemblyReference
    {
        #region IAssemblyReference Members

        public string Culture
        {
            get { return string.Empty; }
        }

        public IEnumerable<byte> PublicKeyToken
        {
            get { return IteratorHelper.GetEmptyEnumerable<byte>(); }
        }

        public Version Version
        {
            get { return new Version(0, 0); }
        }

        #endregion

        #region IUnitReference Members

        public string Name
        {
            get { return Dummy.Name; }
        }

        #endregion

        #region IAssemblyReference Members

        public bool IsRetargetable
        {
            get { return false; }
        }

        #endregion

        #region IModuleReference Members

        public IAssemblyReference/*?*/ ContainingAssembly
        {
            get { return null; }
        }

        #endregion

        #region IUnitReference Members

        public void Dispatch(IMetadataVisitor visitor)
        {
        }

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