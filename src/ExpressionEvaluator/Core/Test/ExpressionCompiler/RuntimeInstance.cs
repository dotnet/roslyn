// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
extern alias PDB;

using System;
using System.Linq;
using System.Collections.Immutable;
using Microsoft.DiaSymReader;
using PDB::Roslyn.Test.PdbUtilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using System.IO;
using System.Reflection.PortableExecutable;

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

        internal static RuntimeInstance Create(Compilation compilation, DebugInformationFormat debugFormat = 0)
        {
            var pdbStream = (debugFormat != 0) ? new MemoryStream() : null;
            var peImage = compilation.EmitToArray(new EmitOptions(debugInformationFormat: debugFormat), pdbStream: pdbStream);
            var symReader = (debugFormat != 0) ? SymReaderFactory.CreateReader(pdbStream, new PEReader(peImage)) : null;
            var references = compilation.GetEmittedReferences(peImage).AddIntrinsicAssembly();

            return Create(compilation.AssemblyName, references, peImage, symReader);
        }

        internal static RuntimeInstance Create(
            string assemblyName,
            ImmutableArray<MetadataReference> references,
            ImmutableArray<byte> peImage,
            ISymUnmanagedReader symReaderOpt,
            bool includeLocalSignatures = true)
        {
            var exeReference = AssemblyMetadata.CreateFromImage(peImage).GetReference(display: assemblyName);
            var modulesBuilder = ArrayBuilder<ModuleInstance>.GetInstance();
            // Create modules for the references
            modulesBuilder.AddRange(references.Select(r => r.ToModuleInstance(fullImage: null, symReader: null, includeLocalSignatures: includeLocalSignatures)));
            // Create a module for the exe.
            modulesBuilder.Add(exeReference.ToModuleInstance(peImage.ToArray(), symReaderOpt, includeLocalSignatures: includeLocalSignatures));

            var modules = modulesBuilder.ToImmutableAndFree();
            modules.VerifyAllModules();

            return new RuntimeInstance(modules);
        }
    }
}
