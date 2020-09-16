// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Common base for ordinary methods synthesized by compiler for records.
    /// </summary>
    internal abstract class SynthesizedRecordOrdinaryMethod : SourceOrdinaryMethodSymbolBase
    {
        private readonly int _memberOffset;

        protected SynthesizedRecordOrdinaryMethod(SourceMemberContainerTypeSymbol containingType, string name, bool hasBody, int memberOffset, DiagnosticBag diagnostics)
            : base(containingType, name, containingType.Locations[0], (CSharpSyntaxNode)containingType.SyntaxReferences[0].GetSyntax(), MethodKind.Ordinary,
                   isIterator: false, isExtensionMethod: false, isPartial: false, hasBody, diagnostics)
        {
            _memberOffset = memberOffset;
        }

        protected sealed override bool HasAnyBody => true;

        internal sealed override bool IsExpressionBodied => false;

        public sealed override bool IsImplicitlyDeclared => true;

        protected sealed override Location ReturnTypeLocation => Locations[0];

        protected sealed override MethodSymbol? FindExplicitlyImplementedMethod(DiagnosticBag diagnostics) => null;

        internal sealed override LexicalSortKey GetLexicalSortKey() => LexicalSortKey.GetSynthesizedMemberKey(_memberOffset);

        protected sealed override ImmutableArray<TypeParameterSymbol> MakeTypeParameters(CSharpSyntaxNode node, DiagnosticBag diagnostics) => ImmutableArray<TypeParameterSymbol>.Empty;

        public sealed override ImmutableArray<TypeParameterConstraintClause> GetTypeParameterConstraintClauses() => ImmutableArray<TypeParameterConstraintClause>.Empty;

        protected sealed override void PartialMethodChecks(DiagnosticBag diagnostics)
        {
        }

        protected sealed override void ExtensionMethodChecks(DiagnosticBag diagnostics)
        {
        }

        protected sealed override void CompleteAsyncMethodChecksBetweenStartAndFinish()
        {
        }

        protected sealed override TypeSymbol? ExplicitInterfaceType => null;

        protected sealed override void CheckConstraintsForExplicitInterfaceType(ConversionsBase conversions, DiagnosticBag diagnostics)
        {
        }

        protected sealed override SourceMemberMethodSymbol? BoundAttributesSource => null;

        internal sealed override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations() => OneOrMany.Create(default(SyntaxList<AttributeListSyntax>));

        public sealed override string? GetDocumentationCommentXml(CultureInfo? preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default) => null;

        public sealed override bool IsVararg => false;

        public sealed override RefKind RefKind => RefKind.None;

        internal sealed override bool GenerateDebugInfo => false;

        internal sealed override bool SynthesizesLoweredBoundBody => true;
    }
}
