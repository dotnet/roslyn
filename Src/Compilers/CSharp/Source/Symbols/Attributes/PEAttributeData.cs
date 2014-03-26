// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    /// <summary>
    /// Represents a PE custom attribute
    /// </summary>
    internal sealed class PEAttributeData : CSharpAttributeData
    {
        private readonly MetadataDecoder decoder;
        private readonly CustomAttributeHandle handle;
        private NamedTypeSymbol lazyAttributeClass = ErrorTypeSymbol.UnknownResultType; // Indicates unitialized.
        private MethodSymbol lazyAttributeConstructor;
        private ImmutableArray<TypedConstant> lazyConstructorArguments;
        private ImmutableArray<KeyValuePair<string, TypedConstant>> lazyNamedArguments;
        private ThreeState lazyHasErrors = ThreeState.Unknown;

        internal PEAttributeData(PEModuleSymbol moduleSymbol, CustomAttributeHandle handle)
        {
            decoder = new MetadataDecoder(moduleSymbol);
            this.handle = handle;
        }

        public override NamedTypeSymbol AttributeClass
        {
            get
            {
                EnsureClassAndConstructorSymbolsAreLoaded();
                return this.lazyAttributeClass;
            }
        }

        public override MethodSymbol AttributeConstructor
        {
            get
            {
                EnsureClassAndConstructorSymbolsAreLoaded();
                return this.lazyAttributeConstructor;
            }
        }

        public override SyntaxReference ApplicationSyntaxReference
        {
            get { return null; }
        }

        internal protected override ImmutableArray<TypedConstant> CommonConstructorArguments
        {
            get
            {
                EnsureAttributeArgumentsAreLoaded();
                return this.lazyConstructorArguments;
            }
        }

        internal protected override ImmutableArray<KeyValuePair<string, TypedConstant>> CommonNamedArguments
        {
            get
            {
                EnsureAttributeArgumentsAreLoaded();
                return this.lazyNamedArguments;
            }
        }

        private void EnsureClassAndConstructorSymbolsAreLoaded()
        {
#pragma warning disable 0252
            if ((object)this.lazyAttributeClass == ErrorTypeSymbol.UnknownResultType)
            {
                TypeSymbol attributeClass;
                MethodSymbol attributeConstructor;

                if (!decoder.GetCustomAttribute(this.handle, out attributeClass, out attributeConstructor))
                {
                    // TODO: should we create CSErrorTypeSymbol for attribute class??
                    lazyHasErrors = ThreeState.True;
                }
                else if ((object)attributeClass == null || attributeClass.IsErrorType() || (object)attributeConstructor == null)
                {
                    lazyHasErrors = ThreeState.True;
                }

                Interlocked.CompareExchange(ref this.lazyAttributeConstructor, attributeConstructor, null);
                Interlocked.CompareExchange(ref this.lazyAttributeClass, (NamedTypeSymbol)attributeClass, ErrorTypeSymbol.UnknownResultType); // Serves as a flag, so do it last.
            }
#pragma warning restore 0252
        }

        private void EnsureAttributeArgumentsAreLoaded()
        {
            if (this.lazyConstructorArguments.IsDefault || this.lazyNamedArguments.IsDefault)
            {
                TypedConstant[] lazyConstructorArguments = null;
                KeyValuePair<string, TypedConstant>[] lazyNamedArguments = null;

                if (!decoder.GetCustomAttribute(this.handle, out lazyConstructorArguments, out lazyNamedArguments))
                {
                    lazyHasErrors = ThreeState.True;
                }

                Debug.Assert(lazyConstructorArguments != null && lazyNamedArguments != null);

                ImmutableInterlocked.InterlockedInitialize(ref this.lazyConstructorArguments,
                    ImmutableArray.Create<TypedConstant>(lazyConstructorArguments));

                ImmutableInterlocked.InterlockedInitialize(ref this.lazyNamedArguments,
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
            return this.decoder.IsTargetAttribute(this.handle, namespaceName, typeName);
        }

        /// <summary>
        /// Matches an attribute by metadata namespace, metadata type name and metadata signature. Does not load the
        /// type symbol for the attribute.
        /// </summary>
        /// <param name="targetSymbol">Target symbol.</param>
        /// <param name="description">Attribute to match.</param>
        /// <returns>
        /// An index of the target constructor signature in
        /// signatures array, -1 if
        /// this is not the target attribute.
        /// </returns>
        internal override int GetTargetAttributeSignatureIndex(Symbol targetSymbol, AttributeDescription description)
        {
            // Matching an attribute by name should not load the attribute class.
            return this.decoder.GetTargetAttributeSignatureIndex(handle, description);
        }

        internal override bool HasErrors
        {
            get
            {
                if (lazyHasErrors == ThreeState.Unknown)
                {
                    EnsureClassAndConstructorSymbolsAreLoaded();
                    EnsureAttributeArgumentsAreLoaded();

                    if (lazyHasErrors == ThreeState.Unknown)
                    {
                        lazyHasErrors = ThreeState.False;
                    }
                }

                return lazyHasErrors.Value();
            }
        }
    }
}
