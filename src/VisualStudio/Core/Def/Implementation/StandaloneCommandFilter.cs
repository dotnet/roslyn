﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    /// <summary>
    /// A CommandFilter used for "normal" files, as opposed to Venus files which are special.
    /// </summary>
    internal sealed class StandaloneCommandFilter<TPackage, TLanguageService> : AbstractVsTextViewFilter<TPackage, TLanguageService>
        where TPackage : AbstractPackage<TPackage, TLanguageService>
        where TLanguageService : AbstractLanguageService<TPackage, TLanguageService>
    {
        /// <summary>
        /// Creates a new command handler that is attached to an IVsTextView.
        /// </summary>
        /// <param name="wpfTextView">The IWpfTextView of the view.</param>
        /// <param name="commandHandlerServiceFactory">The MEF imported ICommandHandlerServiceFactory.</param>
        /// <param name="editorAdaptersFactoryService">The editor adapter</param>
        /// <param name="languageService">The language service</param>
        internal StandaloneCommandFilter(
            TLanguageService languageService,
            IWpfTextView wpfTextView,
            ICommandHandlerServiceFactory commandHandlerServiceFactory,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService)
            : base(languageService, wpfTextView, editorAdaptersFactoryService, commandHandlerServiceFactory)
        {
            wpfTextView.Closed += OnTextViewClosed;
            wpfTextView.BufferGraph.GraphBufferContentTypeChanged += OnGraphBuffersChanged;
            wpfTextView.BufferGraph.GraphBuffersChanged += OnGraphBuffersChanged;

            RefreshCommandFilters();
        }

        private void OnGraphBuffersChanged(object sender, EventArgs e)
        {
            RefreshCommandFilters();
        }

        private void OnTextViewClosed(object sender, EventArgs e)
        {
            WpfTextView.Closed -= OnTextViewClosed;
            WpfTextView.BufferGraph.GraphBufferContentTypeChanged -= OnGraphBuffersChanged;
            WpfTextView.BufferGraph.GraphBuffersChanged -= OnGraphBuffersChanged;
        }
    }
}
