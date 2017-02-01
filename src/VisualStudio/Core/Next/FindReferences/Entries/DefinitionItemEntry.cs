// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Windows.Documents;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    /// <summary>
    /// Shows a DefinitionItem as a Row in the FindReferencesWindow.  Only used for
    /// GoToDefinition/FindImplementations.  In these operations, we don't want to 
    /// create a DefinitionBucket.  So we instead just so the symbol as a normal row.
    /// </summary>
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

        protected override IList<Inline> CreateLineTextInlines()
            => DefinitionBucket.DefinitionItem.DisplayParts.ToInlines(Presenter.TypeMap);
    }
}