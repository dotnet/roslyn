// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a .NET assembly, consisting of one or more modules.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IAssemblySymbol : ISymbol
    {
        /// <summary>
        /// True if the assembly contains interactive code.
        /// </summary>
        bool IsInteractive { get; }

        /// <summary>
        /// Gets the name of this assembly.
        /// </summary>
        AssemblyIdentity Identity { get; }

        /// <summary>
        /// Gets the merged root namespace that contains all namespaces and types defined in the modules
        /// of this assembly. If there is just one module in this assembly, this property just returns the 
        /// GlobalNamespace of that module.
        /// </summary>
        INamespaceSymbol GlobalNamespace { get; }

        /// <summary>
        /// Gets the modules in this assembly. (There must be at least one.) The first one is the main module
        /// that holds the assembly manifest.
        /// </summary>
        IEnumerable<IModuleSymbol> Modules { get; }

        /// <summary>
        /// Gets the set of type identifiers from this assembly.
        /// </summary>
        ICollection<string> TypeNames { get; }

        /// <summary>
        /// Gets the set of namespace names from this assembly.
        /// </summary>
        ICollection<string> NamespaceNames { get; }

        /// <summary>
        /// Gets a value indicating whether this assembly gives 
        /// <paramref name="toAssembly"/> access to internal symbols</summary>
        bool GivesAccessTo(IAssemblySymbol toAssembly);

        /// <summary>
        /// Lookup a type within the assembly using the canonical CLR metadata name of the type.
        /// </summary>
        /// <param name="fullyQualifiedMetadataName">Type name.</param>
        /// <returns>Symbol for the type or null if type cannot be found or is ambiguous. </returns>
        INamedTypeSymbol GetTypeByMetadataName(string fullyQualifiedMetadataName);

        /// <summary>
        /// Determines if the assembly might contain extension methods.
        /// If false, the assembly does not contain extension methods.
        /// </summary>
        bool MightContainExtensionMethods { get; }

        /// <summary>
        /// Returns the type symbol for a forwarded type based its canonical CLR metadata name.
        /// The name should refer to a non-nested type. If type with this name is not forwarded,
        /// null is returned.
        /// </summary>
        INamedTypeSymbol ResolveForwardedType(string fullyQualifiedMetadataName);

        /// <summary>
        /// If this symbol represents a metadata assembly returns the underlying <see cref="AssemblyMetadata"/>.
        /// 
        /// Otherwise, this returns <see langword="null"/>.
        /// </summary>
        AssemblyMetadata GetMetadata();
    }
}
