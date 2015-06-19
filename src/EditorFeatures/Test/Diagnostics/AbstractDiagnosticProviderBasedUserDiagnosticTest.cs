// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests.Diagnostics;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    public abstract class AbstractDiagnosticProviderBasedUserDiagnosticTest : AbstractUserDiagnosticTest
    {
        internal abstract Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace);

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
            Document document;
            TextSpan span;
            string annotation = null;
            if (!TryGetDocumentAndSelectSpan(workspace, out document, out span))
            {
                document = GetDocumentAndAnnotatedSpan(workspace, out annotation, out span);
            }

            var diagnostics = DiagnosticProviderTestUtilities.GetAllDiagnostics(provider, document, span);

            var fixer = providerAndFixer.Item2;
            var ids = new HashSet<string>(fixer.FixableDiagnosticIds);
            var dxs = diagnostics.Where(d => ids.Contains(d.Id)).ToList();

            foreach (var diagnostic in dxs)
            {
                if (annotation == null)
                {
                    var fixes = new List<CodeFix>();
                    var context = new CodeFixContext(document, diagnostic, (a, d) => fixes.Add(new CodeFix(a, d)), CancellationToken.None);
                    fixer.RegisterCodeFixesAsync(context).Wait();
                    if (fixes.Any())
                    {
                        var codeFix = new CodeFixCollection(fixer, diagnostic.Location.SourceSpan, fixes);
                        yield return Tuple.Create(diagnostic, codeFix);
                    }
                }
                else
                {
                    var fixAllProvider = fixer.GetFixAllProvider();
                    Assert.NotNull(fixAllProvider);
                    FixAllScope scope = GetFixAllScope(annotation);

                    Func<Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getDocumentDiagnosticsAsync =
                        (d, diagIds, c) =>
                        {
                            var root = d.GetSyntaxRootAsync().Result;
                            var diags = DiagnosticProviderTestUtilities.GetDocumentDiagnostics(provider, d, root.FullSpan);
                            diags = diags.Where(diag => diagIds.Contains(diag.Id));
                            return Task.FromResult(diags);
                        };

                    Func<Project, bool, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getProjectDiagnosticsAsync =
                        (p, includeAllDocumentDiagnostics, diagIds, c) =>
                        {
                            var diags = includeAllDocumentDiagnostics ?
                                DiagnosticProviderTestUtilities.GetAllDiagnostics(provider, p) :
                                DiagnosticProviderTestUtilities.GetProjectDiagnostics(provider, p);
                            diags = diags.Where(diag => diagIds.Contains(diag.Id));
                            return Task.FromResult(diags);
                        };

                    var diagnosticIds = ImmutableHashSet.Create(diagnostic.Id);
                    var fixAllDiagnosticProvider = new FixAllCodeActionContext.FixAllDiagnosticProvider(diagnosticIds, getDocumentDiagnosticsAsync, getProjectDiagnosticsAsync);
                    var fixAllContext = new FixAllContext(document, fixer, scope, fixAllActionId, diagnosticIds, fixAllDiagnosticProvider, CancellationToken.None);
                    var fixAllFix = fixAllProvider.GetFixAsync(fixAllContext).WaitAndGetResult(CancellationToken.None);
                    if (fixAllFix != null)
                    {
                        var codeFix = new CodeFixCollection(fixAllProvider, diagnostic.Location.SourceSpan, ImmutableArray.Create(new CodeFix(fixAllFix, diagnostic)));
                        yield return Tuple.Create(diagnostic, codeFix);
                    }
                }
            }
        }
    }
}
