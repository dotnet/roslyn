// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A NamespaceExtent represents whether a namespace contains types and sub-namespaces from a
    /// particular module, assembly, or merged across all modules (source and metadata) in a
    /// particular compilation.
    /// </summary>
    internal struct NamespaceExtent : IEquatable<NamespaceExtent>
    {
        private readonly NamespaceKind kind;
        private readonly object symbolOrCompilation;

        /// <summary>
        /// Returns what kind of extent: Module, Assembly, or Compilation.
        /// </summary>
        public NamespaceKind Kind
        {
            get
            {
                return kind;
            }
        }

        /// <summary>
        /// If the Kind is ExtendKind.Module, returns the module symbol that this namespace
        /// encompasses. Otherwise throws InvalidOperationException.
        /// </summary>
        public ModuleSymbol Module
        {
            get
            {
                if (kind == NamespaceKind.Module)
                {
                    return (ModuleSymbol)symbolOrCompilation;
                }

                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// If the Kind is ExtendKind.Assembly, returns the assembly symbol that this namespace
        /// encompasses. Otherwise throws InvalidOperationException.
        /// </summary>
        public AssemblySymbol Assembly
        {
            get
            {
                if (kind == NamespaceKind.Assembly)
                {
                    return (AssemblySymbol)symbolOrCompilation;
                }

                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// If the Kind is ExtendKind.Compilation, returns the compilation symbol that this
        /// namespace encompasses. Otherwise throws InvalidOperationException.
        /// </summary>
        public CSharpCompilation Compilation
        {
            get
            {
                if (kind == NamespaceKind.Compilation)
                {
                    return (CSharpCompilation)symbolOrCompilation;
                }

                throw new InvalidOperationException();
            }
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", kind, symbolOrCompilation);
        }

        /// <summary>
        /// Create a NamespaceExtent that represents a given ModuleSymbol.
        /// </summary>
        internal NamespaceExtent(ModuleSymbol module)
        {
            this.kind = NamespaceKind.Module;
            this.symbolOrCompilation = module;
        }

        /// <summary>
        /// Create a NamespaceExtent that represents a given AssemblySymbol.
        /// </summary>
        internal NamespaceExtent(AssemblySymbol assembly)
        {
            this.kind = NamespaceKind.Assembly;
            this.symbolOrCompilation = assembly;
        }

        /// <summary>
        /// Create a NamespaceExtent that represents a given Compilation.
        /// </summary>
        internal NamespaceExtent(CSharpCompilation compilation)
        {
            this.kind = NamespaceKind.Compilation;
            this.symbolOrCompilation = compilation;
        }

        public override bool Equals(object obj)
        {
            return obj is NamespaceExtent && Equals((NamespaceExtent)obj);
        }

        public bool Equals(NamespaceExtent other)
        {
            return object.Equals(this.symbolOrCompilation, other.symbolOrCompilation);
        }

        public override int GetHashCode()
        {
            return (this.symbolOrCompilation == null) ? 0 : symbolOrCompilation.GetHashCode();
        }
    }
}