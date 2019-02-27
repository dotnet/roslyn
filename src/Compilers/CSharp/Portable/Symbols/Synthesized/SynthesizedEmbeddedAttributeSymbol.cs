// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Cci;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a compiler generated and embedded attribute type with a single default constructor
    /// </summary>
    internal sealed class SynthesizedEmbeddedAttributeSymbol : SynthesizedEmbeddedAttributeSymbolBase
    {
        private readonly ImmutableArray<MethodSymbol> _constructors;

        public SynthesizedEmbeddedAttributeSymbol(
            AttributeDescription description,
            CSharpCompilation compilation,
            DiagnosticBag diagnostics)
            : base(description, compilation, diagnostics)
        {
            _constructors = ImmutableArray.Create<MethodSymbol>(new SynthesizedEmbeddedAttributeConstructorSymbol(this, m => ImmutableArray<ParameterSymbol>.Empty));
            Debug.Assert(_constructors.Length == description.Signatures.Length);
        }

        public override ImmutableArray<MethodSymbol> Constructors => _constructors;
    }
}
