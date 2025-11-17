// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public SynthesizedRecordBaseEquals(SourceMemberContainerTypeSymbol containingType, int memberOffset)
            : base(containingType, WellKnownMemberNames.ObjectEquals, memberOffset, DeclarationModifiers.Public | DeclarationModifiers.Override | DeclarationModifiers.Sealed)
        {
            Debug.Assert(!containingType.IsRecordStruct);
        }

        protected override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters) MakeParametersAndBindReturnType(BindingDiagnosticBag diagnostics)
        {
            var compilation = DeclaringCompilation;
            var location = ReturnTypeLocation;
            return (ReturnType: TypeWithAnnotations.Create(Binder.GetSpecialType(compilation, SpecialType.System_Boolean, location, diagnostics)),
                    Parameters: ImmutableArray.Create<ParameterSymbol>(
                                    new SourceSimpleParameterSymbol(owner: this,
                                                                    TypeWithAnnotations.Create(ContainingType.BaseTypeNoUseSiteDiagnostics, NullableAnnotation.Annotated),
                                                                    ordinal: 0, RefKind.None, "other", Locations)));
        }

        protected override int GetParameterCountFromSyntax() => 1;

        protected override void MethodChecks(BindingDiagnosticBag diagnostics)
        {
            base.MethodChecks(diagnostics);

            NamedTypeSymbol baseType = ContainingType.BaseTypeNoUseSiteDiagnostics;

            // If the base type is not a record, ERR_BadRecordBase will already be reported.
            // Don't cascade an override error in this case.
            var useSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            if (SynthesizedRecordClone.FindValidCloneMethod(baseType, ref useSiteInfo) is null)
            {
                return;
            }

            var overridden = OverriddenMethod;

            if (overridden is object &&
                !overridden.ContainingType.Equals(baseType, TypeCompareKind.AllIgnoreOptions))
            {
                diagnostics.Add(ErrorCode.ERR_DoesNotOverrideBaseMethod, GetFirstLocation(), this, baseType);
            }
        }

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
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
