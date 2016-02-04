// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.DiaSymReader;
using Xunit;

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
                references = ExpressionCompilerTestHelpers.GetEmittedReferences(compilation, module.GetMetadataReader());
            }

            references = references.Concat(new[] { ExpressionCompilerTestHelpers.IntrinsicAssemblyReference });

            return Create(module, references);
        }

        internal static RuntimeInstance Create(
            ModuleInstance module,
            IEnumerable<MetadataReference> references)
        {
            // Create modules for the references and the program
            var modules = ImmutableArray.CreateRange(
                references.Select(r => r.ToModuleInstance()).
                Concat(new[] { module }));

            VerifyAllModules(modules);
            return new RuntimeInstance(modules);
        }

        /// <summary>
        /// Verify the set of module metadata blocks
        /// contains all blocks referenced by the set.
        /// </summary>
        private static void VerifyAllModules(IEnumerable<ModuleInstance> modules)
        {
            var blocks = modules.Select(m => m.MetadataBlock).Select(b => ModuleMetadata.CreateFromMetadata(b.Pointer, b.Size));
            var names = new HashSet<string>(blocks.Select(b => b.Name));
            foreach (var block in blocks)
            {
                foreach (var name in block.GetModuleNames())
                {
                    Assert.True(names.Contains(name));
                }
            }
        }
    }
}
