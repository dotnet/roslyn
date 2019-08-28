// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
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
            throw ExceptionUtilities.Unreachable;
        }

        protected override void AddEmbeddedResourcesFromAddedModules(ArrayBuilder<Cci.ManagedResource> builder, DiagnosticBag diagnostics)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override int CurrentGenerationOrdinal => 0;
        public override IEnumerable<Cci.IFileReference> GetFiles(EmitContext context) => SpecializedCollections.EmptyEnumerable<Cci.IFileReference>();
        public override ISourceAssemblySymbolInternal SourceAssemblyOpt => null;
    }
}
