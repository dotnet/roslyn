// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// If the record type is derived from a base record type Base, the record type includes
    /// a synthesized override of the strongly-typed Equals(Base other). The synthesized
    /// override is sealed. It is an error if the override is declared explicitly.
    /// The synthesized override returns Equals((object?)other).
    /// </summary>
    internal sealed class SynthesizedRecordBaseEquals : SynthesizedRecordOrdinaryMethod
    {
        public SynthesizedRecordBaseEquals(SourceMemberContainerTypeSymbol containingType, int memberOffset, DiagnosticBag diagnostics)
            : base(containingType, WellKnownMemberNames.ObjectEquals, hasBody: true, memberOffset, diagnostics)
        {
        }

        protected override DeclarationModifiers MakeDeclarationModifiers(DeclarationModifiers allowedModifiers, DiagnosticBag diagnostics)
        {
            const DeclarationModifiers result = DeclarationModifiers.Public | DeclarationModifiers.Override | DeclarationModifiers.Sealed;
            Debug.Assert((result & ~allowedModifiers) == 0);
            return result;
        }

        protected override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters, bool IsVararg, ImmutableArray<TypeParameterConstraintClause> DeclaredConstraintsForOverrideOrImplementation) MakeParametersAndBindReturnType(DiagnosticBag diagnostics)
        {
            var compilation = DeclaringCompilation;
            var location = ReturnTypeLocation;
            return (ReturnType: TypeWithAnnotations.Create(Binder.GetSpecialType(compilation, SpecialType.System_Boolean, location, diagnostics)),
                    Parameters: ImmutableArray.Create<ParameterSymbol>(
                                    new SourceSimpleParameterSymbol(owner: this,
                                                                    TypeWithAnnotations.Create(ContainingType.BaseTypeNoUseSiteDiagnostics, NullableAnnotation.Annotated),
                                                                    ordinal: 0, RefKind.None, "other", isDiscard: false, Locations)),
                    IsVararg: false,
                    DeclaredConstraintsForOverrideOrImplementation: ImmutableArray<TypeParameterConstraintClause>.Empty);
        }

        protected override int GetParameterCountFromSyntax() => 1;

        protected override void MethodChecks(DiagnosticBag diagnostics)
        {
            base.MethodChecks(diagnostics);

            var overridden = OverriddenMethod;

            if (overridden is object &&
                !overridden.ContainingType.Equals(ContainingType.BaseTypeNoUseSiteDiagnostics, TypeCompareKind.AllIgnoreOptions))
            {
                diagnostics.Add(ErrorCode.ERR_DoesNotOverrideBaseEquals, Locations[0], this, ContainingType.BaseTypeNoUseSiteDiagnostics);
            }
        }

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, this.SyntaxNode, compilationState, diagnostics);

            try
            {
                ParameterSymbol parameter = Parameters[0];

                if (parameter.Type.IsErrorType())
                {
                    F.CloseMethod(F.ThrowNull());
                    return;
                }

                var retExpr = F.Call(
                    F.This(),
                    ContainingType.GetMembersUnordered().OfType<SynthesizedRecordObjEquals>().Single(),
                    F.Convert(F.SpecialType(SpecialType.System_Object), F.Parameter(parameter)));

                F.CloseMethod(F.Block(F.Return(retExpr)));
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                F.CloseMethod(F.ThrowNull());
            }
        }
    }
}
