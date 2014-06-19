using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyLocalVariable : ILocalDefinition
    {
        #region ILocalDefinition Members

        public IMetadataConstant CompileTimeValue
        {
            get { return Dummy.Constant; }
        }

        public IEnumerable<ICustomModifier> CustomModifiers
        {
            get { return IteratorHelper.GetEmptyEnumerable<ICustomModifier>(); }
        }

        public bool IsConstant
        {
            get { return false; }
        }

        public bool IsModified
        {
            get { return false; }
        }

        public bool IsPinned
        {
            get { return false; }
        }

        public bool IsReference
        {
            get { return false; }
        }

        public IEnumerable<ILocation> Locations
        {
            get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
        }

        public IMethodDefinition MethodDefinition
        {
            get { return Dummy.Method; }
        }

        public ITypeReference Type
        {
            get { return Dummy.TypeReference; }
        }

        #endregion

        #region INamedEntity Members

        public string Name
        {
            get { return Dummy.Name; }
        }

        #endregion
    }
}