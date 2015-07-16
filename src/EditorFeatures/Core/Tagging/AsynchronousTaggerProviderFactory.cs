// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    /// <summary>
    /// Creates <see cref="ITaggerProvider"/>s and <see cref="IViewTaggerProvider"/>s that can create
    /// <see cref="ITagger{T}"/>s that compute <see cref="ITag"/>s for a text buffer in an asynchronous
    /// manner.  Computation of the tags is handled by a provided <see cref="IAsynchronousTaggerDataSource{TTag}"/>.
    /// The <see cref="ITagger{T}"/> produced by the provider handles the jobs of scheduling when to 
    /// compute tags, managing the collection of tags, as well as notifying and keeping the user 
    /// interface up to date with the latest tags produced.
    /// 
    /// For <see cref="ITaggerProvider"/>s and <see cref="IViewTaggerProvider"/>s that would like to 
    /// pick up a lot of default behavior, there are also <see cref="AsynchronousTaggerProvider{T}"/> and
    /// <see cref="AsynchronousViewTaggerProvider{T}"/> that can be subclassed.  However, for consumers
    /// that cannot subclass (for example, because they're already subclassing something else), this 
    /// factory serves as a convenient way to create asynchronous <see cref="ITaggerProvider"/>s that
    /// they can then defer to.
    /// </summary>
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