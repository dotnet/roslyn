// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings;

internal sealed partial class SettingsEditorPane
{
    internal sealed class SearchTask : VsSearchTask
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IWpfTableControl[] _controls;

        public SearchTask(uint dwCookie,
                          IVsSearchQuery pSearchQuery,
                          IVsSearchCallback pSearchCallback,
                          IWpfTableControl[] controls,
                          IThreadingContext threadingContext)
            : base(dwCookie, pSearchQuery, pSearchCallback)
        {
            _threadingContext = threadingContext;
            _controls = controls;
        }

        protected override void OnStartSearch()
        {
            _ = _threadingContext.JoinableTaskFactory.RunAsync(
                async () =>
                {
                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
                    foreach (var control in _controls)
                    {
                        _ = control.SetFilter(string.Empty, new SearchFilter(SearchQuery, control));
                    }

                    await TaskScheduler.Default;
                    uint resultCount = 0;
                    foreach (var control in _controls)
                    {
                        var results = await control.ForceUpdateAsync().ConfigureAwait(false);
                        resultCount += (uint)results.FilteredAndSortedEntries.Count;
                    }

                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
                    SearchCallback.ReportComplete(this, dwResultsFound: resultCount);
                });
        }

        protected override void OnStopSearch()
        {
            _ = _threadingContext.JoinableTaskFactory.RunAsync(
                async () =>
                {
                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
                    foreach (var control in _controls)
                    {
                        _ = control.SetFilter(string.Empty, null);
                    }
                });
        }
    }
}
