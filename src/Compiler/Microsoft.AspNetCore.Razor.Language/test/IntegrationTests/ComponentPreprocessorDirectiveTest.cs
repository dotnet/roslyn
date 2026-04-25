// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public class ComponentPreprocessorDirectiveTest(bool designTime = false)
    : RazorBaselineIntegrationTestBase(layer: TestProject.Layer.Compiler)
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;

    internal override bool UseTwoPhaseCompilation => true;

    internal string ComponentName = "TestComponent";

    internal override string DefaultFileName => ComponentName + ".razor";

    internal override bool DesignTime => designTime;

    protected override string GetDirectoryPath(string testName)
    {
        var directory = DesignTime ? "ComponentDesignTimePreprocessorDirectiveTest" : "ComponentRuntimePreprocessorDirectiveTest";
        return $"TestFiles/IntegrationTests/{directory}/{testName}";
    }

    [IntegrationTestFact]
    public void IfDefAndPragma()
    {
        var generated = CompileToCSharp("""
            @{
            #pragma warning disable 219 // variable declared but not used
            #if true
                var x = 1;
            #endif
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void DisabledText_01()
    {
        var generated = CompileToCSharp("""
            @{
            #if false
                <p>Some text</p>
            #endif
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void PassParseOptionsThrough_01()
    {
        var parseOptions = CSharpParseOptions.WithPreprocessorSymbols("SomeSymbol");

        var generated = CompileToCSharp("""
            @{
            #if SomeSymbol
                <p>Some text</p>
            #endif
            }
            """,
            csharpParseOptions: parseOptions);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void PassParseOptionsThrough_02()
    {
        var parseOptions = CSharpParseOptions.WithPreprocessorSymbols("SomeSymbol");

        var generated = CompileToCSharp("""
            @{
            #if !SomeSymbol
                <p>Some text</p>
            #endif
            }
            """,
            csharpParseOptions: parseOptions);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void DefineAndUndef()
    {
        var generated = CompileToCSharp("""
            @{
            #define SomeSymbol
            #undef SomeSymbol
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated,
            // x:\dir\subdir\Test\TestComponent.cshtml(2,2): error CS1032: Cannot define/undefine preprocessor symbols after first token in file
            // #define SomeSymbol
            Diagnostic(ErrorCode.ERR_PPDefFollowsToken, "define").WithLocation(2, 2),
            // x:\dir\subdir\Test\TestComponent.cshtml(3,2): error CS1032: Cannot define/undefine preprocessor symbols after first token in file
            // #undef SomeSymbol
            Diagnostic(ErrorCode.ERR_PPDefFollowsToken, "undef").WithLocation(3, 2)
        );
    }

    [IntegrationTestFact]
    public void AfterTag()
    {
        var generated = CompileToCSharp("""
            @{
                <div>
            #if true
                <div>
            }
            @{
            #endif
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument, verifyLinePragmas: false);
        CompileToAssembly(generated,
            // x:\dir\subdir\Test\TestComponent.cshtml(7,1): error CS1028: Unexpected preprocessor directive
            // #endif
            Diagnostic(ErrorCode.ERR_UnexpectedDirective, "#endif").WithLocation(7, 1));
    }

    [IntegrationTestFact]
    public void StartOfLine_01()
    {
        var generated = CompileToCSharp("""
            @{ #if true }
            @{ #endif }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated,
            // x:\dir\subdir\Test\TestComponent.cshtml(1,13): error CS1025: Single-line comment or end-of-line expected
            //    #if true }
            Diagnostic(ErrorCode.ERR_EndOfPPLineExpected, "}").WithLocation(1, 13),
            // x:\dir\subdir\Test\TestComponent.cshtml(2,11): error CS1025: Single-line comment or end-of-line expected
            //    #endif }
            Diagnostic(ErrorCode.ERR_EndOfPPLineExpected, "}").WithLocation(2, 11));
    }

    [IntegrationTestFact]
    public void StartOfLine_02()
    {
        var generated = CompileToCSharp("""
            @{
            /* test */ #if true
            }
            @{
            /* test */ #endif
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated,
            // x:\dir\subdir\Test\TestComponent.cshtml(2,12): error CS1040: Preprocessor directives must appear as the first non-whitespace character on a line
            // /* test */ #if true
            Diagnostic(ErrorCode.ERR_BadDirectivePlacement, "#").WithLocation(2, 12),
            // x:\dir\subdir\Test\TestComponent.cshtml(5,12): error CS1040: Preprocessor directives must appear as the first non-whitespace character on a line
            // /* test */ #endif
            Diagnostic(ErrorCode.ERR_BadDirectivePlacement, "#").WithLocation(5, 12));
    }

    [IntegrationTestFact]
    public void StartOfLine_03()
    {
        var generated = CompileToCSharp("""
            @{
            #pragma warning disable 219
            var x = 1; #if true
            }
            @{
            var y = 2; #endif
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated,
            // x:\dir\subdir\Test\TestComponent.cshtml(3,12): error CS1040: Preprocessor directives must appear as the first non-whitespace character on a line
            // var x = 1; #if true
            Diagnostic(ErrorCode.ERR_BadDirectivePlacement, "#").WithLocation(3, 12),
            // x:\dir\subdir\Test\TestComponent.cshtml(6,12): error CS1040: Preprocessor directives must appear as the first non-whitespace character on a line
            // var y = 2; #endif
            Diagnostic(ErrorCode.ERR_BadDirectivePlacement, "#").WithLocation(6, 12));
    }

    [IntegrationTestFact]
    public void StartOfLine_04()
    {
        var generated = CompileToCSharp("""
            @{
            var x = #if true;
            }
            @{
            x #endif;
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated,
            // x:\dir\subdir\Test\TestComponent.razor(2,9): error CS1040: Preprocessor directives must appear as the first non-whitespace character on a line
            // var x = #if true;
            Diagnostic(ErrorCode.ERR_BadDirectivePlacement, "#").WithLocation(2, 9),
            // x:\dir\subdir\Test\TestComponent.razor(5,1): error CS0841: Cannot use local variable 'x' before it is declared
            // x #endif;
            Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x").WithArguments("x").WithLocation(5, 1),
            // x:\dir\subdir\Test\TestComponent.razor(5,2): error CS1002: ; expected
            // x #endif;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(5, 2),
            // x:\dir\subdir\Test\TestComponent.razor(5,3): error CS1040: Preprocessor directives must appear as the first non-whitespace character on a line
            // x #endif;
            Diagnostic(ErrorCode.ERR_BadDirectivePlacement, "#").WithLocation(5, 3));
    }

    [IntegrationTestFact]
    public void StartOfLine_05()
    {
        var generated = CompileToCSharp("""
            @{
            <div>#if true</div>
            }
            @{
            <div>#endif</div>
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument, verifyLinePragmas: false);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void StartOfLine_06()
    {
        var generated = CompileToCSharp("""
            @{
                #if true
            }
            @{
                #endif
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void StartOfLine_07()
    {
        // This test uses tabs as the leading whitespace
        var generated = CompileToCSharp("""
            @{
            	#if true
            }
            @{
            	#endif
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void StartOfLine_08()
    {
        // vertical tab
        var generated = CompileToCSharp($$"""
            @{
            {{'\v'}}#if true
            }
            @{
            {{'\v'}}#endif
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void StartOfLine_09()
    {
        // Form feed
        var generated = CompileToCSharp($$"""
            @{
            {{'\f'}}#if true
            }
            @{
            {{'\f'}}#endif
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void StartOfLine_10()
    {
        // NBSP
        var generated = CompileToCSharp($$"""
            @{
            {{'\u00A0'}}#if true
            }
            @{
            {{'\u00A0'}}#endif
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void StartOfLine_11()
    {
        // ZWNBSP
        var generated = CompileToCSharp($$"""
            @{
            {{'\uFEFF'}}#if true
            }
            @{
            {{'\uFEFF'}}#endif
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void StartOfLine_12()
    {
        var generated = CompileToCSharp("""
            @{ #if true
                var x = 1;
                #endif
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated,
            // x:\dir\subdir\Test\TestComponent.razor(2,9): warning CS0219: The variable 'x' is assigned but its value is never used
            //     var x = 1;
            Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(2, 9)
        );
    }

    [IntegrationTestFact]
    public void MisplacedEndingDirective_01()
    {
        var generated = CompileToCSharp("""
            @{
            #if false
            }
            @{ #endif }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        var expectedDiagnostic = DesignTime ?
            new[]
            {
                // x:\dir\subdir\Test\TestComponent.cshtml(14,1): error CS1027: #endif directive expected
                //
                Diagnostic(ErrorCode.ERR_EndifDirectiveExpected, "").WithLocation(14, 1),
                // (26,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(26, 10),
                // (26,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(26, 10),
                // (26,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(26, 10)
            }
            :
            new[]
            {

               // x:\dir\subdir\Test\TestComponent.cshtml(15,1): error CS1027: #endif directive expected
                //
                Diagnostic(ErrorCode.ERR_EndifDirectiveExpected, "").WithLocation(15, 1),
                // (19,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(19, 10),
                // (19,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(19, 10),
                // (19,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(19, 10)
            };

        CompileToAssembly(generated, expectedDiagnostic);
    }

    [IntegrationTestFact]
    public void MisplacedEndingDirective_02()
    {
        var generated = CompileToCSharp("""
            @{
            #if false
            }
            @{ Test #endif }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        var expectedDiagnostics = DesignTime ?
            new[]
            {
                // x:\dir\subdir\Test\TestComponent.cshtml(14,1): error CS1027: #endif directive expected
                //
                Diagnostic(ErrorCode.ERR_EndifDirectiveExpected, "").WithLocation(14, 1),
                // (26,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(26, 10),
                // (26,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(26, 10),
                // (26,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(26, 10)
            }
            :
            new[]
            {
                // x:\dir\subdir\Test\TestComponent.cshtml(15,1): error CS1027: #endif directive expected
                //
                Diagnostic(ErrorCode.ERR_EndifDirectiveExpected, "").WithLocation(15, 1),
                // (19,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(19, 10),
                // (19,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(19, 10),
                // (19,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(19, 10)
            };

        CompileToAssembly(generated, expectedDiagnostics);
    }

    [IntegrationTestFact]
    public void MisplacedEndingDirective_03()
    {
        var generated = CompileToCSharp("""
            @{
            #if false
            }
            @{
            /* test */ #endif
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        var expectedDiagnostics = DesignTime ?
            new[]
            {
                // x:\dir\subdir\Test\TestComponent.cshtml(16,1): error CS1027: #endif directive expected
                //
                Diagnostic(ErrorCode.ERR_EndifDirectiveExpected, "").WithLocation(16, 1),
                // (26,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(26, 10),
                // (26,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(26, 10),
                // (26,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(26, 10)
            }
            :
            new[]
            {
                // x:\dir\subdir\Test\TestComponent.cshtml(17,1): error CS1027: #endif directive expected
                //
                Diagnostic(ErrorCode.ERR_EndifDirectiveExpected, "").WithLocation(17, 1),
                // (19,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(19, 10),
                // (19,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(19, 10),
                // (19,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(19, 10)
            };

        CompileToAssembly(generated, expectedDiagnostics);
    }

    [IntegrationTestFact]
    public void MisplacedEndingDirective_04()
    {
        var generated = CompileToCSharp("""
            @{
            #if false
            }
            @{
            /* test */ #endif
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        var expectedDiagnostics = DesignTime ?
            new[]
            {
                // x:\dir\subdir\Test\TestComponent.cshtml(16,1): error CS1027: #endif directive expected
                //
                Diagnostic(ErrorCode.ERR_EndifDirectiveExpected, "").WithLocation(16, 1),
                // (26,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(26, 10),
                // (26,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(26, 10),
                // (26,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(26, 10)
            }
            :
            new[]
            {
                // x:\dir\subdir\Test\TestComponent.cshtml(17,1): error CS1027: #endif directive expected
                //
                Diagnostic(ErrorCode.ERR_EndifDirectiveExpected, "").WithLocation(17, 1),
                // (19,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(19, 10),
                // (19,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(19, 10),
                // (19,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(19, 10)
            };

        CompileToAssembly(generated, expectedDiagnostics);
    }

    [IntegrationTestFact]
    public void MisplacedEndingDirective_05()
    {
        var generated = CompileToCSharp("""
            @{
            #if false
            }
            @{
            <div>#endif</div>
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        var expectedDiagnostics = DesignTime ?
            new[]
            {
                // x:\dir\subdir\Test\TestComponent.cshtml(16,1): error CS1027: #endif directive expected
                //
                Diagnostic(ErrorCode.ERR_EndifDirectiveExpected, "").WithLocation(16, 1),
                // (26,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(26, 10),
                // (26,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(26, 10),
                // (26,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(26, 10)
            }
            :
            new[]
            {
                // x:\dir\subdir\Test\TestComponent.cshtml(17,1): error CS1027: #endif directive expected
                //
                Diagnostic(ErrorCode.ERR_EndifDirectiveExpected, "").WithLocation(17, 1),
                // (19,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(19, 10),
                // (19,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(19, 10),
                // (19,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(19, 10)
            };

        CompileToAssembly(generated, expectedDiagnostics);
    }

    [IntegrationTestFact]
    public void MisplacedEndingDirective_06()
    {
        var generated = CompileToCSharp("""
            @{
            #if false
            }
            @{ #else }
            @{
            #endif
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void MisplacedEndingDirective_07()
    {
        var generated = CompileToCSharp("""
            @{
            #if false
            }
            @{ Test #else }
            @{
            #endif
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void MisplacedEndingDirective_08()
    {
        var generated = CompileToCSharp("""
            @{
            #if false
            }
            @{
            /* test */ #else
            }
            @{
            #endif
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void MisplacedEndingDirective_09()
    {
        var generated = CompileToCSharp("""
            @{
            #if false
            }
            @{
            /* test */ #else
            }
            @{
            #endif
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [IntegrationTestFact]
    public void MisplacedEndingDirective_10()
    {
        var generated = CompileToCSharp("""
            @{
            #if false
            }
            @{
            <div>#else</div>
            }
            @{
            #endif
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

        CompileToAssembly(generated);
    }
}
