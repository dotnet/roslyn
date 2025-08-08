// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UpgradeProject;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UpgradeProject;

[Trait(Traits.Feature, Traits.Features.CodeActionsUpgradeProject)]
public sealed partial class UpgradeProjectTests(ITestOutputHelper logger) : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new CSharpUpgradeProjectCodeFixProvider());

    private async Task TestLanguageVersionUpgradedAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup,
        LanguageVersion expected,
        ParseOptions? parseOptions,
        int index = 0)
    {
        var parameters = new TestParameters(parseOptions: parseOptions, index: index);
        using (var workspace = CreateWorkspaceFromOptions(initialMarkup, parameters))
        {
            var (_, action) = await GetCodeActionsAsync(workspace, parameters);
            var operations = await VerifyActionAndGetOperationsAsync(workspace, action);

            var appliedChanges = await ApplyOperationsAndGetSolutionAsync(workspace, operations);
            var oldSolution = appliedChanges.Item1;
            var newSolution = appliedChanges.Item2;
            Assert.All(newSolution.Projects.Where(p => p.Language == LanguageNames.CSharp),
                p => Assert.Equal(expected, ((CSharpParseOptions)p.ParseOptions!).SpecifiedLanguageVersion));

            // Verify no document changes when upgrade project
            var changedDocs = SolutionUtilities.GetTextChangedDocuments(oldSolution, newSolution);
            Assert.Empty(changedDocs);
        }

        await TestAsync(initialMarkup, initialMarkup, new(parseOptions)); // no change to markup
    }

    private async Task TestLanguageVersionNotUpgradedAsync(string initialMarkup, ParseOptions parseOptions, int index = 0)
    {
        var parameters = new TestParameters(parseOptions: parseOptions, index: index);
        using var workspace = CreateWorkspaceFromOptions(initialMarkup, parameters);
        var (actions, actionsToInvoke) = await GetCodeActionsAsync(workspace, parameters);

        Assert.Empty(actions);
        Assert.Null(actionsToInvoke);
    }

    [Fact]
    public Task UpgradeProjectFromCSharp7_2ToCSharp8()
        => TestLanguageVersionUpgradedAsync(
            """
            class C
            {
                object F = [|null!|];
            }
            """,
            LanguageVersion.CSharp8,
            new CSharpParseOptions(LanguageVersion.CSharp7_2));

    [Fact]
    public Task UpgradeProjectFromCSharp7ToCSharp8()
        => TestLanguageVersionUpgradedAsync(
            """
            class C
            {
                object F = [|null!|];
            }
            """,
            LanguageVersion.CSharp8,
            new CSharpParseOptions(LanguageVersion.CSharp7));

    [Fact]
    public Task UpgradeProjectFromCSharp6ToCSharp7()
        => TestLanguageVersionUpgradedAsync(
            """
            class Program
            {
                void A()
                {
                    var x = [|(1, 2)|];
                }
            }
            """,
            LanguageVersion.CSharp7,
            new CSharpParseOptions(LanguageVersion.CSharp6));

    [Fact]
    public Task UpgradeProjectFromCSharp5ToCSharp6()
        => TestLanguageVersionUpgradedAsync(
            """
            class Program
            {
                void A()
                {
                    var x = [|nameof(A)|];
                }
            }
            """,
            LanguageVersion.CSharp6,
            new CSharpParseOptions(LanguageVersion.CSharp5));

    [Fact]
    public Task UpgradeProjectFromCSharp4ToCSharp5()
        => TestLanguageVersionUpgradedAsync(
            """
            class Program
            {
                void A()
                {
                    Func<int, Task<int>> f = [|async|] x => x;
                }
            }
            """,
            LanguageVersion.CSharp5,
            new CSharpParseOptions(LanguageVersion.CSharp4));

    [Fact]
    public Task UpgradeProjectFromCSharp7ToLatest()
        => TestLanguageVersionUpgradedAsync(
            $$"""
            class Program
            {
            #error version:[|{{LanguageVersion.Latest.MapSpecifiedToEffectiveVersion().ToDisplayString()}}|]
            }
            """,
            LanguageVersion.Latest.MapSpecifiedToEffectiveVersion(),
            new CSharpParseOptions(LanguageVersion.CSharp7));

    [Fact]
    public Task UpgradeProjectFromCSharp7To7_1_TriggeredByInferredTupleNames()
        => TestLanguageVersionUpgradedAsync(
            """
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
            """,
            LanguageVersion.CSharp7_1,
            new CSharpParseOptions(LanguageVersion.CSharp7));

    [Fact]
    public Task UpgradeProjectFromCSharp7_1ToLatest()
        => TestLanguageVersionUpgradedAsync(
            $$"""
            class Program
            {
            #error version:[|{{LanguageVersion.Latest.MapSpecifiedToEffectiveVersion().ToDisplayString()}}|]
            }
            """,
            LanguageVersion.Latest.MapSpecifiedToEffectiveVersion(),
            new CSharpParseOptions(LanguageVersion.CSharp7_1));

    [Fact]
    public Task UpgradeProjectFromCSharp7ToCSharp7_1()
        => TestLanguageVersionUpgradedAsync(
            """
            class Program
            {
            #error [|version:7.1|]
            }
            """,
            LanguageVersion.CSharp7_1,
            new CSharpParseOptions(LanguageVersion.CSharp7));

    [Fact]
    public Task UpgradeProjectWithNonTrailingNamedArgumentToCSharp7_2()
        => TestLanguageVersionUpgradedAsync(
            """
            class Program
            {
                void M()
                {
                    [|M2(a: 1, 2);|]
                }
            }
            """,
            LanguageVersion.CSharp7_2,
            new CSharpParseOptions(LanguageVersion.CSharp7_1));

    [Fact]
    public Task UpgradeProjectFromCSharp7ToCSharp7_1_B()
        => TestLanguageVersionUpgradedAsync(
            """
            public class Base { }
            public class Derived : Base { }
            public class Program
            {
                public static void M<T>(T x) where T: Base
                {
                    System.Console.Write(x is [|Derived|] b0);
                }
            }
            """,
            LanguageVersion.CSharp7_1,
            new CSharpParseOptions(LanguageVersion.CSharp7));

    #region C# 7.3
    [Fact]
    public Task UpgradeProjectFromCSharp7_2ToLatest()
        => TestLanguageVersionUpgradedAsync(
            $$"""
            class Program
            {
            #error version:[|{{LanguageVersion.Latest.MapSpecifiedToEffectiveVersion().ToDisplayString()}}|]
            }
            """,
            LanguageVersion.Latest.MapSpecifiedToEffectiveVersion(),
            new CSharpParseOptions(LanguageVersion.CSharp7_2));

    [Fact]
    public Task UpgradeProjectFromCSharp7_2To7_3_TriggeredByAttributeOnBackingField()
        => TestLanguageVersionUpgradedAsync(
            """
            class A : System.Attribute { }
            class Program
            {
                [|[field: A]|]
                int P { get; set; }
            }
            """,
            LanguageVersion.CSharp7_3,
            new CSharpParseOptions(LanguageVersion.CSharp7_2));

    [Fact]
    public Task UpgradeProjectFromCSharp7_2To7_3_EnumConstraint()
        => TestLanguageVersionUpgradedAsync(
            """
            public class X<T> where T : [|System.Enum|]
            {
            }
            """,
            LanguageVersion.CSharp7_3,
            new CSharpParseOptions(LanguageVersion.CSharp7_2));

    [Fact]
    public Task UpgradeProjectFromCSharp7_2To7_3_DelegateConstraint()
        => TestLanguageVersionUpgradedAsync(
            """
            public class X<T> where T : [|System.Delegate|]
            {
            }
            """,
            LanguageVersion.CSharp7_3,
            new CSharpParseOptions(LanguageVersion.CSharp7_2));

    [Fact]
    public Task UpgradeProjectFromCSharp7_2To7_3_MulticastDelegateConstraint()
        => TestLanguageVersionUpgradedAsync(
            """
            public class X<T> where T : [|System.MulticastDelegate|]
            {
            }
            """,
            LanguageVersion.CSharp7_3,
            new CSharpParseOptions(LanguageVersion.CSharp7_2));
    #endregion C# 7.3

    #region C# 8.0
    [Fact(Skip = "https://github.com/dotnet/roslyn/pull/29820")]
    public Task UpgradeProjectFromCSharp7_3ToLatest()
        => TestLanguageVersionUpgradedAsync(
            $$"""
            class Program
            {
            #error version:[|{{LanguageVersion.Latest.MapSpecifiedToEffectiveVersion().ToDisplayString()}}|]
            }
            """,
            LanguageVersion.Latest.MapSpecifiedToEffectiveVersion(),
            new CSharpParseOptions(LanguageVersion.CSharp7_3));

    [Fact(Skip = "https://github.com/dotnet/roslyn/pull/29820")]
    public Task UpgradeProjectFromCSharp7_3To8_0()
        => TestLanguageVersionUpgradedAsync(
            $$"""
            class Program
            {
            #error version:[|{{LanguageVersion.CSharp8.ToDisplayString()}}|]
            }
            """,
            LanguageVersion.Latest.MapSpecifiedToEffectiveVersion(),
            new CSharpParseOptions(LanguageVersion.CSharp7_3));

    [Fact]
    public Task UpgradeProjectForVerbatimInterpolatedString()
        => TestLanguageVersionUpgradedAsync(
            """
            class Program
            {
                void A()
                {
                    var x = [|@$"hello"|];
                }
            }
            """,
            expected: LanguageVersion.CSharp8,
            new CSharpParseOptions(LanguageVersion.CSharp7_3));
    #endregion

    [Fact]
    public Task UpgradeAllProjectsToCSharp7()
        => TestLanguageVersionUpgradedAsync(
            """
            <Workspace>
                <Project Language="C#" LanguageVersion="6" CommonReferences="true">
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
                <Project Language="C#" LanguageVersion="6" CommonReferences="true">
                </Project>
                <Project Language="C#" LanguageVersion="7" CommonReferences="true">
                </Project>
                <Project Language="Visual Basic">
                </Project>
            </Workspace>
            """,
            LanguageVersion.CSharp7,
            parseOptions: null,
            index: 1);

    [Fact]
    public Task UpgradeAllProjectsToCSharp8()
        => TestLanguageVersionUpgradedAsync(
            """
            <Workspace>
                <Project Language="C#" LanguageVersion="6" CommonReferences="true">
                    <Document>
            class C
            {
                object F = [|null!|];
            }
                    </Document>
                </Project>
                <Project Language="C#" LanguageVersion="6" CommonReferences="true">
                </Project>
                <Project Language="C#" LanguageVersion="7" CommonReferences="true">
                </Project>
                <Project Language="Visual Basic" CommonReferences="true">
                </Project>
            </Workspace>
            """,
            LanguageVersion.CSharp8,
            parseOptions: null,
            index: 1);

    [Fact]
    public Task UpgradeAllProjectsToCSharp8_NullableReferenceType()
        => TestLanguageVersionUpgradedAsync(
            """
            <Workspace>
                <Project Language="C#" LanguageVersion="6" CommonReferences="True">
                    <Document>
            class C
            {
                void A(string[|?|] s)
                {
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" LanguageVersion="6">
                </Project>
                <Project Language="C#" LanguageVersion="7">
                </Project>
                <Project Language="C#" LanguageVersion="8">
                </Project>
                <Project Language="Visual Basic">
                </Project>
            </Workspace>
            """,
            LanguageVersion.CSharp8,
            parseOptions: null,
            index: 1);

    [Fact]
    public Task UpgradeAllProjectsToCSharp9()
        => TestLanguageVersionUpgradedAsync(
            """
            <Workspace>
                <Project Language="C#" LanguageVersion="6" CommonReferences="true">
                    <Document>
            [|System.Console.WriteLine();|]
                    </Document>
                </Project>
                <Project Language="C#" LanguageVersion="6" CommonReferences="true">
                </Project>
                <Project Language="C#" LanguageVersion="7" CommonReferences="true">
                </Project>
                <Project Language="C#" LanguageVersion="8" CommonReferences="true">
                </Project>
                <Project Language="Visual Basic" CommonReferences="true">
                </Project>
            </Workspace>
            """,
            LanguageVersion.CSharp9,
            parseOptions: null,
            index: 1);

    [Fact]
    public Task ListAllSuggestions()
        => TestExactActionSetOfferedAsync(

            """
            <Workspace>
                <Project Language="C#" LanguageVersion="6" CommonReferences="true">
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
                <Project Language="C#" LanguageVersion="7" CommonReferences="true">
                </Project>
                <Project Language="C#" LanguageVersion="6" CommonReferences="true">
                </Project>
            </Workspace>
            """,
            [
                string.Format(CSharpCodeFixesResources.Upgrade_this_project_to_csharp_language_version_0, "7.0"),
                string.Format(CSharpCodeFixesResources.Upgrade_all_csharp_projects_to_language_version_0, "7.0")
            ]);

    [Fact]
    public Task ListAllSuggestions_CSharp8()
        => TestExactActionSetOfferedAsync(

            """
            <Workspace>
                <Project Language="C#" LanguageVersion="6">
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
                <Project Language="C#" LanguageVersion="7">
                </Project>
                <Project Language="C#" LanguageVersion="8">
                </Project>
            </Workspace>
            """,
            [
                string.Format(CSharpCodeFixesResources.Upgrade_this_project_to_csharp_language_version_0, "8.0"),
                string.Format(CSharpCodeFixesResources.Upgrade_all_csharp_projects_to_language_version_0, "8.0")
            ]
);

    [Fact]
    public Task FixAllProjectsNotOffered()
        => TestExactActionSetOfferedAsync(

            """
            <Workspace>
                <Project Language="C#" LanguageVersion="6" CommonReferences="true">
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
                <Project Language="Visual Basic" CommonReferences="true">
                </Project>
            </Workspace>
            """,
            [
                string.Format(CSharpCodeFixesResources.Upgrade_this_project_to_csharp_language_version_0, "7.0")
                ]);

    [Fact]
    public Task OnlyOfferFixAllProjectsFromCSharp6ToCSharp7WhenApplicable()
        => TestExactActionSetOfferedAsync(

            """
            <Workspace>
                <Project Language="C#" LanguageVersion="6" CommonReferences="true">
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
                <Project Language="C#" LanguageVersion="7" CommonReferences="true">
                </Project>
                <Project Language="Visual Basic" CommonReferences="true">
                </Project>
            </Workspace>
            """,
            [
                string.Format(CSharpCodeFixesResources.Upgrade_this_project_to_csharp_language_version_0, "7.0")
                ]);

    [Fact]
    public async Task OnlyOfferFixAllProjectsFromCSharp6ToDefaultWhenApplicable()
    {
        var defaultVersion = LanguageVersion.Default.MapSpecifiedToEffectiveVersion().ToDisplayString();
        await TestExactActionSetOfferedAsync(

            $$"""
            <Workspace>
                <Project Language="C#" LanguageVersion="6">
                    <Document>
            class C
            {
                void A()
                {
            #error version:[|{{defaultVersion}}|]
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" LanguageVersion="Default">
                </Project>
                <Project Language="Visual Basic">
                </Project>
            </Workspace>
            """,
            [
                string.Format(CSharpCodeFixesResources.Upgrade_this_project_to_csharp_language_version_0, defaultVersion),
                string.Format(CSharpCodeFixesResources.Upgrade_all_csharp_projects_to_language_version_0, defaultVersion)
            ]);
    }

    [Fact]
    public Task OnlyOfferFixAllProjectsToCSharp8WhenApplicable()
        => TestExactActionSetOfferedAsync(
            """
            <Workspace>
                <Project Language="C#" LanguageVersion="6" CommonReferences="true">
                    <Document>
            class C
            {
                object F = [|null!|];
            }
                    </Document>
                </Project>
                <Project Language="C#" LanguageVersion="8" CommonReferences="true">
                </Project>
                <Project Language="Visual Basic" CommonReferences="true">
                </Project>
            </Workspace>
            """,
            [string.Format(CSharpCodeFixesResources.Upgrade_this_project_to_csharp_language_version_0, "8.0")]);

    [Fact]
    public async Task OnlyOfferFixAllProjectsToDefaultWhenApplicable()
    {
        var defaultEffectiveVersion = LanguageVersion.Default.MapSpecifiedToEffectiveVersion().ToDisplayString();
        await TestExactActionSetOfferedAsync(
            $$"""
            <Workspace>
                <Project Language="C#" LanguageVersion="6">
                    <Document>
            class C
            {
                void A()
                {
            #error version:[|{{defaultEffectiveVersion}}|]
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" LanguageVersion="Default">
                </Project>
                <Project Language="Visual Basic">
                </Project>
            </Workspace>
            """,
            [
                string.Format(CSharpCodeFixesResources.Upgrade_this_project_to_csharp_language_version_0, defaultEffectiveVersion),
                string.Format(CSharpCodeFixesResources.Upgrade_all_csharp_projects_to_language_version_0, defaultEffectiveVersion)
            ]);
    }

    [Fact]
    public Task UpgradeProjectWithUnconstrainedNullableTypeParameter()
        => TestLanguageVersionUpgradedAsync(
            """
            #nullable enable
            class C<T>
            {
                static void F([|T?|] t) { }
            }
            """,
            LanguageVersion.CSharp9,
            new CSharpParseOptions(LanguageVersion.CSharp8));

    [Fact]
    public Task UpgradeProjectWithUnmanagedConstraintTo7_3_Type()
        => TestLanguageVersionUpgradedAsync(
            """
            class Test<T> where T : [|unmanaged|]
            {
            }
            """,
            LanguageVersion.CSharp7_3,
            new CSharpParseOptions(LanguageVersion.CSharp7));

    [Fact]
    public Task UpgradeProjectWithUnmanagedConstraintTo7_3_Type_AlreadyDefined()
        => TestExactActionSetOfferedAsync(
            """
            <Workspace>
                <Project Language="C#" LanguageVersion="7">
                    <Document>
            interface unmanaged { }
            class Test&lt;T&gt; where T : [|unmanaged|]
            {
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            expectedActionSet: []);

    [Fact]
    public Task UpgradeProjectWithUnmanagedConstraintTo7_3_Method()
        => TestLanguageVersionUpgradedAsync(
            """
            class Test
            {
                public void M<T>() where T : [|unmanaged|] { }
            }
            """,
            LanguageVersion.CSharp7_3,
            new CSharpParseOptions(LanguageVersion.CSharp7));

    [Fact]
    public Task UpgradeProjectWithUnmanagedConstraintTo7_3_Method_AlreadyDefined()
        => TestExactActionSetOfferedAsync(
            """
            <Workspace>
                <Project Language="C#" LanguageVersion="7">
                    <Document>
            interface unmanaged { }
            class Test
            {
                public void M&lt;T&gt;() where T : [|unmanaged|] { }
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            expectedActionSet: []);

    [Fact]
    public Task UpgradeProjectWithUnmanagedConstraintTo7_3_Delegate()
        => TestLanguageVersionUpgradedAsync(
            @"delegate void D<T>() where T : [|unmanaged|];",
            LanguageVersion.CSharp7_3,
            new CSharpParseOptions(LanguageVersion.CSharp7));

    [Fact]
    public Task UpgradeProjectWithUnmanagedConstraintTo7_3_Delegate_AlreadyDefined()
        => TestExactActionSetOfferedAsync(
            """
            <Workspace>
                <Project Language="C#" LanguageVersion="7">
                    <Document>
            interface unmanaged { }
            delegate void D&lt;T&gt;() where T : [| unmanaged |];
                    </Document>
                </Project>
            </Workspace>
            """,
            expectedActionSet: []);

    [Fact]
    public Task UpgradeProjectWithUnmanagedConstraintTo7_3_LocalFunction()
        => TestLanguageVersionUpgradedAsync(
            """
            class Test
            {
                public void N()
                {
                    void M<T>() where T : [|unmanaged|] { }
                }
            }
            """,
            LanguageVersion.CSharp7_3,
            new CSharpParseOptions(LanguageVersion.CSharp7));

    [Fact]
    public Task UpgradeProjectWithUnmanagedConstraintTo7_3_LocalFunction_AlreadyDefined()
        => TestExactActionSetOfferedAsync(
            """
            <Workspace>
                <Project Language="C#" LanguageVersion="7">
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
            </Workspace>
            """,
            expectedActionSet: []);

    [Fact]
    public Task UpgradeProjectForDefaultInterfaceImplementation_CS8703()
        => TestLanguageVersionUpgradedAsync(
            """
            public interface I1
            {
                public void [|M01|]();
            }
            """,
            expected: LanguageVersion.CSharp8,
            new CSharpParseOptions(LanguageVersion.CSharp7_3));

    [Fact]
    public Task UpgradeProjectForDefaultInterfaceImplementation_CS8706()
        => TestLanguageVersionNotUpgradedAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <ProjectReference>Assembly2</ProjectReference>
                    <Document FilePath="Test1.cs">
            class Test1 : [|I1|]
            {}
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true" LanguageVersion="Preview">
                    <Document FilePath="Test2.cs">
            public interface I1
            {
                void M1() 
                {
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            new CSharpParseOptions(LanguageVersion.CSharp7_3));

    [Fact]
    public Task UpgradeProjectWithOpenTypeMatchingConstantPattern_01()
        => TestLanguageVersionUpgradedAsync(
            """
            class Test
            {
                bool M<T>(T t) => t is [|null|];
            }
            """,
            LanguageVersion.CSharp8,
            new CSharpParseOptions(LanguageVersion.CSharp7_3));

    [Fact]
    public Task UpgradeProjectWithOpenTypeMatchingConstantPattern_02()
        => TestLanguageVersionUpgradedAsync(
            """
            class Test
            {
                bool M<T>(T t) => t is [|100|];
            }
            """,
            LanguageVersion.CSharp8,
            new CSharpParseOptions(LanguageVersion.CSharp7_3));

    [Fact]
    public Task UpgradeProjectWithOpenTypeMatchingConstantPattern_03()
        => TestLanguageVersionUpgradedAsync(
            """
            class Test
            {
                bool M<T>(T t) => t is [|"frog"|];
            }
            """,
            LanguageVersion.CSharp8,
            new CSharpParseOptions(LanguageVersion.CSharp7_3));

    [Fact]
    public Task UpgradeProjectWithNotNullConstraintTo8_0_Type()
        => TestLanguageVersionUpgradedAsync(
            """
            class Test<T> where T : [|notnull|]
            {
            }
            """,
            LanguageVersion.CSharp8,
            new CSharpParseOptions(LanguageVersion.CSharp7_3));

    [Fact]
    public Task UpgradeProjectWithNotNullConstraintTo8_0_Type_AlreadyDefined()
        => TestExactActionSetOfferedAsync(
            """
            <Workspace>
                <Project Language="C#" LanguageVersion="7.3">
                    <Document>
            interface notnull { }
            class Test&lt;T&gt; where T : [|notnull|]
            {
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            expectedActionSet: []);

    [Fact]
    public Task UpgradeProjectWithNotNullConstraintTo8_0_Method()
        => TestLanguageVersionUpgradedAsync(
            """
            class Test
            {
                public void M<T>() where T : [|notnull|] { }
            }
            """,
            LanguageVersion.CSharp8,
            new CSharpParseOptions(LanguageVersion.CSharp7_3));

    [Fact]
    public Task UpgradeProjectWithNotNullConstraintTo8_0_Method_AlreadyDefined()
        => TestExactActionSetOfferedAsync(
            """
            <Workspace>
                <Project Language="C#" LanguageVersion="7.3">
                    <Document>
            interface notnull { }
            class Test
            {
                public void M&lt;T&gt;() where T : [|notnull|] { }
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            expectedActionSet: []);

    [Fact]
    public Task UpgradeProjectWithNotNullConstraintTo8_0_Delegate()
        => TestLanguageVersionUpgradedAsync(
@"delegate void D<T>() where T : [|notnull|];",
            LanguageVersion.CSharp8,
            new CSharpParseOptions(LanguageVersion.CSharp7_3));

    [Fact]
    public Task UpgradeProjectWithNotNullConstraintTo8_0_Delegate_AlreadyDefined()
        => TestExactActionSetOfferedAsync(
            """
            <Workspace>
                <Project Language="C#" LanguageVersion="7.3">
                    <Document>
            interface notnull { }
            delegate void D&lt;T&gt;() where T : [| notnull |];
                    </Document>
                </Project>
            </Workspace>
            """,
            expectedActionSet: []);

    [Fact]
    public Task UpgradeProjectWithNotNullConstraintTo8_0_LocalFunction()
        => TestLanguageVersionUpgradedAsync(
            """
            class Test
            {
                public void N()
                {
                    void M<T>() where T : [|notnull|] { }
                }
            }
            """,
            LanguageVersion.CSharp8,
            new CSharpParseOptions(LanguageVersion.CSharp7_3));

    [Fact]
    public Task UpgradeProjectWithNotNullConstraintTo8_0_LocalFunction_AlreadyDefined()
        => TestExactActionSetOfferedAsync(
            """
            <Workspace>
                <Project Language="C#" LanguageVersion="7.3">
                    <Document>
            interface notnull { }
            class Test
            {
                public void N()
                {
                    void M&lt;T&gt;() where T : [|notnull|] { }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            expectedActionSet: []);

    [Fact]
    public Task UpgradeProjectForVarianceSafetyForStaticInterfaceMembers_CS8904()
        => TestLanguageVersionUpgradedAsync(
            """
            interface I2<out T1>
            {
                static T1 M1([|T1|] x) => x;
            }
            """,
            expected: LanguageVersion.CSharp9,
            new CSharpParseOptions(LanguageVersion.CSharp8));

    [Fact]
    public Task UpgradeProjectForSealedToStringInRecords_CS8912()
        => TestLanguageVersionUpgradedAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" LanguageVersion="9">
                    <ProjectReference>Assembly2</ProjectReference>
                    <Document FilePath="Derived.cs">
            record [|Derived|] : Base;
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true" LanguageVersion="10">
                    <Document FilePath="Base.cs">
            public record Base
            {
                public sealed override string ToString() => throw null;
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            expected: LanguageVersion.CSharp10,
            new CSharpParseOptions(LanguageVersion.CSharp9));

    [Fact]
    public Task UpgradeProjectForPrimaryConstructors_Class()
        => TestLanguageVersionUpgradedAsync(
            """
            class Program[|()|];
            """,
            LanguageVersion.CSharp12,
            new CSharpParseOptions(LanguageVersion.CSharp11));

    [Fact]
    public Task UpgradeProjectForPrimaryConstructors_Struct()
        => TestLanguageVersionUpgradedAsync(
            """
            struct Program[|()|];
            """,
            LanguageVersion.CSharp12,
            new CSharpParseOptions(LanguageVersion.CSharp11));

    [Fact]
    public Task UpgradeProjectForSemicolonBody_Class()
        => TestLanguageVersionUpgradedAsync(
            """
            class Program[|;|]
            """,
            LanguageVersion.CSharp12,
            new CSharpParseOptions(LanguageVersion.CSharp11));

    [Fact]
    public Task UpgradeProjectForSemicolonBody_Struct()
        => TestLanguageVersionUpgradedAsync(
            """
            struct Program[|;|]
            """,
            LanguageVersion.CSharp12,
            new CSharpParseOptions(LanguageVersion.CSharp11));

    [Fact]
    public Task UpgradeProjectForSemicolonBody_Interface()
        => TestLanguageVersionUpgradedAsync(
            """
            interface Program[|;|]
            """,
            LanguageVersion.CSharp12,
            new CSharpParseOptions(LanguageVersion.CSharp11));

    [Fact]
    public Task UpgradeProjectForSemicolonBody_Enum()
        => TestLanguageVersionUpgradedAsync(
            """
            enum Program[|;|]
            """,
            LanguageVersion.CSharp12,
            new CSharpParseOptions(LanguageVersion.CSharp11));

    [Fact]
    public Task UpgradeProjectForTargetTypedNew()
        => TestLanguageVersionUpgradedAsync("""
            class Test
            {
                Test t = [|new()|];
            }
            """,
            LanguageVersion.CSharp9,
            new CSharpParseOptions(LanguageVersion.CSharp8));

    [Fact]
    public Task UpgradeProjectForGlobalUsing()
        => TestLanguageVersionUpgradedAsync("""
            [|global using System;|]
            """,
            LanguageVersion.CSharp10,
            new CSharpParseOptions(LanguageVersion.CSharp9));

    [Fact]
    public Task UpgradeProjectForImplicitImplementationOfNonPublicMembers_CS8704()
        => TestLanguageVersionUpgradedAsync(
            """
            public interface I1
            {
                protected void M01();
            }

            class C1 : I1
            {
                public void [|M01|]() {}
            }
            """,
            expected: LanguageVersion.CSharp10,
            new CSharpParseOptions(LanguageVersion.CSharp9));

    [Fact]
    public Task UpgradeProjectForTargetTypedConditional()
        => TestLanguageVersionUpgradedAsync("""
            class C
            {
                void M(bool b)
                {
                    int? i = [|b ? 1 : null|];
                }
            }
            """,
            expected: LanguageVersion.CSharp9,
            new CSharpParseOptions(LanguageVersion.CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57154")]
    public Task UpgradeProjectForNewLinesInInterpolations()
        => TestLanguageVersionUpgradedAsync("""
            class Test
            {
                void M()
                {
                    var v = $"x{
                                1 + 1
                             [|}|]y";
                }
            }
            """,
            expected: LanguageVersion.CSharp11,
            new CSharpParseOptions(LanguageVersion.CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60167")]
    public Task UpgradeProjectForStructAutoDefaultError_1()
        => TestLanguageVersionUpgradedAsync("""
            struct Test
            {
                public int X;
                public [|Test|]() { }
            }
            """,
            expected: LanguageVersion.CSharp11,
            new CSharpParseOptions(LanguageVersion.CSharp10));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60167")]
    public Task UpgradeProjectForStructAutoDefaultError_2()
        => TestLanguageVersionUpgradedAsync("""
            struct Test
            {
                public int X;
                public [|Test|]() { this.ToString(); }
            }
            """,
            expected: LanguageVersion.CSharp11,
            new CSharpParseOptions(LanguageVersion.CSharp10));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60167")]
    public Task UpgradeProjectForStructAutoDefaultError_3()
        => TestLanguageVersionUpgradedAsync("""
            struct Test
            {
                public int X { get; set; }
                public [|Test|]() { this.ToString(); }
            }
            """,
            expected: LanguageVersion.CSharp11,
            new CSharpParseOptions(LanguageVersion.CSharp10));

    [Fact]
    public Task UpgradeProjectForRefInMismatch()
        => TestLanguageVersionUpgradedAsync("""
            class C
            {
                void M1(in int x) { }
                void M2(ref int y)
                {
                    M1(ref [|y|]);
                }
            }
            """,
            expected: LanguageVersion.CSharp12,
            new CSharpParseOptions(LanguageVersion.CSharp11));

    [Fact]
    public Task UpgradeProjectForUserDefinedCompoundAssignment()
        => TestLanguageVersionUpgradedAsync("""
            class C
            {
                public C operator [|+=|](C c) => c;
            }
            """,
            expected: LanguageVersion.CSharp14,
            new CSharpParseOptions(LanguageVersion.CSharp13));
}
