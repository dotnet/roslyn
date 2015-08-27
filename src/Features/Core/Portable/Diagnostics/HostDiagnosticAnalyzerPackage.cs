// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Information on vsix that contains diagnostic analyzers
    /// </summary>
    internal class HostDiagnosticAnalyzerPackage
    {
        public readonly string Name;
        public readonly ImmutableArray<string> Assemblies;

        public HostDiagnosticAnalyzerPackage(string name, ImmutableArray<string> assemblies)
        {
            this.Name = name;
            this.Assemblies = assemblies;
        }
    }
}
