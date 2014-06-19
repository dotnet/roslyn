using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyTypeReference : ITypeReference
    {
        #region ITypeReference Members

        public IAliasForType AliasForType
        {
            get { return Dummy.AliasForType; }
        }

        ITypeDefinition ITypeReference.ResolvedType
        {
            get
            {
                // ^ assume false;
                return Dummy.Type;
            }
        }

        public PrimitiveTypeCode TypeCode
        {
            get { return PrimitiveTypeCode.Invalid; }
        }

        public uint TypeDefRowId
        {
            get { return 0; }
        }

        public bool IsAlias
        {
            get { return false; }
        }

        public bool IsEnum
        {
            get { return false; }
        }

        public bool IsValueType
        {
            get { return false; }
        }

        #endregion

        #region IReference Members

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

        bool ITypeReference.IsEnum
        {
            get { throw new NotImplementedException(); }
        }

        bool ITypeReference.IsValueType
        {
            get { throw new NotImplementedException(); }
        }

        PrimitiveTypeCode ITypeReference.TypeCode
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<ICustomAttribute> IReference.Attributes
        {
            get { throw new NotImplementedException(); }
        }

        void IReference.Dispatch(IMetadataVisitor visitor)
        {
            throw new NotImplementedException();
        }

        IEnumerable<ILocation> IObjectWithLocations.Locations
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsNamespaceTypeReference
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsGenericTypeInstance
        {
            get { throw new NotImplementedException(); }
        }
    }
}