// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal sealed class RazorSpanMappingServiceWrapper : ISpanMappingService
    {
        private readonly IRazorSpanMappingService _razorSpanMappingService;

        public RazorSpanMappingServiceWrapper(IRazorSpanMappingService razorSpanMappingService)
        {
            _razorSpanMappingService = razorSpanMappingService ?? throw new ArgumentNullException(nameof(razorSpanMappingService));
        }

        /// <summary>
        /// Modern razor span mapping service can handle if we add imports.  Razor will then rewrite that
        /// to their own form.
        /// </summary>
        public bool SupportsMappingImportDirectives => true;

        public async Task<ImmutableArray<(string mappedFilePath, TextChange textChange)>> GetTextChangesAsync(
            Document oldDocument,
            Document newDocument,
            CancellationToken cancellationToken)
        {
            var diffService = newDocument.Project.Solution.Workspace.Services.GetRequiredService<IDocumentTextDifferencingService>();
            var textChanges = await diffService.GetTextChangesAsync(oldDocument, newDocument, cancellationToken).ConfigureAwait(false);
            var mappedSpanResults = await MapSpansAsync(oldDocument, textChanges.Select(tc => tc.Span), cancellationToken).ConfigureAwait(false);

            Contract.ThrowIfFalse(mappedSpanResults.Length == textChanges.Length);

            using var _ = ArrayBuilder<(string, TextChange)>.GetInstance(out var mappedFilePathToTextChange);
            for (var i = 0; i < mappedSpanResults.Length; i++)
            {
                // Only include changes that could be mapped.
                var newText = textChanges[i].NewText;
                if (!mappedSpanResults[i].IsDefault && newText != null)
                {
                    var newTextChange = new TextChange(mappedSpanResults[i].Span, newText);
                    mappedFilePathToTextChange.Add((mappedSpanResults[i].FilePath, newTextChange));
                }
            }

            return mappedFilePathToTextChange.ToImmutable();
        }

        public async Task<ImmutableArray<MappedSpanResult>> MapSpansAsync(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken)
        {
            var razorSpans = await _razorSpanMappingService.MapSpansAsync(document, spans, cancellationToken).ConfigureAwait(false);
            var roslynSpans = new MappedSpanResult[razorSpans.Length];
            for (var i = 0; i < razorSpans.Length; i++)
            {
                var razorSpan = razorSpans[i];
                if (razorSpan.IsDefault)
                {
                    // Unmapped location
                    roslynSpans[i] = default;
                }
                else
                {
                    roslynSpans[i] = new MappedSpanResult(razorSpan.FilePath, razorSpan.LinePositionSpan, razorSpan.Span);
                }
            }

            return roslynSpans.ToImmutableArray();
        }
    }
}
