// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal sealed class VisualStudioProjectCreationInfo
    {
        public string? AssemblyName { get; set; }
        public CompilationOptions? CompilationOptions { get; set; }
        public string? FilePath { get; set; }
        public ParseOptions? ParseOptions { get; set; }

        public IVsHierarchy? Hierarchy { get; set; }
        public Guid ProjectGuid { get; set; }
    }
}
