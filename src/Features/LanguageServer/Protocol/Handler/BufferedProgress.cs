// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Helper type to allow command handlers to report data either in a streaming fashion (if a client supports that),
    /// or as an array of results.  This type is thread-safe in the same manner that <see cref="IProgress{T}"/> is
    /// expected to be.  Namely, multiple client can be calling <see cref="IProgress{T}.Report(T)"/> on it at the same
    /// time.  This is safe, though the order that the items are reported in when called concurrently is not specified.
    /// </summary>
    internal readonly struct BufferedProgress<T> : IProgress<T>, IDisposable
    {
        /// <summary>
        /// The progress stream to report results to.  May be <see langword="null"/> for clients that do not support streaming.
        /// If <see langword="null"/> then <see cref="_buffer"/> will be non null and will contain all the produced values.
        /// </summary>
        private readonly IProgress<T>? _underlyingProgress;

        /// <summary>
        /// A buffer that results are held in if the client does not support streaming.  Values of this can be retrieved
        /// using <see cref="GetValues"/>.
        /// </summary>
        private readonly ArrayBuilder<T>? _buffer;

        public BufferedProgress(IProgress<T>? underlyingProgress)
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
            // Don't need to lock _underlyingProgress.  It is inherently thread-safe itself being an IProgress implementation.
            _underlyingProgress?.Report(value);

            if (_buffer != null)
            {
                lock (_buffer)
                {
                    _buffer.Add(value);
                }
            }
        }

        /// <summary>
        /// Gets the set of buffered values.  Will return null if the client supports streaming.  Must be called after
        /// all calls to <see cref="Report(T)"/> have been made.  Not safe to call concurrently with any call to <see
        /// cref="Report(T)"/>.
        /// </summary>
        public T[]? GetValues()
            => _buffer?.ToArray();
    }

    internal static class BufferedProgress
    {
        public static BufferedProgress<T> Create<T>(IProgress<T>? progress)
            => new BufferedProgress<T>(progress);

        public static void Report<T>(this BufferedProgress<T[]> progress, T item)
        {
            progress.Report([item]);
        }

        public static T[]? GetFlattenedValues<T>(this BufferedProgress<T[]> progress)
        {
            return progress.GetValues()?.Flatten().ToArray();
        }
    }
}
