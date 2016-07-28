// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.FindReferences;
using Microsoft.VisualStudio.Shell.FindAllReferences;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using System.Linq;

namespace Microsoft.VisualStudio.LanguageServices.FindReferences
{
    internal partial class StreamingFindReferencesPresenter
    {
        private class RoslynDefinitionBucket : DefinitionBucket, ISupportsNavigation
        {
            private readonly StreamingFindReferencesPresenter _presenter;
            private readonly TableDataSourceFindReferencesContext _context;

            public readonly DefinitionItem DefinitionItem;

            public RoslynDefinitionBucket(
                StreamingFindReferencesPresenter presenter,
                TableDataSourceFindReferencesContext context,
                DefinitionItem definitionItem)
                : base(name: definitionItem.DisplayParts.JoinText() + " " + definitionItem.GetHashCode(),
                       sourceTypeIdentifier: context.SourceTypeIdentifier,
                       identifier: context.Identifier)
            {
                _presenter = presenter;
                _context = context;
                DefinitionItem = definitionItem;
            }

            public bool TryNavigateTo()
            {
                foreach (var location in DefinitionItem.Locations)
                {
                    if (location.TryNavigateTo())
                    {
                        return true;
                    }
                }

                return false;
            }

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
                    inlines.AddRange(DefinitionItem.DisplayParts.ToInlines(_presenter._typeMap));
                    foreach (var inline in inlines)
                    {
                        inline.SetValue(TextElement.FontWeightProperty, FontWeights.Bold);
                    }
                    return inlines;

                case StandardTableKeyNames2.DefinitionIcon:
                    return DefinitionItem.Tags.GetGlyph().GetImageMoniker();

                    //case StandardTableKeyNames2.TextInlines:
                    //    // content of the bucket displayed as a rich text
                    //    var inlines = new List<Inline>();
                    //    inlines.Add(new Run("testing") { FontWeight = FontWeights.Bold });
                    //    inlines.Add(new Run(": defined in "));

                    //    return inlines;
                }

                return null;
            }
        }
    }
}