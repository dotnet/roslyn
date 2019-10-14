// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// a <see cref="CodeAnalysis.Document"/>.  Navigation to that location is provided by this type.
        /// Subclasses can be used to provide customized line text to display in the entry.
        /// </summary>
        private abstract class AbstractDocumentSpanEntry : AbstractItemEntry
        {
            private readonly string _projectName;
            private readonly object _boxedProjectGuid;

            private readonly SourceText _lineText;
            private readonly MappedSpanResult _mappedSpanResult;

            protected AbstractDocumentSpanEntry(
                AbstractTableDataSourceFindUsagesContext context,
                RoslynDefinitionBucket definitionBucket,
                string projectName,
                Guid projectGuid,
                SourceText lineText,
                MappedSpanResult mappedSpanResult)
                : base(definitionBucket, context.Presenter)
            {
                _projectName = projectName;
                _boxedProjectGuid = projectGuid;

                _lineText = lineText;
                _mappedSpanResult = mappedSpanResult;
            }

            protected override object GetValueWorker(string keyName)
            {
                switch (keyName)
                {
                    case StandardTableKeyNames.DocumentName:
                        return _mappedSpanResult.FilePath;
                    case StandardTableKeyNames.Line:
                        return _mappedSpanResult.LinePositionSpan.Start.Line;
                    case StandardTableKeyNames.Column:
                        return _mappedSpanResult.LinePositionSpan.Start.Character;
                    case StandardTableKeyNames.ProjectName:
                        return _projectName;
                    case StandardTableKeyNames.ProjectGuid:
                        return _boxedProjectGuid;
                    case StandardTableKeyNames.Text:
                        return _lineText.ToString().Trim();
                }

                return null;
            }

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
                return results.FirstOrNullable(r => !r.IsDefault);
            }

            public static SourceText GetLineContainingPosition(SourceText text, int position)
            {
                var line = text.Lines.GetLineFromPosition(position);

                return text.GetSubText(line.Span);
            }
        }
    }
}
