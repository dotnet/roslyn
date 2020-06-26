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
    internal sealed class SynthesizedRecordObjEquals : SourceOrdinaryMethodSymbolBase
    {
        private readonly MethodSymbol _typedRecordEquals;
        private readonly int _memberOffset;

        public SynthesizedRecordObjEquals(SourceMemberContainerTypeSymbol containingType, MethodSymbol typedRecordEquals, int memberOffset, DiagnosticBag diagnostics)
            : base(containingType, "Equals", containingType.Locations[0], (CSharpSyntaxNode)containingType.SyntaxReferences[0].GetSyntax(), MethodKind.Ordinary,
                   isIterator: false, isExtensionMethod: false, isPartial: false, hasBody: true, diagnostics)
        {
            var compilation = containingType.DeclaringCompilation;
            _typedRecordEquals = typedRecordEquals;
            _memberOffset = memberOffset;
        }

        protected override DeclarationModifiers MakeDeclarationModifiers(DeclarationModifiers allowedModifiers, DiagnosticBag diagnostics)
        {
            const DeclarationModifiers result = DeclarationModifiers.Public | DeclarationModifiers.Override;
            Debug.Assert((result & ~allowedModifiers) == 0);
            return result;
        }

        protected override bool HasAnyBody => true;

        internal override bool IsExpressionBodied => false;

        public override bool IsImplicitlyDeclared => true;

        protected override Location ReturnTypeLocation => Locations[0];

        protected override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters, bool IsVararg, ImmutableArray<TypeParameterConstraintClause> DeclaredConstraintsForOverrideOrImplement) MakeParametersAndBindReturnType(DiagnosticBag diagnostics)
        {
            var compilation = DeclaringCompilation;
            var location = ReturnTypeLocation;
            return (ReturnType: TypeWithAnnotations.Create(Binder.GetSpecialType(compilation, SpecialType.System_Boolean, location, diagnostics)),
                    Parameters: ImmutableArray.Create<ParameterSymbol>(
                                    new SourceSimpleParameterSymbol(owner: this,
                                                                    TypeWithAnnotations.Create(Binder.GetSpecialType(compilation, SpecialType.System_Object, location, diagnostics), NullableAnnotation.Annotated),
                                                                    ordinal: 0, RefKind.None, "obj", isDiscard: false, Locations)),
                    IsVararg: false,
                    DeclaredConstraintsForOverrideOrImplement: ImmutableArray<TypeParameterConstraintClause>.Empty);
        }

        protected override int GetParameterCountFromSyntax() => 1;

        protected override MethodSymbol? FindExplicitlyImplementedMethod(DiagnosticBag diagnostics) => null;

        protected override void MethodChecks(DiagnosticBag diagnostics)
        {
            base.MethodChecks(diagnostics);

            var overridden = OverriddenMethod?.OriginalDefinition;

            if (overridden is null || (overridden is SynthesizedRecordObjEquals && overridden.DeclaringCompilation == DeclaringCompilation))
            {
                return;
            }

            MethodSymbol leastOverridden = GetLeastOverriddenMethod(accessingTypeOpt: null);

            if (leastOverridden is object &&
                leastOverridden.ReturnType.SpecialType == SpecialType.System_Boolean &&
                leastOverridden.ContainingType.SpecialType != SpecialType.System_Object)
            {
                diagnostics.Add(ErrorCode.ERR_DoesNotOverrideMethodFromObject, Locations[0], this);
            }
        }

        protected override ImmutableArray<TypeParameterSymbol> MakeTypeParameters(CSharpSyntaxNode node, DiagnosticBag diagnostics) => ImmutableArray<TypeParameterSymbol>.Empty;

        public override ImmutableArray<TypeParameterConstraintClause> GetTypeParameterConstraintClauses() => ImmutableArray<TypeParameterConstraintClause>.Empty;


        protected override void PartialMethodChecks(DiagnosticBag diagnostics)
        {
        }

        protected override void ExtensionMethodChecks(DiagnosticBag diagnostics)
        {
        }

        protected override void CompleteAsyncMethodChecksBetweenStartAndFinish()
        {
        }

        protected override TypeSymbol? ExplicitInterfaceType => null;

        protected override void CheckConstraintsForExplicitInterfaceType(ConversionsBase conversions, DiagnosticBag diagnostics)
        {
        }

        protected override SourceMemberMethodSymbol? BoundAttributesSource => null;

        internal override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations() => OneOrMany.Create(default(SyntaxList<AttributeListSyntax>));

        public override string? GetDocumentationCommentXml(CultureInfo? preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default) => null;

        public override bool IsVararg => false;

        public override RefKind RefKind => RefKind.None;

        internal override LexicalSortKey GetLexicalSortKey() => LexicalSortKey.GetSynthesizedMemberKey(_memberOffset);

        internal override bool GenerateDebugInfo => false;

        internal override bool SynthesizesLoweredBoundBody => true;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, ContainingType.GetNonNullSyntaxNode(), compilationState, diagnostics);

            var paramAccess = F.Parameter(Parameters[0]);

            BoundExpression expression;
            if (ContainingType.IsStructType())
            {
                throw ExceptionUtilities.Unreachable;
            }
            else
            {
                // For classes:
                //      return this.Equals(param as ContainingType);
                expression = F.Call(F.This(), _typedRecordEquals, F.As(paramAccess, ContainingType));
            }

            F.CloseMethod(F.Block(ImmutableArray.Create<BoundStatement>(F.Return(expression))));
        }
    }
}
