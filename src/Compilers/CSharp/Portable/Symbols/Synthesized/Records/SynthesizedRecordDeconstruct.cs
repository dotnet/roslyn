// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordDeconstruct : SynthesizedRecordOrdinaryMethod
    {
        private readonly SynthesizedRecordConstructor _ctor;
        private readonly ImmutableArray<PropertySymbol> _properties;

        public SynthesizedRecordDeconstruct(
            SourceMemberContainerTypeSymbol containingType,
            SynthesizedRecordConstructor ctor,
            ImmutableArray<PropertySymbol> properties,
            int memberOffset,
            DiagnosticBag diagnostics)
            : base(containingType, WellKnownMemberNames.DeconstructMethodName, hasBody: true, memberOffset, diagnostics)
        {
            Debug.Assert(properties.All(prop => prop.GetMethod is object));
            _ctor = ctor;
            _properties = properties;
        }

        protected override DeclarationModifiers MakeDeclarationModifiers(DeclarationModifiers allowedModifiers, DiagnosticBag diagnostics)
        {
            const DeclarationModifiers result = DeclarationModifiers.Public;
            Debug.Assert((result & ~allowedModifiers) == 0);
            return result;
        }

        protected override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters, bool IsVararg, ImmutableArray<TypeParameterConstraintClause> DeclaredConstraintsForOverrideOrImplementation) MakeParametersAndBindReturnType(DiagnosticBag diagnostics)
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
                                                isDiscard: false,
                                                locations),
                                        arg: Locations),
                    IsVararg: false,
                    DeclaredConstraintsForOverrideOrImplementation: ImmutableArray<TypeParameterConstraintClause>.Empty);
        }

        protected override int GetParameterCountFromSyntax() => _ctor.ParameterCount;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, ContainingType.GetNonNullSyntaxNode(), compilationState, diagnostics);

            if (ParameterCount != _properties.Length)
            {
                // There is a mismatch, an error was reported elsewhere
                F.CloseMethod(F.ThrowNull());
                return;
            }

            var statementsBuilder = ArrayBuilder<BoundStatement>.GetInstance(_properties.Length + 1);
            for (int i = 0; i < _properties.Length; i++)
            {
                var parameter = Parameters[i];
                var property = _properties[i];

                if (!parameter.Type.Equals(property.Type, TypeCompareKind.AllIgnoreOptions))
                {
                    // There is a mismatch, an error was reported elsewhere
                    statementsBuilder.Free();
                    F.CloseMethod(F.ThrowNull());
                    return;
                }

                // parameter_i = property_i;
                statementsBuilder.Add(F.Assignment(F.Parameter(parameter), F.Property(F.This(), property)));
            }

            statementsBuilder.Add(F.Return());
            F.CloseMethod(F.Block(statementsBuilder.ToImmutableAndFree()));
        }
    }
}
