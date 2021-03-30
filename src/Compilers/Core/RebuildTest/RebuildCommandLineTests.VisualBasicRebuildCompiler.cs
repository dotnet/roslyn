// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using BuildValidator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.Extensions.Logging;
using Roslyn.Test.Utilities;
using Xunit;

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
