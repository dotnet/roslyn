﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.CodeStyle.ViewModel
{
    internal partial class CodeStyleSettingsViewModel
    {
        internal class SettingsEntriesSnapshot : SettingsEntriesSnapshotBase<CodeStyleSetting>
        {
            public SettingsEntriesSnapshot(ImmutableArray<CodeStyleSetting> data, int currentVersionNumber) : base(data, currentVersionNumber) { }

            protected override bool TryGetValue(CodeStyleSetting result, string keyName, out object? content)
            {
                content = keyName switch
                {
                    ColumnDefinitions.CodeStyle.Description => result.Description,
                    ColumnDefinitions.CodeStyle.Category => result.Category,
                    ColumnDefinitions.CodeStyle.Severity => result,
                    ColumnDefinitions.CodeStyle.Value => result,
                    _ => null,
                };

                return content is not null;
            }
        }
    }
}
