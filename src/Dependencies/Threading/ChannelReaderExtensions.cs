// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !MICROSOFT_CODEANALYSIS_THREADING_NO_CHANNELS

#nullable enable

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Threading.Channels;

internal static class RoslynChannelReaderExtensions
{
#if NET // binary compatibility
    public static IAsyncEnumerable<T> ReadAllAsync<T>(ChannelReader<T> reader, CancellationToken cancellationToken)
        => reader.ReadAllAsync(cancellationToken);
#else
    public static async IAsyncEnumerable<T> ReadAllAsync<T>(this ChannelReader<T> reader, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            while (reader.TryRead(out var item))
                yield return item;
        }
    }
#endif
}

#endif
