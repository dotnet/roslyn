// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Contains common arguments to Symbol.DecodeWellKnownAttribute method in both the language compilers.
    /// </summary>
    internal struct DecodeWellKnownAttributeArguments<TAttributeSyntax, TAttributeData, TAttributeLocation>
        where TAttributeSyntax : SyntaxNode
        where TAttributeData : AttributeData
    {
        /// <summary>
        /// Object to store the decoded data from bound well-known attributes.
        /// Created lazily only when some decoded data needs to be stored, null otherwise.
        /// </summary>
        private WellKnownAttributeData? _lazyDecodeData;

        /// <summary>
        /// Gets or creates the decoded data object.
        /// </summary>
        /// <remarks>
        /// This method must be called only when some decoded data will be stored into it subsequently.
        /// </remarks>
        public T GetOrCreateData<T>() where T : WellKnownAttributeData, new()
        {
            if (_lazyDecodeData == null)
            {
                _lazyDecodeData = new T();
            }

            return (T)_lazyDecodeData;
        }

        /// <summary>
        /// Returns true if some decoded data has been stored into <see cref="_lazyDecodeData"/>.
        /// </summary>
        public readonly bool HasDecodedData
        {
            get
            {
                if (_lazyDecodeData != null)
                {
                    _lazyDecodeData.VerifyDataStored(expected: true);
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
        public readonly WellKnownAttributeData DecodedData
        {
            get
            {
                Debug.Assert(this.HasDecodedData);
                return _lazyDecodeData!;
            }
        }

        /// <summary>
        /// Syntax of the attribute to decode. Might be null when the attribute information is not coming 
        /// from syntax. For example, an assembly attribute propagated from added module to the resulting assembly.
        /// </summary>
        public TAttributeSyntax? AttributeSyntaxOpt { get; set; }

        /// <summary>
        /// Bound attribute to decode.
        /// </summary>
        public TAttributeData Attribute { get; set; }

        /// <summary>
        /// The index of the attribute in the list of attributes to decode.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Total count of attributes to decode.
        /// </summary>
        public int AttributesCount { get; set; }

        /// <summary>
        /// Diagnostic bag.
        /// </summary>
        public BindingDiagnosticBag Diagnostics { get; set; }

        /// <summary>
        /// Specific part of the symbol to which the attributes apply, or AttributeLocation.None if the attributes apply to the symbol itself.
        /// Used e.g. for return type attributes of a method symbol.
        /// </summary>
        public TAttributeLocation SymbolPart { get; set; }
    }
}
