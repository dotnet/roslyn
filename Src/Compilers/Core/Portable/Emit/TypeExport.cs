// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Emit
{
    internal class TypeExport<TypeSymbol> : Cci.ITypeExport where TypeSymbol : class, Cci.ITypeReference
    {
        public readonly TypeSymbol AliasedType;

        public TypeExport(TypeSymbol aliasedType)
        {
            Debug.Assert(aliasedType != null);

            this.AliasedType = aliasedType;
        }

        Cci.ITypeReference Cci.ITypeExport.ExportedType
        {
            get
            {
                return AliasedType;
            }
        }

        IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(EmitContext context)
        {
            return SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();
        }

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext m)
        {
            return this;
        }
    }
}
