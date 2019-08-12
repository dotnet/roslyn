// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Suppression
{
    public class CSharpSuppressionAllCodeTests : AbstractSuppressionAllCodeTests
    {
        protected override TestWorkspace CreateWorkspaceFromFile(string definition, ParseOptions parseOptions)
            => TestWorkspace.CreateCSharp(definition, (CSharpParseOptions)parseOptions);

        internal override Tuple<Analyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<Analyzer, IConfigurationFixProvider>(new Analyzer(), new CSharpSuppressionCodeFixProvider());
        }

        [WorkItem(956453, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/956453")]
        [WorkItem(1007071, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1007071")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
        public async Task TestPragmaWarningOnEveryNodes()
        {
            await TestPragmaAsync(TestResource.AllInOneCSharpCode, CSharpParseOptions.Default, verifier: t => t.IndexOf("#pragma warning disable", StringComparison.Ordinal) >= 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
        public async Task TestSuppressionWithAttributeOnEveryNodes()
        {
            await TestSuppressionWithAttributeAsync(
                TestResource.AllInOneCSharpCode,
                CSharpParseOptions.Default,
                digInto: n => !(n is StatementSyntax) || n is BlockSyntax,
                verifier: t => t.IndexOf("SuppressMessage", StringComparison.Ordinal) >= 0);
        }
    }
}
