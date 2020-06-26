// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordObjEquals : SynthesizedRecordObjectMethod
    {
        private readonly MethodSymbol _typedRecordEquals;

        public SynthesizedRecordObjEquals(SourceMemberContainerTypeSymbol containingType, MethodSymbol typedRecordEquals, int memberOffset, DiagnosticBag diagnostics)
            : base(containingType, "Equals", memberOffset, diagnostics)
        {
            _typedRecordEquals = typedRecordEquals;
        }

        protected override DeclarationModifiers MakeDeclarationModifiers(DeclarationModifiers allowedModifiers, DiagnosticBag diagnostics)
        {
            const DeclarationModifiers result = DeclarationModifiers.Public | DeclarationModifiers.Override;
            Debug.Assert((result & ~allowedModifiers) == 0);
            return result;
        }

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
