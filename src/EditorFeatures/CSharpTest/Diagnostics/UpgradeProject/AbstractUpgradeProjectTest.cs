// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.UnitTests;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.UpgradeProject
{
    public abstract class AbstractUpgradeProjectTest : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        protected async Task TestLanguageVersionUpgradedAsync(
            string initialMarkup,
            LanguageVersion expected,
            ParseOptions parseOptions,
            CompilationOptions compilationOptions = null,
            int index = 0)
        {
            var parameters = new TestParameters(parseOptions, compilationOptions, index: index);
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

            await TestAsync(initialMarkup, initialMarkup, parseOptions, compilationOptions); // no change to markup
        }
    }
}
