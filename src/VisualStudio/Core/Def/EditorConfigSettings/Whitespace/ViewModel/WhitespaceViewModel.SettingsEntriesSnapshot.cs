// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Whitespace.ViewModel
{
    internal partial class WhitespaceViewModel
    {
        internal sealed class SettingsEntriesSnapshot : SettingsEntriesSnapshotBase<Setting>
        {
            public SettingsEntriesSnapshot(ImmutableArray<Setting> data, int currentVersionNumber) : base(data, currentVersionNumber) { }

            protected override bool TryGetValue(Setting result, string keyName, out object? content)
            {
                content = keyName switch
                {
                    ColumnDefinitions.Whitespace.Description => result.Description,
                    ColumnDefinitions.Whitespace.Category => result.Category,
                    ColumnDefinitions.Whitespace.Value => result,
                    ColumnDefinitions.Whitespace.Location => GetLocationString(result.Location),
                    _ => null,
                };

                return content is not null;
            }

            private static string? GetLocationString(SettingLocation location)
            {
                return location.LocationKind switch
                {
                    LocationKind.EditorConfig or LocationKind.GlobalConfig => location.Path,
                    _ => ServicesVSResources.Visual_Studio_Settings
                };
            }
        }
    }
}
