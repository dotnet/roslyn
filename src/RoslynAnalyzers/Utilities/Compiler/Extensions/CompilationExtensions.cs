// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if CODEANALYSIS_V3_OR_BETTER
using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
#endif

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Extensions
{
    /// <summary>
    /// Provides extensions to <see cref="Compilation"/>.
    /// </summary>
    internal static class CompilationExtensions
    {
        //        private static readonly byte[] mscorlibPublicKeyToken = new byte[]
        //            { 0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89 };

        //#if CODEANALYSIS_V3_OR_BETTER
        //        private const string WebAppProjectGuidString = "{349C5851-65DF-11DA-9384-00065B846F21}";
        //        private const string WebSiteProjectGuidString = "{E24C65DC-7377-472B-9ABA-BC803B73C61A}";

        //        /// <summary>
        //        /// Gets a value indicating whether the project of the compilation is a Web SDK project based on project properties.
        //        /// </summary>
        //        internal static bool IsWebProject(this Compilation compilation, AnalyzerOptions options)
        //        {
        //            var propertyValue = AnalyzerOptionsExtensions.GetMSBuildPropertyValue(options, MSBuildPropertyOptionNames.UsingMicrosoftNETSdkWeb, compilation);
        //            if (string.Equals(propertyValue?.Trim(), "true", StringComparison.OrdinalIgnoreCase))
        //            {
        //                return true;
        //            }

        //            propertyValue = AnalyzerOptionsExtensions.GetMSBuildPropertyValue(options, MSBuildPropertyOptionNames.ProjectTypeGuids, compilation);
        //            if (!RoslynString.IsNullOrEmpty(propertyValue) &&
        //                (propertyValue.Contains(WebAppProjectGuidString, StringComparison.OrdinalIgnoreCase) ||
        //                 propertyValue.Contains(WebSiteProjectGuidString, StringComparison.OrdinalIgnoreCase)))
        //            {
        //                var guids = propertyValue.Split(';').Select(g => g.Trim()).ToImmutableArray();
        //                return guids.Contains(WebAppProjectGuidString, StringComparer.OrdinalIgnoreCase) ||
        //                    guids.Contains(WebSiteProjectGuidString, StringComparer.OrdinalIgnoreCase);
        //            }

        //            return false;
        //        }
        //#endif

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

        //        /// <summary>
        //        /// Gets a value indicating, whether the compilation of assembly targets .NET Framework.
        //        /// This method differentiates between .NET Framework and other frameworks (.NET Core, .NET Standard, .NET 5 in future).
        //        /// </summary>
        //        /// <param name="compilation">The compilation</param>
        //        /// <returns><c>True</c> if the compilation targets .NET Framework; otherwise <c>false</c>.</returns>
        //        internal static bool TargetsDotNetFramework(this Compilation compilation)
        //        {
        //            var objectType = compilation.GetSpecialType(SpecialType.System_Object);
        //            var assemblyIdentity = objectType.ContainingAssembly.Identity;
        //            if (assemblyIdentity.Name == "mscorlib" &&
        //                assemblyIdentity.IsStrongName &&
        //                (assemblyIdentity.Version == new System.Version(4, 0, 0, 0) || assemblyIdentity.Version == new System.Version(2, 0, 0, 0)) &&
        //                assemblyIdentity.PublicKeyToken.Length == mscorlibPublicKeyToken.Length)
        //            {
        //                for (int i = 0; i < mscorlibPublicKeyToken.Length; i++)
        //                {
        //                    if (assemblyIdentity.PublicKeyToken[i] != mscorlibPublicKeyToken[i])
        //                    {
        //                        return false;
        //                    }
        //                }

        //                return true;
        //            }

        //            return false;
        //        }
    }
}
