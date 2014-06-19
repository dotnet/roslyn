using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers.CSharp.Emit
{
    internal abstract class GenericParameterDefinition : GenericParameterReference, Microsoft.Cci.IGenericParameter
    {
        public GenericParameterDefinition(Module moduleBeingBuilt, TypeParameterSymbol underlyingTypeParameter)
            : base(moduleBeingBuilt, underlyingTypeParameter)
        {
        }

        IEnumerable<Microsoft.Cci.ITypeReference> Microsoft.Cci.IGenericParameter.Constraints
        {
            get { return Enumerable.Empty<Microsoft.Cci.ITypeReference>(); }
        }

        bool Microsoft.Cci.IGenericParameter.MustBeReferenceType
        {
            get 
            {
                return UnderlyingTypeParameter.HasReferenceTypeConstraint; 
            }
        }

        bool Microsoft.Cci.IGenericParameter.MustBeValueType
        {
            get
            {
                return UnderlyingTypeParameter.HasValueTypeConstraint;
            }
        }

        bool Microsoft.Cci.IGenericParameter.MustHaveDefaultConstructor
        {
            get
            {
                return UnderlyingTypeParameter.HasConstructorConstraint;
            }
        }

        Microsoft.Cci.TypeParameterVariance Microsoft.Cci.IGenericParameter.Variance
        {
            get 
            {
                switch (UnderlyingTypeParameter.Variance)
                {
                    case VarianceKind.VarianceNone:
                        return Microsoft.Cci.TypeParameterVariance.NonVariant;
                    case VarianceKind.VarianceIn:
                        return Microsoft.Cci.TypeParameterVariance.Covariant;
                    case VarianceKind.VarianceOut:
                        return Microsoft.Cci.TypeParameterVariance.Contravariant;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
}
