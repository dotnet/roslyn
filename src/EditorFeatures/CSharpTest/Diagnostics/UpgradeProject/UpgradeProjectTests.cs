// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UpgradeProject;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Async
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
            var parameters = new TestParameters(parseOptions: parseOptions);
            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, parameters))
            {
                var actions = await GetCodeActionsAsync(workspace, parameters);
                var operations = await VerifyInputsAndGetOperationsAsync(index, actions, priority: null);

                var appliedChanges = ApplyOperationsAndGetSolution(workspace, operations);
                var oldSolution = appliedChanges.Item1;
                var newSolution = appliedChanges.Item2;
                Assert.True(newSolution.Projects.Where(p => p.Language == LanguageNames.CSharp)
                    .All(p => ((CSharpParseOptions)p.ParseOptions).SpecifiedLanguageVersion == expected));
            }

            await TestAsync(initialMarkup, initialMarkup, parseOptions); // no change to markup
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp6ToDefault()
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
                LanguageVersion.Default,
                new CSharpParseOptions(LanguageVersion.CSharp6));
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
                new CSharpParseOptions(LanguageVersion.CSharp6),
                index: 1);
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
                new CSharpParseOptions(LanguageVersion.CSharp5),
                index: 1);
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
                new CSharpParseOptions(LanguageVersion.CSharp4),
                index: 1);
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7ToLatest()
        {
            await TestLanguageVersionUpgradedAsync(
@"
class Program
{
#error version:[|7.1|]
}",
                LanguageVersion.Latest,
                new CSharpParseOptions(LanguageVersion.CSharp7));
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7ToLatest_TriggeredByInferredTupleNames()
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
                LanguageVersion.Latest,
                new CSharpParseOptions(LanguageVersion.CSharp7));
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7_1ToLatest()
        {
            await TestLanguageVersionUpgradedAsync(
@"
class Program
{
#error version:[|7.1|]
}",
                LanguageVersion.Latest,
                new CSharpParseOptions(LanguageVersion.CSharp7_1));
        }

        [Fact]
        public async Task UpgradeProjectFromCSharp7_2ToLatest()
        {
            await TestLanguageVersionUpgradedAsync(
@"
class Program
{
#error version:[|7.2|]
}",
                LanguageVersion.Latest,
                new CSharpParseOptions(LanguageVersion.CSharp7_2));
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
                new CSharpParseOptions(LanguageVersion.CSharp7),
                index: 1);
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
                new CSharpParseOptions(LanguageVersion.CSharp7_1),
                index: 1);
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
                new CSharpParseOptions(LanguageVersion.CSharp7),
                index: 1);
        }

        [Fact]
        public async Task UpgradeAllProjectsToDefault()
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
    <Project Language=""C#"" LanguageVersion=""Default"">
    </Project>
    <Project Language=""C#"" LanguageVersion=""7"">
    </Project>
    <Project Language=""Visual Basic"">
    </Project>
</Workspace>",
                LanguageVersion.Default,
                parseOptions: null,
                index: 2);

        }

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
    <Project Language=""C#"" LanguageVersion=""Default"">
    </Project>
    <Project Language=""C#"" LanguageVersion=""7"">
    </Project>
    <Project Language=""Visual Basic"">
    </Project>
</Workspace>",
                LanguageVersion.Default,
                parseOptions: null,
                index: 2);
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
    <Project Language=""C#"" LanguageVersion=""Default"">
    </Project>
</Workspace>",
                new[] {
                    string.Format(CSharpFeaturesResources.Upgrade_this_project_to_csharp_language_version_0, "default"),
                    string.Format(CSharpFeaturesResources.Upgrade_this_project_to_csharp_language_version_0, "7.0"),
                    string.Format(CSharpFeaturesResources.Upgrade_all_csharp_projects_to_language_version_0, "default"),
                    string.Format(CSharpFeaturesResources.Upgrade_all_csharp_projects_to_language_version_0, "7.0")
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
                    string.Format(CSharpFeaturesResources.Upgrade_this_project_to_csharp_language_version_0, "default"),
                    string.Format(CSharpFeaturesResources.Upgrade_this_project_to_csharp_language_version_0, "7.0")
                    });
        }

        [Fact]
        public async Task OnlyOfferFixAllProjectsToCSharp7WhenApplicable()
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
                    string.Format(CSharpFeaturesResources.Upgrade_this_project_to_csharp_language_version_0, "default"),
                    string.Format(CSharpFeaturesResources.Upgrade_this_project_to_csharp_language_version_0, "7.0"),
                    string.Format(CSharpFeaturesResources.Upgrade_all_csharp_projects_to_language_version_0, "default")
                    });
        }

        [Fact]
        public async Task OnlyOfferFixAllProjectsToDefaultWhenApplicable()
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
    <Project Language=""C#"" LanguageVersion=""Default"">
    </Project>
    <Project Language=""Visual Basic"">
    </Project>
</Workspace>",
                new[] {
                    string.Format(CSharpFeaturesResources.Upgrade_this_project_to_csharp_language_version_0, "default"),
                    string.Format(CSharpFeaturesResources.Upgrade_this_project_to_csharp_language_version_0, "7.0"),
                    string.Format(CSharpFeaturesResources.Upgrade_all_csharp_projects_to_language_version_0, "7.0")
                    });
        }
    }
}
