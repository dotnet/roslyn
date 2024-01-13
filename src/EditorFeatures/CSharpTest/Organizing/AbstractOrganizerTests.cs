// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Organizing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Organizing
{
    [UseExportProvider]
    public abstract class AbstractOrganizerTests
    {
        protected static async Task CheckAsync(string initial, string final)
        {
            await CheckAsync(initial, final, options: null);
            await CheckAsync(initial, final, Options.Script);
        }

        protected static async Task CheckAsync(string initial, string final, CSharpParseOptions options)
        {
            using var workspace = EditorTestWorkspace.CreateCSharp(initial, options);
            var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);
            var newRoot = await (await OrganizingService.OrganizeAsync(document)).GetSyntaxRootAsync();
            Assert.Equal(final.NormalizeLineEndings(), newRoot.ToFullString());
        }
    }
}
