// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.DiaSymReader;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    internal sealed class RuntimeInstance : IDisposable
    {
        internal readonly ImmutableArray<ModuleInstance> Modules;

        internal RuntimeInstance(ImmutableArray<ModuleInstance> modules)
        {
            this.Modules = modules;
        }

        void IDisposable.Dispose()
        {
            foreach (var module in this.Modules)
            {
                module.Dispose();
            }
        }

        internal static RuntimeInstance Create(IEnumerable<ModuleInstance> modules)
        {
            return new RuntimeInstance(ImmutableArray.CreateRange(modules));
        }

        internal static RuntimeInstance Create(
            Compilation compilation,
            IEnumerable<MetadataReference> references = null, 
            DebugInformationFormat debugFormat = 0,
            bool includeLocalSignatures = true)
        {
            var exeModuleInstance = compilation.ToModuleInstance(debugFormat, includeLocalSignatures);

            if (references == null)
            {
                references = compilation.GetEmittedReferences(exeModuleInstance.FullImage);
            }

            references = references.Concat(new[] { ExpressionCompilerTestHelpers.IntrinsicAssemblyReference });

            return Create(references, exeModuleInstance, includeLocalSignatures);
        }

        internal static RuntimeInstance Create(
            IEnumerable<MetadataReference> references,
            ModuleInstance exeModuleInstance,
            bool includeLocalSignatures = true)
        {
            // Create modules for the references and the program
            var modules = ImmutableArray.CreateRange(
                references.Select(r => r.ToModuleInstance(includeLocalSignatures: includeLocalSignatures)).
                Concat(new[] { exeModuleInstance }));

            modules.VerifyAllModules();
            return new RuntimeInstance(modules);
        }

        // TODO: remove
        internal static RuntimeInstance Create(
            IEnumerable<MetadataReference> references,
            ImmutableArray<byte> peImage,
            ISymUnmanagedReader symReaderOpt,
            string assemblyName = null,
            bool includeLocalSignatures = true)
        {
            var exeReference = AssemblyMetadata.CreateFromImage(peImage).GetReference(display: assemblyName);
            var exeModuleInstance = exeReference.ToModuleInstance(peImage, symReaderOpt, includeLocalSignatures: includeLocalSignatures);
            return Create(references, exeModuleInstance, includeLocalSignatures);
        }
    }
}
