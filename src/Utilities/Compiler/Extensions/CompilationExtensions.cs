// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Extensions
{
    /// <summary>
    /// Provides extensions to <see cref="Compilation"/>.
    /// </summary>
    internal static class CompilationExtensions
    {
        private static readonly byte[] mscorlibPublicKeyToken = new byte[]
            { 0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89 };

        /// <summary>
        /// Gets a type by its full type name and cache it at the compilation level.
        /// </summary>
        /// <param name="compilation">The compilation.</param>
        /// <param name="fullTypeName">Namespace + type name, e.g. "System.Exception".</param>
        /// <returns>The <see cref="INamedTypeSymbol"/> if found, null otherwise.</returns>
        internal static INamedTypeSymbol? GetOrCreateTypeByMetadataName(this Compilation compilation, string fullTypeName) =>
            WellKnownTypeProvider.GetOrCreate(compilation).GetOrCreateTypeByMetadataName(fullTypeName);

        /// <summary>
        /// Gets a type by its full type name and cache it at the compilation level.
        /// </summary>
        /// <param name="compilation">The compilation.</param>
        /// <param name="fullTypeName">Namespace + type name, e.g. "System.Exception".</param>
        /// <returns>The <see cref="INamedTypeSymbol"/> if found, null otherwise.</returns>
        internal static bool TryGetOrCreateTypeByMetadataName(this Compilation compilation, string fullTypeName, [NotNullWhen(returnValue: true)] out INamedTypeSymbol? namedTypeSymbol) =>
            WellKnownTypeProvider.GetOrCreate(compilation).TryGetOrCreateTypeByMetadataName(fullTypeName, out namedTypeSymbol);

        /// <summary>
        /// Gets a value indicating, whether the compilation of assembly targets .NET Framework.
        /// This method differentiates between .NET Framework and other frameworks (.NET Core, .NET Standard, .NET 5 in future).
        /// </summary>
        /// <param name="compilation">The compilation</param>
        /// <returns><c>True</c> if the compilation targets .NET Framework; otherwise <c>false</c>.</returns>
        internal static bool DoesTargetDotNetFramework(this Compilation compilation)
        {
            var objectType = compilation.GetSpecialType(SpecialType.System_Object);
            var assemblyIdentity = objectType.ContainingAssembly.Identity;
            if (assemblyIdentity.Name == "mscorlib" &&
                assemblyIdentity.IsStrongName &&
                (assemblyIdentity.Version == new System.Version(4, 0, 0, 0) || assemblyIdentity.Version == new System.Version(2, 0, 0, 0)) &&
                assemblyIdentity.PublicKeyToken.Length == mscorlibPublicKeyToken.Length)
            {
                for (int i = 0; i < mscorlibPublicKeyToken.Length; i++)
                {
                    if (assemblyIdentity.PublicKeyToken[i] != mscorlibPublicKeyToken[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }
    }
}
