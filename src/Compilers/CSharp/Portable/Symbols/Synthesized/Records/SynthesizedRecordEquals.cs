// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A strongly-typed `public bool Equals(T other)` method.
    /// There are two types of strongly-typed Equals methods:
    /// the strongly-typed virtual method where T is the containing type; and
    /// overrides of the strongly-typed virtual methods from base record types.
    /// </summary>
    internal sealed class SynthesizedRecordEquals : SynthesizedInstanceMethodSymbol
    {
        private readonly PropertySymbol _equalityContract;
        private readonly MethodSymbol? _otherEqualsMethod;
        private readonly int _memberOffset;

        public override NamedTypeSymbol ContainingType { get; }

        public SynthesizedRecordEquals(
            NamedTypeSymbol containingType,
            TypeSymbol parameterType,
            bool isOverride,
            PropertySymbol equalityContract,
            MethodSymbol? otherEqualsMethod,
            int memberOffset)
        {
            // If the parameter is a struct type, it should be declared `in`
            // and we need to call EnsureIsReadOnlyAttributeExists().
            Debug.Assert(!parameterType.IsStructType());

            var compilation = containingType.DeclaringCompilation;

            _equalityContract = equalityContract;
            _otherEqualsMethod = otherEqualsMethod;
            _memberOffset = memberOffset;

            ContainingType = containingType;
            IsVirtual = !isOverride;
            IsOverride = isOverride;
            Parameters = ImmutableArray.Create(SynthesizedParameterSymbol.Create(
                this,
                TypeWithAnnotations.Create(parameterType, nullableAnnotation: NullableAnnotation.Annotated),
                ordinal: 0,
                RefKind.None));
            ReturnTypeWithAnnotations = TypeWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_Boolean));
        }

        public override string Name => "Equals";

        public override MethodKind MethodKind => MethodKind.Ordinary;

        public override int Arity => 0;

        public override bool IsExtensionMethod => false;

        public override bool HidesBaseMethodsByName => true;

        public override bool IsVararg => false;

        public override bool ReturnsVoid => false;

        public override bool IsAsync => false;

        public override RefKind RefKind => RefKind.None;

        public override ImmutableArray<ParameterSymbol> Parameters { get; }

        public override TypeWithAnnotations ReturnTypeWithAnnotations { get; }

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
            => ImmutableArray<TypeWithAnnotations>.Empty;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public override Symbol? AssociatedSymbol => null;

        public override Symbol ContainingSymbol => ContainingType;

        public override ImmutableArray<Location> Locations => ContainingType.Locations;

        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        public override bool IsStatic => false;

        public override bool IsVirtual { get; }

        public override bool IsOverride { get; }

        public override bool IsAbstract => false;

        public override bool IsSealed => false;

        public override bool IsExtern => false;

        internal override bool HasSpecialName => false;

        internal override LexicalSortKey GetLexicalSortKey() => LexicalSortKey.GetSynthesizedMemberKey(_memberOffset);

        internal override MethodImplAttributes ImplementationAttributes => MethodImplAttributes.Managed;

        internal override bool HasDeclarativeSecurity => false;

        internal override MarshalPseudoCustomAttributeData? ReturnValueMarshallingInformation => null;

        internal override bool RequiresSecurityObject => false;

        internal override CallingConvention CallingConvention => CallingConvention.HasThis;

        internal override bool GenerateDebugInfo => false;

        public override DllImportData? GetDllImportData() => null;

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
            => ImmutableArray<string>.Empty;

        internal override IEnumerable<SecurityAttribute> GetSecurityInformation()
            => Array.Empty<SecurityAttribute>();

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => !IsOverride;

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => true;

        internal override bool SynthesizesLoweredBoundBody => true;

        // Consider the following types:
        //   record A(int X);
        //   record B(int X, int Y) : A(X);
        //   record C(int X, int Y, int Z) : B(X, Y);
        //
        // Each record class defines a strongly-typed Equals method, with derived
        // types overriding the methods from base classes:
        //   class A
        //   {
        //       public virtual bool Equals(A other) => other != null && EqualityContract == other.EqualityContract && X == other.X;
        //   }
        //   class B : A
        //   {
        //       public virtual bool Equals(B other) => base.Equals((A)other) && Y == other.Y;
        //       public override bool Equals(A other) => Equals(other as B);
        //   }
        //   class C : B
        //   {
        //       public virtual bool Equals(C other) => base.Equals((B)other) && Z == other.Z;
        //       public override bool Equals(B other) => Equals(other as C);
        //       public override bool Equals(A other) => Equals(other as C);
        //   }
        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, ContainingType.GetNonNullSyntaxNode(), compilationState, diagnostics);
            var other = F.Parameter(Parameters[0]);
            BoundExpression? retExpr;

            if (IsOverride)
            {
                // This method is an override of a strongly-typed Equals method from a base record type.
                // The definition of the method is as follows, and _otherEqualsMethod
                // is the method to delegate to (see B.Equals(A), C.Equals(A), C.Equals(B) above):
                //
                // override bool Equals(Base other) => Equals(other as Derived);
                retExpr = F.Call(
                    F.This(),
                    _otherEqualsMethod!,
                    F.As(other, ContainingType));
            }
            else
            {
                // This method is the strongly-typed Equals method where the parameter type is
                // the containing type.

                if (_otherEqualsMethod is null)
                {
                    // There are no base record types.
                    // The definition of the method is as follows (see A.Equals(A) above):
                    //
                    // virtual bool Equals(T other) =>
                    //     other != null &&
                    //     EqualityContract == other.EqualityContract &&
                    //     field1 == other.field1 && ... && fieldN == other.fieldN;

                    // other != null
                    Debug.Assert(!other.Type.IsStructType());
                    retExpr = F.ObjectNotEqual(other, F.Null(F.SpecialType(SpecialType.System_Object)));

                    // EqualityContract == other.EqualityContract
                    var contractsEqual = F.Binary(
                        BinaryOperatorKind.ObjectEqual,
                        F.SpecialType(SpecialType.System_Boolean),
                        F.Property(F.This(), _equalityContract),
                        F.Property(other, _equalityContract));

                    retExpr = retExpr is null ? contractsEqual : F.LogicalAnd(retExpr, contractsEqual);
                }
                else
                {
                    // There are base record types.
                    // The definition of the method is as follows, and _otherEqualsMethod
                    // is the corresponding method on the nearest base record type to
                    // delegate to (see B.Equals(B), C.Equals(C) above):
                    //
                    // virtual bool Equals(Derived other) =>
                    //     base.Equals((Base)other) &&
                    //     field1 == other.field1 && ... && fieldN == other.fieldN;
                    retExpr = F.Call(
                        F.Base(_otherEqualsMethod.ContainingType),
                        _otherEqualsMethod!,
                        F.Convert(_otherEqualsMethod.Parameters[0].Type, other));
                }

                // field1 == other.field1 && ... && fieldN == other.fieldN
                // https://github.com/dotnet/roslyn/issues/44895: Should compare fields from non-record base classes.
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
            }

            F.CloseMethod(F.Block(F.Return(retExpr)));
        }
    }
}
