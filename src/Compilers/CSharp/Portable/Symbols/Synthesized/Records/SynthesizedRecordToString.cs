// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// The record includes a synthesized override of object.ToString().
    /// For `record R(int I) { public int J; }` it prints `R { I = ..., J = ... }`.
    /// 
    /// The method can be declared explicitly. It is an error if the explicit 
    /// declaration does not match the expected signature or accessibility, or 
    /// if the explicit declaration doesn't allow overriding it in a derived type and 
    /// the record type is not sealed. 
    /// It is an error if either synthesized, or explicitly declared method doesn't 
    /// override `object.ToString()` (for example, due to shadowing in intermediate base types, etc.).
    /// </summary>
    internal sealed class SynthesizedRecordToString : SynthesizedRecordObjectMethod
    {
        MethodSymbol _printMethod;
        public SynthesizedRecordToString(SourceMemberContainerTypeSymbol containingType, MethodSymbol printMethod, int memberOffset, DiagnosticBag diagnostics)
            : base(containingType, WellKnownMemberNames.ObjectToString, memberOffset, diagnostics)
        {
            Debug.Assert(printMethod is object);
            _printMethod = printMethod;
        }

        public override TypeWithAnnotations ReturnTypeWithAnnotations => TypeWithAnnotations.Create(
            isNullableEnabled: true,
            ContainingType.DeclaringCompilation.GetSpecialType(SpecialType.System_String));

        protected override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters, bool IsVararg, ImmutableArray<TypeParameterConstraintClause> DeclaredConstraintsForOverrideOrImplementation) MakeParametersAndBindReturnType(DiagnosticBag diagnostics)
        {
            var compilation = DeclaringCompilation;
            var location = ReturnTypeLocation;
            return (ReturnType: TypeWithAnnotations.Create(Binder.GetSpecialType(compilation, SpecialType.System_String, location, diagnostics)),
                    Parameters: ImmutableArray<ParameterSymbol>.Empty,
                    IsVararg: false,
                    DeclaredConstraintsForOverrideOrImplementation: ImmutableArray<TypeParameterConstraintClause>.Empty);
        }

        protected override int GetParameterCountFromSyntax() => 0;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, this.SyntaxNode, compilationState, diagnostics);

            try
            {
                CSharpCompilation compilation = ContainingType.DeclaringCompilation;
                var stringBuilder = F.WellKnownType(WellKnownType.System_Text_StringBuilder);
                var stringBuilderCtor = F.WellKnownMethod(WellKnownMember.System_StringBuilder__ctor);
                var stringBuilderAppend = F.WellKnownMethod(WellKnownMember.System_StringBuilder__AppendString);

                var builderLocal = F.SynthesizedLocal(stringBuilder);
                var block = ArrayBuilder<BoundStatement>.GetInstance();
                // var builder = new StringBuilder();
                block.Add(F.Assignment(F.Local(builderLocal), F.New(stringBuilderCtor)));

                // builder.Append(<name>);
                block.Add(F.ExpressionStatement(F.Call(F.Local(builderLocal), stringBuilderAppend, F.StringLiteral(ContainingType.Name))));

                // builder.Append(" { ");
                block.Add(F.ExpressionStatement(F.Call(F.Local(builderLocal), stringBuilderAppend, F.StringLiteral(" { "))));

                // this.print(builder);
                block.Add(F.ExpressionStatement(F.Call(F.This(), _printMethod, F.Local(builderLocal))));

                // builder.Append(" } ");
                block.Add(F.ExpressionStatement(F.Call(F.Local(builderLocal), stringBuilderAppend, F.StringLiteral(" } "))));

                // return builder.ToString();
                block.Add(F.Return(F.Call(F.Local(builderLocal), F.SpecialMethod(SpecialMember.System_Object__ToString))));

                F.CloseMethod(F.Block(ImmutableArray.Create(builderLocal), block.ToImmutableAndFree()));
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                F.CloseMethod(F.ThrowNull());
            }
        }
    }
}
