﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.ValueTracking
{
    [Guid(Guids.ValueTrackingToolWindowIdString)]
    internal class ValueTrackingToolWindow : ToolWindowPane
    {
        private readonly ValueTrackingRoot _root = new();

        [MemberNotNullWhen(returnValue: true, nameof(_workspace), nameof(_threadingContext), nameof(ViewModel))]
        public bool Initialized { get; private set; }

        private Workspace? _workspace;

        private IThreadingContext? _threadingContext;

        private ValueTrackingTreeViewModel? _viewModel;
        public ValueTrackingTreeViewModel? ViewModel
        {
            get => _viewModel;
            private set
            {
                if (_viewModel is not null)
                {
                    throw new InvalidOperationException();
                }

                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _viewModel = value;
                _root.SetChild(new ValueTrackingTree(_viewModel));
            }
        }

        /// <summary>
        /// This paramterless constructor is used when
        /// the tool window is initialized on open without any
        /// context. If the tool window is left open across shutdown/restart
        /// of VS for example, then this gets called. 
        /// </summary>
        public ValueTrackingToolWindow() : base(null)
        {
            Caption = ServicesVSResources.Value_Tracking;
            Content = _root;
        }

        public void Initialize(ValueTrackingTreeViewModel viewModel, Workspace workspace, IThreadingContext threadingContext)
        {
            Contract.ThrowIfTrue(Initialized);

            Initialized = true;
            ViewModel = viewModel;
            _workspace = workspace;
            _threadingContext = threadingContext;

            _workspace.WorkspaceChanged += OnWorkspaceChanged;
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            Contract.ThrowIfFalse(Initialized);

            if (e.Kind is WorkspaceChangeKind.SolutionCleared
                       or WorkspaceChangeKind.SolutionRemoved)
            {
                _ = _threadingContext.JoinableTaskFactory.RunAsync(async () =>
                {
                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
                    ViewModel.Roots.Clear();
                });
            }
        }
    }
}
