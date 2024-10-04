// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages;

internal partial class StreamingFindUsagesPresenter
{
    private sealed class MetadataDefinitionItemEntry(
        AbstractTableDataSourceFindUsagesContext context,
        RoslynDefinitionBucket definitionBucket,
        AssemblyLocation metadataLocation,
        IThreadingContext threadingContext)
        : AbstractItemEntry(definitionBucket, context.Presenter), ISupportsNavigation
    {
        protected override object? GetValueWorker(string keyName)
            => keyName switch
            {
                StandardTableKeyNames.ProjectName => metadataLocation.Version != Versions.Null
                    ? string.Format(ServicesVSResources.AssemblyNameAndVersionDisplay, metadataLocation.Name, metadataLocation.Version)
                    : metadataLocation.Name,
                StandardTableKeyNames.DisplayPath => metadataLocation.FilePath,
                StandardTableKeyNames.Text => DefinitionBucket.DefinitionItem.DisplayParts.JoinText(),
                StandardTableKeyNames.ItemOrigin => ItemOrigin.ExactMetadata,
                _ => null,
            };

        public bool CanNavigateTo()
            => true;

        public async Task NavigateToAsync(NavigationOptions options, CancellationToken cancellationToken)
        {
            var location = await DefinitionBucket.DefinitionItem.GetNavigableLocationAsync(
                Presenter._workspace, cancellationToken).ConfigureAwait(false);
            await location.TryNavigateToAsync(threadingContext, options, cancellationToken).ConfigureAwait(false);
        }

        protected override IList<Inline> CreateLineTextInlines()
            => DefinitionBucket.DefinitionItem.DisplayParts
                .ToInlines(Presenter.ClassificationFormatMap, Presenter.TypeMap);
    }
}
