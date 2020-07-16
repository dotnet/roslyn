// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal class DashboardAdornmentManager : IDisposable
    {
        private readonly IWpfTextView _textView;
        private readonly InlineRenameService _renameService;
        private readonly IEditorFormatMapService _editorFormatMapService;
        private readonly IAdornmentLayer _adornmentLayer;

        private static readonly ConditionalWeakTable<InlineRenameSession, DashboardViewModel> s_createdViewModels =
            new ConditionalWeakTable<InlineRenameSession, DashboardViewModel>();

        public DashboardAdornmentManager(
            InlineRenameService renameService,
            IEditorFormatMapService editorFormatMapService,
            IWpfTextView textView)
        {
            _renameService = renameService;
            _editorFormatMapService = editorFormatMapService;
            _textView = textView;

            _adornmentLayer = textView.GetAdornmentLayer(DashboardAdornmentProvider.AdornmentLayerName);

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
                var newAdornment = new Dashboard(
                    s_createdViewModels.GetValue(_renameService.ActiveSession, session => new DashboardViewModel(session)),
                    _editorFormatMapService,
                    _textView);
                _adornmentLayer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative, null, null, newAdornment,
                    (tag, adornment) => ((Dashboard)adornment).Dispose());
            }
        }

        private static bool ViewIncludesBufferFromWorkspace(IWpfTextView textView, Workspace workspace)
        {
            return textView.BufferGraph.GetTextBuffers(b => GetWorkspace(b.AsTextContainer()) == workspace)
                                       .Any();
        }

        private static Workspace GetWorkspace(SourceTextContainer textContainer)
        {
            Workspace.TryGetWorkspace(textContainer, out var workspace);
            return workspace;
        }
    }
}
