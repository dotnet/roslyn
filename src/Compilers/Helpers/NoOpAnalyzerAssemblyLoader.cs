// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Workaround for assembly loading in core clr -- this loader does nothing.
    /// </summary>
    internal sealed class NoOpAnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
    {
        public Assembly LoadFromPath(string fullPath) => null;

        public void AddDependencyLocation(string fullPath) { }
    }
}
