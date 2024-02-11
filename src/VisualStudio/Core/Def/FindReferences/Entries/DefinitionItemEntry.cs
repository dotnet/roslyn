// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    internal partial class StreamingFindUsagesPresenter
    {
        /// <summary>
        /// Shows a DefinitionItem as a Row in the FindReferencesWindow.  Only used for
        /// GoToDefinition/FindImplementations.  In these operations, we don't want to 
        /// create a DefinitionBucket.  So we instead just so the symbol as a normal row.
        /// </summary>
        private class DefinitionItemEntry(
            AbstractTableDataSourceFindUsagesContext context,
            RoslynDefinitionBucket definitionBucket,
            string projectName,
            Guid projectGuid,
            SourceText lineText,
            MappedSpanResult mappedSpanResult,
            Document document,
            IThreadingContext threadingContext)
            : AbstractDocumentSpanEntry(context, definitionBucket, projectGuid, lineText, mappedSpanResult), ISupportsNavigation
        {
            protected override string GetProjectName()
                => projectName;

            protected override IList<Inline> CreateLineTextInlines()
                => DefinitionBucket.DefinitionItem.DisplayParts.ToInlines(Presenter.ClassificationFormatMap, Presenter.TypeMap);

            public bool CanNavigateTo()
            {
                var workspace = document.Project.Solution.Workspace;
                var documentNavigationService = workspace.Services.GetService<IDocumentNavigationService>();
                return documentNavigationService != null;
            }

            public async Task NavigateToAsync(NavigationOptions options, CancellationToken cancellationToken)
            {
                Contract.ThrowIfFalse(CanNavigateTo());

                var workspace = document.Project.Solution.Workspace;
                var documentNavigationService = workspace.Services.GetRequiredService<IDocumentNavigationService>();

                await documentNavigationService.TryNavigateToSpanAsync(
                    threadingContext,
                    workspace,
                    document.Id,
                    MappedSpanResult.Span,
                    options,
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
