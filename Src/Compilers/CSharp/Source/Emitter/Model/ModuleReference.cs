// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal sealed class ModuleReference : Microsoft.Cci.IModuleReference, Microsoft.Cci.IFileReference
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

        void Microsoft.Cci.IReference.Dispatch(Microsoft.Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Microsoft.Cci.IModuleReference)this);
        }

        string Microsoft.Cci.INamedEntity.Name
        {
            get
            {
                return underlyingModule.MetadataName;
            }
        }

        bool Microsoft.Cci.IFileReference.HasMetadata
        {
            get
            {
                return true;
            }
        }

        string Microsoft.Cci.IFileReference.FileName
        {
            get
            {
                return underlyingModule.Name;
            }
        }

        ImmutableArray<byte> Microsoft.Cci.IFileReference.GetHashValue(AssemblyHashAlgorithm algorithmId)
        {
            return underlyingModule.GetHash(algorithmId);
        }

        Microsoft.Cci.IAssemblyReference Microsoft.Cci.IModuleReference.GetContainingAssembly(Microsoft.CodeAnalysis.Emit.Context context)
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

        IEnumerable<Microsoft.Cci.ICustomAttribute> Microsoft.Cci.IReference.GetAttributes(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.ICustomAttribute>();
        }

        Microsoft.Cci.IDefinition Microsoft.Cci.IReference.AsDefinition(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return null;
        }
    }
}
