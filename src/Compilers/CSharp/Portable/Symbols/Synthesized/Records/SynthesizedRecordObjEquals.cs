// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// The record type includes a synthesized override of object.Equals(object? obj).
    /// It is an error if the override is declared explicitly. The synthesized override
    /// returns Equals(other as R) where R is the record type.
    /// </summary>
    internal sealed class SynthesizedRecordObjEquals : SynthesizedRecordObjectMethod
    {
        private readonly MethodSymbol _typedRecordEquals;

        public SynthesizedRecordObjEquals(SourceMemberContainerTypeSymbol containingType, MethodSymbol typedRecordEquals, int memberOffset, DiagnosticBag diagnostics)
            : base(containingType, WellKnownMemberNames.ObjectEquals, memberOffset, diagnostics)
        {
            _typedRecordEquals = typedRecordEquals;
        }

        protected override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters, bool IsVararg, ImmutableArray<TypeParameterConstraintClause> DeclaredConstraintsForOverrideOrImplementation) MakeParametersAndBindReturnType(DiagnosticBag diagnostics)
        {
            var compilation = DeclaringCompilation;
            var location = ReturnTypeLocation;
            return (ReturnType: TypeWithAnnotations.Create(Binder.GetSpecialType(compilation, SpecialType.System_Boolean, location, diagnostics)),
                    Parameters: ImmutableArray.Create<ParameterSymbol>(
                                    new SourceSimpleParameterSymbol(owner: this,
                                                                    TypeWithAnnotations.Create(Binder.GetSpecialType(compilation, SpecialType.System_Object, location, diagnostics), NullableAnnotation.Annotated),
                                                                    ordinal: 0, RefKind.None, "obj", isDiscard: false, Locations)),
                    IsVararg: false,
                    DeclaredConstraintsForOverrideOrImplementation: ImmutableArray<TypeParameterConstraintClause>.Empty);
        }

        protected override int GetParameterCountFromSyntax() => 1;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, this.SyntaxNode, compilationState, diagnostics);

            try
            {
                var paramAccess = F.Parameter(Parameters[0]);

                BoundExpression expression;
                if (ContainingType.IsStructType())
                {
                    throw ExceptionUtilities.Unreachable;
                }
                else
                {
                    if (_typedRecordEquals.ReturnType.SpecialType != SpecialType.System_Boolean)
                    {
                        // There is a signature mismatch, an error was reported elsewhere
                        F.CloseMethod(F.ThrowNull());
                        return;
                    }

                    // For classes:
                    //      return this.Equals(param as ContainingType);
                    expression = F.Call(F.This(), _typedRecordEquals, F.As(paramAccess, ContainingType));
                }

                F.CloseMethod(F.Block(ImmutableArray.Create<BoundStatement>(F.Return(expression))));
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                F.CloseMethod(F.ThrowNull());
            }
        }
    }
}
