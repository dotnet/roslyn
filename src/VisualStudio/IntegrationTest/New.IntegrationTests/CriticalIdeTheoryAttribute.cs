// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace Xunit;

internal sealed class CriticalIdeTheoryAttribute : IdeTheoryAttribute
{
    public CriticalIdeTheoryAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
    }

    [Obsolete("Critical tests cannot be skipped.", error: true)]
    public new string? Skip
    {
        get { return base.Skip; }
        set { base.Skip = value; }
    }
}
