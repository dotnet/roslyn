// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a module within an assembly. Every assembly contains one or more modules.
    /// </summary>
    internal abstract class ModuleSymbol : Symbol, IModuleSymbol
    {
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // Changes to the public interface of this class should remain synchronized with the VB version.
        // Do not make any changes to the public interface without making the corresponding change
        // to the VB version.
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        /// <summary>
        /// Returns a NamespaceSymbol representing the global (root) namespace, with
        /// module extent, that can be used to browse all of the symbols defined in this module.
        /// </summary>
        public abstract NamespaceSymbol GlobalNamespace { get; }

        /// <summary>
        /// Returns the containing assembly. Modules are always directly contained by an assembly,
        /// so this property always returns the same as ContainingSymbol.
        /// </summary>
        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return (AssemblySymbol)ContainingSymbol;
            }
        }

        internal sealed override ModuleSymbol ContainingModule
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Returns value 'NetModule' of the <see cref="SymbolKind"/>
        /// </summary>
        public sealed override SymbolKind Kind
        {
            get
            {
                return SymbolKind.NetModule;
            }
        }

        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitModule(this, argument);
        }

        public override void Accept(CSharpSymbolVisitor visitor)
        {
            visitor.VisitModule(this);
        }

        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor)
        {
            return visitor.VisitModule(this);
        }

        // Only the compiler can create ModuleSymbols.
        internal ModuleSymbol()
        {
        }

        /// <summary>
        /// Module's ordinal within containing assembly's Modules array.
        /// 0 - for a source module, etc.
        /// -1 - for a module that doesn't have containing assembly, or has it, but is not part of Modules array. 
        /// </summary>
        internal abstract int Ordinal { get; }

        /// <summary>
        /// Target architecture of the machine.
        /// </summary>
        internal abstract Machine Machine { get; }

        /// <summary>
        /// Indicates that this PE file makes Win32 calls. See CorPEKind.pe32BitRequired for more information (http://msdn.microsoft.com/en-us/library/ms230275.aspx).
        /// </summary>
        internal abstract bool Bit32Required { get; }

        /// <summary>
        /// Does this symbol represent a missing module.
        /// </summary>
        internal abstract bool IsMissing
        {
            get;
        }

        /// <summary>
        /// Returns 'NotApplicable'
        /// </summary>
        public sealed override Accessibility DeclaredAccessibility
        {
            get
            {
                return Accessibility.NotApplicable;
            }
        }

        /// <summary>
        /// Returns false because module can't be declared as 'static'.
        /// </summary>
        public sealed override bool IsStatic
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns false because module can't be virtual.
        /// </summary>
        public sealed override bool IsVirtual
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns false because module can't be overridden.
        /// </summary>
        public sealed override bool IsOverride
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns false because module can't be abstract.
        /// </summary>
        public sealed override bool IsAbstract
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns false because module can't be sealed.
        /// </summary>
        public sealed override bool IsSealed
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns false because module can't be defined externally.
        /// </summary>
        public sealed override bool IsExtern
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns data decoded from Obsolete attribute or null if there is no Obsolete attribute.
        /// This property returns ObsoleteAttributeData.Uninitialized if attribute arguments haven't been decoded yet.
        /// </summary>
        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return null; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        /// <summary>
        /// Returns an array of assembly identities for assemblies referenced by this module.
        /// Items at the same position from ReferencedAssemblies and from ReferencedAssemblySymbols 
        /// correspond to each other.
        /// </summary>
        public ImmutableArray<AssemblyIdentity> ReferencedAssemblies
        {
            get
            {
                return GetReferencedAssemblies();
            }
        }

        /// <summary>
        /// Returns an array of assembly identities for assemblies referenced by this module.
        /// Items at the same position from GetReferencedAssemblies and from GetReferencedAssemblySymbols 
        /// should correspond to each other.
        /// 
        /// The array and its content is provided by ReferenceManager and must not be modified.
        /// </summary>
        /// <returns></returns>
        internal abstract ImmutableArray<AssemblyIdentity> GetReferencedAssemblies(); // TODO: Remove this method and make ReferencedAssemblies property abstract instead.


        /// <summary>
        /// Returns an array of AssemblySymbol objects corresponding to assemblies referenced 
        /// by this module. Items at the same position from ReferencedAssemblies and 
        /// from ReferencedAssemblySymbols correspond to each other.
        /// </summary>
        public ImmutableArray<AssemblySymbol> ReferencedAssemblySymbols
        {
            get
            {
                return GetReferencedAssemblySymbols();
            }
        }

        /// <summary>
        /// Returns an array of AssemblySymbol objects corresponding to assemblies referenced 
        /// by this module. Items at the same position from GetReferencedAssemblies and 
        /// from GetReferencedAssemblySymbols should correspond to each other. If reference is 
        /// not resolved by compiler, GetReferencedAssemblySymbols returns MissingAssemblySymbol in the
        /// corresponding item.
        /// 
        /// The array and its content is provided by ReferenceManager and must not be modified.
        /// </summary>
        internal abstract ImmutableArray<AssemblySymbol> GetReferencedAssemblySymbols(); // TODO: Remove this method and make ReferencedAssemblySymbols property abstract instead.

        /// <summary>
        /// A helper method for ReferenceManager to set assembly identities for assemblies 
        /// referenced by this module and corresponding AssemblySymbols.
        /// </summary>
        /// <param name="moduleReferences">A description of the assemblies referenced by this module.</param>
        /// <param name="originatingSourceAssemblyDebugOnly">
        /// Source assembly that triggered creation of this module symbol.
        /// For debug purposes only, this assembly symbol should not be persisted within
        /// this module symbol because the module can be shared across multiple source 
        /// assemblies. This method will only be called for the first one.
        /// </param>
        internal abstract void SetReferences(ModuleReferences<AssemblySymbol> moduleReferences, SourceAssemblySymbol originatingSourceAssemblyDebugOnly = null);

        /// <summary>
        /// True if this module has any unified references.
        /// </summary>
        internal abstract bool HasUnifiedReferences { get; }

        /// <summary> 
        /// Returns a unification use-site error (if any) for a symbol contained in this module 
        /// that is referring to a specified <paramref name="dependentType"/>.
        /// </summary> 
        /// <remarks> 
        /// If an assembly referenced by this module isn't exactly matching any reference given to compilation 
        /// the Assembly Manager might decide to use another reference if it matches except for version 
        /// (it unifies the version with the existing reference).  
        /// </remarks>
        internal abstract bool GetUnificationUseSiteDiagnostic(ref DiagnosticInfo result, TypeSymbol dependentType);

        /// <summary>
        /// Lookup a top level type referenced from metadata, names should be
        /// compared case-sensitively.
        /// </summary>
        /// <param name="emittedName">
        /// Full type name, possibly with generic name mangling.
        /// </param>
        /// <returns>
        /// Symbol for the type, or MissingMetadataSymbol if the type isn't found.
        /// </returns>
        /// <remarks></remarks>
        internal abstract NamedTypeSymbol LookupTopLevelMetadataType(ref MetadataTypeName emittedName);

        internal abstract ICollection<string> TypeNames { get; }

        internal abstract ICollection<string> NamespaceNames { get; }

        /// <summary>
        /// Returns true if there is any applied CompilationRelaxationsAttribute assembly attribute for this module.
        /// </summary>
        internal abstract bool HasAssemblyCompilationRelaxationsAttribute { get; }

        /// <summary>
        /// Returns true if there is any applied RuntimeCompatibilityAttribute assembly attribute for this module.
        /// </summary>
        internal abstract bool HasAssemblyRuntimeCompatibilityAttribute { get; }

        /// <summary>
        /// Default char set for contained types, or null if not specified.
        /// </summary>
        internal abstract CharSet? DefaultMarshallingCharSet { get; }

        internal virtual ImmutableArray<byte> GetHash(AssemblyHashAlgorithm algorithmId)
        {
            throw ExceptionUtilities.Unreachable;
        }

        /// <summary>
        /// Given a namespace symbol, returns the corresponding module specific namespace symbol
        /// </summary>
        public NamespaceSymbol GetModuleNamespace(INamespaceSymbol namespaceSymbol)
        {
            if (namespaceSymbol == null)
            {
                throw new ArgumentNullException(nameof(namespaceSymbol));
            }

            var moduleNs = namespaceSymbol as NamespaceSymbol;
            if ((object)moduleNs != null && moduleNs.Extent.Kind == NamespaceKind.Module && moduleNs.ContainingModule == this)
            {
                // this is already the correct module namespace
                return moduleNs;
            }

            if (namespaceSymbol.IsGlobalNamespace || (object)namespaceSymbol.ContainingNamespace == null)
            {
                return this.GlobalNamespace;
            }
            else
            {
                var cns = GetModuleNamespace(namespaceSymbol.ContainingNamespace);
                if ((object)cns != null)
                {
                    return cns.GetNestedNamespace(namespaceSymbol.Name);
                }
                return null;
            }
        }

        #region IModuleSymbol Members

        INamespaceSymbol IModuleSymbol.GlobalNamespace
        {
            get { return this.GlobalNamespace; }
        }

        INamespaceSymbol IModuleSymbol.GetModuleNamespace(INamespaceSymbol namespaceSymbol)
        {
            return this.GetModuleNamespace(namespaceSymbol);
        }

        ImmutableArray<IAssemblySymbol> IModuleSymbol.ReferencedAssemblySymbols
        {
            get
            {
                return ImmutableArray<IAssemblySymbol>.CastUp(ReferencedAssemblySymbols);
            }
        }

        #endregion

        #region ISymbol Members

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitModule(this);
        }

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitModule(this);
        }

        /// <summary>
        /// If this symbol represents a metadata module returns the underlying <see cref="ModuleMetadata"/>.
        /// 
        /// Otherwise, this returns <code>null</code>.
        /// </summary>
        public abstract ModuleMetadata GetMetadata();

        #endregion
    }
}
