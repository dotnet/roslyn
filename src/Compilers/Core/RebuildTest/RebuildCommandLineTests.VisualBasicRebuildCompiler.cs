// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.Rebuild.UnitTests
{
    public partial class RebuildCommandLineTests
    {
        private sealed class VisualBasicRebuildCompiler : VisualBasicCompiler
        {
            internal VisualBasicRebuildCompiler(string[] args)
                : base(VisualBasicCommandLineParser.Default, responseFile: null, args, StandardBuildPaths, additionalReferenceDirectories: null, new DefaultAnalyzerAssemblyLoader())
            {
            }
        }
    }
}
