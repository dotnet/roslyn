// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordCopyCtor : SynthesizedInstanceConstructor
    {
        private readonly int _memberOffset;

        public SynthesizedRecordCopyCtor(
            SourceMemberContainerTypeSymbol containingType,
            int memberOffset)
            : base(containingType)
        {
            _memberOffset = memberOffset;
            Parameters = ImmutableArray.Create(SynthesizedParameterSymbol.Create(
                this,
                TypeWithAnnotations.Create(
                    isNullableEnabled: true,
                    ContainingType),
                ordinal: 0,
                RefKind.None,
                "original"));
        }

        public override ImmutableArray<ParameterSymbol> Parameters { get; }

        public override Accessibility DeclaredAccessibility => ContainingType.IsSealed ? Accessibility.Private : Accessibility.Protected;

        internal override LexicalSortKey GetLexicalSortKey() => LexicalSortKey.GetSynthesizedMemberKey(_memberOffset);

        internal override void GenerateMethodBodyStatements(SyntheticBoundNodeFactory F, ArrayBuilder<BoundStatement> statements, BindingDiagnosticBag diagnostics)
        {
            // Write assignments to fields
            // .ctor(DerivedRecordType original) : base((BaseRecordType)original)
            // {
            //     this.field1 = parameter.field1
            //     ...
            //     this.fieldN = parameter.fieldN
            // }
            var param = F.Parameter(Parameters[0]);
            foreach (var field in ContainingType.GetFieldsToEmit())
            {
                if (!field.IsStatic)
                {
                    statements.Add(F.Assignment(F.Field(F.This(), field), F.Field(param, field)));
                }
            }

            // Add a sequence point at the end of the copy-constructor, so that a breakpoint placed on the record constructor
            // can be hit whenever a new instance of the record is created (either via a primary constructor or via a copy constructor).
            // 
            // If the record doesn't have base initializer the span overlaps the span of the sequence point generated for primary
            // constructor implicit base initializer:
            //
            // record [|C<T>(int P, int Q)|]               // implicit base constructor initializer span
            // record C<T>(int P, int Q) : [|B(...)|]      // explicit base constructor initializer span
            // record [|C<T>|](int P, int Q)               // copy-constructor span
            //
            // The copy-constructor span does not include the parameter list since the parameters are not in scope.
            var recordDeclaration = (RecordDeclarationSyntax)F.Syntax;
            statements.Add(new BoundSequencePointWithSpan(recordDeclaration, statementOpt: null,
                (recordDeclaration.TypeParameterList == null) ? recordDeclaration.Identifier.Span :
                TextSpan.FromBounds(recordDeclaration.Identifier.Span.Start, recordDeclaration.TypeParameterList.Span.End)));
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);
            Debug.Assert(IsImplicitlyDeclared);
            var compilation = this.DeclaringCompilation;
            AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
            Debug.Assert(WellKnownMembers.IsSynthesizedAttributeOptional(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));

            if (HasSetsRequiredMembersImpl)
            {
                AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Diagnostics_CodeAnalysis_SetsRequiredMembersAttribute__ctor));
            }
        }

        internal static MethodSymbol? FindCopyConstructor(NamedTypeSymbol containingType, NamedTypeSymbol within, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            MethodSymbol? bestCandidate = null;
            int bestModifierCountSoFar = -1; // stays as -1 unless we hit an ambiguity
            foreach (var member in containingType.InstanceConstructors)
            {
                if (HasCopyConstructorSignature(member) &&
                    !member.HasUnsupportedMetadata &&
                    AccessCheck.IsSymbolAccessible(member, within, ref useSiteInfo))
                {
                    // If one has fewer custom modifiers, that is better
                    // (see OverloadResolution.BetterFunctionMember)

                    if (bestCandidate is null && bestModifierCountSoFar < 0)
                    {
                        bestCandidate = member;
                        continue;
                    }

                    if (bestModifierCountSoFar < 0)
                    {
                        bestModifierCountSoFar = bestCandidate.CustomModifierCount();
                    }

                    var memberModCount = member.CustomModifierCount();
                    if (memberModCount > bestModifierCountSoFar)
                    {
                        continue;
                    }

                    if (memberModCount == bestModifierCountSoFar)
                    {
                        bestCandidate = null;
                        continue;
                    }

                    bestCandidate = member;
                    bestModifierCountSoFar = memberModCount;
                }
            }

            return bestCandidate;
        }

        internal static bool IsCopyConstructor(Symbol member)
        {
            if (member is MethodSymbol { ContainingType.IsRecord: true, MethodKind: MethodKind.Constructor } method)
            {
                return HasCopyConstructorSignature(method);
            }

            return false;
        }

        internal static bool HasCopyConstructorSignature(MethodSymbol member)
        {
            NamedTypeSymbol containingType = member.ContainingType;
            return member is MethodSymbol { IsStatic: false, ParameterCount: 1, Arity: 0 } method &&
                method.Parameters[0].Type.Equals(containingType, TypeCompareKind.AllIgnoreOptions) &&
                method.Parameters[0].RefKind == RefKind.None;
        }

        protected sealed override bool HasSetsRequiredMembersImpl
            // If the record type has a required members error, then it does have required members of some kind, we emit the SetsRequiredMembers attribute.
            => ContainingType.HasAnyRequiredMembers || ContainingType.HasRequiredMembersError;
    }
}
