// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.NamingStyles
{
    internal partial class NamingStyleCodeFixProvider
    {
        private class NamingStyleCodeFixAllProvider : FixAllProvider
        {
            public static readonly NamingStyleCodeFixAllProvider Instance = new();

            private NamingStyleCodeFixAllProvider() { }

            public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
            {
                var diagnostics = fixAllContext.Scope switch
                {
                    FixAllScope.Document when fixAllContext.Document is not null => await fixAllContext.GetDocumentDiagnosticsAsync(fixAllContext.Document).ConfigureAwait(false),
                    FixAllScope.Project => await fixAllContext.GetAllDiagnosticsAsync(fixAllContext.Project).ConfigureAwait(false),
                    FixAllScope.Solution => await GetSolutionDiagnosticsAsync(fixAllContext).ConfigureAwait(false),
                    _ => default
                };

                var keys = fixAllContext.CodeActionEquivalenceKey!.Split('|');
                RoslynDebug.Assert(keys.Length == 2);
                RoslynDebug.Assert(Guid.TryParse(keys[0], out _));
                RoslynDebug.Assert(int.TryParse(keys[1], out _));

                var symbolSpecificationId = keys[0];
                var diagnosticsToFix = diagnostics.WhereAsArray(
                    d => d.Location.IsInSource && d.Properties["SymbolSpecificationID"] == symbolSpecificationId);
                if (diagnosticsToFix.IsDefaultOrEmpty)
                {
                    return null;
                }

                var complaintNameIndex = int.Parse(keys[1]);
                var symbolsToRename = await GetSymbolsToRenameAsync(
                    fixAllContext.Solution, diagnosticsToFix, complaintNameIndex, fixAllContext.CancellationToken).ConfigureAwait(false);
                if (symbolsToRename.IsEmpty)
                {
                    return null;
                }

                return new CustomCodeActions.SolutionChangeAction(
                    CodeFixesResources.Fix_all_name_violations,
                    c => FixAllAsync(fixAllContext.Solution, symbolsToRename, c),
                    fixAllContext.CodeActionEquivalenceKey);
            }

            private static Task<Solution> FixAllAsync(Solution solution, ImmutableHashSet<(ISymbol symbol, string newName)> _1, CancellationToken _2)
            {
                // TODO:
                // Renamer needs a new API which could rename multiple symbols using the same solution snapshot
                // and calculate the changed documents for each symbol so that we could notify IRefactorNotifyService
                return Task.FromResult(solution);
            }

            private static async Task<ImmutableHashSet<(ISymbol symbol, string newName)>> GetSymbolsToRenameAsync(
                Solution solution,
                ImmutableArray<Diagnostic> diagnostics,
                int complaintNameIndex,
                CancellationToken cancellationToken)
            {
                using var _ = PooledHashSet<(ISymbol symbol, string newName)>.GetInstance(out var builder);

                var diagnosticGroups = diagnostics.GroupBy(d => d.Location.SourceTree!);
                foreach (var (syntaxTree, diagnosticsInTree) in diagnosticGroups)
                {
                    var document = solution.GetRequiredDocument(syntaxTree);
                    var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                    var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();
                    foreach (var diagnostic in diagnosticsInTree)
                    {
                        var symbol = GetSymbol(root, diagnostic.Location.SourceSpan, syntaxFactsService, semanticModel, cancellationToken);
                        if (symbol is not null)
                        {
                            var serializedNamingStyle = diagnostic.Properties[nameof(NamingStyle)];
                            var style = NamingStyle.FromXElement(XElement.Parse(serializedNamingStyle));
                            var compliantNames = style.MakeCompliant(symbol.Name).ToImmutableArray();
                            // Try to use the second name if there is one, otherwise, fall back to the first name.
                            var newName = complaintNameIndex < compliantNames.Length
                                ? compliantNames[complaintNameIndex]
                                : compliantNames[0];
                            builder.Add((symbol, newName));
                        }
                    }
                }

                return builder.ToImmutableHashSet();
            }

            private static async Task<ImmutableArray<Diagnostic>> GetSolutionDiagnosticsAsync(FixAllContext fixAllContext)
            {
                using var _ = ArrayBuilder<Diagnostic>.GetInstance(out var diagnostics);
                foreach (var project in fixAllContext.Solution.Projects)
                {
                    var projectDiagnostics = await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
                    diagnostics.AddRange(projectDiagnostics);
                }

                return diagnostics.ToImmutable();
            }
        }
    }
}
