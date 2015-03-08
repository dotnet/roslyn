// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    internal class AggregateAsynchronousOperationListener : IAsynchronousOperationListener
    {
        private readonly IAsynchronousOperationListener _listener;

        public AggregateAsynchronousOperationListener(
            IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> listeners,
            string featureName)
        {
            _listener = (from lazy in listeners
                         where lazy.Metadata.FeatureName == featureName
                         select lazy.Value).SingleOrDefault();
        }

        public static readonly IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> EmptyListeners =
                SpecializedCollections.EmptyEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>>();

        public static IAsynchronousOperationListener CreateEmptyListener()
        {
            return new AggregateAsynchronousOperationListener(EmptyListeners, string.Empty);
        }

        public IAsyncToken BeginAsyncOperation(string name, object tag)
        {
            return _listener == null ? AsyncToken.Singleton : _listener.BeginAsyncOperation(name, tag);
        }

        private class AsyncToken : IAsyncToken
        {
            public static readonly AsyncToken Singleton = new AsyncToken();

            public void Dispose()
            {
            }
        }
    }
}
