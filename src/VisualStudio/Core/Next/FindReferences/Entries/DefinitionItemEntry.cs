// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
        internal class DefinitionItemEntry : AbstractDocumentSpanEntry
        {
            public DefinitionItemEntry(
                AbstractTableDataSourceFindUsagesContext context,
                RoslynDefinitionBucket definitionBucket,
                DocumentSpan documentSpan,
                Guid projectGuid,
                SourceText sourceText)
                : base(context, definitionBucket, documentSpan, projectGuid, sourceText)
            {
            }

            public override bool TryCreateColumnContent(string columnName, out FrameworkElement content)
            {
                if (columnName == StandardTableColumnDefinitions2.LineText)
                {
                    var inlines = DefinitionBucket.DefinitionItem.DisplayParts.ToInlines(Presenter.TypeMap);
                    var textBlock = inlines.ToTextBlock(Presenter.TypeMap, wrap: false);

                    content = textBlock;
                    return true;
                }

                content = null;
                return false;
            }
        }
}