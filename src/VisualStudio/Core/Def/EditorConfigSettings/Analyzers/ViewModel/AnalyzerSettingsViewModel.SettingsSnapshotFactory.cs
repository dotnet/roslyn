// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Analyzers.ViewModel;

internal partial class AnalyzerSettingsViewModel : SettingsViewModelBase<
    AnalyzerSetting,
    AnalyzerSettingsViewModel.SettingsSnapshotFactory,
    AnalyzerSettingsViewModel.SettingsEntriesSnapshot>
{
    internal sealed class SettingsSnapshotFactory : SettingsSnapshotFactoryBase<AnalyzerSetting, SettingsEntriesSnapshot>
    {
        public SettingsSnapshotFactory(ISettingsProvider<AnalyzerSetting> data) : base(data) { }

        protected override SettingsEntriesSnapshot CreateSnapshot(ImmutableArray<AnalyzerSetting> data, int currentVersionNumber)
            => new(data, currentVersionNumber);
    }
}
