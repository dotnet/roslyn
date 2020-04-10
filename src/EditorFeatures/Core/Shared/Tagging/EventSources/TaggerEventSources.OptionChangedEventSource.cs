// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal partial class TaggerEventSources
    {
        private class OptionChangedEventSource : AbstractWorkspaceTrackingTaggerEventSource
        {
            private readonly IOption _option;
            private IOptionService _optionService;

            public OptionChangedEventSource(ITextBuffer subjectBuffer, IOption option, TaggerDelay delay) : base(subjectBuffer, delay)
                => _option = option;

            protected override void ConnectToWorkspace(Workspace workspace)
            {
                _optionService = workspace.Services.GetService<IOptionService>();
                if (_optionService != null)
                {
                    _optionService.OptionChanged += OnOptionChanged;
                }
            }

            protected override void DisconnectFromWorkspace(Workspace workspace)
            {
                if (_optionService != null)
                {
                    _optionService.OptionChanged -= OnOptionChanged;
                    _optionService = null;
                }
            }

            private void OnOptionChanged(object sender, OptionChangedEventArgs e)
            {
                if (e.Option == _option)
                {
                    this.RaiseChanged();
                }
            }
        }
    }
}
