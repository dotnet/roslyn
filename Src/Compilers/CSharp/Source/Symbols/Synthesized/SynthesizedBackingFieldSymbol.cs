// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a compiler generated backing field for an automatically implemented property.
    /// </summary>
    internal sealed class SynthesizedBackingFieldSymbol : SynthesizedFieldSymbolBase
    {
        private readonly SourcePropertySymbol property;
        private readonly bool hasInitializer;

        public SynthesizedBackingFieldSymbol(
            SourcePropertySymbol property,
            string name,
            bool isReadOnly,
            bool isStatic,
            bool hasInitializer)
            : base(property.ContainingType, name, isPublic: false, isReadOnly: isReadOnly, isStatic: isStatic)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));

            this.property = property;
            this.hasInitializer = hasInitializer;
        }

        public bool HasInitializer
        {
            get { return hasInitializer; }
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                return this.property;
            }
        }

        internal override LexicalSortKey GetLexicalSortKey()
        {
            return property.GetLexicalSortKey();
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return this.property.Locations;
            }
        }

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return this.property.Type;
        }

        internal override bool HasPointerType
        {
            get
            {
                return this.property.HasPointerType;
            }
        }

        internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(compilationState, ref attributes);

            var compilation = this.DeclaringCompilation;

            // Dev11 doesn't synthesize this attribute, the debugger has a knowledge 
            // of special name C# compiler uses for backing fields, which is not desirable.
            AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDebuggerBrowsableNeverAttribute());
        }

        internal override int IteratorLocalIndex
        {
            get { throw ExceptionUtilities.Unreachable; }
        }
    }
}