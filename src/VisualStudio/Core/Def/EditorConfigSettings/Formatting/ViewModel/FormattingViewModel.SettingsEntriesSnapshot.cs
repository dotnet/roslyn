// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Formatting.ViewModel
{
    internal partial class FormattingViewModel
    {
        internal sealed class SettingsEntriesSnapshot : SettingsEntriesSnapshotBase<FormattingSetting>
        {
            public SettingsEntriesSnapshot(ImmutableArray<FormattingSetting> data, int currentVersionNumber) : base(data, currentVersionNumber) { }

            protected override bool TryGetValue(FormattingSetting result, string keyName, out object? content)
            {
                content = keyName switch
                {
                    ColumnDefinitions.Formatting.Description => result.Description,
                    ColumnDefinitions.Formatting.Category => result.Category,
                    ColumnDefinitions.Formatting.Value => result,
                    _ => null,
                };

                return content is not null;
            }
        }
    }
}
