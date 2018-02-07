// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    internal partial class AsynchronousOperationListener : IAsynchronousOperationListener, IAsynchronousOperationWaiter
    {
        internal static IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> CreateListeners(
            string featureName, IAsynchronousOperationListener listener)
        {
            return CreateListeners(ValueTuple.Create(featureName, listener));
        }

        internal static IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> CreateListeners<T>(
            params ValueTuple<string, T>[] pairs) where T : IAsynchronousOperationListener
        {
            return pairs.Select(CreateLazy).ToList();
        }

        private static Lazy<IAsynchronousOperationListener, FeatureMetadata> CreateLazy<T>(
            ValueTuple<string, T> tuple) where T : IAsynchronousOperationListener
        {
            return new Lazy<IAsynchronousOperationListener, FeatureMetadata>(
                () => tuple.Item2, new FeatureMetadata(new Dictionary<string, object>() { { "FeatureName", tuple.Item1 } }));
        }
    }
}
