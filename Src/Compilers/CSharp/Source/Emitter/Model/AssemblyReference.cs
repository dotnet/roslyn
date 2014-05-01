// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal sealed class AssemblyReference : Cci.IAssemblyReference
    {
        // Assembly identity used in metadata to refer to the target assembly.
        // NOTE: this could be different from assemblySymbol.AssemblyName due to mapping.
        // For example, multiple assembly symbols might be emitted into a single dynamic assembly whose identity is stored here.
        public readonly AssemblyIdentity MetadataIdentity;

        // assembly symbol that represents the target assembly:
        private readonly AssemblySymbol targetAssembly;

        internal AssemblyReference(AssemblySymbol assemblySymbol, Func<AssemblySymbol, AssemblyIdentity> symbolMapper)
        {
            Debug.Assert((object)assemblySymbol != null);
            this.MetadataIdentity = (symbolMapper != null) ? symbolMapper(assemblySymbol) : assemblySymbol.Identity;
            this.targetAssembly = assemblySymbol;
        }

        public override string ToString()
        {
            return targetAssembly.ToString();
        }

        #region Cci.IAssemblyReference

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        string Cci.IAssemblyReference.Culture
        {
            get
            {
                return MetadataIdentity.CultureName;
            }
        }

        bool Cci.IAssemblyReference.IsRetargetable
        {
            get
            {
                return MetadataIdentity.IsRetargetable;
            }
        }

        AssemblyContentType Cci.IAssemblyReference.ContentType
        {
            get
            {
                return MetadataIdentity.ContentType;
            }
        }

        IEnumerable<byte> Cci.IAssemblyReference.PublicKeyToken
        {
            get { return MetadataIdentity.PublicKeyToken; }
        }

        Version Cci.IAssemblyReference.Version
        {
            get { return MetadataIdentity.Version; }
        }

        string Cci.INamedEntity.Name
        {
            get { return MetadataIdentity.Name; }
        }

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


        #endregion
    }
}
