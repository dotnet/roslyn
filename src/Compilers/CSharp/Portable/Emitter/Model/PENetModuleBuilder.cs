// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal sealed class PENetModuleBuilder : PEModuleBuilder
    {
        internal PENetModuleBuilder(
            SourceModuleSymbol sourceModule,
            EmitOptions emitOptions,
            Cci.ModulePropertiesForSerialization serializationProperties,
            IEnumerable<ResourceDescription> manifestResources)
            : base(sourceModule, emitOptions, OutputKind.NetModule, serializationProperties, manifestResources)
        {
        }

        internal override SynthesizedAttributeData SynthesizeEmbeddedAttribute()
        {
            // Embedded attributes should never be synthesized in modules.
            throw ExceptionUtilities.Unreachable();
        }

        protected override void AddEmbeddedResourcesFromAddedModules(ArrayBuilder<Cci.ManagedResource> builder, DiagnosticBag diagnostics)
        {
            throw ExceptionUtilities.Unreachable();
        }

        // Emitting netmodules is not supported by EnC.
        public override EmitBaseline? PreviousGeneration => null;
        public override SymbolChanges? EncSymbolChanges => null;
        public override bool FieldRvaSupported => true;
        public override bool MethodImplSupported => true;

        public override INamedTypeSymbolInternal? TryGetOrCreateSynthesizedHotReloadExceptionType()
            => null;

        public override IMethodSymbolInternal GetOrCreateHotReloadExceptionConstructorDefinition()
            => throw ExceptionUtilities.Unreachable();

        public override INamedTypeSymbolInternal? GetUsedSynthesizedHotReloadExceptionType()
            => null;

        public override IEnumerable<Cci.IFileReference> GetFiles(EmitContext context) => SpecializedCollections.EmptyEnumerable<Cci.IFileReference>();
        public override ISourceAssemblySymbolInternal? SourceAssemblyOpt => null;
    }
}
