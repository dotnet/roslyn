// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.InlineRename;
using Microsoft.CodeAnalysis.Editor.InlineRename.Adornment;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal class InlineRenameAdornmentManager : IDisposable
    {
        private readonly IWpfTextView _textView;
        private readonly IGlobalOptionService _globalOptionService;
        private readonly InlineRenameService _renameService;
        private readonly IEditorFormatMapService _editorFormatMapService;
        private readonly IInlineRenameColorUpdater? _dashboardColorUpdater;

        private readonly IAdornmentLayer _adornmentLayer;

        private static readonly ConditionalWeakTable<InlineRenameSession, object> s_createdViewModels =
            new ConditionalWeakTable<InlineRenameSession, object>();

        public InlineRenameAdornmentManager(
            InlineRenameService renameService,
            IEditorFormatMapService editorFormatMapService,
            IInlineRenameColorUpdater? dashboardColorUpdater,
            IWpfTextView textView,
            IGlobalOptionService globalOptionService)
        {
            _renameService = renameService;
            _editorFormatMapService = editorFormatMapService;
            _dashboardColorUpdater = dashboardColorUpdater;
            _textView = textView;
            _globalOptionService = globalOptionService;
            _adornmentLayer = textView.GetAdornmentLayer(InlineRenameAdornmentProvider.AdornmentLayerName);

            _renameService.ActiveSessionChanged += OnActiveSessionChanged;
            _textView.Closed += OnTextViewClosed;

            UpdateAdornments();
        }

        public void Dispose()
        {
            _renameService.ActiveSessionChanged -= OnActiveSessionChanged;
            _textView.Closed -= OnTextViewClosed;
        }

        private void OnTextViewClosed(object sender, EventArgs e)
            => Dispose();

        private void OnActiveSessionChanged(object sender, EventArgs e)
            => UpdateAdornments();

        private void UpdateAdornments()
        {
            _adornmentLayer.RemoveAllAdornments();

            if (_renameService.ActiveSession != null &&
                ViewIncludesBufferFromWorkspace(_textView, _renameService.ActiveSession.Workspace))
            {
                _dashboardColorUpdater?.UpdateColors();

                var useInlineAdornment = _globalOptionService.GetOption(InlineRenameExperimentationOptions.UseInlineAdornment);
                if (useInlineAdornment)
                {
                    var adornment = new InlineRenameAdornment(
                        (InlineRenameAdornmentViewModel)s_createdViewModels.GetValue(_renameService.ActiveSession, session => new InlineRenameAdornmentViewModel(session)),
                        _textView);

                    _adornmentLayer.AddAdornment(
                        AdornmentPositioningBehavior.ViewportRelative,
                        null, // Set no visual span because we don't want the editor to automatically remove the adornment if the overlapping span changes
                        tag: null,
                        adornment,
                        (tag, adornment) => ((InlineRenameAdornment)adornment).Dispose());
                }
                else
                {
                    var newAdornment = new Dashboard(
                        (DashboardViewModel)s_createdViewModels.GetValue(_renameService.ActiveSession, session => new DashboardViewModel(session)),
                        _editorFormatMapService,
                        _textView);

                    _adornmentLayer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative, null, null, newAdornment,
                        (tag, adornment) => ((Dashboard)adornment).Dispose());
                }
            }
        }

        private static bool ViewIncludesBufferFromWorkspace(IWpfTextView textView, Workspace workspace)
        {
            return textView.BufferGraph.GetTextBuffers(b => GetWorkspace(b.AsTextContainer()) == workspace)
                                       .Any();
        }

        private static Workspace? GetWorkspace(SourceTextContainer textContainer)
        {
            Workspace.TryGetWorkspace(textContainer, out var workspace);
            return workspace;
        }
    }
}
