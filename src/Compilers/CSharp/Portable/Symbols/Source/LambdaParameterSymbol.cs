﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class LambdaParameterSymbol : SourceComplexParameterSymbolBase
    {
        private readonly SyntaxList<AttributeListSyntax> _attributeLists;
        private readonly DeclarationScope? _effectiveScope;

        public LambdaParameterSymbol(
           LambdaSymbol owner,
           SyntaxReference? syntaxRef,
           SyntaxList<AttributeListSyntax> attributeLists,
           TypeWithAnnotations parameterType,
           int ordinal,
           RefKind refKind,
           DeclarationScope? declaredScope,
           DeclarationScope? effectiveScope,
           string name,
           bool isDiscard,
           bool isParams,
           ImmutableArray<Location> locations)
           : base(owner, ordinal, parameterType, refKind, name, locations, syntaxRef, isParams, isExtensionMethodThis: false, scope: declaredScope)
        {
            Debug.Assert(declaredScope.HasValue != effectiveScope.HasValue);
            _attributeLists = attributeLists;
            _effectiveScope = effectiveScope;
            IsDiscard = isDiscard;
        }

        public override bool IsDiscard { get; }

        internal override DeclarationScope EffectiveScope => _effectiveScope ?? base.EffectiveScope;

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

        internal override bool IsExtensionMethodThis
        {
            get { return false; }
        }

        internal override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations() => OneOrMany.Create(_attributeLists);
    }
}

