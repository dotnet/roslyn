// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class
#if DEBUG
        SymbolAdapter
#else
        Symbol
#endif 
        : Cci.IReference
    {
        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            throw ExceptionUtilities.Unreachable();
        }

        CodeAnalysis.Symbols.ISymbolInternal Cci.IReference.GetInternalSymbol() => AdaptedSymbol;

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            throw ExceptionUtilities.Unreachable();
        }

        IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(EmitContext context)
        {
            return AdaptedSymbol.GetCustomAttributesToEmit((PEModuleBuilder)context.Module);
        }
    }

    internal partial class Symbol
    {
#if DEBUG
        internal SymbolAdapter GetCciAdapter() => GetCciAdapterImpl();
        protected virtual SymbolAdapter GetCciAdapterImpl() => throw ExceptionUtilities.Unreachable();
#else
        internal Symbol AdaptedSymbol => this;
        internal Symbol GetCciAdapter() => this;
#endif 

        /// <summary>
        /// Checks if this symbol is a definition and its containing module is a SourceModuleSymbol.
        /// </summary>
        [Conditional("DEBUG")]
        protected internal void CheckDefinitionInvariant()
        {
            // can't be generic instantiation
            Debug.Assert(this.IsDefinition);

            // must be declared in the module we are building
            Debug.Assert(this.ContainingModule is SourceModuleSymbol ||
                         (this.Kind == SymbolKind.Assembly && this is SourceAssemblySymbol) ||
                         (this.Kind == SymbolKind.NetModule && this is SourceModuleSymbol));
        }

        Cci.IReference CodeAnalysis.Symbols.ISymbolInternal.GetCciAdapter() => GetCciAdapter();

        /// <summary>
        /// Return whether the symbol is either the original definition
        /// or distinct from the original. Intended for use in Debug.Assert
        /// only since it may include a deep comparison.
        /// </summary>
        internal bool IsDefinitionOrDistinct()
        {
            return this.IsDefinition || !this.Equals(this.OriginalDefinition, SymbolEqualityComparer.ConsiderEverything.CompareKind);
        }

        internal virtual IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(PEModuleBuilder moduleBuilder)
        {
            CheckDefinitionInvariant();

            Debug.Assert(this.Kind != SymbolKind.Assembly);
            return GetCustomAttributesToEmit(moduleBuilder, emittingAssemblyAttributesInNetModule: false);
        }

        internal IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(PEModuleBuilder moduleBuilder, bool emittingAssemblyAttributesInNetModule)
        {
            CheckDefinitionInvariant();
            Debug.Assert(this.Kind != SymbolKind.Assembly);

            ImmutableArray<CSharpAttributeData> userDefined;
            ArrayBuilder<SynthesizedAttributeData> synthesized = null;
            userDefined = this.GetAttributes();
            this.AddSynthesizedAttributes(moduleBuilder, ref synthesized);

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

            //PERF: Avoid creating an iterator for the common case of no attributes.
            if (userDefined.IsEmpty && synthesized == null)
            {
                return SpecializedCollections.EmptyEnumerable<CSharpAttributeData>();
            }

            return GetCustomAttributesToEmitIterator(userDefined, synthesized, isReturnType, emittingAssemblyAttributesInNetModule);
        }

        private IEnumerable<CSharpAttributeData> GetCustomAttributesToEmitIterator(
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
                    // We need to filter out duplicate assembly attributes (i.e. attributes that
                    // bind to the same constructor and have identical arguments) and invalid
                    // InternalsVisibleTo attributes.
                    if (((SourceAssemblySymbol)this).IsIndexOfOmittedAssemblyAttribute(i))
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

#if DEBUG
    internal partial class SymbolAdapter
    {
        internal abstract Symbol AdaptedSymbol { get; }

        public sealed override string ToString()
        {
            return AdaptedSymbol.ToString();
        }

        public sealed override bool Equals(object obj)
        {
            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            throw Roslyn.Utilities.ExceptionUtilities.Unreachable();
        }

        public sealed override int GetHashCode()
        {
            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            throw Roslyn.Utilities.ExceptionUtilities.Unreachable();
        }

        [Conditional("DEBUG")]
        protected internal void CheckDefinitionInvariant() => AdaptedSymbol.CheckDefinitionInvariant();

        internal bool IsDefinitionOrDistinct()
        {
            return AdaptedSymbol.IsDefinitionOrDistinct();
        }
    }
#endif
}
