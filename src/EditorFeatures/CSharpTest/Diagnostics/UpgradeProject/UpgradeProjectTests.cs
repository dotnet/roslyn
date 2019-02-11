﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UpgradeProject;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.UpgradeProject
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsUpgradeProject)]
    public partial class UpgradeProjectTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpUpgradeProjectCodeFixProvider());

        private async Task TestLanguageVersionUpgradedAsync(
            string initialMarkup,
            LanguageVersion expected,
            ParseOptions parseOptions,
            int index = 0)
        {
            var parameters = new TestParameters(parseOptions: parseOptions, index: index);
            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, parameters))
            {
                var (_, action) = await GetCodeActionsAsync(workspace, parameters);
                var operations = await VerifyActionAndGetOperationsAsync(action, default);

                var (oldSolution, newSolution) = ApplyOperationsAndGetSolution(workspace, operations);
                Assert.All(newSolution.Projects.Where(p => p.Language == LanguageNames.CSharp),
                    p => Assert.Equal(expected, ((CSharpParseOptions)p.ParseOptions).SpecifiedLanguageVersion));

                // Verify no document changes when upgrade project
                var changedDocs = SolutionUtilities.GetTextChangedDocuments(oldSolution, newSolution);
                Assert.Empty(changedDocs);
            }

            await TestAsync(initialMarkup, initialMarkup, parseOptions); // no change to markup
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7_2ToCSharp8()
        {
            await TestLanguageVersionUpgradedAsync(
@"class C
{
    object F = [|null!|];
}",
                LanguageVersion.Preview,
                new CSharpParseOptions(LanguageVersion.CSharp7_2));
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7ToCSharp8()
        {
            await TestLanguageVersionUpgradedAsync(
@"class C
{
    object F = [|null!|];
}",
                LanguageVersion.Preview,
                new CSharpParseOptions(LanguageVersion.CSharp7));
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp6ToCSharp7()
        {
            await TestLanguageVersionUpgradedAsync(
@"
class Program
{
    void A()
    {
        var x = [|(1, 2)|];
    }
}",
                LanguageVersion.CSharp7,
                new CSharpParseOptions(LanguageVersion.CSharp6));
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp5ToCSharp6()
        {
            await TestLanguageVersionUpgradedAsync(
@"
class Program
{
    void A()
    {
        var x = [|nameof(A)|];
    }
}",
                LanguageVersion.CSharp6,
                new CSharpParseOptions(LanguageVersion.CSharp5));
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp4ToCSharp5()
        {
            await TestLanguageVersionUpgradedAsync(
@"
class Program
{
    void A()
    {
        Func<int, Task<int>> f = [|async|] x => x;
    }
}",
                LanguageVersion.CSharp5,
                new CSharpParseOptions(LanguageVersion.CSharp4));
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7ToLatest()
        {
            await TestLanguageVersionUpgradedAsync(
$@"
class Program
{{
#error version:[|{LanguageVersion.Latest.MapSpecifiedToEffectiveVersion().ToDisplayString()}|]
}}",
                LanguageVersion.Latest.MapSpecifiedToEffectiveVersion(),
                new CSharpParseOptions(LanguageVersion.CSharp7));
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7To7_1_TriggeredByInferredTupleNames()
        {
            await TestLanguageVersionUpgradedAsync(
@"
class Program
{
    void M()
    {
        int b = 2;
        var t = (1, b);
        System.Console.Write(t.[|b|]);
    }
}

namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }
    }
}
",
                LanguageVersion.CSharp7_1,
                new CSharpParseOptions(LanguageVersion.CSharp7));
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7_1ToLatest()
        {
            await TestLanguageVersionUpgradedAsync(
$@"
class Program
{{
#error version:[|{LanguageVersion.Latest.MapSpecifiedToEffectiveVersion().ToDisplayString()}|]
}}",
                LanguageVersion.Latest.MapSpecifiedToEffectiveVersion(),
                new CSharpParseOptions(LanguageVersion.CSharp7_1));
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7ToCSharp7_1()
        {
            await TestLanguageVersionUpgradedAsync(
@"
class Program
{
#error [|version:7.1|]
}",
                LanguageVersion.CSharp7_1,
                new CSharpParseOptions(LanguageVersion.CSharp7));
        }

        [Fact]
        public async Task UpgradeProjectWithNonTrailingNamedArgumentToCSharp7_2()
        {
            await TestLanguageVersionUpgradedAsync(
@"
class Program
{
    void M()
    {
        [|M2(a: 1, 2);|]
    }
}",
                LanguageVersion.CSharp7_2,
                new CSharpParseOptions(LanguageVersion.CSharp7_1));
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7ToCSharp7_1_B()
        {
            await TestLanguageVersionUpgradedAsync(
@"public class Base { }
public class Derived : Base { }
public class Program
{
    public static void M<T>(T x) where T: Base
    {
        System.Console.Write(x is [|Derived|] b0);
    }
}
",
                LanguageVersion.CSharp7_1,
                new CSharpParseOptions(LanguageVersion.CSharp7));
        }

        #region C# 7.3
        [Fact]
        public async Task UpgradeProjectFromCSharp7_2ToLatest()
        {
            await TestLanguageVersionUpgradedAsync(
$@"
class Program
{{
#error version:[|{LanguageVersion.Latest.MapSpecifiedToEffectiveVersion().ToDisplayString()}|]
}}",
                LanguageVersion.Latest.MapSpecifiedToEffectiveVersion(),
                new CSharpParseOptions(LanguageVersion.CSharp7_2));
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7_2To7_3_TriggeredByAttributeOnBackingField()
        {
            await TestLanguageVersionUpgradedAsync(
@"
class A : System.Attribute { }
class Program
{
    [|[field: A]|]
    int P { get; set; }
}",
                LanguageVersion.CSharp7_3,
                new CSharpParseOptions(LanguageVersion.CSharp7_2));
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7_2To7_3_EnumConstraint()
        {
            await TestLanguageVersionUpgradedAsync(
@"public class X<T> where T : [|System.Enum|]
{
}
",
                LanguageVersion.CSharp7_3,
                new CSharpParseOptions(LanguageVersion.CSharp7_2));
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7_2To7_3_DelegateConstraint()
        {
            await TestLanguageVersionUpgradedAsync(
@"public class X<T> where T : [|System.Delegate|]
{
}
",
                LanguageVersion.CSharp7_3,
                new CSharpParseOptions(LanguageVersion.CSharp7_2));
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7_2To7_3_MulticastDelegateConstraint()
        {
            await TestLanguageVersionUpgradedAsync(
@"public class X<T> where T : [|System.MulticastDelegate|]
{
}
",
                LanguageVersion.CSharp7_3,
                new CSharpParseOptions(LanguageVersion.CSharp7_2));
        }
        #endregion C# 7.3

        #region C# 8.0
        [Fact(Skip = "https://github.com/dotnet/roslyn/pull/29820")]
        public async Task UpgradeProjectFromCSharp7_3ToLatest()
        {
            await TestLanguageVersionUpgradedAsync(
$@"
class Program
{{
#error version:[|{LanguageVersion.Latest.MapSpecifiedToEffectiveVersion().ToDisplayString()}|]
}}",
                LanguageVersion.Latest.MapSpecifiedToEffectiveVersion(),
                new CSharpParseOptions(LanguageVersion.CSharp7_3));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/pull/29820")]
        public async Task UpgradeProjectFromCSharp7_3To8_0()
        {
            await TestLanguageVersionUpgradedAsync(
$@"
class Program
{{
#error version:[|{LanguageVersion.CSharp8.ToDisplayString()}|]
}}",
                LanguageVersion.Latest.MapSpecifiedToEffectiveVersion(),
                new CSharpParseOptions(LanguageVersion.CSharp7_3));
        }

        [Fact]
        public async Task UpgradeProjectForVerbatimInterpolatedString()
        {
            await TestLanguageVersionUpgradedAsync(
@"
class Program
{
    void A()
    {
        var x = [|@$""hello""|];
    }
}",
                expected: LanguageVersion.CSharp8,
                new CSharpParseOptions(LanguageVersion.CSharp7_3));
        }
        #endregion

        [Fact]
        public async Task UpgradeAllProjectsToCSharp7()
        {
            await TestLanguageVersionUpgradedAsync(
@"<Workspace>
    <Project Language=""C#"" LanguageVersion=""6"">
        <Document>
class C
{
    void A()
    {
        var x = [|(1, 2)|];
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" LanguageVersion=""6"">
    </Project>
    <Project Language=""C#"" LanguageVersion=""7"">
    </Project>
    <Project Language=""Visual Basic"">
    </Project>
</Workspace>",
                LanguageVersion.CSharp7,
                parseOptions: null,
                index: 1);
        }

        [Fact]
        public async Task UpgradeAllProjectsToCSharp8()
        {
            await TestLanguageVersionUpgradedAsync(
@"<Workspace>
    <Project Language=""C#"" LanguageVersion=""6"">
        <Document>
class C
{
    object F = [|null!|];
}
        </Document>
    </Project>
    <Project Language=""C#"" LanguageVersion=""6"">
    </Project>
    <Project Language=""C#"" LanguageVersion=""Default"">
    </Project>
    <Project Language=""C#"" LanguageVersion=""7"">
    </Project>
    <Project Language=""Visual Basic"">
    </Project>
</Workspace>",
                LanguageVersion.Preview,
                parseOptions: null,
                index: 1);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/30027")]
        [WorkItem(30027, "https://github.com/dotnet/roslyn/issues/30027")]
        public async Task UpgradeAllProjectsToCSharp8_NullableReferenceType()
        {
            await TestLanguageVersionUpgradedAsync(
@"<Workspace>
    <Project Language=""C#"" LanguageVersion=""6"">
        <Document>
class C
{
    void A(string? [|s|])
    {
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" LanguageVersion=""6"">
    </Project>
    <Project Language=""C#"" LanguageVersion=""Default"">
    </Project>
    <Project Language=""C#"" LanguageVersion=""7"">
    </Project>
    <Project Language=""Visual Basic"">
    </Project>
</Workspace>",
                LanguageVersion.CSharp8,
                parseOptions: null,
                index: 1);
        }

        [Fact]
        public async Task ListAllSuggestions()
        {
            await TestExactActionSetOfferedAsync(

@"<Workspace>
    <Project Language=""C#"" LanguageVersion=""6"">
        <Document>
class C
{
    void A()
    {
        var x = [|(1, 2)|];
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" LanguageVersion=""7"">
    </Project>
    <Project Language=""C#"" LanguageVersion=""6"">
    </Project>
</Workspace>",
                new[] {
                    string.Format(CSharpFeaturesResources.Upgrade_this_project_to_csharp_language_version_0, "7.0"),
                    string.Format(CSharpFeaturesResources.Upgrade_all_csharp_projects_to_language_version_0, "7.0")
                });
        }

        [Fact]
        public async Task ListAllSuggestions_CSharp8()
        {
            var version8 = LanguageVersion.CSharp8.ToDisplayString();
            await TestExactActionSetOfferedAsync(

@"<Workspace>
    <Project Language=""C#"" LanguageVersion=""6"">
        <Document>
class C
{
    void A()
    {
#error version:[|8|]
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" LanguageVersion=""7"">
    </Project>
    <Project Language=""C#"" LanguageVersion=""800"">
    </Project>
</Workspace>",
                new[] {
                    string.Format(CSharpFeaturesResources.Upgrade_this_project_to_csharp_language_version_0, version8),
                    string.Format(CSharpFeaturesResources.Upgrade_all_csharp_projects_to_language_version_0, version8)
    });
        }

        [Fact]
        public async Task FixAllProjectsNotOffered()
        {
            await TestExactActionSetOfferedAsync(

@"<Workspace>
    <Project Language=""C#"" LanguageVersion=""6"">
        <Document>
class C
{
    void A()
    {
        var x = [|(1, 2)|];
    }
}
        </Document>
    </Project>
    <Project Language=""Visual Basic"">
    </Project>
</Workspace>",
                new[] {
                    string.Format(CSharpFeaturesResources.Upgrade_this_project_to_csharp_language_version_0, "7.0")
                    });
        }

        [Fact]
        public async Task OnlyOfferFixAllProjectsFromCSharp6ToCSharp7WhenApplicable()
        {
            await TestExactActionSetOfferedAsync(

@"<Workspace>
    <Project Language=""C#"" LanguageVersion=""6"">
        <Document>
class C
{
    void A()
    {
        var x = [|(1, 2)|];
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" LanguageVersion=""7"">
    </Project>
    <Project Language=""Visual Basic"">
    </Project>
</Workspace>",
                new[] {
                    string.Format(CSharpFeaturesResources.Upgrade_this_project_to_csharp_language_version_0, "7.0")
                    });
        }

        [Fact]
        public async Task OnlyOfferFixAllProjectsFromCSharp6ToDefaultWhenApplicable()
        {
            var defaultVersion = LanguageVersion.Default.MapSpecifiedToEffectiveVersion().ToDisplayString();
            await TestExactActionSetOfferedAsync(

$@"<Workspace>
    <Project Language=""C#"" LanguageVersion=""6"">
        <Document>
class C
{{
    void A()
    {{
#error version:[|{defaultVersion}|]
    }}
}}
        </Document>
    </Project>
    <Project Language=""C#"" LanguageVersion=""Default"">
    </Project>
    <Project Language=""Visual Basic"">
    </Project>
</Workspace>",
                new[] {
                    string.Format(CSharpFeaturesResources.Upgrade_this_project_to_csharp_language_version_0, defaultVersion),
                    string.Format(CSharpFeaturesResources.Upgrade_all_csharp_projects_to_language_version_0, defaultVersion)
                    });
        }

        [Fact]
        public async Task OnlyOfferFixAllProjectsToCSharp8WhenApplicable()
        {
            var previewVersion = LanguageVersion.Preview.ToDisplayString();
            await TestExactActionSetOfferedAsync(

@"<Workspace>
    <Project Language=""C#"" LanguageVersion=""6"">
        <Document>
class C
{
    object F = [|null!|];
}
        </Document>
    </Project>
    <Project Language=""C#"" LanguageVersion=""800"">
    </Project>
    <Project Language=""Visual Basic"">
    </Project>
</Workspace>",
                new[] {
                    string.Format(CSharpFeaturesResources.Upgrade_this_project_to_csharp_language_version_0, previewVersion),
                    string.Format(CSharpFeaturesResources.Upgrade_all_csharp_projects_to_language_version_0, previewVersion)
                    });
        }

        [Fact]
        public async Task OnlyOfferFixAllProjectsToDefaultWhenApplicable()
        {
            string defaultEffectiveVersion = LanguageVersion.Default.MapSpecifiedToEffectiveVersion().ToDisplayString();
            await TestExactActionSetOfferedAsync(

$@"<Workspace>
    <Project Language=""C#"" LanguageVersion=""6"">
        <Document>
class C
{{
    void A()
    {{
#error version:[|{defaultEffectiveVersion}|]
    }}
}}
        </Document>
    </Project>
    <Project Language=""C#"" LanguageVersion=""Default"">
    </Project>
    <Project Language=""Visual Basic"">
    </Project>
</Workspace>",
                new[] {
                    string.Format(CSharpFeaturesResources.Upgrade_this_project_to_csharp_language_version_0, defaultEffectiveVersion),
                    string.Format(CSharpFeaturesResources.Upgrade_all_csharp_projects_to_language_version_0, defaultEffectiveVersion)
                    });
        }

        [Fact]
        public async Task UpgradeProjectWithUnmanagedConstraintTo7_3_Type()
        {
            await TestLanguageVersionUpgradedAsync(
@"
class Test<T> where T : [|unmanaged|]
{
}",
                LanguageVersion.CSharp7_3,
                new CSharpParseOptions(LanguageVersion.CSharp7));
        }

        [Fact]
        public async Task UpgradeProjectWithUnmanagedConstraintTo7_3_Type_AlreadyDefined()
        {
            await TestExactActionSetOfferedAsync(
@"<Workspace>
    <Project Language=""C#"" LanguageVersion=""7"">
        <Document>
interface unmanaged { }
class Test&lt;T&gt; where T : [|unmanaged|]
{
}
        </Document>
    </Project>
</Workspace>",
                expectedActionSet: Enumerable.Empty<string>());
        }

        [Fact]
        public async Task UpgradeProjectWithUnmanagedConstraintTo7_3_Method()
        {
            await TestLanguageVersionUpgradedAsync(
@"
class Test
{
    public void M<T>() where T : [|unmanaged|] { }
}",
                LanguageVersion.CSharp7_3,
                new CSharpParseOptions(LanguageVersion.CSharp7));
        }

        [Fact]
        public async Task UpgradeProjectWithUnmanagedConstraintTo7_3_Method_AlreadyDefined()
        {
            await TestExactActionSetOfferedAsync(
@"<Workspace>
    <Project Language=""C#"" LanguageVersion=""7"">
        <Document>
interface unmanaged { }
class Test
{
    public void M&lt;T&gt;() where T : [|unmanaged|] { }
}
        </Document>
    </Project>
</Workspace>",
                expectedActionSet: Enumerable.Empty<string>());
        }

        [Fact]
        public async Task UpgradeProjectWithUnmanagedConstraintTo7_3_Delegate()
        {
            await TestLanguageVersionUpgradedAsync(
@"delegate void D<T>() where T : [|unmanaged|];",
                LanguageVersion.CSharp7_3,
                new CSharpParseOptions(LanguageVersion.CSharp7));
        }

        [Fact]
        public async Task UpgradeProjectWithUnmanagedConstraintTo7_3_Delegate_AlreadyDefined()
        {
            await TestExactActionSetOfferedAsync(
@"<Workspace>
    <Project Language=""C#"" LanguageVersion=""7"">
        <Document>
interface unmanaged { }
delegate void D&lt;T&gt;() where T : [| unmanaged |];
        </Document>
    </Project>
</Workspace>",
                expectedActionSet: Enumerable.Empty<string>());
        }

        [Fact]
        public async Task UpgradeProjectWithUnmanagedConstraintTo7_3_LocalFunction()
        {
            await TestLanguageVersionUpgradedAsync(
@"
class Test
{
    public void N()
    {
        void M<T>() where T : [|unmanaged|] { }
    }
}",
                LanguageVersion.CSharp7_3,
                new CSharpParseOptions(LanguageVersion.CSharp7));
        }

        [Fact]
        public async Task UpgradeProjectWithUnmanagedConstraintTo7_3_LocalFunction_AlreadyDefined()
        {
            await TestExactActionSetOfferedAsync(
@"<Workspace>
    <Project Language=""C#"" LanguageVersion=""7"">
        <Document>
interface unmanaged { }
class Test
{
    public void N()
    {
        void M&lt;T&gt;() where T : [|unmanaged|] { }
    }
}
        </Document>
    </Project>
</Workspace>",
                expectedActionSet: Enumerable.Empty<string>());
        }
    }
}
