// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    internal sealed class RuntimeInstance : IDisposable
    {
        internal readonly ImmutableArray<ModuleInstance> Modules;
        internal readonly DebugInformationFormat DebugFormat;

        internal RuntimeInstance(ImmutableArray<ModuleInstance> modules, DebugInformationFormat debugFormat)
        {
            Modules = modules;
            DebugFormat = debugFormat;
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
            return new RuntimeInstance(ImmutableArray.CreateRange(modules), DebugInformationFormat.Pdb);
        }

        internal static RuntimeInstance Create(
            Compilation compilation,
            IEnumerable<MetadataReference> references,
            DebugInformationFormat debugFormat,
            bool includeLocalSignatures,
            bool includeIntrinsicAssembly)
        {
            var module = compilation.ToModuleInstance(debugFormat, includeLocalSignatures);

            references ??= ExpressionCompilerTestHelpers.GetEmittedReferences(compilation, module.GetMetadataReader());

            if (includeIntrinsicAssembly)
            {
                references = references.Concat(new[] { ExpressionCompilerTestHelpers.IntrinsicAssemblyReference });
            }

            return Create(module, references, debugFormat);
        }

        internal static RuntimeInstance Create(
            ModuleInstance module,
            IEnumerable<MetadataReference> references,
            DebugInformationFormat debugFormat)
        {
            // Create modules for the references and the program
            var modules = ImmutableArray.CreateRange(
                references.Select(r => r.ToModuleInstance()).
                Concat(new[] { module }));

            VerifyAllModules(modules);
            return new RuntimeInstance(modules, debugFormat);
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
