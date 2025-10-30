// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class EEAssemblyBuilder : PEAssemblyBuilderBase
    {
        private readonly Func<NamedTypeSymbol, NamedTypeSymbol> _getDynamicOperationContextType;

        public EEAssemblyBuilder(
            SourceAssemblySymbol sourceAssembly,
            EmitOptions emitOptions,
            Cci.ModulePropertiesForSerialization serializationProperties,
            ImmutableArray<NamedTypeSymbol> additionalTypes,
            Func<NamedTypeSymbol, NamedTypeSymbol> getDynamicOperationContextType,
            CompilationTestData? testData) :
            base(
                  sourceAssembly,
                  emitOptions,
                  outputKind: OutputKind.DynamicallyLinkedLibrary,
                  serializationProperties: serializationProperties,
                  manifestResources: [],
                  additionalTypes: additionalTypes)
        {
            _getDynamicOperationContextType = getDynamicOperationContextType;

            if (testData != null)
            {
                SetTestData(testData);
            }
        }

        protected override Cci.IModuleReference TranslateModule(ModuleSymbol symbol, DiagnosticBag diagnostics)
        {
            if (symbol is PEModuleSymbol moduleSymbol)
            {
                var module = moduleSymbol.Module;
                // Expose the individual runtime Windows.*.winmd modules as assemblies.
                // (The modules were wrapped in a placeholder Windows.winmd assembly
                // in MetadataUtilities.MakeAssemblyReferences.)
                if (MetadataUtilities.IsWindowsComponent(module.MetadataReader, module.Name) &&
                    MetadataUtilities.IsWindowsAssemblyName(moduleSymbol.ContainingAssembly.Name))
                {
                    var identity = module.ReadAssemblyIdentityOrThrow();
                    return new Microsoft.CodeAnalysis.ExpressionEvaluator.AssemblyReference(identity);
                }
            }
            return base.TranslateModule(symbol, diagnostics);
        }

        internal override bool IgnoreAccessibility => true;
        public override EmitBaseline? PreviousGeneration => null;
        public override SymbolChanges? EncSymbolChanges => null;
        public override bool FieldRvaSupported => true;

        public override INamedTypeSymbolInternal? TryGetOrCreateSynthesizedHotReloadExceptionType()
            => null;

        public override IMethodSymbolInternal GetOrCreateHotReloadExceptionConstructorDefinition()
            => throw ExceptionUtilities.Unreachable();

        public override INamedTypeSymbolInternal? GetUsedSynthesizedHotReloadExceptionType()
            => null;

        internal override NamedTypeSymbol GetDynamicOperationContextType(NamedTypeSymbol contextType)
        {
            return _getDynamicOperationContextType(contextType);
        }

        internal override VariableSlotAllocator? TryCreateVariableSlotAllocator(MethodSymbol symbol, MethodSymbol topLevelMethod, DiagnosticBag diagnostics)
            => (symbol is EEMethodSymbol method) ? new SlotAllocator(GetLocalDefinitions(method.Locals, diagnostics)) : null;

        private ImmutableArray<LocalDefinition> GetLocalDefinitions(ImmutableArray<LocalSymbol> locals, DiagnosticBag diagnostics)
        {
            var builder = ArrayBuilder<LocalDefinition>.GetInstance();
            foreach (var local in locals)
            {
                if (local.DeclarationKind == LocalDeclarationKind.Constant)
                {
                    continue;
                }
                var def = ToLocalDefinition(local, builder.Count, diagnostics);
                Debug.Assert(((EELocalSymbol)local).Ordinal == def.SlotIndex);
                builder.Add(def);
            }
            return builder.ToImmutableAndFree();
        }

        private LocalDefinition ToLocalDefinition(LocalSymbol local, int index, DiagnosticBag diagnostics)
        {
            // See EvaluationContext.GetLocals.
            TypeSymbol type;
            LocalSlotConstraints constraints;
            if (local.DeclarationKind == LocalDeclarationKind.FixedVariable)
            {
                type = ((PointerTypeSymbol)local.Type).PointedAtType;
                constraints = LocalSlotConstraints.ByRef | LocalSlotConstraints.Pinned;
            }
            else
            {
                type = local.Type;
                constraints = (local.IsPinned ? LocalSlotConstraints.Pinned : LocalSlotConstraints.None) |
                    ((local.RefKind == RefKind.None) ? LocalSlotConstraints.None : LocalSlotConstraints.ByRef);
            }
            return new LocalDefinition(
                local,
                local.Name,
                Translate(type, syntaxNodeOpt: null, diagnostics),
                slot: index,
                synthesizedKind: local.SynthesizedKind,
                id: LocalDebugId.None,
                pdbAttributes: LocalVariableAttributes.None,
                constraints: constraints,
                dynamicTransformFlags: ImmutableArray<bool>.Empty,
                tupleElementNames: ImmutableArray<string>.Empty);
        }

        private sealed class SlotAllocator : VariableSlotAllocator
        {
            private readonly ImmutableArray<LocalDefinition> _locals;

            internal SlotAllocator(ImmutableArray<LocalDefinition> locals)
            {
                _locals = locals;
            }

            public override void AddPreviousLocals(ArrayBuilder<Cci.ILocalDefinition> builder)
            {
                builder.AddRange(_locals);
            }

            public override LocalDefinition? GetPreviousLocal(
                Cci.ITypeReference type,
                ILocalSymbolInternal symbol,
                string? name,
                SynthesizedLocalKind synthesizedKind,
                LocalDebugId id,
                LocalVariableAttributes pdbAttributes,
                LocalSlotConstraints constraints,
                ImmutableArray<bool> dynamicTransformFlags,
                ImmutableArray<string> tupleElementNames)
            {
                return (symbol is EELocalSymbol local) ? _locals[local.Ordinal] : null;
            }

            public override bool TryGetPreviousHoistedLocalSlotIndex(SyntaxNode currentDeclarator, Cci.ITypeReference currentType, SynthesizedLocalKind synthesizedKind, LocalDebugId currentId, DiagnosticBag diagnostics, out int slotIndex)
            {
                slotIndex = -1;
                return false;
            }

            public override bool TryGetPreviousAwaiterSlotIndex(Cci.ITypeReference currentType, DiagnosticBag diagnostics, out int slotIndex)
            {
                slotIndex = -1;
                return false;
            }

            public override bool TryGetPreviousClosure(SyntaxNode closureSyntax, DebugId? parentClosureId, ImmutableArray<string> structCaptures, out DebugId closureId, out RuntimeRudeEdit? runtimeRudeEdit)
            {
                closureId = default;
                runtimeRudeEdit = null;
                return false;
            }

            public override bool TryGetPreviousLambda(SyntaxNode lambdaOrLambdaBodySyntax, bool isLambdaBody, int closureOrdinal, ImmutableArray<DebugId> structClosureIds, out DebugId lambdaId, out RuntimeRudeEdit? runtimeRudeEdit)
            {
                lambdaId = default;
                runtimeRudeEdit = null;
                return false;
            }

            public override bool TryGetPreviousStateMachineState(SyntaxNode syntax, AwaitDebugId awaitId, out StateMachineState state)
            {
                state = 0;
                return false;
            }

            public override StateMachineState? GetFirstUnusedStateMachineState(bool increasing) => null;
            public override string? PreviousStateMachineTypeName => null;
            public override int PreviousHoistedLocalSlotCount => 0;
            public override int PreviousAwaiterSlotCount => 0;
            public override DebugId? MethodId => null;
        }
    }
}
