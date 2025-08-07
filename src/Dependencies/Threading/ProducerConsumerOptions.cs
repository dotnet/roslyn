// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !MICROSOFT_CODEANALYSIS_THREADING_NO_CHANNELS

#nullable enable

using System.Threading.Channels;

namespace Microsoft.CodeAnalysis.Threading;

internal readonly record struct ProducerConsumerOptions
{
    /// <summary>
    /// Used when the consumeItems routine will only pull items on a single thread (never concurrently). produceItems
    /// can be called concurrently on many threads.
    /// </summary>
    public static readonly ProducerConsumerOptions SingleReaderOptions = new() { SingleReader = true };

    /// <summary>
    /// Used when the consumeItems routine will only pull items on a single thread (never concurrently). produceItems
    /// can be called on a single thread as well (never concurrently).
    /// </summary>
    public static readonly ProducerConsumerOptions SingleReaderWriterOptions = new() { SingleReader = true, SingleWriter = true };

#if NET
    /// <inheritdoc cref="ChannelOptions.SingleWriter"/>
#endif
    public bool SingleWriter { get; init; }

#if NET
    /// <inheritdoc cref="ChannelOptions.SingleReader"/>
#endif
    public bool SingleReader { get; init; }
}

#endif
