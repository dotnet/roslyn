// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using System.Collections.Generic;
using System.Diagnostics;
using Cci = Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CSharp.Emit.NoPia
{
    internal sealed class EmbeddedTypeParameter : EmbeddedTypesManager.CommonEmbeddedTypeParameter
    {
        public EmbeddedTypeParameter(EmbeddedMethod containingMethod, TypeParameterSymbol underlyingTypeParameter) :
            base(containingMethod, underlyingTypeParameter)
        {
            Debug.Assert(underlyingTypeParameter.IsDefinition);
        }

        protected override IEnumerable<Cci.TypeReferenceWithAttributes> GetConstraints(EmitContext context)
        {
            return ((Cci.IGenericParameter)UnderlyingTypeParameter).GetConstraints(context);
        }

        protected override bool MustBeReferenceType
        {
            get
            {
                return UnderlyingTypeParameter.HasReferenceTypeConstraint;
            }
        }

        protected override bool MustBeValueType
        {
            get
            {
                return UnderlyingTypeParameter.HasValueTypeConstraint;
            }
        }

        protected override bool MustHaveDefaultConstructor
        {
            get
            {
                return UnderlyingTypeParameter.HasConstructorConstraint;
            }
        }

        protected override string Name
        {
            get { return UnderlyingTypeParameter.MetadataName; }
        }

        protected override ushort Index
        {
            get
            {
                return (ushort)UnderlyingTypeParameter.Ordinal;
            }
        }
    }
}

