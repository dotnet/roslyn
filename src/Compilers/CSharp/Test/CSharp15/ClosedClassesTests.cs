// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class ClosedClassesTests : CSharpTestBase
{
    [Fact]
    public void LangVersion()
    {
        var source = """
            closed class C { }
            """;

        var comp = CreateCompilation(source, parseOptions: TestOptions.Regular14);
        comp.VerifyDiagnostics(
            // (1,14): error CS8652: The feature 'closed classes' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // closed class C { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "C").WithArguments("closed classes").WithLocation(1, 14));

        comp = CreateCompilation(source, parseOptions: TestOptions.RegularNext);
        comp.VerifyDiagnostics();

        comp = CreateCompilation(source);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void InheritFromMetadata_01()
    {
        var source1 = """
            public closed class C { }
            """;
        var source2 = """
            public class D : C { }
            """;

        var comp = CreateCompilation([source1, source2]);
        comp.VerifyEmitDiagnostics();

        var comp1 = CreateCompilation(source1);

        // PROTOTYPE(cc): other module should be blocked from inheriting
        var comp2 = CreateCompilation(source2, references: [comp1.ToMetadataReference()]);
        comp2.VerifyEmitDiagnostics();

        comp2 = CreateCompilation(source2, references: [comp1.EmitToImageReference()]);
        comp2.VerifyEmitDiagnostics();

        comp1 = CreateCompilation(source1, options: TestOptions.ReleaseModule);
        comp2 = CreateCompilation(source2, references: [comp1.EmitToImageReference()]);
        comp2.VerifyEmitDiagnostics();
    }
}
