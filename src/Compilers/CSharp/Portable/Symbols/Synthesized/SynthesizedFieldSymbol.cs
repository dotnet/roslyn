﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a compiler generated field.
    /// </summary>
    /// <summary>
    /// Represents a compiler generated field of given type and name.
    /// </summary>
    internal sealed class SynthesizedFieldSymbol : SynthesizedFieldSymbolBase
    {
        private readonly TypeSymbol _type;

        public SynthesizedFieldSymbol(
            NamedTypeSymbol containingType,
            TypeSymbol type,
            string name,
            bool isPublic = false,
            bool isReadOnly = false,
            bool isStatic = false)
            : base(containingType, name, isPublic, isReadOnly, isStatic)
        {
            Debug.Assert((object)type != null);
            _type = type;
        }

        internal override bool SuppressDynamicAttribute
        {
            get { return true; }
        }

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return _type;
        }
    }
}
