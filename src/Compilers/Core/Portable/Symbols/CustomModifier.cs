// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis
{
    public abstract class CustomModifier
    {
        /// <summary>
        /// If true, a language may use the modified storage location without 
        /// being aware of the meaning of the modification, modopt vs. modreq. 
        /// </summary>
        public abstract bool IsOptional { get; }

        /// <summary>
        /// A type used as a tag that indicates which type of modification applies.
        /// </summary>
        public abstract INamedTypeSymbol Modifier { get; }

        internal abstract Cci.ITypeReference GetModifier(EmitContext context);
    }
}
