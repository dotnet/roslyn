// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UpgradeProject
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsUpgradeProject)]
    public partial class UpgradeProjectTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
    {
        public UpgradeProjectTests(ITestOutputHelper logger)
           : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpUpgradeProjectCodeFixProvider());

        private async Task TestLanguageVersionUpgradedAsync(
            string initialMarkup,
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

            await TestAsync(initialMarkup, initialMarkup, parseOptions); // no change to markup
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
        public async Task UpgradeProjectFromCSharp7_2ToCSharp8()
        {
            await TestLanguageVersionUpgradedAsync(
                """
                class C
                {
                    object F = [|null!|];
                }
                """,
                LanguageVersion.CSharp8,
                new CSharpParseOptions(LanguageVersion.CSharp7_2));
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7ToCSharp8()
        {
            await TestLanguageVersionUpgradedAsync(
                """
                class C
                {
                    object F = [|null!|];
                }
                """,
                LanguageVersion.CSharp8,
                new CSharpParseOptions(LanguageVersion.CSharp7));
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp6ToCSharp7()
        {
            await TestLanguageVersionUpgradedAsync(
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
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp5ToCSharp6()
        {
            await TestLanguageVersionUpgradedAsync(
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
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp4ToCSharp5()
        {
            await TestLanguageVersionUpgradedAsync(
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
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7ToLatest()
        {
            await TestLanguageVersionUpgradedAsync(
                $$"""
                class Program
                {
                #error version:[|{{LanguageVersion.Latest.MapSpecifiedToEffectiveVersion().ToDisplayString()}}|]
                }
                """,
                LanguageVersion.Latest.MapSpecifiedToEffectiveVersion(),
                new CSharpParseOptions(LanguageVersion.CSharp7));
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7To7_1_TriggeredByInferredTupleNames()
        {
            await TestLanguageVersionUpgradedAsync(
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
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7_1ToLatest()
        {
            await TestLanguageVersionUpgradedAsync(
                $$"""
                class Program
                {
                #error version:[|{{LanguageVersion.Latest.MapSpecifiedToEffectiveVersion().ToDisplayString()}}|]
                }
                """,
                LanguageVersion.Latest.MapSpecifiedToEffectiveVersion(),
                new CSharpParseOptions(LanguageVersion.CSharp7_1));
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7ToCSharp7_1()
        {
            await TestLanguageVersionUpgradedAsync(
                """
                class Program
                {
                #error [|version:7.1|]
                }
                """,
                LanguageVersion.CSharp7_1,
                new CSharpParseOptions(LanguageVersion.CSharp7));
        }

        [Fact]
        public async Task UpgradeProjectWithNonTrailingNamedArgumentToCSharp7_2()
        {
            await TestLanguageVersionUpgradedAsync(
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
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7ToCSharp7_1_B()
        {
            await TestLanguageVersionUpgradedAsync(
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
        }

        #region C# 7.3
        [Fact]
        public async Task UpgradeProjectFromCSharp7_2ToLatest()
        {
            await TestLanguageVersionUpgradedAsync(
                $$"""
                class Program
                {
                #error version:[|{{LanguageVersion.Latest.MapSpecifiedToEffectiveVersion().ToDisplayString()}}|]
                }
                """,
                LanguageVersion.Latest.MapSpecifiedToEffectiveVersion(),
                new CSharpParseOptions(LanguageVersion.CSharp7_2));
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7_2To7_3_TriggeredByAttributeOnBackingField()
        {
            await TestLanguageVersionUpgradedAsync(
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
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7_2To7_3_EnumConstraint()
        {
            await TestLanguageVersionUpgradedAsync(
                """
                public class X<T> where T : [|System.Enum|]
                {
                }
                """,
                LanguageVersion.CSharp7_3,
                new CSharpParseOptions(LanguageVersion.CSharp7_2));
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7_2To7_3_DelegateConstraint()
        {
            await TestLanguageVersionUpgradedAsync(
                """
                public class X<T> where T : [|System.Delegate|]
                {
                }
                """,
                LanguageVersion.CSharp7_3,
                new CSharpParseOptions(LanguageVersion.CSharp7_2));
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7_2To7_3_MulticastDelegateConstraint()
        {
            await TestLanguageVersionUpgradedAsync(
                """
                public class X<T> where T : [|System.MulticastDelegate|]
                {
                }
                """,
                LanguageVersion.CSharp7_3,
                new CSharpParseOptions(LanguageVersion.CSharp7_2));
        }
        #endregion C# 7.3

        #region C# 8.0
        [Fact(Skip = "https://github.com/dotnet/roslyn/pull/29820")]
        public async Task UpgradeProjectFromCSharp7_3ToLatest()
        {
            await TestLanguageVersionUpgradedAsync(
                $$"""
                class Program
                {
                #error version:[|{{LanguageVersion.Latest.MapSpecifiedToEffectiveVersion().ToDisplayString()}}|]
                }
                """,
                LanguageVersion.Latest.MapSpecifiedToEffectiveVersion(),
                new CSharpParseOptions(LanguageVersion.CSharp7_3));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/pull/29820")]
        public async Task UpgradeProjectFromCSharp7_3To8_0()
        {
            await TestLanguageVersionUpgradedAsync(
                $$"""
                class Program
                {
                #error version:[|{{LanguageVersion.CSharp8.ToDisplayString()}}|]
                }
                """,
                LanguageVersion.Latest.MapSpecifiedToEffectiveVersion(),
                new CSharpParseOptions(LanguageVersion.CSharp7_3));
        }

        [Fact]
        public async Task UpgradeProjectForVerbatimInterpolatedString()
        {
            await TestLanguageVersionUpgradedAsync(
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
        }
        #endregion

        [Fact]
        public async Task UpgradeAllProjectsToCSharp7()
        {
            await TestLanguageVersionUpgradedAsync(
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
        }

        [Fact]
        public async Task UpgradeAllProjectsToCSharp8()
        {
            await TestLanguageVersionUpgradedAsync(
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
        }

        [Fact]
        public async Task UpgradeAllProjectsToCSharp8_NullableReferenceType()
        {
            await TestLanguageVersionUpgradedAsync(
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
        }

        [Fact]
        public async Task UpgradeAllProjectsToCSharp9()
        {
            await TestLanguageVersionUpgradedAsync(
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
        }

        [Fact]
        public async Task ListAllSuggestions()
        {
            await TestExactActionSetOfferedAsync(

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
        }

        [Fact]
        public async Task ListAllSuggestions_CSharp8()
        {
            await TestExactActionSetOfferedAsync(

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
        }

        [Fact]
        public async Task FixAllProjectsNotOffered()
        {
            await TestExactActionSetOfferedAsync(

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
        }

        [Fact]
        public async Task OnlyOfferFixAllProjectsFromCSharp6ToCSharp7WhenApplicable()
        {
            await TestExactActionSetOfferedAsync(

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
        }

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
        public async Task OnlyOfferFixAllProjectsToCSharp8WhenApplicable()
        {
            await TestExactActionSetOfferedAsync(
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
        }

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
        public async Task UpgradeProjectWithUnconstrainedNullableTypeParameter()
        {
            await TestLanguageVersionUpgradedAsync(
                """
                #nullable enable
                class C<T>
                {
                    static void F([|T?|] t) { }
                }
                """,
                LanguageVersion.CSharp9,
                new CSharpParseOptions(LanguageVersion.CSharp8));
        }

        [Fact]
        public async Task UpgradeProjectWithUnmanagedConstraintTo7_3_Type()
        {
            await TestLanguageVersionUpgradedAsync(
                """
                class Test<T> where T : [|unmanaged|]
                {
                }
                """,
                LanguageVersion.CSharp7_3,
                new CSharpParseOptions(LanguageVersion.CSharp7));
        }

        [Fact]
        public async Task UpgradeProjectWithUnmanagedConstraintTo7_3_Type_AlreadyDefined()
        {
            await TestExactActionSetOfferedAsync(
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
        }

        [Fact]
        public async Task UpgradeProjectWithUnmanagedConstraintTo7_3_Method()
        {
            await TestLanguageVersionUpgradedAsync(
                """
                class Test
                {
                    public void M<T>() where T : [|unmanaged|] { }
                }
                """,
                LanguageVersion.CSharp7_3,
                new CSharpParseOptions(LanguageVersion.CSharp7));
        }

        [Fact]
        public async Task UpgradeProjectWithUnmanagedConstraintTo7_3_Method_AlreadyDefined()
        {
            await TestExactActionSetOfferedAsync(
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
        }

        [Fact]
        public async Task UpgradeProjectWithUnmanagedConstraintTo7_3_LocalFunction()
        {
            await TestLanguageVersionUpgradedAsync(
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
        }

        [Fact]
        public async Task UpgradeProjectWithUnmanagedConstraintTo7_3_LocalFunction_AlreadyDefined()
        {
            await TestExactActionSetOfferedAsync(
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
        }

        [Fact]
        public async Task UpgradeProjectForDefaultInterfaceImplementation_CS8703()
        {
            await TestLanguageVersionUpgradedAsync(
                """
                public interface I1
                {
                    public void [|M01|]();
                }
                """,
                expected: LanguageVersion.CSharp8,
                new CSharpParseOptions(LanguageVersion.CSharp7_3));
        }

        [Fact]
        public async Task UpgradeProjectForDefaultInterfaceImplementation_CS8706()
        {
            await TestLanguageVersionNotUpgradedAsync(
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
        }

        [Fact]
        public async Task UpgradeProjectWithOpenTypeMatchingConstantPattern_01()
        {
            await TestLanguageVersionUpgradedAsync(
                """
                class Test
                {
                    bool M<T>(T t) => t is [|null|];
                }
                """,
                LanguageVersion.CSharp8,
                new CSharpParseOptions(LanguageVersion.CSharp7_3));
        }

        [Fact]
        public async Task UpgradeProjectWithOpenTypeMatchingConstantPattern_02()
        {
            await TestLanguageVersionUpgradedAsync(
                """
                class Test
                {
                    bool M<T>(T t) => t is [|100|];
                }
                """,
                LanguageVersion.CSharp8,
                new CSharpParseOptions(LanguageVersion.CSharp7_3));
        }

        [Fact]
        public async Task UpgradeProjectWithOpenTypeMatchingConstantPattern_03()
        {
            await TestLanguageVersionUpgradedAsync(
                """
                class Test
                {
                    bool M<T>(T t) => t is [|"frog"|];
                }
                """,
                LanguageVersion.CSharp8,
                new CSharpParseOptions(LanguageVersion.CSharp7_3));
        }

        [Fact]
        public async Task UpgradeProjectWithNotNullConstraintTo8_0_Type()
        {
            await TestLanguageVersionUpgradedAsync(
                """
                class Test<T> where T : [|notnull|]
                {
                }
                """,
                LanguageVersion.CSharp8,
                new CSharpParseOptions(LanguageVersion.CSharp7_3));
        }

        [Fact]
        public async Task UpgradeProjectWithNotNullConstraintTo8_0_Type_AlreadyDefined()
        {
            await TestExactActionSetOfferedAsync(
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
        }

        [Fact]
        public async Task UpgradeProjectWithNotNullConstraintTo8_0_Method()
        {
            await TestLanguageVersionUpgradedAsync(
                """
                class Test
                {
                    public void M<T>() where T : [|notnull|] { }
                }
                """,
                LanguageVersion.CSharp8,
                new CSharpParseOptions(LanguageVersion.CSharp7_3));
        }

        [Fact]
        public async Task UpgradeProjectWithNotNullConstraintTo8_0_Method_AlreadyDefined()
        {
            await TestExactActionSetOfferedAsync(
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
        }

        [Fact]
        public async Task UpgradeProjectWithNotNullConstraintTo8_0_Delegate()
        {
            await TestLanguageVersionUpgradedAsync(
@"delegate void D<T>() where T : [|notnull|];",
                LanguageVersion.CSharp8,
                new CSharpParseOptions(LanguageVersion.CSharp7_3));
        }

        [Fact]
        public async Task UpgradeProjectWithNotNullConstraintTo8_0_Delegate_AlreadyDefined()
        {
            await TestExactActionSetOfferedAsync(
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
        }

        [Fact]
        public async Task UpgradeProjectWithNotNullConstraintTo8_0_LocalFunction()
        {
            await TestLanguageVersionUpgradedAsync(
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
        }

        [Fact]
        public async Task UpgradeProjectWithNotNullConstraintTo8_0_LocalFunction_AlreadyDefined()
        {
            await TestExactActionSetOfferedAsync(
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
        }

        [Fact]
        public async Task UpgradeProjectForVarianceSafetyForStaticInterfaceMembers_CS8904()
        {
            await TestLanguageVersionUpgradedAsync(
                """
                interface I2<out T1>
                {
                    static T1 M1([|T1|] x) => x;
                }
                """,
                expected: LanguageVersion.CSharp9,
                new CSharpParseOptions(LanguageVersion.CSharp8));
        }

        [Fact]
        public async Task UpgradeProjectForSealedToStringInRecords_CS8912()
        {
            await TestLanguageVersionUpgradedAsync(
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
        }

        [Fact]
        public async Task UpgradeProjectForPrimaryConstructors_Class()
        {
            await TestLanguageVersionUpgradedAsync(
                """
                class Program[|()|];
                """,
                LanguageVersion.CSharp12,
                new CSharpParseOptions(LanguageVersion.CSharp11));
        }

        [Fact]
        public async Task UpgradeProjectForPrimaryConstructors_Struct()
        {
            await TestLanguageVersionUpgradedAsync(
                """
                struct Program[|()|];
                """,
                LanguageVersion.CSharp12,
                new CSharpParseOptions(LanguageVersion.CSharp11));
        }

        [Fact]
        public async Task UpgradeProjectForSemicolonBody_Class()
        {
            await TestLanguageVersionUpgradedAsync(
                """
                class Program[|;|]
                """,
                LanguageVersion.CSharp12,
                new CSharpParseOptions(LanguageVersion.CSharp11));
        }

        [Fact]
        public async Task UpgradeProjectForSemicolonBody_Struct()
        {
            await TestLanguageVersionUpgradedAsync(
                """
                struct Program[|;|]
                """,
                LanguageVersion.CSharp12,
                new CSharpParseOptions(LanguageVersion.CSharp11));
        }

        [Fact]
        public async Task UpgradeProjectForSemicolonBody_Interface()
        {
            await TestLanguageVersionUpgradedAsync(
                """
                interface Program[|;|]
                """,
                LanguageVersion.CSharp12,
                new CSharpParseOptions(LanguageVersion.CSharp11));
        }

        [Fact]
        public async Task UpgradeProjectForSemicolonBody_Enum()
        {
            await TestLanguageVersionUpgradedAsync(
                """
                enum Program[|;|]
                """,
                LanguageVersion.CSharp12,
                new CSharpParseOptions(LanguageVersion.CSharp11));
        }

        [Fact]
        public async Task UpgradeProjectForTargetTypedNew()
        {
            await TestLanguageVersionUpgradedAsync("""
                class Test
                {
                    Test t = [|new()|];
                }
                """,
                LanguageVersion.CSharp9,
                new CSharpParseOptions(LanguageVersion.CSharp8));
        }

        [Fact]
        public async Task UpgradeProjectForGlobalUsing()
        {
            await TestLanguageVersionUpgradedAsync("""
                [|global using System;|]
                """,
                LanguageVersion.CSharp10,
                new CSharpParseOptions(LanguageVersion.CSharp9));
        }

        [Fact]
        public async Task UpgradeProjectForImplicitImplementationOfNonPublicMembers_CS8704()
        {
            await TestLanguageVersionUpgradedAsync(
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
        }

        [Fact]
        public async Task UpgradeProjectForTargetTypedConditional()
        {
            await TestLanguageVersionUpgradedAsync("""
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57154")]
        public async Task UpgradeProjectForNewLinesInInterpolations()
        {
            await TestLanguageVersionUpgradedAsync("""
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60167")]
        public async Task UpgradeProjectForStructAutoDefaultError_1()
        {
            await TestLanguageVersionUpgradedAsync("""
                struct Test
                {
                    public int X;
                    public [|Test|]() { }
                }
                """,
                expected: LanguageVersion.CSharp11,
                new CSharpParseOptions(LanguageVersion.CSharp10));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60167")]
        public async Task UpgradeProjectForStructAutoDefaultError_2()
        {
            await TestLanguageVersionUpgradedAsync("""
                struct Test
                {
                    public int X;
                    public [|Test|]() { this.ToString(); }
                }
                """,
                expected: LanguageVersion.CSharp11,
                new CSharpParseOptions(LanguageVersion.CSharp10));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60167")]
        public async Task UpgradeProjectForStructAutoDefaultError_3()
        {
            await TestLanguageVersionUpgradedAsync("""
                struct Test
                {
                    public int X { get; set; }
                    public [|Test|]() { this.ToString(); }
                }
                """,
                expected: LanguageVersion.CSharp11,
                new CSharpParseOptions(LanguageVersion.CSharp10));
        }

        [Fact]
        public async Task UpgradeProjectForRefInMismatch()
        {
            await TestLanguageVersionUpgradedAsync("""
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
        }
    }
}
