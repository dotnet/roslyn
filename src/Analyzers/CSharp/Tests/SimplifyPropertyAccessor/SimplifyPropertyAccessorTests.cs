// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.TransposeRecordKeyword;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.SimplifyPropertyAccessor;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SimplifyPropertyAccessor;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpSimplifyPropertyAccessorDiagnosticAnalyzer,
    CSharpSimplifyPropertyAccessorCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyPropertyAccessor)]
public sealed class SimplifyPropertyAccessorTests
{
    private static async Task TestAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup,
        LanguageVersion languageVersion = LanguageVersion.Preview)
    {
        await new VerifyCS.Test
        {
            TestCode = markup,
            FixedCode = markup,
            LanguageVersion = languageVersion,
        }.RunAsync();
    }

    private static async Task TestAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string fixedMarkup)
    {
        await new VerifyCS.Test
        {
            TestCode = initialMarkup,
            FixedCode = fixedMarkup,
            LanguageVersion = LanguageVersion.Preview,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80, // for 'IsExternalInit' type
        }.RunAsync();
    }

    public static IEnumerable<string> SimplifiableGetterBodies => ["{ return field; }", "=> field;"];

    public static IEnumerable<string> TrailingTrivia => ["", " // useful comment"];

    public static IEnumerable<string> SimplifiableSetterBodies => ["{ field = value; }", "=> field = value;"];

    public static IEnumerable<string> SetInitKeywords => ["set", "init"];

    [Fact]
    public async Task NotInCSharpVersionBefore14()
    {
        // 'field' is not even parsed as a keyword before C# 14, but let's test this anyway
        await TestAsync("""
            class C
            {
                public int Prop
                {
                    get => {|CS0103:field|};
                    set => {|CS0103:field|} = value;
                }
            }
            """, LanguageVersion.CSharp13);
    }

    [Fact]
    public async Task NotWhenOptionIsDisabled()
    {
        var code = """
            class C
            {
                public int Prop
                {
                    get => field;
                    set => field = value;
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            LanguageVersion = LanguageVersion.Preview,
            Options =
            {
                { CSharpCodeStyleOptions.PreferSimplePropertyAccessors, false }
            }
        }.RunAsync();
    }

    [Fact]
    public async Task NotWhenAccessorHasSyntaxError()
    {
        var code = """
            class C
            {
                public int Prop
                {
                    get => field{|CS1002:|}
                    set { field = value {|CS1002:}|}
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            LanguageVersion = LanguageVersion.Preview
        }.RunAsync();
    }

    [Theory]
    [CombinatorialData]
    public async Task SimpleGetter(
        [CombinatorialMemberData(nameof(SimplifiableGetterBodies))] string getterBody,
        [CombinatorialMemberData(nameof(TrailingTrivia))] string trailingTrivia)
    {
        await TestAsync($$"""
            class C
            {
                public int Prop
                {
                    {|IDE0360:get {{getterBody}}|}{{trailingTrivia}}
                    set;
                }
            }
            """, $$"""
            class C
            {
                public int Prop
                {
                    get;{{trailingTrivia}}
                    set;
                }
            }
            """);
    }

    [Theory]
    [CombinatorialData]
    public async Task SimpleSetter(
        [CombinatorialMemberData(nameof(SetInitKeywords))] string setterKeyword,
        [CombinatorialMemberData(nameof(SimplifiableSetterBodies))] string setterBody,
        [CombinatorialMemberData(nameof(TrailingTrivia))] string trailingTrivia)
    {
        await TestAsync($$"""
            class C
            {
                public int Prop
                {
                    get;
                    {|IDE0360:{{setterKeyword}} {{setterBody}}|}{{trailingTrivia}}
                }
            }
            """, $$"""
            class C
            {
                public int Prop
                {
                    get;
                    {{setterKeyword}};{{trailingTrivia}}
                }
            }
            """);
    }

    [Theory]
    [CombinatorialData]
    public async Task FixAll(
        [CombinatorialMemberData(nameof(SimplifiableGetterBodies))] string getterBody,
        [CombinatorialMemberData(nameof(TrailingTrivia))] string getterTrailingTrivia,
        [CombinatorialMemberData(nameof(SetInitKeywords))] string setterKeyword,
        [CombinatorialMemberData(nameof(SimplifiableSetterBodies))] string setterBody,
        [CombinatorialMemberData(nameof(TrailingTrivia))] string setterTrailingTrivia)
    {
        await TestAsync($$"""
            class C
            {
                public int Prop
                {
                    {|IDE0360:get {{getterBody}}|}{{getterTrailingTrivia}}
                    {|IDE0360:{{setterKeyword}} {{setterBody}}|}{{setterTrailingTrivia}}
                }
            }
            """, $$"""
            class C
            {
                public int Prop
                {
                    get;{{getterTrailingTrivia}}
                    {{setterKeyword}};{{setterTrailingTrivia}}
                }
            }
            """);
    }

    [Fact]
    public async Task NotWhenPropertyHasNoAccessors()
    {
        // Just to verify we do not crash etc.
        await TestAsync("""
            class C
            {
                public int {|CS0548:Prop|} { }
            }
            """);
    }

    [Fact]
    public async Task NotWhenPropertyHasTooManyAccessors()
    {
        await TestAsync("""
            class C
            {
                public int Prop { get { return field; } set => field = value; {|CS1007:init|} { field = value; } }
            }
            """);
    }

    [Fact]
    public async Task NotWhenPropertyHasDuplicateAccessors()
    {
        await TestAsync("""
            class C
            {
                public int Prop { get { return field; } {|CS1007:get|} => field; }
            }
            """);
    }

    [Fact]
    public async Task NotOnIndexerAccessor()
    {
        // Again 'field' is not even parsed as a keyword
        await TestAsync("""
            class C
            {
                public int this[int i]
                {
                    get => {|CS0103:field|};
                    set => {|CS0103:field|} = value;
                }
            }
            """);
    }

    [Fact]
    public async Task NotOnPartialPropertyImplementationWithAnotherAccessorEmpty_Get()
    {
        await TestAsync("""
            partial class C
            {
                public partial int Prop { get; set; }
                public partial int Prop { get => field; set; }
            }
            """);
    }

    [Fact]
    public async Task NotOnPartialPropertyImplementationWithAnotherAccessorEmpty_Set()
    {
        await TestAsync("""
            partial class C
            {
                public partial int Prop { get; set; }
                public partial int Prop { get; set { field = value; } }
            }
            """);
    }

    [Fact]
    public async Task PartialPropertyImplementation_BothAccessors1()
    {
        await TestAsync("""
            partial class C
            {
                public partial int Prop { get; set; }

                public partial int Prop
                {
                    {|IDE0360:get => field;|}
                    {|IDE0360:set { field = value; }|}
                }
            }
            """, """"
            partial class C
            {
                public partial int Prop { get; set; }

                public partial int Prop
                {
                    get;
                    set { field = value; }
                }
            }
            """");
    }

    [Fact]
    public async Task PartialPropertyImplementation_BothAccessors2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                partial class C
                {
                    public partial int Prop { get; set; }

                    public partial int Prop
                    {
                        {|IDE0360:get => field;|}
                        {|IDE0360:set { field = value; }|}
                    }
                }
                """,
            FixedCode = """
                partial class C
                {
                    public partial int Prop { get; set; }
            
                    public partial int Prop
                    {
                        get => field;
                        set;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.Preview,
            CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne,
            DiagnosticSelector = diagnostics => diagnostics[1],
        }.RunAsync();
    }
}
