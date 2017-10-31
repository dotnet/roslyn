// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal sealed class AssemblyReference : Cci.IAssemblyReference
    {
        // assembly symbol that represents the target assembly:
        private readonly AssemblySymbol _targetAssembly;

        internal AssemblyReference(AssemblySymbol assemblySymbol)
        {
            Debug.Assert((object)assemblySymbol != null);
            _targetAssembly = assemblySymbol;
        }

        /// <summary>
        /// Does the assembly reference the recognizable error assembly (directly or indirectly)
        /// </summary>
        private static bool HasMetadataError(AssemblySymbol assembly)
        {
            ImmutableArray<ModuleSymbol> modules = assembly.Modules;

            for (int i = 0; i < modules.Length; i++)
            {
                foreach (AssemblySymbol assemblyRef in modules[i].GetReferencedAssemblySymbols())
                {
                    if (ErrorAssembly.IsErrorAssembly(assemblyRef))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public AssemblyIdentity Identity => _targetAssembly.Identity;
        public Version AssemblyVersionPattern => _targetAssembly.AssemblyVersionPattern;

        public override string ToString()
        {
            return _targetAssembly.ToString();
        }

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        string Cci.INamedEntity.Name => Identity.Name;

        bool IAssemblyReference.HasMetadataError => HasMetadataError(_targetAssembly);

        Cci.IAssemblyReference Cci.IModuleReference.GetContainingAssembly(CodeAnalysis.Emit.EmitContext context)
        {
            return this;
        }

        IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(CodeAnalysis.Emit.EmitContext context)
        {
            return SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();
        }

        Cci.IDefinition Cci.IReference.AsDefinition(CodeAnalysis.Emit.EmitContext context)
        {
            return null;
        }
    }
}
