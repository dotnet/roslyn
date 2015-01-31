// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal interface IWorkspaceDiagnosticAnalyzerProviderService
    {
        /// <summary>
        /// Gets the analyzers shared across the entire workspace session.
        /// This includes the analyzers included through VSIX installations.
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetWorkspaceAnalyzerAssemblies();
    }
}
