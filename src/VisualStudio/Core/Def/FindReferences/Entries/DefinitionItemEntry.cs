// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Windows.Documents;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages;

internal partial class StreamingFindUsagesPresenter
{
    /// <summary>
    /// Entry created for a definition with a single source location.
    /// </summary>
    private sealed class DefinitionItemEntry(
        AbstractTableDataSourceFindUsagesContext context,
        RoslynDefinitionBucket definitionBucket,
        Guid projectGuid,
        string projectName,
        SourceText lineText,
        MappedSpanResult mappedSpanResult,
        DocumentSpan documentSpan,
        IThreadingContext threadingContext)
        : AbstractDocumentSpanEntry(context, definitionBucket, projectGuid, projectName, lineText, mappedSpanResult, threadingContext)
    {
        protected override Document Document
            => documentSpan.Document;

        protected override TextSpan NavigateToTargetSpan
            => documentSpan.SourceSpan;

        protected override IList<Inline> CreateLineTextInlines()
            => DefinitionBucket.DefinitionItem.DisplayParts.ToInlines(Presenter.ClassificationFormatMap, Presenter.TypeMap);
    }
}
