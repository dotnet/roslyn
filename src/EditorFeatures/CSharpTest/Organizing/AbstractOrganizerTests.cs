// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Organizing;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Organizing;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Organizing
{
    public abstract class AbstractOrganizerTests
    {
        protected async Task CheckAsync(string initial, string final)
        {
            await CheckResultAsync(initial, final);
            await CheckResultAsync(initial, final, Options.Script);
        }

        protected async Task CheckAsync(string initial, string final, bool specialCaseSystem)
        {
            await CheckResultAsync(initial, final, specialCaseSystem);
            await CheckResultAsync(initial, final, specialCaseSystem, Options.Script);
        }

        protected async Task CheckResultAsync(string initial, string final, bool specialCaseSystem, CSharpParseOptions options = null)
        {
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromFileAsync(initial))
            {
                var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);
                var newRoot = await (await OrganizingService.OrganizeAsync(document)).GetSyntaxRootAsync();
                Assert.Equal(final.NormalizeLineEndings(), newRoot.ToFullString());
            }
        }

        protected Task CheckResultAsync(string initial, string final, CSharpParseOptions options = null)
        {
            return CheckResultAsync(initial, final, false, options);
        }
    }
}
