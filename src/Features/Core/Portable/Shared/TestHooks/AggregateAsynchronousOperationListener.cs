// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Roslyn.Utilities;
using System.Runtime.CompilerServices;

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

        IAsyncToken IAsynchronousOperationListener.BeginAsyncOperation(string name, object tag)
        {
            return BeginAsyncOperation(name, tag);
        }

        public IAsyncToken BeginAsyncOperation(string name, object tag = null, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            return _listener == null ? EmptyAsyncToken.Instance : _listener.BeginAsyncOperation(name, tag, filePath, lineNumber);
        }
    }
}
