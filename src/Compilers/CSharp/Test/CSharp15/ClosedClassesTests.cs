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
        comp.VerifyEmitDiagnostics(
            // (1,14): error CS8652: The feature 'closed classes' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // closed class C { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "C").WithArguments("closed classes").WithLocation(1, 14));

        comp = CreateCompilation(source, parseOptions: TestOptions.RegularNext);
        comp.VerifyEmitDiagnostics();

        comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();
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

    // PROTOTYPE(cc): NamedTypeSymbol.IsClosed API

    [Fact]
    public void Sealed_01()
    {
        var source = """
            sealed closed class C { }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (1,21): error CS9366: 'C': a closed type cannot be sealed or static
            // sealed closed class C { }
            Diagnostic(ErrorCode.ERR_ClosedSealedStatic, "C").WithArguments("C").WithLocation(1, 21));

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.IsSealed);
    }

    [Fact]
    public void Static_01()
    {
        var source = """
            static closed class C { }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics(
            // (1,21): error CS9366: 'C': a closed type cannot be sealed or static
            // static closed class C { }
            Diagnostic(ErrorCode.ERR_ClosedSealedStatic, "C").WithArguments("C").WithLocation(1, 21));

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.IsStatic);
    }

    [Fact]
    public void Abstract_01()
    {
        var source = """
            abstract closed class C { }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        Assert.True(classC.IsAbstract);
    }

    [Fact]
    public void ImplicitlyAbstract_01()
    {
        var source = """
            closed class C { }
            """;

        var comp = CreateCompilation(source);
        comp.VerifyEmitDiagnostics();

        var classC = comp.GetMember<NamedTypeSymbol>("C");
        // PROTOTYPE(cc): Should closed types implicitly return true from IsAbstract, like interfaces do?
        Assert.False(classC.IsAbstract);
    }
}
