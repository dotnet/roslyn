﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    /// For record class:
    /// public static bool operator==(R? left, R? right)
    ///      => (object) left == right || ((object)left != null &amp;&amp; left.Equals(right));
    /// public static bool operator !=(R? left, R? right)
    ///      => !(left == right);
    ///
    /// For record struct:
    /// public static bool operator==(R left, R right)
    ///      => left.Equals(right);
    /// public static bool operator !=(R left, R right)
    ///      => !(left == right);
    ///
    ///The 'Equals' method called by the '==' operator is the 'Equals(R? other)' (<see cref="SynthesizedRecordEquals"/>).
    ///The '!=' operator delegates to the '==' operator. It is an error if the operators are declared explicitly.
    /// </summary>
    internal abstract class SynthesizedRecordEqualityOperatorBase : SourceUserDefinedOperatorSymbolBase
    {
        private readonly int _memberOffset;

        protected SynthesizedRecordEqualityOperatorBase(SourceMemberContainerTypeSymbol containingType, string name, int memberOffset, BindingDiagnosticBag diagnostics)
            : base(MethodKind.UserDefinedOperator, explicitInterfaceType: null, name, containingType, containingType.Locations[0], (CSharpSyntaxNode)containingType.SyntaxReferences[0].GetSyntax(),
                   DeclarationModifiers.Public | DeclarationModifiers.Static, hasBody: true, isExpressionBodied: false, isIterator: false, isNullableAnalysisEnabled: false, diagnostics)
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

        protected sealed override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters) MakeParametersAndBindReturnType(BindingDiagnosticBag diagnostics)
        {
            var compilation = DeclaringCompilation;
            var location = ReturnTypeLocation;
            var annotation = ContainingType.IsRecordStruct ? NullableAnnotation.Oblivious : NullableAnnotation.Annotated;
            return (ReturnType: TypeWithAnnotations.Create(Binder.GetSpecialType(compilation, SpecialType.System_Boolean, location, diagnostics)),
                    Parameters: ImmutableArray.Create<ParameterSymbol>(
                                    new SourceSimpleParameterSymbol(owner: this,
                                                                    TypeWithAnnotations.Create(ContainingType, annotation),
                                                                    ordinal: 0, RefKind.None, "left", Locations),
                                    new SourceSimpleParameterSymbol(owner: this,
                                                                    TypeWithAnnotations.Create(ContainingType, annotation),
                                                                    ordinal: 1, RefKind.None, "right", Locations)));
        }

        protected override int GetParameterCountFromSyntax() => 2;
    }
}
