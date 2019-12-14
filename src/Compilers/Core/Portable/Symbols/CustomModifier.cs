// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis
{
    public abstract class CustomModifier : Cci.ICustomModifier
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

        #region ICustomModifier

        bool Cci.ICustomModifier.IsOptional
        {
            get
            {
                return this.IsOptional;
            }
        }

        Cci.ITypeReference Cci.ICustomModifier.GetModifier(EmitContext context)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
