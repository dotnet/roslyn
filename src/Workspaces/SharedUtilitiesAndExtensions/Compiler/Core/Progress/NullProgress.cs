// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Progress;

internal sealed class NullProgress<T> : IProgress<T>
{
    public static readonly IProgress<T> Instance = new NullProgress<T>();

    private NullProgress()
    {
    }

    public void Report(T value)
    {
    }
}
