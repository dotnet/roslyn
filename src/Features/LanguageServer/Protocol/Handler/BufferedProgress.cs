// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Helper type to allow command handlers to report data either in a streaming fashion (if a client supports that),
    /// or as an array of results.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal struct BufferedProgress<T> : IProgress<T>, IDisposable
    {
        /// <summary>
        /// The progress stream to report results to.  May be <see langword="null"/> for clients that do not support streaming.
        /// If <see langword="null"/> then <see cref="_buffer"/> will be non null and will contain all the produced values.
        /// </summary>
        private readonly IProgress<T[]>? _underlyingProgress;

        /// <summary>
        /// A buffer that results are held in if the client does not support streaming.  Values of this can be retrieved
        /// using <see cref="GetValues"/>.
        /// </summary>
        private readonly ArrayBuilder<T>? _buffer;

        public BufferedProgress(IProgress<T[]>? underlyingProgress)
        {
            _underlyingProgress = underlyingProgress;
            _buffer = underlyingProgress == null ? ArrayBuilder<T>.GetInstance() : null;
        }

        public void Dispose()
            => _buffer?.Free();

        /// <summary>
        /// Report a value either in a streaming or buffered fashion depending on what the client supports.
        /// </summary>
        public void Report(T value)
        {
            _underlyingProgress?.Report(new[] { value });
            _buffer?.Add(value);
        }

        /// <summary>
        /// Gets the set of buffered values.  Will return null if the client supports streaming.
        /// </summary>
        public T[]? GetValues()
            => _buffer?.ToArray();
    }

    internal static class BufferedProgress
    {
        public static BufferedProgress<T> Create<T>(IProgress<T[]>? progress)
            => new BufferedProgress<T>(progress);
    }
}
