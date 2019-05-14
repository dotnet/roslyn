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
        private readonly ISuppressionOrConfigurationFixProvider _suppressionOrConfigurationFixProvider;

        public WrapperCodeFixProvider(ISuppressionOrConfigurationFixProvider suppressionFixProvider, IEnumerable<string> diagnosticIds)
        {
            _suppressionOrConfigurationFixProvider = suppressionFixProvider;
            _originalDiagnosticIds = diagnosticIds.Distinct().ToImmutableArray();
        }

        public ISuppressionOrConfigurationFixProvider SuppressionFixProvider => _suppressionOrConfigurationFixProvider;
        public override ImmutableArray<string> FixableDiagnosticIds => _originalDiagnosticIds;

        public async override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostics = context.Diagnostics.Where(_suppressionOrConfigurationFixProvider.IsFixableDiagnostic);

            var documentDiagnostics = diagnostics.Where(d => d.Location.IsInSource).ToImmutableArray();
            if (!documentDiagnostics.IsEmpty)
            {
                var suppressionFixes = await _suppressionOrConfigurationFixProvider.GetFixesAsync(context.Document, context.Span, documentDiagnostics, context.CancellationToken).ConfigureAwait(false);
                RegisterSuppressionFixes(context, suppressionFixes);
            }

            var projectDiagnostics = diagnostics.Where(d => !d.Location.IsInSource).ToImmutableArray();
            if (!projectDiagnostics.IsEmpty)
            {
                var suppressionFixes = await _suppressionOrConfigurationFixProvider.GetFixesAsync(context.Project, projectDiagnostics, context.CancellationToken).ConfigureAwait(false);
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
            return _suppressionOrConfigurationFixProvider.GetFixAllProvider();
        }
    }
}
