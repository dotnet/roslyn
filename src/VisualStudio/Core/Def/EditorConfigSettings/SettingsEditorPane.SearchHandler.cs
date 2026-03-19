// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings;

internal sealed partial class SettingsEditorPane
{
    private sealed partial class SearchHandler : IVsWindowSearch
    {
        private readonly IThreadingContext _threadingContext;
        private readonly int _controlMinWidth;
        private readonly int _controlMaxWidth;
        private readonly IWpfTableControl[] _wpfTableControls;

        public SearchHandler(IThreadingContext threadingContext, int controlMinWidth, int controlMaxWidth, IWpfTableControl[] wpfTableControls)
        {
            _threadingContext = threadingContext;
            _controlMinWidth = controlMinWidth;
            _controlMaxWidth = controlMaxWidth;
            _wpfTableControls = wpfTableControls;
        }

        public IVsSearchTask? CreateSearch(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback)
            => new SearchTask(dwCookie, pSearchQuery, pSearchCallback, _wpfTableControls, _threadingContext);

        public void ClearSearch()
        {
            _threadingContext.ThrowIfNotOnUIThread();
            // remove filter on tablar data controls
            foreach (var tableControl in _wpfTableControls)
            {
                _ = tableControl.SetFilter(string.Empty, null);
            }
        }

        public void ProvideSearchSettings(IVsUIDataSource pSearchSettings)
        {
            if (pSearchSettings is SearchSettingsDataSource dataSource)
            {
                dataSource.ControlMaxWidth = (uint)_controlMaxWidth;
                dataSource.ControlMinWidth = (uint)_controlMinWidth;
                dataSource.MaximumMRUItems = 25;
                dataSource.SearchWatermark = ServicesVSResources.Search_Settings;
            }
        }

        public bool OnNavigationKeyDown(uint dwNavigationKey, uint dwModifiers) => false;

        public bool SearchEnabled { get; } = true;
        public Guid Category { get; } = Guid.Empty;
        public IVsEnumWindowSearchFilters? SearchFiltersEnum => null;
        public IVsEnumWindowSearchOptions? SearchOptionsEnum => null;
    }
}
