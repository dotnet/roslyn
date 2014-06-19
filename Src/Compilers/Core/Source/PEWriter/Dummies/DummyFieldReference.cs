using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal sealed class DummyFieldReference : IFieldReference
    {
        #region IFieldReference Members

        public ITypeReference Type
        {
            get { return Dummy.TypeReference; }
        }

        public IFieldDefinition ResolvedField
        {
            get { return Dummy.Field; }
        }

        #endregion

        #region ITypeMemberReference Members

        public ITypeReference ContainingType
        {
            get { return Dummy.Type; }
        }

        public ITypeDefinitionMember ResolvedTypeDefinitionMember
        {
            get { return Dummy.Field; }
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

        #region INamedEntity Members

        public string Name
        {
            get { return Dummy.Name; }
        }

        #endregion

        public bool IsSpecialized
        {
            get { throw new NotImplementedException(); }
        }
    }
}