using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp.Emit
{
    internal abstract class GenericParameterReference : TypeReference, Microsoft.Cci.IGenericParameterReference
    {
        protected readonly TypeParameterSymbol UnderlyingTypeParameter;

        public GenericParameterReference(Module moduleBeingBuilt, TypeParameterSymbol underlyingTypeParameter)
            : base(moduleBeingBuilt)
        {
            Contract.ThrowIfNull(underlyingTypeParameter);

            this.UnderlyingTypeParameter = underlyingTypeParameter;
        }

        protected override Symbol UnderlyingSymbol
        {
            get 
            {
                return UnderlyingTypeParameter;
            }
        }

        protected override TypeSymbol UnderlyingType
        {
            get
            {
                return UnderlyingTypeParameter;
            }
        }

        string Microsoft.Cci.INamedEntity.Name
        {
            get 
            {
                return UnderlyingTypeParameter.Name; 
            }
        }

        ushort Microsoft.Cci.IParameterListEntry.Index
        {
            get 
            {
                return (ushort)UnderlyingTypeParameter.Ordinal;
            }
        }

        public override uint TypeDefRowId
        {
            get { return 0; }
        }
    }
}
