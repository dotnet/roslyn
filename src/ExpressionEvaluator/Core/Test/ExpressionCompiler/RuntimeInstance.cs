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
            var module = compilation.ToModuleInstance(debugFormat, includeLocalSignatures);

            if (references == null)
            {
                references = compilation.GetEmittedReferences(module.FullImage);
            }

            references = references.Concat(new[] { ExpressionCompilerTestHelpers.IntrinsicAssemblyReference });

            return Create(module, references, includeLocalSignatures);
        }

        internal static RuntimeInstance Create(
            ModuleInstance module,
            IEnumerable<MetadataReference> references,
            bool includeLocalSignatures = true)
        {
            // Create modules for the references and the program
            var modules = ImmutableArray.CreateRange(
                references.Select(r => r.ToModuleInstance(includeLocalSignatures: includeLocalSignatures)).
                Concat(new[] { module }));

            modules.VerifyAllModules();
            return new RuntimeInstance(modules);
        }
    }
}
