// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            private readonly string _projectName;

            public DefinitionItemEntry(
                AbstractTableDataSourceFindUsagesContext context,
                RoslynDefinitionBucket definitionBucket,
                string projectName,
                Guid projectGuid,
                SourceText lineText,
                MappedSpanResult mappedSpanResult)
                : base(context, definitionBucket, projectGuid, lineText, mappedSpanResult)
            {
                _projectName = projectName;
            }

            protected override string GetProjectName()
                => _projectName;

            protected override IList<Inline> CreateLineTextInlines()
                => DefinitionBucket.DefinitionItem.DisplayParts.ToInlines(Presenter.ClassificationFormatMap, Presenter.TypeMap);
        }
    }
}
