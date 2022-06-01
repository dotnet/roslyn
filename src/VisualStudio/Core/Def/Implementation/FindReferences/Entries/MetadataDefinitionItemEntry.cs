// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    internal partial class StreamingFindUsagesPresenter
    {
        private class MetadataDefinitionItemEntry : AbstractItemEntry, ISupportsNavigation
        {
            public MetadataDefinitionItemEntry(
                AbstractTableDataSourceFindUsagesContext context,
                RoslynDefinitionBucket definitionBucket)
                : base(definitionBucket, context.Presenter)
            {
            }

            protected override object? GetValueWorker(string keyName)
            {
                switch (keyName)
                {
                    case StandardTableKeyNames.ProjectName:
                        return DefinitionBucket.DefinitionItem.OriginationParts.JoinText();
                    case StandardTableKeyNames.DocumentName:
                        return DefinitionBucket.DefinitionItem.Properties[AbstractReferenceFinder.ContainingTypeInfoPropertyName];
                    case StandardTableKeyNames.Text:
                        return DefinitionBucket.DefinitionItem.DisplayParts.JoinText();
                    case StandardTableKeyNames.ItemOrigin:
                        return ItemOrigin.ExactMetadata;
                }

                return null;
            }

            public bool CanNavigateTo()
                => true;

            public Task NavigateToAsync(NavigationOptions options, CancellationToken cancellationToken)
                => DefinitionBucket.DefinitionItem.TryNavigateToAsync(
                    Presenter._workspace, options, cancellationToken); // Only activate the tab if requested

            protected override IList<Inline> CreateLineTextInlines()
                => DefinitionBucket.DefinitionItem.DisplayParts
                    .ToInlines(Presenter.ClassificationFormatMap, Presenter.TypeMap);
        }
    }
}
