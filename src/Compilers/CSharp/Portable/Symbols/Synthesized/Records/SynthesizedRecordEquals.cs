// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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
        private readonly PropertySymbol? _equalityContract;

        public SynthesizedRecordEquals(SourceMemberContainerTypeSymbol containingType, PropertySymbol? equalityContract, int memberOffset)
            : base(containingType, WellKnownMemberNames.ObjectEquals, memberOffset,
                   DeclarationModifiers.Public |
                   (containingType.IsSealed ? 0 : DeclarationModifiers.Virtual) |
                   (containingType.IsRecordStruct ? DeclarationModifiers.ReadOnly : 0))
        {
            Debug.Assert(equalityContract is null == containingType.IsRecordStruct);
            _equalityContract = equalityContract;
        }

        protected override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters)
            MakeParametersAndBindReturnType(BindingDiagnosticBag diagnostics)
        {
            var compilation = DeclaringCompilation;
            var location = ReturnTypeLocation;
            var annotation = ContainingType.IsRecordStruct ? NullableAnnotation.Oblivious : NullableAnnotation.Annotated;
            return (ReturnType: TypeWithAnnotations.Create(Binder.GetSpecialType(compilation, SpecialType.System_Boolean, location, diagnostics)),
                    Parameters: ImmutableArray.Create<ParameterSymbol>(
                                    new SourceSimpleParameterSymbol(owner: this,
                                                                    TypeWithAnnotations.Create(ContainingType, annotation),
                                                                    ordinal: 0, RefKind.None, "other", Locations)));
        }

        protected override int GetParameterCountFromSyntax() => 1;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, ContainingType.GetNonNullSyntaxNode(), compilationState, diagnostics);

            try
            {
                var other = F.Parameter(Parameters[0]);
                BoundExpression? retExpr;

                // This method is the strongly-typed Equals method where the parameter type is
                // the containing type.

                bool isRecordStruct = ContainingType.IsRecordStruct;
                if (isRecordStruct)
                {
                    // We'll produce:
                    // bool Equals(T other) =>
                    //     field1 == other.field1 && ... && fieldN == other.fieldN;
                    // or simply true if no fields.
                    retExpr = null;
                }
                else if (ContainingType.BaseTypeNoUseSiteDiagnostics.IsObjectType())
                {
                    Debug.Assert(_equalityContract is not null);
                    if (_equalityContract.GetMethod is null)
                    {
                        // The equality contract isn't usable, an error was reported elsewhere
                        F.CloseMethod(F.ThrowNull());
                        return;
                    }

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
                    //     (object)other == this || (base.Equals((Base)other) &&
                    //     field1 == other.field1 && ... && fieldN == other.fieldN);
                    retExpr = F.Call(
                        F.Base(baseEquals.ContainingType),
                        baseEquals,
                        F.Convert(baseEquals.Parameters[0].Type, other));
                }

                // field1 == other.field1 && ... && fieldN == other.fieldN
                var fields = ArrayBuilder<FieldSymbol>.GetInstance();
                bool foundBadField = false;
                foreach (var f in ContainingType.GetFieldsToEmit())
                {
                    if (!f.IsStatic)
                    {
                        fields.Add(f);

                        var parameterType = f.Type;
                        if (parameterType.IsPointerOrFunctionPointer())
                        {
                            diagnostics.Add(ErrorCode.ERR_BadFieldTypeInRecord, f.GetFirstLocationOrNone(), parameterType);
                            foundBadField = true;
                        }
                        else if (parameterType.IsRestrictedType())
                        {
                            // We'll have reported a diagnostic elsewhere (SourceMemberFieldSymbol.TypeChecks)
                            foundBadField = true;
                        }
                    }
                }

                if (fields.Count > 0 && !foundBadField)
                {
                    retExpr = MethodBodySynthesizer.GenerateFieldEquals(
                        retExpr,
                        other,
                        fields,
                        F);
                }
                else if (retExpr is null)
                {
                    retExpr = F.Literal(true);
                }

                fields.Free();

                if (!isRecordStruct)
                {
                    retExpr = F.LogicalOr(F.ObjectEqual(F.This(), other), retExpr);
                }

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
