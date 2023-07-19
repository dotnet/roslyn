// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider
{
    internal class CombinedProvider<T>(ImmutableArray<ISettingsProvider<T>> providers) : ISettingsProvider<T>
    {
        private readonly ImmutableArray<ISettingsProvider<T>> _providers = providers;

        public async Task<SourceText> GetChangedEditorConfigAsync(SourceText sourceText)
        {
            foreach (var provider in _providers)
            {
                sourceText = await provider.GetChangedEditorConfigAsync(sourceText).ConfigureAwait(false);
            }

            return sourceText;
        }

        public ImmutableArray<T> GetCurrentDataSnapshot()
        {
            var snapShot = ImmutableArray<T>.Empty;
            foreach (var provider in _providers)
            {
                snapShot = snapShot.Concat(provider.GetCurrentDataSnapshot());
            }

            return snapShot;
        }

        public void RegisterViewModel(ISettingsEditorViewModel model)
        {
            foreach (var provider in _providers)
            {
                provider.RegisterViewModel(model);
            }
        }
    }
}
