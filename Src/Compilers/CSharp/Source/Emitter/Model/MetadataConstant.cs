using System.Collections.Generic;
using Microsoft.Cci;

namespace Roslyn.Compilers.CSharp
{
    internal sealed class MetadataConstant : IMetadataConstant
    {
        private readonly ITypeReference type;
        private readonly object value;

        public MetadataConstant(ITypeReference type, object value)
        {
            this.type = type;
            this.value = value;
        }

        object IMetadataConstant.Value
        {
            get { return this.value; }
        }

        void IMetadataExpression.Dispatch(IMetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        ITypeReference IMetadataExpression.Type
        {
            get { return this.type; }
        }

        IEnumerable<Microsoft.Cci.ILocation> IObjectWithLocations.Locations
        {
            get { throw new System.NotImplementedException(); }
        }
    }
}
