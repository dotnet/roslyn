// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages;

internal partial class StreamingFindUsagesPresenter
{
    /// <summary>
    /// Represents a single entry (i.e. row) in the ungrouped FAR table.
    /// </summary>
    private abstract class Entry
    {
        public readonly RoslynDefinitionBucket DefinitionBucket;

        protected Entry(RoslynDefinitionBucket definitionBucket)
            => DefinitionBucket = definitionBucket;

        public bool TryGetValue(string keyName, out object? content)
        {
            content = GetValue(keyName);
            return content != null;
        }

        private object? GetValue(string keyName)
        {
            switch (keyName)
            {
                case StandardTableKeyNames2.Definition:
                    return DefinitionBucket;

                case StandardTableKeyNames2.DefinitionIcon:
                    return DefinitionBucket?.DefinitionItem.Tags.GetFirstGlyph().GetImageMoniker();
            }

            return GetValueWorker(keyName);
        }

        protected abstract object? GetValueWorker(string keyName);

        public virtual bool TryCreateColumnContent(string columnName, [NotNullWhen(true)] out FrameworkElement? content)
        {
            content = null;
            return false;
        }
    }
}
