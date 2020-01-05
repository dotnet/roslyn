// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.VisualStudio.Shell.FindAllReferences;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    internal partial class StreamingFindUsagesPresenter
    {
        private class RoslynDefinitionBucket : DefinitionBucket, ISupportsNavigation
        {
            private readonly StreamingFindUsagesPresenter _presenter;

            public readonly DefinitionItem DefinitionItem;

            public RoslynDefinitionBucket(
                StreamingFindUsagesPresenter presenter,
                AbstractTableDataSourceFindUsagesContext context,
                DefinitionItem definitionItem)
                : base(name: definitionItem.DisplayParts.JoinText() + " " + definitionItem.GetHashCode(),
                       sourceTypeIdentifier: context.SourceTypeIdentifier,
                       identifier: context.Identifier)
            {
                _presenter = presenter;
                DefinitionItem = definitionItem;
            }

            public bool TryNavigateTo(bool isPreview)
                => DefinitionItem.TryNavigateTo(_presenter._workspace, isPreview);

            public override bool TryGetValue(string key, out object content)
            {
                content = GetValue(key);
                return content != null;
            }

            private object GetValue(string key)
            {
                switch (key)
                {
                    case StandardTableKeyNames.Text:
                    case StandardTableKeyNames.FullText:
                        return DefinitionItem.DisplayParts.JoinText();

                    case StandardTableKeyNames2.TextInlines:
                        var inlines = new List<Inline> { new Run(" ") };
                        inlines.AddRange(DefinitionItem.DisplayParts.ToInlines(_presenter.ClassificationFormatMap, _presenter.TypeMap));
                        foreach (var inline in inlines)
                        {
                            inline.SetValue(TextElement.FontWeightProperty, FontWeights.Bold);
                        }
                        return inlines;

                    case StandardTableKeyNames2.DefinitionIcon:
                        return DefinitionItem.Tags.GetFirstGlyph().GetImageMoniker();
                }

                return null;
            }
        }
    }
}
