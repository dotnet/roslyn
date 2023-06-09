// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    /// It is an error if either synthesized or explicitly declared method doesn't
    /// override `object.ToString()` (for example, due to shadowing in intermediate base types, etc.).
    /// </summary>
    internal sealed class SynthesizedRecordToString : SynthesizedRecordObjectMethod
    {
        private readonly MethodSymbol _printMethod;
        public SynthesizedRecordToString(SourceMemberContainerTypeSymbol containingType, MethodSymbol printMethod, int memberOffset, bool isReadOnly, BindingDiagnosticBag diagnostics)
            : base(
                  containingType,
                  WellKnownMemberNames.ObjectToString,
                  memberOffset,
                  isReadOnly: isReadOnly,
                  diagnostics)
        {
            Debug.Assert(printMethod is object);
            _printMethod = printMethod;
        }

        protected override SpecialMember OverriddenSpecialMember => SpecialMember.System_Object__ToString;

        protected override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters, ImmutableArray<TypeParameterConstraintClause> DeclaredConstraintsForOverrideOrImplementation) MakeParametersAndBindReturnType(BindingDiagnosticBag diagnostics)
        {
            var compilation = DeclaringCompilation;
            var location = ReturnTypeLocation;
            var annotation = ContainingType.IsRecordStruct ? NullableAnnotation.Oblivious : NullableAnnotation.NotAnnotated;
            return (ReturnType: TypeWithAnnotations.Create(Binder.GetSpecialType(compilation, SpecialType.System_String, location, diagnostics), annotation),
                    Parameters: ImmutableArray<ParameterSymbol>.Empty,
                    DeclaredConstraintsForOverrideOrImplementation: ImmutableArray<TypeParameterConstraintClause>.Empty);
        }

        protected override int GetParameterCountFromSyntax() => 0;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, this.SyntaxNode, compilationState, diagnostics);

            try
            {
                CSharpCompilation compilation = ContainingType.DeclaringCompilation;
                var stringBuilder = F.WellKnownType(WellKnownType.System_Text_StringBuilder);
                var stringBuilderCtor = F.WellKnownMethod(WellKnownMember.System_Text_StringBuilder__ctor);

                var builderLocalSymbol = F.SynthesizedLocal(stringBuilder);
                BoundLocal builderLocal = F.Local(builderLocalSymbol);
                var block = ArrayBuilder<BoundStatement>.GetInstance();
                // var builder = new StringBuilder();
                block.Add(F.Assignment(builderLocal, F.New(stringBuilderCtor)));

                // builder.Append(<name>);
                block.Add(makeAppendString(F, builderLocal, ContainingType.Name));

                // builder.Append(" { ");
                block.Add(makeAppendString(F, builderLocal, " { "));

                // if (this.PrintMembers(builder)) builder.Append(' ');
                block.Add(F.If(F.Call(F.This(), _printMethod, builderLocal), makeAppendChar(F, builderLocal, ' ')));

                // builder.Append('}');
                block.Add(makeAppendChar(F, builderLocal, '}'));

                // return builder.ToString();
                block.Add(F.Return(F.Call(builderLocal, F.SpecialMethod(SpecialMember.System_Object__ToString))));

                F.CloseMethod(F.Block(ImmutableArray.Create(builderLocalSymbol), block.ToImmutableAndFree()));
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                F.CloseMethod(F.ThrowNull());
            }

            static BoundStatement makeAppendString(SyntheticBoundNodeFactory F, BoundLocal builder, string value)
            {
                return F.ExpressionStatement(F.Call(receiver: builder, F.WellKnownMethod(WellKnownMember.System_Text_StringBuilder__AppendString), F.StringLiteral(value)));
            }

            static BoundStatement makeAppendChar(SyntheticBoundNodeFactory F, BoundLocal builder, char value)
            {
                return F.ExpressionStatement(F.Call(receiver: builder, F.WellKnownMethod(WellKnownMember.System_Text_StringBuilder__AppendChar), F.CharLiteral(value)));
            }
        }
    }
}
