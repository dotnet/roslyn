// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider
{
    internal class CombinedProvider<T> : ISettingsProvider<T>
    {
        private readonly ImmutableArray<ISettingsProvider<T>> _providers;

        public CombinedProvider(ImmutableArray<ISettingsProvider<T>> providers)
        {
            _providers = providers;
        }

        public async Task<IReadOnlyList<TextChange>?> GetChangedEditorConfigAsync()
        {
            var changes = await Task.WhenAll(_providers.Select(x => x.GetChangedEditorConfigAsync())).ConfigureAwait(false);

            changes = changes.Where(x => x is not null).ToArray();
            if (!changes.Any())
                return null;

            var result = new List<TextChange>();
            foreach (var change in changes)
            {
                result.AddRange(change!); // compiler does not see through linq in nullable yet
            }

            return result;
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
