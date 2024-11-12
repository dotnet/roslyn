// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    /// <summary>
    /// Represents a PE custom attribute
    /// </summary>
    internal sealed class PEAttributeData : CSharpAttributeData
    {
        private readonly MetadataDecoder _decoder;
        private readonly CustomAttributeHandle _handle;
        private NamedTypeSymbol? _lazyAttributeClass = ErrorTypeSymbol.UnknownResultType; // Indicates uninitialized.
        private MethodSymbol? _lazyAttributeConstructor;
        private ImmutableArray<TypedConstant> _lazyConstructorArguments;
        private ImmutableArray<KeyValuePair<string, TypedConstant>> _lazyNamedArguments;
        private ThreeState _lazyHasErrors = ThreeState.Unknown;

        internal PEAttributeData(PEModuleSymbol moduleSymbol, CustomAttributeHandle handle)
        {
            _decoder = new MetadataDecoder(moduleSymbol);
            _handle = handle;
        }

        public override NamedTypeSymbol? AttributeClass
        {
            get
            {
                EnsureClassAndConstructorSymbolsAreLoaded();
                return _lazyAttributeClass;
            }
        }

        public override MethodSymbol? AttributeConstructor
        {
            get
            {
                EnsureClassAndConstructorSymbolsAreLoaded();
                return _lazyAttributeConstructor;
            }
        }

        public override SyntaxReference? ApplicationSyntaxReference
        {
            get { return null; }
        }

        protected internal override ImmutableArray<TypedConstant> CommonConstructorArguments
        {
            get
            {
                EnsureAttributeArgumentsAreLoaded();
                return _lazyConstructorArguments;
            }
        }

        protected internal override ImmutableArray<KeyValuePair<string, TypedConstant>> CommonNamedArguments
        {
            get
            {
                EnsureAttributeArgumentsAreLoaded();
                return _lazyNamedArguments;
            }
        }

        private void EnsureClassAndConstructorSymbolsAreLoaded()
        {
#pragma warning disable 0252
            if ((object?)_lazyAttributeClass == ErrorTypeSymbol.UnknownResultType)
            {
                TypeSymbol? attributeClass;
                MethodSymbol? attributeConstructor;

                if (!_decoder.GetCustomAttribute(_handle, out attributeClass, out attributeConstructor))
                {
                    // TODO: should we create CSErrorTypeSymbol for attribute class??
                    _lazyHasErrors = ThreeState.True;
                }
                else if ((object)attributeClass == null || attributeClass.IsErrorType() || (object)attributeConstructor == null)
                {
                    _lazyHasErrors = ThreeState.True;
                }

                Interlocked.CompareExchange(ref _lazyAttributeConstructor, attributeConstructor, null);
                Interlocked.CompareExchange(ref _lazyAttributeClass, (NamedTypeSymbol?)attributeClass, ErrorTypeSymbol.UnknownResultType); // Serves as a flag, so do it last.
            }
#pragma warning restore 0252
        }

        private void EnsureAttributeArgumentsAreLoaded()
        {
            if (_lazyConstructorArguments.IsDefault || _lazyNamedArguments.IsDefault)
            {
                TypedConstant[]? lazyConstructorArguments = null;
                KeyValuePair<string, TypedConstant>[]? lazyNamedArguments = null;

                if (!_decoder.GetCustomAttribute(_handle, AttributeConstructor, out lazyConstructorArguments, out lazyNamedArguments))
                {
                    _lazyHasErrors = ThreeState.True;
                }

                Debug.Assert(lazyConstructorArguments != null && lazyNamedArguments != null);

                ImmutableInterlocked.InterlockedInitialize(ref _lazyConstructorArguments,
                    ImmutableArray.Create<TypedConstant>(lazyConstructorArguments));

                ImmutableInterlocked.InterlockedInitialize(ref _lazyNamedArguments,
                    ImmutableArray.Create<KeyValuePair<string, TypedConstant>>(lazyNamedArguments));
            }
        }

        /// <summary>
        /// Matches an attribute by metadata namespace, metadata type name. Does not load the type symbol for
        /// the attribute.
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="typeName"></param>
        /// <returns>True if the attribute data matches.</returns>
        internal override bool IsTargetAttribute(string namespaceName, string typeName)
        {
            // Matching an attribute by name should not load the attribute class.
            return _decoder.IsTargetAttribute(_handle, namespaceName, typeName);
        }

        /// <summary>
        /// Matches an attribute by metadata namespace, metadata type name and metadata signature. Does not load the
        /// type symbol for the attribute.
        /// </summary>
        /// <param name="description">Attribute to match.</param>
        /// <returns>
        /// An index of the target constructor signature in
        /// signatures array, -1 if
        /// this is not the target attribute.
        /// </returns>
        internal override int GetTargetAttributeSignatureIndex(AttributeDescription description)
        {
            // Matching an attribute by name should not load the attribute class.
            return _decoder.GetTargetAttributeSignatureIndex(_handle, description);
        }

        internal override Location GetAttributeArgumentLocation(int parameterIndex)
        {
            return new MetadataLocation(_decoder.ModuleSymbol);
        }

        [MemberNotNullWhen(false, nameof(AttributeClass), nameof(AttributeConstructor))]
        internal override bool HasErrors
        {
            get
            {
                if (_lazyHasErrors == ThreeState.Unknown)
                {
                    EnsureClassAndConstructorSymbolsAreLoaded();
                    EnsureAttributeArgumentsAreLoaded();

                    if (_lazyHasErrors == ThreeState.Unknown)
                    {
                        _lazyHasErrors = ThreeState.False;
                    }
                }

                return _lazyHasErrors.Value();
            }
        }

        internal override DiagnosticInfo? ErrorInfo
        {
            get
            {
                if (HasErrors)
                {
                    switch (AttributeConstructor)
                    {
                        case { HasUseSiteError: true } attributeConstructor:
                            return attributeConstructor.GetUseSiteInfo().DiagnosticInfo;

                        case { }:
                            return new CSDiagnosticInfo(ErrorCode.ERR_BogusType, string.Empty);

                        default:
                            switch (AttributeClass)
                            {
                                case { HasUseSiteError: true } attributeClass:
                                    return attributeClass.GetUseSiteInfo().DiagnosticInfo;

                                case { } attributeClass:
                                    return new CSDiagnosticInfo(ErrorCode.ERR_MissingPredefinedMember, attributeClass, WellKnownMemberNames.InstanceConstructorName);

                                default:
                                    return new CSDiagnosticInfo(ErrorCode.ERR_BogusType, string.Empty);
                            }
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        internal override bool IsConditionallyOmitted => false;
    }
}
