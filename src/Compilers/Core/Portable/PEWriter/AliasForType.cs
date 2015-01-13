// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.Cci
{
    internal class AliasForType<TypeSymbol> : IAliasForType where TypeSymbol : class, ITypeReference
    {
        public readonly TypeSymbol AliasedType;

        public AliasForType(TypeSymbol aliasedType)
        {
            Debug.Assert(aliasedType != null);

            this.AliasedType = aliasedType;
        }

        ITypeReference IAliasForType.AliasedType
        {
            get
            {
                return AliasedType;
            }
        }

        IEnumerable<ICustomAttribute> IReference.GetAttributes(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return SpecializedCollections.EmptyEnumerable<ICustomAttribute>();
        }

        void IReference.Dispatch(MetadataVisitor visitor)
        {
            visitor.Visit((IAliasForType)this);
        }

        IDefinition IReference.AsDefinition(Microsoft.CodeAnalysis.Emit.Context m)
        {
            return this;
        }
    }
}
