// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell.TableManager;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages;

internal partial class StreamingFindUsagesPresenter
{
    /// <summary>
    /// Base type of all <see cref="Entry"/>s that represent some source location in a <see cref="Document"/>.
    /// Navigation to that location is provided by this type. Subclasses can be used to provide customized line text to
    /// display in the entry.
    /// 
    /// Implements navigation since the target document doesn't necessarily exist on disk, and only Roslyn knows how to
    /// navigate to it.
    /// </summary>
    private abstract class AbstractDocumentSpanEntry(
        AbstractTableDataSourceFindUsagesContext context,
        RoslynDefinitionBucket definitionBucket,
        Guid projectGuid,
        string projectName,
        SourceText lineText,
        MappedSpanResult mappedSpanResult,
        IThreadingContext threadingContext)
        : AbstractItemEntry(definitionBucket, context.Presenter), ISupportsNavigation
    {
        private readonly object _boxedProjectGuid = projectGuid;
        private readonly string _projectName = projectName;
        private string? _trimmedLineText = null;

        protected abstract Document Document { get; }

        protected virtual TextSpan NavigateToTargetSpan
            => mappedSpanResult.Span;

        public bool CanNavigateTo()
            => true;

        public async Task NavigateToAsync(NavigationOptions options, CancellationToken cancellationToken)
        {
            var document = Document;
            var documentNavigationService = document.Project.Solution.Services.GetRequiredService<IDocumentNavigationService>();

            await documentNavigationService.TryNavigateToPositionAsync(
                threadingContext,
                document.Project.Solution.Workspace,
                document.Id,
                NavigateToTargetSpan.Start,
                virtualSpace: 0,
                // The location we're trying to navigate to may be gone at this point.  For example if the location was
                // at the end of a file, and the user edited the document to be shorter.  We want to not throw in this
                // case as stale results are a normal part of how find-references works.
                allowInvalidPosition: true,
                options,
                cancellationToken).ConfigureAwait(false);
        }

        protected override object? GetValueWorker(string keyName)
            => keyName switch
            {
                StandardTableKeyNames.DocumentName => mappedSpanResult.FilePath,
                StandardTableKeyNames.Line => mappedSpanResult.LinePositionSpan.Start.Line,
                StandardTableKeyNames.Column => mappedSpanResult.LinePositionSpan.Start.Character,
                StandardTableKeyNames.ProjectName => _projectName,
                StandardTableKeyNames.ProjectGuid => _boxedProjectGuid,
                StandardTableKeyNames.Text => _trimmedLineText ??= lineText.ToString().Trim(),
                _ => null,
            };

        public static async Task<MappedSpanResult?> TryMapAndGetFirstAsync(DocumentSpan documentSpan, SourceText sourceText, CancellationToken cancellationToken)
        {
            var results = await SpanMappingHelper.TryGetMappedSpanResultAsync(documentSpan.Document, [documentSpan.SourceSpan], cancellationToken).ConfigureAwait(false);
            if (results is not { } mappedSpans || mappedSpans.IsDefaultOrEmpty)
            {
                return new MappedSpanResult(documentSpan.Document.FilePath!, sourceText.Lines.GetLinePositionSpan(documentSpan.SourceSpan), documentSpan.SourceSpan);
            }

            // if span mapping service filtered out the span, make sure
            // to return null so that we remove the span from the result
            return mappedSpans.FirstOrNull(r => !r.IsDefault);
        }

        public static SourceText GetLineContainingPosition(SourceText text, int position)
        {
            var line = text.Lines.GetLineFromPosition(position);

            return text.GetSubText(line.Span);
        }
    }
}
