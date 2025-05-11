// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Windows.Documents;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages;

internal partial class StreamingFindUsagesPresenter
{
    private sealed class NonNavigableDefinitionItemEntry(AbstractTableDataSourceFindUsagesContext context, RoslynDefinitionBucket definitionBucket)
        : AbstractItemEntry(definitionBucket, context.Presenter)
    {
        protected override object? GetValueWorker(string keyName)
            => keyName switch
            {
                StandardTableKeyNames.Text => DefinitionBucket.DefinitionItem.DisplayParts.JoinText(),
                StandardTableKeyNames.ItemOrigin => ItemOrigin.Exact,
                _ => null,
            };

        protected override IList<Inline> CreateLineTextInlines()
            => DefinitionBucket.DefinitionItem.DisplayParts
                .ToInlines(Presenter.ClassificationFormatMap, Presenter.TypeMap);
    }
}
