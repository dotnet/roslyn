// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal sealed class ModuleReference : Cci.IModuleReference, Cci.IFileReference
    {
        private readonly PEModuleBuilder _moduleBeingBuilt;
        private readonly ModuleSymbol _underlyingModule;

        internal ModuleReference(PEModuleBuilder moduleBeingBuilt, ModuleSymbol underlyingModule)
        {
            Debug.Assert(moduleBeingBuilt != null);
            Debug.Assert((object)underlyingModule != null);

            _moduleBeingBuilt = moduleBeingBuilt;
            _underlyingModule = underlyingModule;
        }

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Cci.IModuleReference)this);
        }

        string Cci.INamedEntity.Name
        {
            get
            {
                return _underlyingModule.MetadataName;
            }
        }

        bool Cci.IFileReference.HasMetadata
        {
            get
            {
                return true;
            }
        }

        string Cci.IFileReference.FileName
        {
            get
            {
                return _underlyingModule.Name;
            }
        }

        ImmutableArray<byte> Cci.IFileReference.GetHashValue(AssemblyHashAlgorithm algorithmId)
        {
            return _underlyingModule.GetHash(algorithmId);
        }

        Cci.IAssemblyReference Cci.IModuleReference.GetContainingAssembly(EmitContext context)
        {
            if (_moduleBeingBuilt.OutputKind.IsNetModule() &&
                ReferenceEquals(_moduleBeingBuilt.SourceModule.ContainingAssembly, _underlyingModule.ContainingAssembly))
            {
                return null;
            }

            return _moduleBeingBuilt.Translate(_underlyingModule.ContainingAssembly, context.Diagnostics);
        }

        public override string ToString()
        {
            return _underlyingModule.ToString();
        }

        IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(EmitContext context)
        {
            return SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();
        }

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            return null;
        }

        CodeAnalysis.Symbols.ISymbolInternal Cci.IReference.GetInternalSymbol() => null;
    }
}
