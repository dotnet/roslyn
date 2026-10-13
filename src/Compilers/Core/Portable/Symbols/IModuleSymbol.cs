// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a module within an assembly. Every assembly contains one or more modules.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IModuleSymbol : ISymbol
    {
        /// <summary>
        /// Returns a NamespaceSymbol representing the global (root) namespace, with
        /// module extent, that can be used to browse all of the symbols defined in this module.
        /// </summary>
        INamespaceSymbol GlobalNamespace { get; }

        /// <summary>
        /// Given a namespace symbol, returns the corresponding module specific namespace symbol
        /// </summary>
        INamespaceSymbol? GetModuleNamespace(INamespaceSymbol namespaceSymbol);

        /// <summary>
        /// Returns an array of assembly identities for assemblies referenced by this module.
        /// Items at the same position from ReferencedAssemblies and from ReferencedAssemblySymbols 
        /// correspond to each other.
        /// </summary>
        ImmutableArray<AssemblyIdentity> ReferencedAssemblies { get; }

        /// <summary>
        /// Returns an array of AssemblySymbol objects corresponding to assemblies referenced 
        /// by this module. Items at the same position from ReferencedAssemblies and 
        /// from ReferencedAssemblySymbols correspond to each other.
        /// </summary>
        ImmutableArray<IAssemblySymbol> ReferencedAssemblySymbols { get; }

        /// <summary>
        /// Which memory safety rules are enabled in this module. Determines which symbols are considered requires-unsafe (<see cref="ISymbol.RequiresUnsafeContext"/>).
        /// </summary>
        /// <remarks>
        /// A metadata module compiled with an unsupported rules version may return a value that is not defined by <see cref="Microsoft.CodeAnalysis.MemorySafetyRulesVersion"/>.
        /// </remarks>
        [Experimental(RoslynExperiments.PreviewLanguageFeatureApi, UrlFormat = "https://github.com/dotnet/roslyn/issues/82789")]
        MemorySafetyRulesVersion MemorySafetyRulesVersion { get; }

        /// <summary>
        /// If this symbol represents a metadata module returns the underlying <see cref="ModuleMetadata"/>.
        /// 
        /// Otherwise, this returns <see langword="null"/>.
        /// </summary>
        ModuleMetadata? GetMetadata();
    }
}
