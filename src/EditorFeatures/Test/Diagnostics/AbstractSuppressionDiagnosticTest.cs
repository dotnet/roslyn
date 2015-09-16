// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    public abstract class AbstractSuppressionDiagnosticTest : AbstractUserDiagnosticTest
    {
        protected abstract int CodeActionIndex { get; }

        protected void Test(string initial, string expected)
        {
            Test(initial, expected, parseOptions: null, index: CodeActionIndex, compareTokens: false);
        }

        protected void TestMissing(string initial)
        {
            TestMissing(initial, parseOptions: null);
        }

        internal abstract Tuple<DiagnosticAnalyzer, ISuppressionFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace);

        internal override IEnumerable<Diagnostic> GetDiagnostics(TestWorkspace workspace)
        {
            var providerAndFixer = CreateDiagnosticProviderAndFixer(workspace);

            var provider = providerAndFixer.Item1;
            TextSpan span;
            var document = GetDocumentAndSelectSpan(workspace, out span);
            return DiagnosticProviderTestUtilities.GetAllDiagnostics(provider, document, span);
        }

        internal override IEnumerable<Tuple<Diagnostic, CodeFixCollection>> GetDiagnosticAndFixes(TestWorkspace workspace, string fixAllActionId)
        {
            var providerAndFixer = CreateDiagnosticProviderAndFixer(workspace);

            var provider = providerAndFixer.Item1;
            TextSpan span;
            var document = GetDocumentAndSelectSpan(workspace, out span);
            var diagnostics = DiagnosticProviderTestUtilities.GetAllDiagnostics(provider, document, span);

            var fixer = providerAndFixer.Item2;
            foreach (var diagnostic in diagnostics)
            {
                if (fixer.CanBeSuppressed(diagnostic))
                {
                    var fixes = fixer.GetSuppressionsAsync(document, diagnostic.Location.SourceSpan, SpecializedCollections.SingletonEnumerable(diagnostic), CancellationToken.None).Result;
                    if (fixes != null && fixes.Any())
                    {
                        yield return Tuple.Create(diagnostic,
                            new CodeFixCollection(fixer, diagnostic.Location.SourceSpan, fixes));
                    }
                }
            }
        }
    }
}
