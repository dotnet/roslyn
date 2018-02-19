﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
{
    internal interface IVisualStudioDiagnosticAnalyzerService
    {
        /// <summary>
        /// Gets a list of the diagnostics that are provided by this service.
        /// If the given <paramref name="hierarchyOpt"/> is non-null and corresponds to an existing project in the workspace, then gets the diagnostics for the project.
        /// Otherwise, returns the global set of diagnostics enabled for the workspace.
        /// </summary>
        /// <returns>A mapping from analyzer name to the diagnostics produced by that analyzer</returns>
        /// <remarks>
        /// This is used by the Ruleset Editor from ManagedSourceCodeAnalysis.dll in VisualStudio.
        /// </remarks>
        IReadOnlyDictionary<string, IEnumerable<DiagnosticDescriptor>> GetAllDiagnosticDescriptors(IVsHierarchy hierarchyOpt);
    }
}
