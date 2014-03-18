// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a compiler-generated field for a hoisted iterator (or async) local.
    /// </summary>
    internal sealed class SynthesizedIteratorLocalFieldSymbol : SynthesizedFieldSymbolBase
    {
        private readonly TypeSymbol type;
        private readonly int index;

        public SynthesizedIteratorLocalFieldSymbol(
            NamedTypeSymbol containingType,
            TypeSymbol type,
            string name,
            int index,
            bool isPublic = false,
            bool isReadOnly = false,
            bool isStatic = false)
            : base(containingType, name, isPublic, isReadOnly, isStatic)
        {
            Debug.Assert(index >= 1);
            Debug.Assert((object)type != null);
            this.type = type;
            this.index = index;
        }

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return this.type;
        }

        internal override int IteratorLocalIndex
        {
            get { return index; }
        }
    }
}