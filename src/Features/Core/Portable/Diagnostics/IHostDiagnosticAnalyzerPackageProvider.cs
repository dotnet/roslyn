// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal interface IHostDiagnosticAnalyzerPackageProvider
    {
        /// <summary>
        /// Gets the analyzers shared across the entire workspace session.
        /// This includes the analyzers included through VSIX installations.
        /// </summary>
        ImmutableArray<HostDiagnosticAnalyzerPackage> GetHostDiagnosticAnalyzerPackages();

        /// <summary>
        /// Gets the loader for VSIX based analyzer assemblies.
        /// </summary>
        /// <returns></returns>
        IAnalyzerAssemblyLoader GetAnalyzerAssemblyLoader();
    }
}
