// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// The record type includes synthesized '==' and '!=' operators equivalent to operators declared as follows:
    /// 
    /// public static bool operator==(R? r1, R? r2)
    ///      => (object) r1 == r2 || ((object)r1 != null &amp;&amp; r1.Equals(r2));
    /// public static bool operator !=(R? r1, R? r2)
    ///      => !(r1 == r2);
    ///        
    ///The 'Equals' method called by the '==' operator is the 'Equals(R? other)' (<see cref="SynthesizedRecordEquals"/>).
    ///The '!=' operator delegates to the '==' operator. It is an error if the operators are declared explicitly.
    /// </summary>
    internal abstract class SynthesizedRecordEqualityOperatorBase : SourceUserDefinedOperatorSymbolBase
    {
        private readonly int _memberOffset;

        protected SynthesizedRecordEqualityOperatorBase(SourceMemberContainerTypeSymbol containingType, string name, int memberOffset, DiagnosticBag diagnostics)
            : base(MethodKind.UserDefinedOperator, name, containingType, containingType.Locations[0], (CSharpSyntaxNode)containingType.SyntaxReferences[0].GetSyntax(),
                   DeclarationModifiers.Public | DeclarationModifiers.Static, hasBody: true, isExpressionBodied: false, isIterator: false, diagnostics)
        {
            Debug.Assert(name == WellKnownMemberNames.EqualityOperatorName || name == WellKnownMemberNames.InequalityOperatorName);
            _memberOffset = memberOffset;
        }

        public sealed override bool IsImplicitlyDeclared => true;

        protected sealed override Location ReturnTypeLocation => Locations[0];

        internal sealed override LexicalSortKey GetLexicalSortKey() => LexicalSortKey.GetSynthesizedMemberKey(_memberOffset);

        protected sealed override SourceMemberMethodSymbol? BoundAttributesSource => null;

        internal sealed override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations() => OneOrMany.Create(default(SyntaxList<AttributeListSyntax>));

        public sealed override string? GetDocumentationCommentXml(CultureInfo? preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default) => null;

        internal sealed override bool GenerateDebugInfo => false;

        internal sealed override bool SynthesizesLoweredBoundBody => true;

        protected sealed override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters) MakeParametersAndBindReturnType(DiagnosticBag diagnostics)
        {
            var compilation = DeclaringCompilation;
            var location = ReturnTypeLocation;
            return (ReturnType: TypeWithAnnotations.Create(Binder.GetSpecialType(compilation, SpecialType.System_Boolean, location, diagnostics)),
                    Parameters: ImmutableArray.Create<ParameterSymbol>(
                                    new SourceSimpleParameterSymbol(owner: this,
                                                                    TypeWithAnnotations.Create(ContainingType, NullableAnnotation.Annotated),
                                                                    ordinal: 0, RefKind.None, "r1", isDiscard: false, Locations),
                                    new SourceSimpleParameterSymbol(owner: this,
                                                                    TypeWithAnnotations.Create(ContainingType, NullableAnnotation.Annotated),
                                                                    ordinal: 1, RefKind.None, "r2", isDiscard: false, Locations)));
        }

        protected override int GetParameterCountFromSyntax() => 2;
    }
}
