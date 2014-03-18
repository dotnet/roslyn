// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Contains common arguments to Symbol.EarlyDecodeWellKnownAttribute method in both the language compilers.
    /// </summary>
    internal struct EarlyDecodeWellKnownAttributeArguments<TEarlyBinder, TNamedTypeSymbol, TAttributeSyntax, TAttributeLocation>
        where TNamedTypeSymbol : INamedTypeSymbol
        where TAttributeSyntax : SyntaxNode
    {
        /// <summary>
        /// Object to store the decoded data from early bound well-known attributes.
        /// Created lazily only when some decoded data needs to be stored, null otherwise.
        /// </summary>
        private EarlyWellKnownAttributeData lazyDecodeData;

        /// <summary>
        /// Gets or creates the decoded data object.
        /// </summary>
        /// <remarks>
        /// This method must be called only when some decoded data will be stored into it subsequently.
        /// </remarks>
        public T GetOrCreateData<T>() where T : EarlyWellKnownAttributeData, new()
        {
            if (this.lazyDecodeData == null)
            {
                this.lazyDecodeData = new T();
            }

            return (T)lazyDecodeData;
        }

        /// <summary>
        /// Returns true if some decoded data has been stored into <see cref="lazyDecodeData"/>.
        /// </summary>
        public bool HasDecodedData
        {
            get
            {
                if (lazyDecodeData != null)
                {
                    lazyDecodeData.VerifyDataStored(expected: true);
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the stored decoded data.
        /// </summary>
        /// <remarks>
        /// Assumes <see cref="HasDecodedData"/> is true.
        /// </remarks>
        public EarlyWellKnownAttributeData DecodedData
        {
            get
            {
                Debug.Assert(this.HasDecodedData);
                return this.lazyDecodeData;
            }
        }

        /// <summary>
        /// Binder to bind early well-known attributes.
        /// </summary>
        public TEarlyBinder Binder { get; set; }

        /// <summary>
        /// Bound type of the attribute to decode.
        /// </summary>
        public TNamedTypeSymbol AttributeType { get; set; }

        /// <summary>
        /// Syntax of the attribute to decode.
        /// </summary>
        public TAttributeSyntax AttributeSyntax { get; set; }

        /// <summary>
        /// Specific part of the symbol to which the attributes apply, or AttributeLocation.None if the attributes apply to the symbol itself.
        /// Used e.g. for return type attributes of a method symbol.
        /// </summary>
        public TAttributeLocation SymbolPart { get; set; }
    }
}
