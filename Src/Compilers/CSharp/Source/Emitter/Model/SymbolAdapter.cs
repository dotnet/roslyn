// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class Symbol : Microsoft.Cci.IReference
    {
        /// <summary>
        /// Checks if this symbol is a definition and its containing module is a SourceModuleSymbol.
        /// </summary>
        [Conditional("DEBUG")]
        internal protected void CheckDefinitionInvariant()
        {
            // can't be generic instantiation
            Debug.Assert(this.IsDefinition);

            // must be declared in the module we are building
            Debug.Assert(this.ContainingModule is SourceModuleSymbol ||
                         (this.Kind == SymbolKind.Assembly && this is SourceAssemblySymbol) ||
                         (this.Kind == SymbolKind.NetModule && this is SourceModuleSymbol));
        }

        Microsoft.Cci.IDefinition Microsoft.Cci.IReference.AsDefinition(Microsoft.CodeAnalysis.Emit.Context context)
        {
            throw ExceptionUtilities.Unreachable;
        }

        void Microsoft.Cci.IReference.Dispatch(Microsoft.Cci.MetadataVisitor visitor)
        {
            throw ExceptionUtilities.Unreachable;
        }

        /// <summary>
        /// Return whether the symbol is either the original definition
        /// or distinct from the original. Intended for use in Debug.Assert
        /// only since it may include a deep comparison.
        /// </summary>
        internal bool IsDefinitionOrDistinct()
        {
            return this.IsDefinition || !this.Equals(this.OriginalDefinition);
        }

        IEnumerable<Microsoft.Cci.ICustomAttribute> Microsoft.Cci.IReference.GetAttributes(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return GetCustomAttributesToEmit();
        }

        internal virtual IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit()
        {
            CheckDefinitionInvariant();

            Debug.Assert(this.Kind != SymbolKind.Assembly);
            return GetCustomAttributesToEmit(emittingAssemblyAttributesInNetModule: false);
        }

        internal IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(bool emittingAssemblyAttributesInNetModule)
        {
            CheckDefinitionInvariant();

            ImmutableArray<CSharpAttributeData> userDefined;
            ArrayBuilder<SynthesizedAttributeData> synthesized = null;
            userDefined = this.GetAttributes();
            this.AddSynthesizedAttributes(ref synthesized);

            if (userDefined.IsEmpty && synthesized == null)
            {
                return SpecializedCollections.EmptyEnumerable<CSharpAttributeData>();
            }

            // Note that callers of this method (CCI and ReflectionEmitter) have to enumerate 
            // all items of the returned iterator, otherwise the synthesized ArrayBuilder may leak.
            return GetCustomAttributesToEmit(userDefined, synthesized, isReturnType: false, emittingAssemblyAttributesInNetModule: emittingAssemblyAttributesInNetModule);
        }

        /// <summary>
        /// Returns a list of attributes to emit to CustomAttribute table.
        /// The <paramref name="synthesized"/> builder is freed after all its items are enumerated.
        /// </summary>
        internal IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(
            ImmutableArray<CSharpAttributeData> userDefined,
            ArrayBuilder<SynthesizedAttributeData> synthesized,
            bool isReturnType,
            bool emittingAssemblyAttributesInNetModule)
        {
            CheckDefinitionInvariant();

            if (synthesized != null)
            {
                foreach (var attribute in synthesized)
                {
                    // only synthesize attributes that are emitted:
                    Debug.Assert(attribute.ShouldEmitAttribute(this, isReturnType, emittingAssemblyAttributesInNetModule));
                    yield return attribute;
                }

                synthesized.Free();
            }

            for (int i = 0; i < userDefined.Length; i++)
            {
                CSharpAttributeData attribute = userDefined[i];
                if (this.Kind == SymbolKind.Assembly)
                {
                    // We need to filter out duplicate assembly attributes,
                    // i.e. attributes that bind to the same constructor and have identical arguments.
                    if (((SourceAssemblySymbol)this).IsIndexOfDuplicateAssemblyAttribute(i))
                    {
                        continue;
                    }
                }

                if (attribute.ShouldEmitAttribute(this, isReturnType, emittingAssemblyAttributesInNetModule))
                {
                    yield return attribute;
                }
            }
        }
    }
}