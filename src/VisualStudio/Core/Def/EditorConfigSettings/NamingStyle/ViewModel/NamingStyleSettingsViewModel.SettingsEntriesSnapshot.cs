// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.NamingStyle.ViewModel
{
    internal partial class NamingStyleSettingsViewModel
    {
        internal class SettingsEntriesSnapshot : SettingsEntriesSnapshotBase<NamingStyleSetting>
        {
            public SettingsEntriesSnapshot(ImmutableArray<NamingStyleSetting> data, int currentVersionNumber) : base(data, currentVersionNumber) { }

            protected override bool TryGetValue(NamingStyleSetting result, string keyName, out object? content)
            {
                content = keyName switch
                {
                    ColumnDefinitions.NamingStyle.Type => result,
                    ColumnDefinitions.NamingStyle.Style => result,
                    ColumnDefinitions.NamingStyle.Severity => result,
                    ColumnDefinitions.NamingStyle.Location => result,
                    _ => null,
                };

                return content is not null;
            }
        }
    }
}
