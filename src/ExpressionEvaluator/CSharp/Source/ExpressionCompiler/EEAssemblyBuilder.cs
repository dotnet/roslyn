// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Roslyn.Utilities;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class EEAssemblyBuilder : PEAssemblyBuilderBase
    {
        internal readonly ImmutableHashSet<MethodSymbol> Methods;

        private readonly NamedTypeSymbol _dynamicOperationContextType;

        public EEAssemblyBuilder(
            SourceAssemblySymbol sourceAssembly,
            EmitOptions emitOptions,
            ImmutableArray<MethodSymbol> methods,
            ModulePropertiesForSerialization serializationProperties,
            ImmutableArray<NamedTypeSymbol> additionalTypes,
            NamedTypeSymbol dynamicOperationContextType,
            CompilationTestData testData) :
            base(
                  sourceAssembly,
                  emitOptions,
                  outputKind: OutputKind.DynamicallyLinkedLibrary,
                  serializationProperties: serializationProperties,
                  manifestResources: SpecializedCollections.EmptyEnumerable<ResourceDescription>(),
                  additionalTypes: additionalTypes)
        {
            Methods = ImmutableHashSet.CreateRange(methods);
            _dynamicOperationContextType = dynamicOperationContextType;

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

        internal override NamedTypeSymbol DynamicOperationContextType => _dynamicOperationContextType;

        public override int CurrentGenerationOrdinal => 0;

        internal override VariableSlotAllocator TryCreateVariableSlotAllocator(MethodSymbol symbol, MethodSymbol topLevelMethod)
        {
            var method = symbol as EEMethodSymbol;
            if (((object)method != null) && Methods.Contains(method))
            {
                var defs = GetLocalDefinitions(method.Locals);
                return new SlotAllocator(defs);
            }

            Debug.Assert(!Methods.Contains(symbol));
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
                synthesizedKind: (SynthesizedLocalKind)local.SynthesizedKind,
                id: LocalDebugId.None,
                pdbAttributes: Cci.PdbWriter.DefaultLocalAttributesValue,
                constraints: constraints,
                isDynamic: false,
                dynamicTransformFlags: ImmutableArray<TypedConstant>.Empty);
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
                uint pdbAttributes,
                LocalSlotConstraints constraints,
                bool isDynamic,
                ImmutableArray<TypedConstant> dynamicTransformFlags)
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
