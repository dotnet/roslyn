// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Documents;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages;

internal partial class StreamingFindUsagesPresenter
{
    private abstract class AbstractItemEntry : Entry
    {
        protected readonly StreamingFindUsagesPresenter Presenter;

        public AbstractItemEntry(RoslynDefinitionBucket definitionBucket, StreamingFindUsagesPresenter presenter)
            : base(definitionBucket)
        {
            Presenter = presenter;
        }

        public override bool TryCreateColumnContent(string columnName, [NotNullWhen(true)] out FrameworkElement? content)
        {
            if (columnName == StandardTableColumnDefinitions2.LineText)
            {
                var inlines = CreateLineTextInlines();
                var textBlock = inlines.ToTextBlock(Presenter.ClassificationFormatMap, wrap: false);

                content = textBlock;
                return true;
            }

            content = null;
            return false;
        }

        protected abstract IList<Inline> CreateLineTextInlines();
    }
}
