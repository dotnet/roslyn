﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    /// <summary>
    /// Factory to handle creation of the <see cref="RequestDispatcher"/>
    /// </summary>
    internal abstract class AbstractRequestDispatcherFactory
    {
        protected readonly ImmutableArray<Lazy<AbstractRequestHandlerProvider, RequestHandlerProviderMetadataView>> _requestHandlerProviders;
        protected readonly string? _languageName;

        protected AbstractRequestDispatcherFactory(IEnumerable<Lazy<AbstractRequestHandlerProvider, RequestHandlerProviderMetadataView>> requestHandlerProviders, string? languageName = null)
        {
            _requestHandlerProviders = requestHandlerProviders.ToImmutableArray();
            _languageName = languageName;
        }

        /// <summary>
        /// Creates a new request dispatcher every time to ensure handlers are not shared
        /// and cleaned up appropriately on server restart.
        /// </summary>
        public virtual RequestDispatcher CreateRequestDispatcher()
        {
            return new RequestDispatcher(_requestHandlerProviders, _languageName);
        }
    }
}
