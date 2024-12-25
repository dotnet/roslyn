// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordDeconstruct : SynthesizedRecordOrdinaryMethod
    {
        private readonly SynthesizedPrimaryConstructor _ctor;
        private readonly ImmutableArray<Symbol> _positionalMembers;

        public SynthesizedRecordDeconstruct(
            SourceMemberContainerTypeSymbol containingType,
            SynthesizedPrimaryConstructor ctor,
            ImmutableArray<Symbol> positionalMembers,
            int memberOffset)
            : base(containingType, WellKnownMemberNames.DeconstructMethodName, memberOffset,
                   DeclarationModifiers.Public | (IsReadOnly(containingType, positionalMembers) ? DeclarationModifiers.ReadOnly : 0))
        {
            Debug.Assert(positionalMembers.All(p => p is PropertySymbol { GetMethod: not null } or FieldSymbol));
            _ctor = ctor;
            _positionalMembers = positionalMembers;
        }

        protected override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters) MakeParametersAndBindReturnType(BindingDiagnosticBag diagnostics)
        {
            var compilation = DeclaringCompilation;
            var location = ReturnTypeLocation;
            return (ReturnType: TypeWithAnnotations.Create(Binder.GetSpecialType(compilation, SpecialType.System_Void, location, diagnostics)),
                    Parameters: _ctor.Parameters.SelectAsArray<ParameterSymbol, ImmutableArray<Location>, ParameterSymbol>(
                                        (param, locations) =>
                                            new SourceSimpleParameterSymbol(owner: this,
                                                param.TypeWithAnnotations,
                                                param.Ordinal,
                                                RefKind.Out,
                                                param.Name,
                                                locations),
                                        arg: Locations));
        }

        protected override int GetParameterCountFromSyntax() => _ctor.ParameterCount;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, ContainingType.GetNonNullSyntaxNode(), compilationState, diagnostics);

            if (ParameterCount != _positionalMembers.Length)
            {
                // There is a mismatch, an error was reported elsewhere
                F.CloseMethod(F.ThrowNull());
                return;
            }

            var statementsBuilder = ArrayBuilder<BoundStatement>.GetInstance(_positionalMembers.Length + 1);
            for (int i = 0; i < _positionalMembers.Length; i++)
            {
                var parameter = Parameters[i];
                var positionalMember = _positionalMembers[i];

                var type = positionalMember switch
                {
                    PropertySymbol property => property.Type,
                    FieldSymbol field => field.Type,
                    _ => throw ExceptionUtilities.Unreachable()
                };

                if (!parameter.Type.Equals(type, TypeCompareKind.AllIgnoreOptions))
                {
                    // There is a mismatch, an error was reported elsewhere
                    statementsBuilder.Free();
                    F.CloseMethod(F.ThrowNull());
                    return;
                }

                switch (positionalMember)
                {
                    case PropertySymbol property:
                        // parameter_i = property_i;
                        statementsBuilder.Add(F.Assignment(F.Parameter(parameter), F.Property(F.This(), property)));
                        break;
                    case FieldSymbol field:
                        // parameter_i = field_i;
                        statementsBuilder.Add(F.Assignment(F.Parameter(parameter), F.Field(F.This(), field)));
                        break;
                }
            }

            statementsBuilder.Add(F.Return());
            F.CloseMethod(F.Block(statementsBuilder.ToImmutableAndFree()));
        }

        private static bool IsReadOnly(SourceMemberContainerTypeSymbol containingType, ImmutableArray<Symbol> positionalMembers)
        {
            return containingType.IsReadOnly || (containingType.IsRecordStruct && !positionalMembers.Any(static m => hasNonReadOnlyGetter(m)));

            static bool hasNonReadOnlyGetter(Symbol m)
            {
                if (m.Kind is SymbolKind.Property)
                {
                    var property = (PropertySymbol)m;
                    var getterMethod = property.GetMethod;
                    return property.GetMethod is not null && !getterMethod.IsEffectivelyReadOnly;
                }

                return false;
            }
        }
    }
}
