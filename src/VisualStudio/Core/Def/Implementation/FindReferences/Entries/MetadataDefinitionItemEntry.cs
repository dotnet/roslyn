// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Windows.Documents;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
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

            protected override object GetValueWorker(string keyName)
            {
                switch (keyName)
                {
                    case StandardTableKeyNames.Text:
                        return DefinitionBucket.DefinitionItem.DisplayParts.JoinText();
                }

                return null;
            }

            bool ISupportsNavigation.TryNavigateTo(bool isPreview)
                => DefinitionBucket.DefinitionItem.TryNavigateTo(_presenter._workspace, isPreview);

            protected override IList<Inline> CreateLineTextInlines()
                => DefinitionBucket.DefinitionItem.DisplayParts
                .ToInlines(_presenter.ClassificationFormatMap, _presenter.TypeMap);
        }
    }
}
