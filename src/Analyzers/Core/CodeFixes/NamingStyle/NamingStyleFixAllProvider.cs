using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.PooledObjects;

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

                if (diagnostics.IsDefaultOrEmpty)
                {
                    return null;
                }
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
