// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal sealed class WrapperCodeFixProvider : CodeFixProvider
    {
        private readonly ImmutableArray<string> _originalDiagnosticIds;
        private readonly IConfigurationFixProvider _suppressionFixProvider;

        public WrapperCodeFixProvider(IConfigurationFixProvider suppressionFixProvider, IEnumerable<string> diagnosticIds)
        {
            _suppressionFixProvider = suppressionFixProvider;
            _originalDiagnosticIds = diagnosticIds.Distinct().ToImmutableArray();
        }

        public IConfigurationFixProvider SuppressionFixProvider => _suppressionFixProvider;
        public override ImmutableArray<string> FixableDiagnosticIds => _originalDiagnosticIds;

        public async override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostics = context.Diagnostics.Where(_suppressionFixProvider.IsFixableDiagnostic);

            var documentDiagnostics = diagnostics.Where(d => d.Location.IsInSource).ToImmutableArray();
            if (!documentDiagnostics.IsEmpty)
            {
                var suppressionFixes = await _suppressionFixProvider.GetFixesAsync(context.Document, context.Span, documentDiagnostics, context.CancellationToken).ConfigureAwait(false);
                RegisterSuppressionFixes(context, suppressionFixes);
            }

            var projectDiagnostics = diagnostics.Where(d => !d.Location.IsInSource).ToImmutableArray();
            if (!projectDiagnostics.IsEmpty)
            {
                var suppressionFixes = await _suppressionFixProvider.GetFixesAsync(context.Project, projectDiagnostics, context.CancellationToken).ConfigureAwait(false);
                RegisterSuppressionFixes(context, suppressionFixes);
            }
        }

        private static void RegisterSuppressionFixes(CodeFixContext context, ImmutableArray<CodeFix> suppressionFixes)
        {
            if (!suppressionFixes.IsDefault)
            {
                foreach (var suppressionFix in suppressionFixes)
                {
                    context.RegisterCodeFix(suppressionFix.Action, suppressionFix.Diagnostics);
                }
            }
        }

        public override FixAllProvider GetFixAllProvider()
        {
            return _suppressionFixProvider.GetFixAllProvider();
        }
    }
}
