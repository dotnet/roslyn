// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
{
    internal interface IVisualStudioDiagnosticAnalyzerService
    {
        /// <summary>
        /// Gets a list of the diagnostics that are provided by this service.
        /// If the given <paramref name="hierarchy"/> is non-null and corresponds to an existing project in the workspace, then gets the diagnostics for the project.
        /// Otherwise, returns the global set of diagnostics enabled for the workspace.
        /// </summary>
        /// <returns>A mapping from analyzer name to the diagnostics produced by that analyzer</returns>
        /// <remarks>
        /// This is used by the Ruleset Editor from ManagedSourceCodeAnalysis.dll in VisualStudio.
        /// </remarks>
        IReadOnlyDictionary<string, IEnumerable<DiagnosticDescriptor>> GetAllDiagnosticDescriptors(IVsHierarchy? hierarchy);

        /// <summary>
        /// Runs all the applicable NuGet and VSIX diagnostic analyzers for the given project OR current solution in background and updates the error list.
        /// </summary>
        /// <param name="hierarchy">
        /// If non-null hierarchy for a project, then analyzers are run on the project.
        /// Otherwise, analyzers are run on the current solution.
        /// </param>
        void RunAnalyzers(IVsHierarchy? hierarchy);

        /// <summary>
        /// Initializes the service.
        /// </summary>
        void Initialize(IServiceProvider serviceProvider);
    }
}
