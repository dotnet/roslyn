// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.Async;
using Microsoft.CodeAnalysis.CSharp.UpgradeProject;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Async
{
    public partial class UpgradeProjectTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpUpgradeProjectCodeFixProvider());

        private async Task TestAsync(
            string initialMarkup,
            LanguageVersion expected,
            int index,
            ParseOptions parseOptions)
        {
            var parameters = new TestParameters(parseOptions: parseOptions);
            using (var workspace = await CreateWorkspaceFromOptionsAsync(initialMarkup, parameters))
            {
                var actions = await GetCodeActionsAsync(workspace, parameters);
                var operations = await VerifyInputsAndGetOperationsAsync(index, actions, priority: null);

                var appliedChanges = ApplyOperationsAndGetSolution(workspace, operations);
                var oldSolution = appliedChanges.Item1;
                var newSolution = appliedChanges.Item2;
                Assert.Equal(expected, ((CSharpParseOptions)newSolution.Projects.First().ParseOptions).SpecifiedLanguageVersion);
            }

            await TestAsync(initialMarkup, initialMarkup, parseOptions); // no change to markup
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUpgradeProject)]
        public async Task UpgradeProjectToDefault()
        {
            await TestAsync(
@"
class Program
{
    void A()
    {
        var x = [|(1, 2)|];
    }
}",
LanguageVersion.Default,
0,
new CSharpParseOptions(LanguageVersion.CSharp6));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUpgradeProject)]
        public async Task UpgradeProjectToCSharp7()
        {
            await TestAsync(
@"
class Program
{
    void A()
    {
        var x = [|(1, 2)|];
    }
}",
LanguageVersion.CSharp7,
1,
new CSharpParseOptions(LanguageVersion.CSharp6));
        }
    }
}