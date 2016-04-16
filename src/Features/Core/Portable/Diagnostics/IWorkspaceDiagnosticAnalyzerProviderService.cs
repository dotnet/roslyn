// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal interface IWorkspaceDiagnosticAnalyzerProviderService
    {
        /// <summary>
        /// Gets the analyzers shared across the entire workspace session.
        /// This includes the analyzers included through VSIX installations.
        /// </summary>
        IEnumerable<HostDiagnosticAnalyzerPackage> GetHostDiagnosticAnalyzerPackages();

        /// <summary>
        /// Gets the loader for VSIX based analyzer assemblies.
        /// </summary>
        /// <returns></returns>
        IAnalyzerAssemblyLoader GetAnalyzerAssemblyLoader();
    }
}
