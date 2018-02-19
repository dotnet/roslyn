// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

