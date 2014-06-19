using System;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A NamespaceExtent represents whether a namespace contains types and sub-namespaces from a
    /// particular module, assembly, or merged across all modules (source and metadata) in a
    /// particular compilation.
    /// </summary>
    public struct CommonNamespaceExtent : IEquatable<CommonNamespaceExtent>
    {
        private readonly object symbolOrCompilation;
        private readonly NamespaceExtentKind kind;

        /// <summary>
        /// Returns what kind of extent: Module, Assembly, or Compilation.
        /// </summary>
        public NamespaceExtentKind Kind { get { return kind; } }

        /// <summary>
        /// If the Kind is ExtendKind.Module, returns the module symbol that this namespace
        /// encompasses. Otherwise throws InvalidOperationException.
        /// </summary>
        public IModuleSymbol Module
        {
            get
            {
                if (this.Kind == NamespaceExtentKind.Module)
                {
                    return (IModuleSymbol)symbolOrCompilation;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        /// <summary>
        /// If the Kind is ExtendKind.Assembly, returns the assembly symbol that this namespace
        /// encompasses. Otherwise throws InvalidOperationException.
        /// </summary>
        public IAssemblySymbol Assembly
        {
            get
            {
                if (this.Kind == NamespaceExtentKind.Assembly)
                {
                    return (IAssemblySymbol)symbolOrCompilation;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        /// <summary>
        /// If the Kind is ExtendKind.Compilation, returns the compilation symbol that this
        /// namespace encompasses. Otherwise throws InvalidOperationException.
        /// </summary>
        public Compilation Compilation
        {
            get
            {
                if (this.Kind == NamespaceExtentKind.Compilation)
                {
                    return (Compilation)symbolOrCompilation;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", this.Kind, this.symbolOrCompilation);
        }

        /// <summary>
        /// Create a NamespaceExtent that represents a given ModuleSymbol.
        /// </summary>
        internal CommonNamespaceExtent(IModuleSymbol module)
        {
            this.kind = NamespaceExtentKind.Module;
            this.symbolOrCompilation = module;
        }

        /// <summary>
        /// Create a NamespaceExtent that represents a given AssemblySymbol.
        /// </summary>
        internal CommonNamespaceExtent(IAssemblySymbol assembly)
        {
            this.kind = NamespaceExtentKind.Assembly;
            this.symbolOrCompilation = assembly;
        }

        /// <summary>
        /// Create a NamespaceExtent that represents a given Compilation.
        /// </summary>
        internal CommonNamespaceExtent(Compilation compilation)
        {
            this.kind = NamespaceExtentKind.Compilation;
            this.symbolOrCompilation = compilation;
        }

        public override bool Equals(object obj)
        {
            return obj is CommonNamespaceExtent && Equals((CommonNamespaceExtent)obj);
        }

        public bool Equals(CommonNamespaceExtent other)
        {
            return object.Equals(this.symbolOrCompilation, other.symbolOrCompilation);
        }

        public override int GetHashCode()
        {
            return (this.symbolOrCompilation == null) ? 0 : symbolOrCompilation.GetHashCode();
        }
    }
}