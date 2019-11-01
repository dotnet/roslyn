// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class EEAssemblyBuilder : PEAssemblyBuilderBase
    {
        private readonly Func<NamedTypeSymbol, NamedTypeSymbol> _getDynamicOperationContextType;

        public EEAssemblyBuilder(
            SourceAssemblySymbol sourceAssembly,
            EmitOptions emitOptions,
            ModulePropertiesForSerialization serializationProperties,
            ImmutableArray<NamedTypeSymbol> additionalTypes,
            Func<NamedTypeSymbol, NamedTypeSymbol> getDynamicOperationContextType,
            CompilationTestData testData) :
            base(
                  sourceAssembly,
                  emitOptions,
                  outputKind: OutputKind.DynamicallyLinkedLibrary,
                  serializationProperties: serializationProperties,
                  manifestResources: SpecializedCollections.EmptyEnumerable<ResourceDescription>(),
                  additionalTypes: additionalTypes)
        {
            _getDynamicOperationContextType = getDynamicOperationContextType;

            if (testData != null)
            {
                this.SetMethodTestData(testData.Methods);
                testData.Module = this;
            }
        }

        protected override IModuleReference TranslateModule(ModuleSymbol symbol, DiagnosticBag diagnostics)
        {
            var moduleSymbol = symbol as PEModuleSymbol;
            if ((object)moduleSymbol != null)
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

        internal override NamedTypeSymbol GetDynamicOperationContextType(NamedTypeSymbol contextType)
        {
            return _getDynamicOperationContextType(contextType);
        }

        public override int CurrentGenerationOrdinal => 0;

        internal override VariableSlotAllocator TryCreateVariableSlotAllocator(MethodSymbol symbol, MethodSymbol topLevelMethod, DiagnosticBag diagnostics)
        {
            var method = symbol as EEMethodSymbol;
            if ((object)method != null)
            {
                var defs = GetLocalDefinitions(method.Locals);
                return new SlotAllocator(defs);
            }
            return null;
        }

        private static ImmutableArray<LocalDefinition> GetLocalDefinitions(ImmutableArray<LocalSymbol> locals)
        {
            var builder = ArrayBuilder<LocalDefinition>.GetInstance();
            foreach (var local in locals)
            {
                if (local.DeclarationKind == LocalDeclarationKind.Constant)
                {
                    continue;
                }
                var def = ToLocalDefinition(local, builder.Count);
                Debug.Assert(((EELocalSymbol)local).Ordinal == def.SlotIndex);
                builder.Add(def);
            }
            return builder.ToImmutableAndFree();
        }

        private static LocalDefinition ToLocalDefinition(LocalSymbol local, int index)
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
                (Cci.ITypeReference)type,
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

            public override LocalDefinition GetPreviousLocal(
                Cci.ITypeReference type,
                ILocalSymbolInternal symbol,
                string nameOpt,
                SynthesizedLocalKind synthesizedKind,
                LocalDebugId id,
                LocalVariableAttributes pdbAttributes,
                LocalSlotConstraints constraints,
                ImmutableArray<bool> dynamicTransformFlags,
                ImmutableArray<string> tupleElementNames)
            {
                var local = symbol as EELocalSymbol;
                if ((object)local == null)
                {
                    return null;
                }

                return _locals[local.Ordinal];
            }

            public override string PreviousStateMachineTypeName
            {
                get { return null; }
            }

            public override bool TryGetPreviousHoistedLocalSlotIndex(SyntaxNode currentDeclarator, Cci.ITypeReference currentType, SynthesizedLocalKind synthesizedKind, LocalDebugId currentId, DiagnosticBag diagnostics, out int slotIndex)
            {
                slotIndex = -1;
                return false;
            }

            public override int PreviousHoistedLocalSlotCount
            {
                get { return 0; }
            }

            public override bool TryGetPreviousAwaiterSlotIndex(Cci.ITypeReference currentType, DiagnosticBag diagnostics, out int slotIndex)
            {
                slotIndex = -1;
                return false;
            }

            public override bool TryGetPreviousClosure(SyntaxNode closureSyntax, out DebugId closureId)
            {
                closureId = default(DebugId);
                return false;
            }

            public override bool TryGetPreviousLambda(SyntaxNode lambdaOrLambdaBodySyntax, bool isLambdaBody, out DebugId lambdaId)
            {
                lambdaId = default(DebugId);
                return false;
            }

            public override int PreviousAwaiterSlotCount
            {
                get { return 0; }
            }

            public override DebugId? MethodId
            {
                get
                {
                    return null;
                }
            }
        }
    }
}
