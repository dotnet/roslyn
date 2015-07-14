// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    /// <summary>
    /// Creates <see cref="ITaggerProvider"/>s and <see cref="IViewTaggerProvider"/> that can create
    ///<see cref="ITagger{T}"/>s that compute <see cref="ITag"/>s for a text buffer in an asynchronous
    /// manner.  Computation of the tags is handled by a provided <see cref="IAsynchronousTaggerDataSource{TTag}"/>.
    /// The <see cref="ITagger{T}"/> produced by the provider handles the jobs of scheduling when to 
    /// compute tags, managing the collection of tags, as well as notifying and keeping the user 
    /// interface up to date with the latest tags produced.
    /// </summary>
    // Note: it might seem desirable to avoid having this class and just expose
    // AsynchronousBufferTaggerProvider and AsynchronousViewTaggerProvider.  However, those types
    // expose a lot of complexity through their protected surface area.  For example, all the code
    // to handle and manipulate complex tag sources.  For taggers that have no need to deal with 
    // that (including 3rd party taggers), this class serves as a much nicer tagging entrypoint.
    internal static class AsynchronousTaggerProviderFactory
    {
        /// <summary>
        /// Creates a new <see cref="ITaggerProvider"/> using the provided 
        /// <see cref="IAsynchronousTaggerDataSource{TTag}"/> to determine when to compute tags 
        /// and to produce tags when appropriate.
        /// </summary>
        public static ITaggerProvider CreateTaggerProvider<TTag>(
            IAsynchronousTaggerDataSource<TTag> dataSource,
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService) where TTag : ITag
        {
            return new AsynchronousBufferTaggerProviderWithTagSource<TTag>(dataSource, asyncListener, notificationService, createTagSource: null);
        }

        /// <summary>
        /// Creates a new <see cref="IViewTaggerProvider"/> using the provided 
        /// <see cref="IAsynchronousTaggerDataSource{TTag}"/> to determine when to compute tags 
        /// and to produce tags when appropriate.
        /// </summary>
        public static IViewTaggerProvider CreateViewTaggerProvider<TTag>(
            IAsynchronousTaggerDataSource<TTag> dataSource,
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService) where TTag : ITag
        {
            return new AsynchronousViewTaggerProviderWithTagSource<TTag>(dataSource, asyncListener, notificationService, createTagSource: null);
        }
    }
}