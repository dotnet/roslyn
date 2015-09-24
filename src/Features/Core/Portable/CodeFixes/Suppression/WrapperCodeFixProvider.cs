// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal sealed class WrapperCodeFixProvider : CodeFixProvider
    {
        private readonly ImmutableArray<string> _originalDiagnosticIds;
        private readonly ISuppressionFixProvider _suppressionFixProvider;

        public WrapperCodeFixProvider(ISuppressionFixProvider suppressionFixProvider, ImmutableArray<Diagnostic> originalDiagnostics)
        {
            _suppressionFixProvider = suppressionFixProvider;
            _originalDiagnosticIds = originalDiagnostics.Select(d => d.Id).Distinct().ToImmutableArray();
        }

        public ISuppressionFixProvider SuppressionFixProvider => _suppressionFixProvider;
        public override ImmutableArray<string> FixableDiagnosticIds => _originalDiagnosticIds;

        public async override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostics = context.Diagnostics.WhereAsArray(_suppressionFixProvider.CanBeSuppressed);
            var suppressionFixes = await _suppressionFixProvider.GetSuppressionsAsync(context.Document, context.Span, diagnostics, context.CancellationToken).ConfigureAwait(false);
            if (suppressionFixes != null)
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
