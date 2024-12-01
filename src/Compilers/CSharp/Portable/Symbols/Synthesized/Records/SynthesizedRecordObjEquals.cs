// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

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

        public SynthesizedRecordObjEquals(SourceMemberContainerTypeSymbol containingType, MethodSymbol typedRecordEquals, int memberOffset)
            : base(containingType, WellKnownMemberNames.ObjectEquals, memberOffset, isReadOnly: containingType.IsRecordStruct)
        {
            _typedRecordEquals = typedRecordEquals;
        }

        protected override SpecialMember OverriddenSpecialMember => SpecialMember.System_Object__Equals;

        protected override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters)
            MakeParametersAndBindReturnType(BindingDiagnosticBag diagnostics)
        {
            var compilation = DeclaringCompilation;
            var location = ReturnTypeLocation;
            var annotation = ContainingType.IsRecordStruct ? NullableAnnotation.Oblivious : NullableAnnotation.Annotated;
            return (ReturnType: TypeWithAnnotations.Create(Binder.GetSpecialType(compilation, SpecialType.System_Boolean, location, diagnostics)),
                    Parameters: ImmutableArray.Create<ParameterSymbol>(
                                    new SourceSimpleParameterSymbol(owner: this,
                                                                    TypeWithAnnotations.Create(Binder.GetSpecialType(compilation, SpecialType.System_Object, location, diagnostics), annotation),
                                                                    ordinal: 0, RefKind.None, "obj", Locations)));
        }

        protected override int GetParameterCountFromSyntax() => 1;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, this.SyntaxNode, compilationState, diagnostics);

            try
            {
                if (_typedRecordEquals.ReturnType.SpecialType != SpecialType.System_Boolean)
                {
                    // There is a signature mismatch, an error was reported elsewhere
                    F.CloseMethod(F.ThrowNull());
                    return;
                }

                var paramAccess = F.Parameter(Parameters[0]);

                BoundExpression expression;
                if (ContainingType.IsRecordStruct)
                {
                    // For record structs:
                    //      return other is R && Equals((R)other)
                    expression = F.LogicalAnd(
                        F.Is(paramAccess, ContainingType),
                        F.Call(F.This(), _typedRecordEquals, F.Convert(ContainingType, paramAccess)));
                }
                else
                {
                    // For record classes:
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
