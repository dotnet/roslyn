// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

public class RefReadonlyParameterTests : CSharpTestBase
{
    [Fact]
    public void Modifier()
    {
        var source = """
            class C
            {
                void M(ref readonly int p) => throw null;
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (3,16): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     void M(ref readonly int p);
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(3, 16));

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
        var comp = CreateCompilation(source).VerifyDiagnostics();

        var syntaxTree = comp.SyntaxTrees.Single();
        var parameter = syntaxTree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().Single();
        Assert.Equal("ref readonly int p", parameter.ToString());
        var model = comp.GetSemanticModel(syntaxTree, ignoreAccessibility: false);
        var symbol = model.GetDeclaredSymbol(parameter);
        Assert.Equal(RefKind.RefReadOnlyParameter, symbol!.RefKind);
    }

    [Fact]
    public void Modifier_Invalid()
    {
        var source = """
            class C
            {
                void M(ref params readonly int[] p) => throw null;
            }
            """;
        var comp = CreateCompilation(source).VerifyDiagnostics(
            // (3,16): error CS8328:  The parameter modifier 'params' cannot be used with 'ref'
            //     void M(ref params readonly int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "params").WithArguments("params", "ref").WithLocation(3, 16),
            // (3,23): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     void M(ref params readonly int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 23));

        var syntaxTree = comp.SyntaxTrees.Single();
        var parameter = syntaxTree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().Single();
        Assert.Equal("ref params readonly int[] p", parameter.ToString());
        var model = comp.GetSemanticModel(syntaxTree, ignoreAccessibility: false);
        var symbol = model.GetDeclaredSymbol(parameter);
        Assert.Equal(RefKind.Ref, symbol!.RefKind);
    }

    [Fact]
    public void DuplicateModifier_01()
    {
        var source = """
            class C
            {
                void M(ref readonly readonly int p) { }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (3,16): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     void M(ref readonly readonly int p) { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(3, 16),
            // (3,25): error CS1107: A parameter can only have one 'readonly' modifier
            //     void M(ref readonly readonly int p) { }
            Diagnostic(ErrorCode.ERR_DupParamMod, "readonly").WithArguments("readonly").WithLocation(3, 25));

        var expectedDiagnostics = new[]
        {
            // (3,25): error CS1107: A parameter can only have one 'readonly' modifier
            //     void M(ref readonly readonly int p) { }
            Diagnostic(ErrorCode.ERR_DupParamMod, "readonly").WithArguments("readonly").WithLocation(3, 25)
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void DuplicateModifier_02()
    {
        var source = """
            class C
            {
                void M(readonly readonly int p) { }
            }
            """;
        var expectedDiagnostics = new[]
        {
            // (3,12): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     void M(readonly readonly int p) { }
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 12),
            // (3,21): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     void M(readonly readonly int p) { }
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 21)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void DuplicateModifier_03()
    {
        var source = """
            class C
            {
                void M(readonly ref readonly int p) { }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (3,12): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     void M(readonly ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 12),
            // (3,25): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     void M(readonly ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(3, 25));

        var expectedDiagnostics = new[]
        {
            // (3,12): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     void M(readonly ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 12)
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void DuplicateModifier_04()
    {
        var source = """
            class C
            {
                void M(readonly readonly ref int p) { }
            }
            """;
        var expectedDiagnostics = new[]
        {
            // (3,12): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     void M(readonly readonly ref int p) { }
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 12),
            // (3,21): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     void M(readonly readonly ref int p) { }
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 21)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void DuplicateModifier_05()
    {
        var source = """
            class C
            {
                void M(ref readonly ref readonly int p) { }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (3,16): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     void M(ref readonly ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(3, 16),
            // (3,25): error CS1107: A parameter can only have one 'ref' modifier
            //     void M(ref readonly ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_DupParamMod, "ref").WithArguments("ref").WithLocation(3, 25),
            // (3,29): error CS1107: A parameter can only have one 'readonly' modifier
            //     void M(ref readonly ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_DupParamMod, "readonly").WithArguments("readonly").WithLocation(3, 29));

        var expectedDiagnostics = new[]
        {
            // (3,25): error CS1107: A parameter can only have one 'ref' modifier
            //     void M(ref readonly ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_DupParamMod, "ref").WithArguments("ref").WithLocation(3, 25),
            // (3,29): error CS1107: A parameter can only have one 'readonly' modifier
            //     void M(ref readonly ref readonly int p) { }
            Diagnostic(ErrorCode.ERR_DupParamMod, "readonly").WithArguments("readonly").WithLocation(3, 29)
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void ReadonlyWithoutRef()
    {
        var source = """
            class C
            {
                void M(readonly int p) => throw null;
            }
            """;
        var expectedDiagnostics = new[]
        {
            // (3,12): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     void M(readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 12)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void ReadonlyWithParams()
    {
        var source = """
            class C
            {
                void M(readonly params int[] p) => throw null;
            }
            """;
        var expectedDiagnostics = new[]
        {
            // (3,12): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     void M(readonly params int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 12)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void RefReadonlyWithParams_01()
    {
        var source = """
            class C
            {
                void M(params ref readonly int[] p) => throw null;
            }
            """;
        var expectedDiagnostics = new[]
        {
            // (3,19): error CS1611: The params parameter cannot be declared as ref
            //     void M(params ref readonly int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_ParamsCantBeWithModifier, "ref").WithArguments("ref").WithLocation(3, 19)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void RefReadonlyWithParams_02()
    {
        var source = """
            class C
            {
                void M(ref readonly params int[] p) => throw null;
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (3,16): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     void M(ref readonly params int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(3, 16),
            // (3,25): error CS8328:  The parameter modifier 'params' cannot be used with 'ref'
            //     void M(ref readonly params int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "params").WithArguments("params", "ref").WithLocation(3, 25));

        var expectedDiagnostics = new[]
        {
            // (3,25): error CS8328:  The parameter modifier 'params' cannot be used with 'ref'
            //     void M(ref readonly params int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "params").WithArguments("params", "ref").WithLocation(3, 25)
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void ReadonlyWithIn()
    {
        var source = """
            class C
            {
                void M(in readonly int[] p) => throw null;
            }
            """;
        var expectedDiagnostics = new[]
        {
            // (3,15): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     void M(in readonly int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 15)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void RefReadonlyWithIn()
    {
        var source = """
            class C
            {
                void M(ref readonly in int[] p) => throw null;
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (3,16): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     void M(ref readonly in int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(3, 16),
            // (3,25): error CS8328:  The parameter modifier 'in' cannot be used with 'ref'
            //     void M(ref readonly in int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "in").WithArguments("in", "ref").WithLocation(3, 25));

        var expectedDiagnostics = new[]
        {
            // (3,25): error CS8328:  The parameter modifier 'in' cannot be used with 'ref'
            //     void M(ref readonly in int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "in").WithArguments("in", "ref").WithLocation(3, 25)
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void ReadonlyWithOut()
    {
        var source = """
            class C
            {
                void M(out readonly int[] p) => throw null;
            }
            """;
        var expectedDiagnostics = new[]
        {
            // (3,16): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     void M(out readonly int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 16)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void RefReadonlyWithOut()
    {
        var source = """
            class C
            {
                void M(ref readonly out int[] p) => throw null;
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (3,16): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     void M(ref readonly out int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(3, 16),
            // (3,25): error CS8328:  The parameter modifier 'out' cannot be used with 'ref'
            //     void M(ref readonly out int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "ref").WithLocation(3, 25));

        var expectedDiagnostics = new[]
        {
            // (3,25): error CS8328:  The parameter modifier 'out' cannot be used with 'ref'
            //     void M(ref readonly out int[] p) => throw null;
            Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "ref").WithLocation(3, 25)
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void ReadonlyWithThis()
    {
        var source = """
            static class C
            {
                public static void M(this readonly int p) => throw null;
            }
            """;
        var expectedDiagnostics = new[]
        {
            // (3,31): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     public static void M(this readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 31)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void RefReadonlyWithThis_01()
    {
        var source = """
            static class C
            {
                public static void M(this ref readonly int p) => throw null;
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (3,35): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static void M(this ref readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(3, 35));

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void RefReadonlyWithThis_02()
    {
        var source = """
            static class C
            {
                public static void M(ref this readonly int p) => throw null;
            }
            """;
        var expectedDiagnostics = new[]
        {
            // (3,35): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     public static void M(ref this readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 35)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void RefReadonlyWithThis_03()
    {
        var source = """
            static class C
            {
                public static void M(ref readonly this int p) => throw null;
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (3,30): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static void M(ref readonly this int p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(3, 30));

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void RefReadonlyWithScoped_01()
    {
        var source = """
            static class C
            {
                public static void M(scoped ref readonly int p) => throw null;
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (3,37): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static void M(scoped ref readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(3, 37));

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void RefReadonlyWithScoped_02()
    {
        var source = """
            static class C
            {
                public static void M(ref scoped readonly int p) => throw null;
            }
            """;
        var expectedDiagnostics = new[]
        {
            // (3,30): error CS0246: The type or namespace name 'scoped' could not be found (are you missing a using directive or an assembly reference?)
            //     public static void M(ref scoped readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "scoped").WithArguments("scoped").WithLocation(3, 30),
            // (3,37): error CS1001: Identifier expected
            //     public static void M(ref scoped readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "readonly").WithLocation(3, 37),
            // (3,37): error CS1003: Syntax error, ',' expected
            //     public static void M(ref scoped readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_SyntaxError, "readonly").WithArguments(",").WithLocation(3, 37),
            // (3,37): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     public static void M(ref scoped readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 37)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void RefReadonlyWithScoped_03()
    {
        var source = """
            static class C
            {
                public static void M(readonly scoped ref int p) => throw null;
            }
            """;
        var expectedDiagnostics = new[]
        {
            // (3,26): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     public static void M(readonly scoped ref int p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 26),
            // (3,35): error CS0246: The type or namespace name 'scoped' could not be found (are you missing a using directive or an assembly reference?)
            //     public static void M(readonly scoped ref int p) => throw null;
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "scoped").WithArguments("scoped").WithLocation(3, 35),
            // (3,42): error CS1001: Identifier expected
            //     public static void M(readonly scoped ref int p) => throw null;
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "ref").WithLocation(3, 42),
            // (3,42): error CS1003: Syntax error, ',' expected
            //     public static void M(readonly scoped ref int p) => throw null;
            Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(3, 42)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void ReadonlyWithScoped()
    {
        var source = """
            static class C
            {
                public static void M(scoped readonly int p) => throw null;
            }
            """;
        var expectedDiagnostics = new[]
        {
            // (3,26): error CS0246: The type or namespace name 'scoped' could not be found (are you missing a using directive or an assembly reference?)
            //     public static void M(scoped readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "scoped").WithArguments("scoped").WithLocation(3, 26),
            // (3,33): error CS1001: Identifier expected
            //     public static void M(scoped readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "readonly").WithLocation(3, 33),
            // (3,33): error CS1003: Syntax error, ',' expected
            //     public static void M(scoped readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_SyntaxError, "readonly").WithArguments(",").WithLocation(3, 33),
            // (3,33): error CS9501: 'readonly' modifier must be specified after 'ref'.
            //     public static void M(scoped readonly int p) => throw null;
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 33)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void RefReadonly_ScopedParameterName()
    {
        var source = """
            static class C
            {
                public static void M(ref readonly int scoped) => throw null;
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (3,30): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static void M(ref readonly int scoped) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(3, 30));

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
        CreateCompilation(source).VerifyDiagnostics();
    }

    [Fact]
    public void RefReadonly_ScopedTypeName()
    {
        var source = """
            struct scoped { }
            static class C
            {
                public static void M(ref readonly scoped p) => throw null;
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (1,8): error CS9062: Types and aliases cannot be named 'scoped'.
            // struct scoped { }
            Diagnostic(ErrorCode.ERR_ScopedTypeNameDisallowed, "scoped").WithLocation(1, 8),
            // (4,30): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static void M(ref readonly scoped p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(4, 30));

        var expectedDiagnostics = new[]
        {
            // (1,8): error CS9062: Types and aliases cannot be named 'scoped'.
            // struct scoped { }
            Diagnostic(ErrorCode.ERR_ScopedTypeNameDisallowed, "scoped").WithLocation(1, 8),
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
            // (1,8): warning CS8981: The type name 'scoped' only contains lower-cased ascii characters. Such names may become reserved for the language.
            // struct scoped { }
            Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "scoped").WithArguments("scoped").WithLocation(1, 8),
            // (4,30): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static void M(ref readonly scoped p) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(4, 30));
    }

    [Fact]
    public void RefReadonly_ScopedBothNames()
    {
        var source = """
            struct scoped { }
            static class C
            {
                public static void M(ref readonly scoped scoped) => throw null;
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (1,8): error CS9062: Types and aliases cannot be named 'scoped'.
            // struct scoped { }
            Diagnostic(ErrorCode.ERR_ScopedTypeNameDisallowed, "scoped").WithLocation(1, 8),
            // (4,30): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static void M(ref readonly scoped scoped) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(4, 30));

        var expectedDiagnostics = new[]
        {
            // (1,8): error CS9062: Types and aliases cannot be named 'scoped'.
            // struct scoped { }
            Diagnostic(ErrorCode.ERR_ScopedTypeNameDisallowed, "scoped").WithLocation(1, 8),
        };

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
            // (1,8): warning CS8981: The type name 'scoped' only contains lower-cased ascii characters. Such names may become reserved for the language.
            // struct scoped { }
            Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "scoped").WithArguments("scoped").WithLocation(1, 8),
            // (4,30): error CS8652: The feature 'ref readonly parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public static void M(ref readonly scoped scoped) => throw null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "readonly").WithArguments("ref readonly parameters").WithLocation(4, 30));
    }
}
