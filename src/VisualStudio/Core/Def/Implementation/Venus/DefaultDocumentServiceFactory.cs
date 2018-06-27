// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Experiment;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus
{
    internal sealed partial class ContainedDocument
    {
        internal class DefaultDocumentServiceFactory : IDocumentServiceFactory
        {
            private readonly SpanMapper _mapper;

            public DefaultDocumentServiceFactory(ContainedDocument owner)
            {
                _mapper = new SpanMapper(owner);
            }

            public TService GetService<TService>()
            {
                if (_mapper is TService service)
                {
                    return service;
                }

                return default;
            }

            private class SpanMapper : ISpanMapper
            {
                private ContainedDocument owner;

                public SpanMapper(ContainedDocument owner)
                {
                    this.owner = owner;
                }

                public async Task<ImmutableArray<SpanMapResult>> MapSpansAsync(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken)
                {
                    var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    // first find ITextSnapshot. we don't actually use this. 
                    // but just as an example how to get editorsnapshot
                    var snapshot = sourceText.FindCorrespondingEditorTextSnapshot();
                    if (snapshot == null)
                    {
                        return spans.Select(s => new SpanMapResult(document, sourceText.Lines.GetLinePositionSpan(s))).ToImmutableArray();
                    }

                    var builder = ArrayBuilder<SpanMapResult>.GetInstance();

                    var linePositionSpan = default(LinePositionSpan);
                    foreach (var span in spans)
                    {
                        var vsSpan = sourceText.GetVsTextSpanForSpan(span);

                        // If we're inside an Venus code nugget, we need to map the span to the surface buffer.
                        // Otherwise, we'll just use the original span.
                        //
                        // this is slightly wrong since we should actually do mapping from snapshot not current buffer.
                        // but buffer cordinator we use doesn't support that.
                        //
                        // to be correct, this should use IProjectionSnapshot.MapToSourceSnapshot
                        // http://index/?query=MapSecondaryToPrimarySpan&rightProject=Microsoft.VisualStudio.Text.Data&file=Model%5CProjection%5CIProjectionSnapshot.cs&line=138
                        //
                        // but that require a bit more plumbing so, using short cut :)
                        if (vsSpan.TryMapSpanFromSecondaryBufferToPrimaryBuffer(document.Project.Solution.Workspace, document.Id, out var mappedSpan))
                        {
                            linePositionSpan = mappedSpan.ToLinePositionSpan();
                        }
                        else
                        {
                            linePositionSpan = sourceText.Lines.GetLinePositionSpan(span);
                        }

                        builder.Add(new SpanMapResult(document, linePositionSpan));
                    }

                    return builder.ToImmutableAndFree();
                }
            }
        }
    }
}
