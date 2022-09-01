// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell.TableManager;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    internal partial class StreamingFindUsagesPresenter
    {
        /// <summary>
        /// Base type of all <see cref="Entry"/>s that represent some source location in 
        /// a <see cref="Document"/>.  Navigation to that location is provided by this type.
        /// Subclasses can be used to provide customized line text to display in the entry.
        /// </summary>
        private abstract class AbstractDocumentSpanEntry : AbstractItemEntry
        {
            private readonly object _boxedProjectGuid;

            private readonly SourceText _lineText;
            private readonly MappedSpanResult _mappedSpanResult;

            protected AbstractDocumentSpanEntry(
                AbstractTableDataSourceFindUsagesContext context,
                RoslynDefinitionBucket definitionBucket,
                Guid projectGuid,
                SourceText lineText,
                MappedSpanResult mappedSpanResult)
                : base(definitionBucket, context.Presenter)
            {
                _boxedProjectGuid = projectGuid;

                _lineText = lineText;
                _mappedSpanResult = mappedSpanResult;
            }

            protected abstract string GetProjectName();

            protected override object? GetValueWorker(string keyName)
                => keyName switch
                {
                    StandardTableKeyNames.DocumentName => _mappedSpanResult.FilePath,
                    StandardTableKeyNames.Line => _mappedSpanResult.LinePositionSpan.Start.Line,
                    StandardTableKeyNames.Column => _mappedSpanResult.LinePositionSpan.Start.Character,
                    StandardTableKeyNames.ProjectName => GetProjectName(),
                    StandardTableKeyNames.ProjectGuid => _boxedProjectGuid,
                    StandardTableKeyNames.Text => _lineText.ToString().Trim(),
                    _ => null,
                };

            public static async Task<MappedSpanResult?> TryMapAndGetFirstAsync(DocumentSpan documentSpan, SourceText sourceText, CancellationToken cancellationToken)
            {
                var service = documentSpan.Document.Services.GetService<ISpanMappingService>();
                if (service == null)
                {
                    return new MappedSpanResult(documentSpan.Document.FilePath, sourceText.Lines.GetLinePositionSpan(documentSpan.SourceSpan), documentSpan.SourceSpan);
                }

                var results = await service.MapSpansAsync(
                    documentSpan.Document, SpecializedCollections.SingletonEnumerable(documentSpan.SourceSpan), cancellationToken).ConfigureAwait(false);

                if (results.IsDefaultOrEmpty)
                {
                    return new MappedSpanResult(documentSpan.Document.FilePath, sourceText.Lines.GetLinePositionSpan(documentSpan.SourceSpan), documentSpan.SourceSpan);
                }

                // if span mapping service filtered out the span, make sure
                // to return null so that we remove the span from the result
                return results.FirstOrNull(r => !r.IsDefault);
            }

            public static SourceText GetLineContainingPosition(SourceText text, int position)
            {
                var line = text.Lines.GetLineFromPosition(position);

                return text.GetSubText(line.Span);
            }
        }
    }
}
