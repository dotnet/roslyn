// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
    internal readonly struct NamespaceExtent : IEquatable<NamespaceExtent>
    {
        private readonly NamespaceKind _kind;
        private readonly object _symbolOrCompilation;

        /// <summary>
        /// Returns what kind of extent: Module, Assembly, or Compilation.
        /// </summary>
        public NamespaceKind Kind
        {
            get
            {
                return _kind;
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
                if (_kind == NamespaceKind.Module)
                {
                    return (ModuleSymbol)_symbolOrCompilation;
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
                if (_kind == NamespaceKind.Assembly)
                {
                    return (AssemblySymbol)_symbolOrCompilation;
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
                if (_kind == NamespaceKind.Compilation)
                {
                    return (CSharpCompilation)_symbolOrCompilation;
                }

                throw new InvalidOperationException();
            }
        }

        public override string ToString()
        {
            return $"{_kind}: {_symbolOrCompilation}";
        }

        /// <summary>
        /// Create a NamespaceExtent that represents a given ModuleSymbol.
        /// </summary>
        internal NamespaceExtent(ModuleSymbol module)
        {
            _kind = NamespaceKind.Module;
            _symbolOrCompilation = module;
        }

        /// <summary>
        /// Create a NamespaceExtent that represents a given AssemblySymbol.
        /// </summary>
        internal NamespaceExtent(AssemblySymbol assembly)
        {
            _kind = NamespaceKind.Assembly;
            _symbolOrCompilation = assembly;
        }

        /// <summary>
        /// Create a NamespaceExtent that represents a given Compilation.
        /// </summary>
        internal NamespaceExtent(CSharpCompilation compilation)
        {
            _kind = NamespaceKind.Compilation;
            _symbolOrCompilation = compilation;
        }

        public override bool Equals(object obj)
        {
            return obj is NamespaceExtent && Equals((NamespaceExtent)obj);
        }

        public bool Equals(NamespaceExtent other)
        {
            return object.Equals(_symbolOrCompilation, other._symbolOrCompilation);
        }

        public override int GetHashCode()
        {
            return (_symbolOrCompilation == null) ? 0 : _symbolOrCompilation.GetHashCode();
        }
    }
}
