// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class LambdaParameterSymbol : SourceComplexParameterSymbol
    {
        private readonly SyntaxList<AttributeListSyntax> _attributeLists;

        public LambdaParameterSymbol(
           LambdaSymbol owner,
           SyntaxList<AttributeListSyntax> attributeLists,
           TypeWithAnnotations parameterType,
           int ordinal,
           RefKind refKind,
           string name,
           bool isDiscard,
           ImmutableArray<Location> locations)
           : base(owner, ordinal, parameterType, refKind, name, locations, syntaxRef: null, isParams: false, isExtensionMethodThis: false)
        {
            _attributeLists = attributeLists;
            IsDiscard = isDiscard;
        }

        public override bool IsDiscard { get; }

        internal override bool IsMetadataOptional
        {
            get { return false; }
        }

        public override bool IsParams
        {
            get { return false; }
        }

        internal override bool HasDefaultArgumentSyntax
        {
            get { return false; }
        }

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

