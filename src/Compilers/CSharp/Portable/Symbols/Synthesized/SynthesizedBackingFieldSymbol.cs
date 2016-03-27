// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a compiler generated backing field for an automatically implemented property.
    /// </summary>
    internal sealed class SynthesizedBackingFieldSymbol : SynthesizedFieldSymbolBase
    {
        private readonly SourcePropertySymbol _property;
        private readonly bool _hasInitializer;

        public SynthesizedBackingFieldSymbol(
            SourcePropertySymbol property,
            string name,
            bool isReadOnly,
            bool isStatic,
            bool hasInitializer)
            : base(property.ContainingType, name, isPublic: false, isReadOnly: isReadOnly, isStatic: isStatic)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));

            _property = property;
            _hasInitializer = hasInitializer;
        }

        public bool HasInitializer
        {
            get { return _hasInitializer; }
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                return _property;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _property.Locations;
            }
        }

        internal override bool SuppressDynamicAttribute
        {
            get
            {
                return false;
            }
        }

        internal override TypeSymbolWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return _property.Type;
        }

        internal override bool HasPointerType
        {
            get
            {
                return _property.HasPointerType;
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
    }
}
