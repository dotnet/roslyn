// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Unless explicitly declared,  a record includes a synthesized strongly-typed overload
    /// of `Equals(R? other)` where `R` is the record type.
    /// The method is `public`, and the method is `virtual` unless the record type is `sealed`.
    /// </summary>
    internal sealed class SynthesizedRecordEquals : SynthesizedRecordOrdinaryMethod
    {
        private readonly PropertySymbol _equalityContract;

        public SynthesizedRecordEquals(SourceMemberContainerTypeSymbol containingType, PropertySymbol equalityContract, int memberOffset, DiagnosticBag diagnostics)
            : base(containingType, WellKnownMemberNames.ObjectEquals, hasBody: true, memberOffset, diagnostics)
        {
            _equalityContract = equalityContract;
        }

        protected override DeclarationModifiers MakeDeclarationModifiers(DeclarationModifiers allowedModifiers, DiagnosticBag diagnostics)
        {
            DeclarationModifiers result = DeclarationModifiers.Public | (ContainingType.IsSealed ? DeclarationModifiers.None : DeclarationModifiers.Virtual);
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
                                                                    TypeWithAnnotations.Create(ContainingType, NullableAnnotation.Annotated),
                                                                    ordinal: 0, RefKind.None, "other", isDiscard: false, Locations)),
                    IsVararg: false,
                    DeclaredConstraintsForOverrideOrImplementation: ImmutableArray<TypeParameterConstraintClause>.Empty);
        }

        protected override int GetParameterCountFromSyntax() => 1;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, ContainingType.GetNonNullSyntaxNode(), compilationState, diagnostics);

            try
            {
                var other = F.Parameter(Parameters[0]);
                BoundExpression? retExpr;

                // This method is the strongly-typed Equals method where the parameter type is
                // the containing type.

                if (ContainingType.BaseTypeNoUseSiteDiagnostics.IsObjectType())
                {
                    if (_equalityContract.IsStatic || !_equalityContract.Type.Equals(DeclaringCompilation.GetWellKnownType(WellKnownType.System_Type), TypeCompareKind.AllIgnoreOptions))
                    {
                        // There is a signature mismatch, an error was reported elsewhere
                        F.CloseMethod(F.ThrowNull());
                        return;
                    }

                    // There are no base record types.
                    // The definition of the method is as follows
                    //
                    // virtual bool Equals(T other) =>
                    //     other != null &&
                    //     EqualityContract == other.EqualityContract &&
                    //     field1 == other.field1 && ... && fieldN == other.fieldN;

                    // other != null
                    Debug.Assert(!other.Type.IsStructType());
                    retExpr = F.ObjectNotEqual(other, F.Null(F.SpecialType(SpecialType.System_Object)));

                    // EqualityContract == other.EqualityContract
                    var contractsEqual = F.Call(receiver: null, F.WellKnownMethod(WellKnownMember.System_Type__op_Equality),
                                                F.Property(F.This(), _equalityContract),
                                                F.Property(other, _equalityContract));

                    retExpr = F.LogicalAnd(retExpr, contractsEqual);
                }
                else
                {
                    MethodSymbol? baseEquals = ContainingType.GetMembersUnordered().OfType<SynthesizedRecordBaseEquals>().Single().OverriddenMethod;

                    if (baseEquals is null || !baseEquals.ContainingType.Equals(ContainingType.BaseTypeNoUseSiteDiagnostics, TypeCompareKind.AllIgnoreOptions) ||
                        baseEquals.ReturnType.SpecialType != SpecialType.System_Boolean)
                    {
                        // There was a problem with overriding of base equals, an error was reported elsewhere
                        F.CloseMethod(F.ThrowNull());
                        return;
                    }

                    // There are base record types.
                    // The definition of the method is as follows, and baseEquals
                    // is the corresponding method on the nearest base record type to
                    // delegate to:
                    //
                    // virtual bool Equals(Derived other) =>
                    //     base.Equals((Base)other) &&
                    //     field1 == other.field1 && ... && fieldN == other.fieldN;
                    retExpr = F.Call(
                        F.Base(baseEquals.ContainingType),
                        baseEquals,
                        F.Convert(baseEquals.Parameters[0].Type, other));
                }

                // field1 == other.field1 && ... && fieldN == other.fieldN
                var fields = ArrayBuilder<FieldSymbol>.GetInstance();
                foreach (var f in ContainingType.GetFieldsToEmit())
                {
                    if (!f.IsStatic)
                    {
                        fields.Add(f);
                    }
                }
                if (fields.Count > 0)
                {
                    retExpr = MethodBodySynthesizer.GenerateFieldEquals(
                        retExpr,
                        other,
                        fields,
                        F);
                }

                fields.Free();

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
