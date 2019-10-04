// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    internal partial class StreamingFindUsagesPresenter
    {
        private class MetadataDefinitionItemEntry : Entry, ISupportsNavigation
        {
            private readonly StreamingFindUsagesPresenter _presenter;

            public MetadataDefinitionItemEntry(
                AbstractTableDataSourceFindUsagesContext context,
                RoslynDefinitionBucket definitionBucket)
                : base(definitionBucket)
            {
                _presenter = context.Presenter;
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

            public override bool TryCreateColumnContent(string columnName, out FrameworkElement content)
            {
                if (columnName == StandardTableColumnDefinitions2.LineText)
                {
                    var inlines = DefinitionBucket.DefinitionItem.DisplayParts
                        .ToInlines(_presenter.ClassificationFormatMap, _presenter.TypeMap);

                    var textBlock = inlines.ToTextBlock(_presenter.ClassificationFormatMap, wrap: false);

                    content = textBlock;
                    return true;
                }

                content = null;
                return false;
            }

            bool ISupportsNavigation.TryNavigateTo(bool isPreview)
                => DefinitionBucket.DefinitionItem.TryNavigateTo(_presenter._workspace, isPreview);
        }
    }
}
