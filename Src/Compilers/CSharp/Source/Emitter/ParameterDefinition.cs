using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers.CSharp.Emit
{
    internal sealed class ParameterDefinition : ParameterTypeInformation, Microsoft.Cci.IParameterDefinition
    {
        public ParameterDefinition(Module moduleBeingBuilt, ParameterSymbol underlyingParameter)
            : base(moduleBeingBuilt, underlyingParameter)
        { 
        }

        Microsoft.Cci.IMetadataConstant Microsoft.Cci.IParameterDefinition.DefaultValue
        {
            get { throw new NotImplementedException(); }
        }

        bool Microsoft.Cci.IParameterDefinition.HasDefaultValue
        {
            get { return false; }
        }

        bool Microsoft.Cci.IParameterDefinition.IsIn
        {
            get { return false; }
        }

        bool Microsoft.Cci.IParameterDefinition.IsMarshalledExplicitly
        {
            get { return false; }
        }

        bool Microsoft.Cci.IParameterDefinition.IsOptional
        {
            get { return false; }
        }

        bool Microsoft.Cci.IParameterDefinition.IsOut
        {
            get { return false; }
        }

        Microsoft.Cci.IMarshallingInformation Microsoft.Cci.IParameterDefinition.MarshallingInformation
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerable<Microsoft.Cci.ICustomAttribute> Microsoft.Cci.IReference.Attributes
        {
            get { return Enumerable.Empty<Microsoft.Cci.ICustomAttribute>(); }
        }

        void Microsoft.Cci.IReference.Dispatch(Microsoft.Cci.IMetadataVisitor visitor)
        {
            visitor.Visit((Microsoft.Cci.IParameterDefinition)this);
        }

        IEnumerable<Microsoft.Cci.ILocation> Microsoft.Cci.IObjectWithLocations.Locations
        {
            get { throw new NotImplementedException(); }
        }

        string Microsoft.Cci.INamedEntity.Name
        {
            get { return UnderlyingParameter.Name; }
        }

        Microsoft.Cci.IMetadataConstant Microsoft.Cci.IMetadataConstantContainer.Constant
        {
            get { throw new NotImplementedException(); }
        }

        Microsoft.Cci.IDefinition Microsoft.Cci.IReference.AsDefinition
        {
            get
            {
                return this;
            }
        }
    }
}
