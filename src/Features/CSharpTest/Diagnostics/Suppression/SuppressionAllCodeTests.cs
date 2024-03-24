// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Suppression
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
    public class CSharpSuppressionAllCodeTests : AbstractSuppressionAllCodeTests
    {
        private static readonly TestComposition s_compositionWithMockDiagnosticUpdateSourceRegistrationService = FeaturesTestCompositions.Features
            .AddAssemblies(typeof(DiagnosticAnalyzerService).Assembly);

        protected override TestWorkspace CreateWorkspaceFromFile(string definition, ParseOptions parseOptions)
            => TestWorkspace.CreateCSharp(definition, (CSharpParseOptions)parseOptions, composition: s_compositionWithMockDiagnosticUpdateSourceRegistrationService);

        internal override Tuple<Analyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
            => new Tuple<Analyzer, IConfigurationFixProvider>(new Analyzer(), new CSharpSuppressionCodeFixProvider());

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1007071")]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/956453")]
        public async Task TestPragmaWarningOnEveryNodes()
            => await TestPragmaAsync(TestResource.AllInOneCSharpCode, CSharpParseOptions.Default, verifier: t => t.IndexOf("#pragma warning disable", StringComparison.Ordinal) >= 0);

        [Fact]
        public async Task TestSuppressionWithAttributeOnEveryNodes()
        {
            await TestSuppressionWithAttributeAsync(
                TestResource.AllInOneCSharpCode,
                CSharpParseOptions.Default,
                digInto: n => n is not StatementSyntax or BlockSyntax,
                verifier: t => t.IndexOf("SuppressMessage", StringComparison.Ordinal) >= 0);
        }
    }
}
