using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp.Emit
{
    internal class AliasForType : Microsoft.Cci.IAliasForType
    {
        private Microsoft.Cci.ITypeReference aliasedType;

        public AliasForType(Microsoft.Cci.ITypeReference aliasedType)
        {
            Contract.ThrowIfNull(aliasedType);

            this.aliasedType = aliasedType;
        }

        Microsoft.Cci.ITypeReference Microsoft.Cci.IAliasForType.AliasedType
        {
            get 
            { 
                return aliasedType; 
            }
        }

        IEnumerable<Microsoft.Cci.ICustomAttribute> Microsoft.Cci.IReference.Attributes
        {
            get 
            {
                return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.ICustomAttribute>(); 
            }
        }

        void Microsoft.Cci.IReference.Dispatch(Microsoft.Cci.IMetadataVisitor visitor)
        {
            visitor.Visit((Microsoft.Cci.IAliasForType)this);
        }

        IEnumerable<Microsoft.Cci.ILocation> Microsoft.Cci.IObjectWithLocations.Locations
        {
            get { throw new NotImplementedException(); }
        }

        Microsoft.Cci.IDefinition Microsoft.Cci.IReference.AsDefinition(object m)
        {
            return this;
        }
    }
}
