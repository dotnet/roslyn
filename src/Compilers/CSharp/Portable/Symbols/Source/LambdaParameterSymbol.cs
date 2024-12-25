// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class LambdaParameterSymbol : SourceComplexParameterSymbolBase
    {
        private readonly TypeWithAnnotations _parameterType;
        private readonly SyntaxList<AttributeListSyntax> _attributeLists;

        public LambdaParameterSymbol(
           LambdaSymbol owner,
           SyntaxReference? syntaxRef,
           SyntaxList<AttributeListSyntax> attributeLists,
           TypeWithAnnotations parameterType,
           int ordinal,
           RefKind refKind,
           ScopedKind scope,
           string name,
           bool isDiscard,
           bool hasParamsModifier,
           Location location)
           : base(owner, ordinal, refKind, name, location, syntaxRef, hasParamsModifier: hasParamsModifier, isParams: hasParamsModifier, isExtensionMethodThis: false, scope)
        {
            _parameterType = parameterType;
            _attributeLists = attributeLists;
            IsDiscard = isDiscard;
        }

        public override TypeWithAnnotations TypeWithAnnotations => _parameterType;

        public override bool IsDiscard { get; }

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

