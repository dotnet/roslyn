// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
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
        protected void Check(string initial, string final)
        {
            CheckResult(initial, final);
            CheckResult(initial, final, Options.Script);
        }

        protected void Check(string initial, string final, bool specialCaseSystem)
        {
            CheckResult(initial, final, specialCaseSystem);
            CheckResult(initial, final, specialCaseSystem, Options.Script);
        }

        protected void CheckResult(string initial, string final, bool specialCaseSystem, CSharpParseOptions options = null)
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(initial))
            {
                var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);
                var newRoot = OrganizingService.OrganizeAsync(document).Result.GetSyntaxRootAsync().Result;
                Assert.Equal(final.NormalizeLineEndings(), newRoot.ToFullString());
            }
        }

        protected void CheckResult(string initial, string final, CSharpParseOptions options = null)
        {
            CheckResult(initial, final, false, options);
        }
    }
}
