// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Windows.Documents;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    internal partial class StreamingFindUsagesPresenter
    {
        /// <summary>
        /// Shows a DefinitionItem as a Row in the FindReferencesWindow.  Only used for
        /// GoToDefinition/FindImplementations.  In these operations, we don't want to 
        /// create a DefinitionBucket.  So we instead just so the symbol as a normal row.
        /// </summary>
        private class DefinitionItemEntry : AbstractDocumentSpanEntry
        {
            public DefinitionItemEntry(
                AbstractTableDataSourceFindUsagesContext context,
                RoslynDefinitionBucket definitionBucket,
                string documentName,
                Guid projectGuid,
                SourceText lineText,
                MappedSpanResult mappedSpanResult)
                : base(context, definitionBucket, documentName, projectGuid, lineText, mappedSpanResult)
            {
            }

            protected override IList<Inline> CreateLineTextInlines()
                => DefinitionBucket.DefinitionItem.DisplayParts.ToInlines(Presenter.ClassificationFormatMap, Presenter.TypeMap);
        }
    }
}
