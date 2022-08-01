// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.DynamicAnalysis.UnitTests;
using Roslyn.Test.Utilities;

namespace Metalama.Compiler.UnitTests.Instrumentation;

public class MetalamaDynamicAnalysisResourceTests : DynamicAnalysisResourceTests
{
    public MetalamaDynamicAnalysisResourceTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

    public override void Dispose()
    {
        MetalamaCompilerTest.ShouldExecuteTransformer = false;
        base.Dispose();
    }
    
}

public class MetalamaDynamicInstrumentationTests : DynamicInstrumentationTests
{
    public MetalamaDynamicInstrumentationTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

    public override void Dispose()
    {
        MetalamaCompilerTest.ShouldExecuteTransformer = false;
        base.Dispose();
    }
}
