// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal sealed class ModuleReference : Cci.IModuleReference, Cci.IFileReference
    {
        private readonly PEModuleBuilder moduleBeingBuilt;
        private readonly ModuleSymbol underlyingModule;

        internal ModuleReference(PEModuleBuilder moduleBeingBuilt, ModuleSymbol underlyingModule)
        {
            Debug.Assert(moduleBeingBuilt != null);
            Debug.Assert((object)underlyingModule != null);

            this.moduleBeingBuilt = moduleBeingBuilt;
            this.underlyingModule = underlyingModule;
        }

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Cci.IModuleReference)this);
        }

        string Cci.INamedEntity.Name
        {
            get
            {
                return underlyingModule.MetadataName;
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
                return underlyingModule.Name;
            }
        }

        ImmutableArray<byte> Cci.IFileReference.GetHashValue(AssemblyHashAlgorithm algorithmId)
        {
            return underlyingModule.GetHash(algorithmId);
        }

        Cci.IAssemblyReference Cci.IModuleReference.GetContainingAssembly(EmitContext context)
        {
            if (this.moduleBeingBuilt.OutputKind.IsNetModule() &&
                ReferenceEquals(moduleBeingBuilt.SourceModule.ContainingAssembly, underlyingModule.ContainingAssembly))
            {
                return null;
            }

            return moduleBeingBuilt.Translate(underlyingModule.ContainingAssembly, context.Diagnostics);
        }

        public override string ToString()
        {
            return underlyingModule.ToString();
        }

        IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(EmitContext context)
        {
            return SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();
        }

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            return null;
        }
    }
}
